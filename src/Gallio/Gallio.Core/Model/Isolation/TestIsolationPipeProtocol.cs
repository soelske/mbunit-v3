// Copyright 2005-2025 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// Portions Copyright 2018-2025 Bart Suelze
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

using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using Gallio.Common.Messaging;

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
        /// Serializes <paramref name="arg"/> to JSON. Returns <c>null</c> if the argument
        /// is null, a <see cref="MarshalByRefObject"/>, or not JSON-serializable.
        /// </summary>
        public static string? SerializeArg(object? arg)
        {
            if (arg == null || arg is MarshalByRefObject)
                return null;
            try
            {
                return JsonSerializer.Serialize(arg, arg.GetType());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deserializes a single argument wire value. Returns <c>null</c> if the type or JSON is missing.
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
                return JsonSerializer.Deserialize(wire.Json, type);
            }
            catch
            {
                return null;
            }
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
