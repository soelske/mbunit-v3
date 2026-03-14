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

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Gallio.Common.Messaging;
using Gallio.Common.Messaging.MessageSinks;
using Gallio.Common.Policies;
using Gallio.Model.Isolation;
using Gallio.Runtime.ProgressMonitoring;

namespace Gallio.AutoCAD.Plugin
{
    /// <summary>
    /// .NET 8 client-side adapter for the TestIsolation pipe protocol.
    /// Connects to the named pipe opened by <c>TestIsolationServer</c> on the Icarus side,
    /// executes <see cref="IsolatedTask"/> instances, and sends results and message-sink
    /// events back via the same pipe.
    /// </summary>
    public class TestIsolationClientAdapter : IDisposable
    {
        private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(2);

        private readonly string ipcPortName;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// Creates a test isolation client adapter.
        /// </summary>
        /// <param name="ipcPortName">The IPC port name (must match the server).</param>
        /// <param name="linkId">Kept for API compatibility; not used internally.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ipcPortName"/> is null.</exception>
        public TestIsolationClientAdapter(string ipcPortName, Guid linkId)
        {
            if (ipcPortName == null)
                throw new ArgumentNullException(nameof(ipcPortName));

            this.ipcPortName = ipcPortName;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        /// <summary>
        /// Connects to the server, runs isolated tasks until a Shutdown message is received,
        /// then returns. Synchronous wrapper around <see cref="RunAsync"/>.
        /// </summary>
        public void Run()
        {
            try
            {
                Task.Run(() => RunAsync()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                UnhandledExceptionPolicy.Report(
                    "An exception occurred while processing test isolation messages. Assuming the server is no longer available.",
                    ex);
            }
        }

        /// <summary>
        /// Async implementation: connects, processes RunTask/Shutdown messages, then returns.
        /// </summary>
        public async Task RunAsync()
        {
            using var pipeClient = new NamedPipeClientStream(".", ipcPortName, PipeDirection.InOut, PipeOptions.Asynchronous);

            using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, connectCts.Token);

            await pipeClient.ConnectAsync(linkedCts.Token);

            while (!cts.Token.IsCancellationRequested)
            {
                var msg = await TestIsolationPipeProtocol.ReadAsync(pipeClient, PollTimeout, cts.Token);

                if (msg == null)
                    continue; // timeout, keep polling

                if (msg.Kind == "Shutdown")
                    break;

                if (msg.Kind == "RunTask")
                    await RunIsolatedTaskAsync(pipeClient, msg);
            }
        }

        private static async Task RunIsolatedTaskAsync(Stream pipe, TestIsolationWireMsg msg)
        {
            string? resultTypeName = null;
            string? resultJson = null;
            string? exceptionTypeName = null;
            string? exceptionMessage = null;
            string? exceptionStackTrace = null;

            try
            {
                Type taskType = Type.GetType(msg.TaskTypeName!, throwOnError: true)!;
                var isolatedTask = (IsolatedTask)Activator.CreateInstance(taskType)!;

                object[] args = DeserializeArgs(pipe, msg);

                object? result = isolatedTask.Run(args);

                if (result != null)
                {
                    resultTypeName = result.GetType().AssemblyQualifiedName;
                    resultJson = TestIsolationPipeProtocol.SerializeArg(result);
                }
            }
            catch (Exception ex)
            {
                exceptionTypeName = ex.GetType().FullName;
                exceptionMessage = ex.Message;
                exceptionStackTrace = ex.StackTrace;
            }

            var response = new TestIsolationWireMsg
            {
                Kind = "TaskFinished",
                Id = msg.Id,
                ResultTypeName = resultTypeName,
                ResultJson = resultJson,
                ExceptionTypeName = exceptionTypeName,
                ExceptionMessage = exceptionMessage,
                ExceptionStackTrace = exceptionStackTrace
            };

            await TestIsolationPipeProtocol.WriteAsync(pipe, response, CancellationToken.None);
        }

        private static object[] DeserializeArgs(Stream pipe, TestIsolationWireMsg msg)
        {
            if (msg.Args == null)
                return Array.Empty<object>();

            var result = new object[msg.Args.Count];
            for (int i = 0; i < msg.Args.Count; i++)
                result[i] = DeserializeArg(pipe, msg.Args[i])!;
            return result;
        }

        private static object? DeserializeArg(Stream pipe, TestIsolationArgWire wire)
        {
            switch (wire.TypeName)
            {
                case TestIsolationArgWire.SentinelType:
                    // Reconstruct a Type from its assembly-qualified name.
                    return wire.Json != null ? Type.GetType(wire.Json, throwOnError: false) : null;

                case TestIsolationArgWire.SentinelMessageSink:
                    // Substitute with a local sink that pipes messages back to Icarus.
                    return new PipeMessageSink(pipe);

                case TestIsolationArgWire.SentinelProgressMonitor:
                    // Substitute with a null progress monitor (progress not streamed back).
                    return NullProgressMonitor.CreateInstance();

                case TestIsolationArgWire.SentinelObjectArray:
                    // Reconstruct object[] with each element deserialized individually.
                    if (wire.NestedArgs == null)
                        return Array.Empty<object>();
                    var nested = new object[wire.NestedArgs.Count];
                    for (int i = 0; i < wire.NestedArgs.Count; i++)
                        nested[i] = DeserializeArg(pipe, wire.NestedArgs[i])!;
                    return nested;

                default:
                    return TestIsolationPipeProtocol.DeserializeArg(wire);
            }
        }

        // -------------------------------------------------------------------------

        /// <summary>
        /// An IMessageSink implementation that serializes messages and sends them
        /// back to Icarus as "MessageSinkPublish" wire messages over the pipe.
        /// </summary>
        private sealed class PipeMessageSink : IMessageSink
        {
            private readonly Stream pipe;

            public PipeMessageSink(Stream pipe)
            {
                this.pipe = pipe;
            }

            /// <inheritdoc />
            public void Publish(Message message)
            {
                if (message == null)
                    return;

                string? messageXml = TestIsolationPipeProtocol.SerializeMessage(message);
                if (messageXml == null)
                    return; // Skip messages that can't be serialized.

                var wireMsg = new TestIsolationWireMsg
                {
                    Kind = "MessageSinkPublish",
                    Id = "",
                    MessageTypeName = message.GetType().AssemblyQualifiedName,
                    MessageData = messageXml
                };

                // Write synchronously — we're already in a Task.Run background thread.
                TestIsolationPipeProtocol.WriteAsync(pipe, wireMsg, CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
        }
    }
}
