// Copyright 2005-2025 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// Portions Copyright 2018-2026 Bart Suelze
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using Gallio.Common.Messaging;
using Gallio.Model.Filters;

namespace Gallio.Model.Isolation
{
    /// <summary>
    /// Low-level read/write helpers for the TestIsolation pipe protocol.
    /// Wire format: 4-byte little-endian length prefix + UTF-8 JSON body.
    /// </summary>
    public static class TestIsolationPipeProtocol
    {
        /// <summary>
        /// Writes a message to <paramref name="stream"/> and flushes.
        /// </summary>
        public static async Task WriteAsync(Stream stream, TestIsolationWireMsg msg, CancellationToken ct)
        {
            byte[] body = JsonSerializer.SerializeToUtf8Bytes(msg);
            byte[] lengthPrefix = BitConverter.GetBytes(body.Length); // little-endian on all .NET 8 platforms
            await stream.WriteAsync(lengthPrefix, 0, 4, ct);
            await stream.WriteAsync(body, 0, body.Length, ct);
            await stream.FlushAsync(ct);
        }

        /// <summary>
        /// Reads the next message from <paramref name="stream"/>.
        /// Returns <c>null</c> on timeout or when the pipe is closed.
        /// </summary>
        public static async Task<TestIsolationWireMsg?> ReadAsync(Stream stream, TimeSpan timeout, CancellationToken ct)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                // Read 4-byte length prefix
                byte[] lengthBuf = new byte[4];
                if (!await ReadExactAsync(stream, lengthBuf, 4, linkedCts.Token))
                    return null;

                int length = BitConverter.ToInt32(lengthBuf, 0);
                if (length <= 0)
                    return null;

                // Read message body
                byte[] body = new byte[length];
                if (!await ReadExactAsync(stream, body, length, linkedCts.Token))
                    return null;

                return JsonSerializer.Deserialize<TestIsolationWireMsg>(body);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // Timed out, not cancelled by caller
                return null;
            }
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int count, CancellationToken ct)
        {
            int offset = 0;
            while (offset < count)
            {
                int n = await stream.ReadAsync(buffer, offset, count - offset, ct);
                if (n == 0)
                    return false; // pipe closed
                offset += n;
            }
            return true;
        }

        /// <summary>
        /// Serializes <paramref name="arg"/> to a string.
        /// Tries JSON first (fast path for simple types). Falls back to DataContractSerializer XML
        /// for types that JSON cannot round-trip (e.g. types with no parameterless constructor
        /// like <c>FilterSet&lt;T&gt;</c>). The XML payload starts with <c>&lt;</c>, which lets
        /// <see cref="DeserializeArg"/> detect which format was used.
        /// Returns <c>null</c> if the argument is null, a <see cref="MarshalByRefObject"/>,
        /// or not serializable by either method.
        /// </summary>
        public static string? SerializeArg(object? arg)
        {
            if (arg == null || arg is MarshalByRefObject)
                return null;

            // TestExecutionOptions contains FilterSet<T> which cannot be round-tripped by
            // either System.Text.Json (no parameterless ctor) or DataContractSerializer
            // (abstract Filter<T> subtypes need KnownType). Use Gallio's own filter expression
            // string instead — it survives any filter shape.
            if (arg is TestExecutionOptions opts)
            {
                return JsonSerializer.Serialize(new TestExecutionOptionsWire
                {
                    FilterExpr   = opts.FilterSet.ToFilterSetExpr(),
                    ExactFilter  = opts.ExactFilter,
                    SkipDynamicTests  = opts.SkipDynamicTests,
                    SkipTestExecution = opts.SkipTestExecution,
                    SingleThreaded    = opts.SingleThreaded,
                    Properties = new Dictionary<string, string>(opts.Properties)
                });
            }

            try
            {
                // Try JSON — works for simple types and any type with a parameterless constructor.
                string json = JsonSerializer.Serialize(arg, arg.GetType());
                // Verify the JSON round-trips (catches types whose deserialization would throw).
                JsonSerializer.Deserialize(json, arg.GetType());
                return json;
            }
            catch
            {
                // Fall back to DataContractSerializer for types JSON cannot round-trip.
                // The resulting XML always starts with '<', which DeserializeArg uses to detect the format.
                try
                {
                    var serializer = new DataContractSerializer(arg.GetType());
                    using var ms = new MemoryStream();
                    serializer.WriteObject(ms, arg);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Deserializes a single argument wire value. Returns <c>null</c> if the type or payload is missing.
        /// If the payload starts with <c>&lt;</c> it is DataContractSerializer XML; otherwise JSON.
        /// </summary>
        public static object? DeserializeArg(TestIsolationArgWire wire)
        {
            if (wire.TypeName == null || wire.Json == null)
                return null;
            try
            {
                Type? type = Type.GetType(wire.TypeName);
                if (type == null)
                    return null;

                // TestExecutionOptions is serialized as a flat JSON by SerializeArg above.
                if (type == typeof(TestExecutionOptions))
                {
                    var w = JsonSerializer.Deserialize<TestExecutionOptionsWire>(wire.Json);
                    if (w == null) return null;
                    var result = new TestExecutionOptions();
                    if (!string.IsNullOrEmpty(w.FilterExpr))
                    {
                        var parser = new FilterParser<ITestDescriptor>(new TestDescriptorFilterFactory<ITestDescriptor>());
                        result.FilterSet = parser.ParseFilterSet(w.FilterExpr);
                    }
                    result.ExactFilter       = w.ExactFilter;
                    result.SkipDynamicTests  = w.SkipDynamicTests;
                    result.SkipTestExecution = w.SkipTestExecution;
                    result.SingleThreaded    = w.SingleThreaded;
                    foreach (var kv in w.Properties)
                        result.AddProperty(kv.Key, kv.Value);
                    return result;
                }

                if (wire.Json.Length > 0 && wire.Json[0] == '<')
                {
                    // DataContractSerializer XML (complex Gallio types with private constructors etc.)
                    var serializer = new DataContractSerializer(type);
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(wire.Json));
                    return serializer.ReadObject(ms);
                }

                return JsonSerializer.Deserialize(wire.Json, type);
            }
            catch
            {
                return null;
            }
        }

        // DTO used by SerializeArg/DeserializeArg for TestExecutionOptions.
        private sealed class TestExecutionOptionsWire
        {
            public string FilterExpr        { get; set; } = "";
            public bool ExactFilter         { get; set; }
            public bool SkipDynamicTests    { get; set; }
            public bool SkipTestExecution   { get; set; }
            public bool SingleThreaded      { get; set; }
            public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        }

        // -----------------------------------------------------------------------
        // Message serialization — uses DataContractSerializer so that complex
        // Gallio message types (private constructors, readonly collections) work.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Serializes a <see cref="Message"/> to an XML string using
        /// <see cref="DataContractSerializer"/>. Returns <c>null</c> on failure.
        /// </summary>
        public static string? SerializeMessage(Message message)
        {
            try
            {
                var serializer = new DataContractSerializer(message.GetType());
                using var ms = new MemoryStream();
                serializer.WriteObject(ms, message);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deserializes a <see cref="Message"/> from an XML string produced by
        /// <see cref="SerializeMessage"/>. Returns <c>null</c> on failure.
        /// </summary>
        public static Message? DeserializeMessage(string typeName, string xml)
        {
            try
            {
                Type? messageType = Type.GetType(typeName);
                if (messageType == null)
                    return null;

                var serializer = new DataContractSerializer(messageType);
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                return (Message?)serializer.ReadObject(ms);
            }
            catch
            {
                return null;
            }
        }
    }
}
