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

using System.IO.Pipes;
using System.Text.Json;
using Gallio.Common.Diagnostics;
using Gallio.Common.Messaging;
using Gallio.Common.Messaging.MessageSinks;
using Gallio.Runtime.ProgressMonitoring;

namespace Gallio.Model.Isolation
{
    /// <summary>
    /// .NET 8 replacement for <c>TestIsolationServer</c>.
    /// Manages the server end of the named-pipe isolation channel to AutoCAD.
    /// </summary>
    /// <remarks>
    /// Uses a single bidirectional named pipe instead of the old .NET Remoting channels.
    /// Icarus sends RunTask / Shutdown; AutoCAD sends back MessageSinkPublish (streamed) and TaskFinished.
    /// </remarks>
    public class TestIsolationServer : IDisposable
    {
        private readonly string ipcPortName;
        private readonly Dictionary<Guid, IsolatedTaskState> activeTasks = new();
        private readonly CancellationTokenSource cts = new();
        private readonly TaskCompletionSource<bool> connectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private NamedPipeServerStream? pipeServer;

        // Current messageSink for routing pipe-back messages. Set before sending RunTask.
        private volatile IMessageSink? currentMessageSink;

        /// <summary>
        /// Creates a test isolation server and immediately starts listening for connections.
        /// </summary>
        /// <param name="ipcPortName">The IPC port name.</param>
        /// <param name="linkId">Kept for API compatibility; not used internally.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ipcPortName"/> is null.</exception>
        public TestIsolationServer(string ipcPortName, Guid linkId)
        {
            if (ipcPortName == null)
                throw new ArgumentNullException(nameof(ipcPortName));

            this.ipcPortName = ipcPortName;
            Task.Run(() => AcceptAndReceiveAsync(cts.Token));
        }

        // Kept for source compatibility with TestIsolationClient.cs (original Gallio source,
        // included via wildcard). The client is not used at runtime in .NET 8 but must compile.
        internal static string GetMessageExchangeLinkServiceName(Guid uniqueId)
            => "TestIsolationServer.MessageExchangeLink." + uniqueId;

        /// <inheritdoc />
        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
            pipeServer?.Dispose();
        }

        /// <summary>
        /// Sends a RunTask message to AutoCAD and waits for the result.
        /// </summary>
        /// <param name="isolatedTaskType">The type of isolated task to run.</param>
        /// <param name="args">The isolated task arguments.</param>
        /// <returns>The isolated task result.</returns>
        public object RunIsolatedTaskOnClient(Type isolatedTaskType, object[] args)
        {
            if (isolatedTaskType == null)
                throw new ArgumentNullException(nameof(isolatedTaskType));

            // Wait until the client has connected (max 30 s).
            if (!connectedTcs.Task.Wait(TimeSpan.FromSeconds(30)))
                throw new TimeoutException("AutoCAD client did not connect within 30 seconds.");

            Guid id = Guid.NewGuid();
            var state = new IsolatedTaskState();

            lock (activeTasks)
                activeTasks.Add(id, state);

            try
            {
                var wireArgs = args?.Select(SerializeTaskArg).ToList();

                // Capture the messageSink before sending so it's ready when pipe-back messages arrive.
                currentMessageSink = ExtractMessageSink(args);

                var wireMsg = new TestIsolationWireMsg
                {
                    Kind = "RunTask",
                    Id = id.ToString(),
                    TaskTypeName = isolatedTaskType.AssemblyQualifiedName,
                    Args = wireArgs
                };

                TestIsolationPipeProtocol.WriteAsync(pipeServer!, wireMsg, CancellationToken.None)
                    .GetAwaiter().GetResult();

                return state.WaitForCompletion();
            }
            finally
            {
                currentMessageSink = null;
                lock (activeTasks)
                    activeTasks.Remove(id);
            }
        }

        /// <summary>
        /// Sends a Shutdown message to AutoCAD and waits up to <paramref name="timeout"/> for delivery.
        /// </summary>
        public void Shutdown(TimeSpan timeout)
        {
            if (pipeServer?.IsConnected != true)
                return;

            var shutdownMsg = new TestIsolationWireMsg { Kind = "Shutdown", Id = "" };
            TestIsolationPipeProtocol.WriteAsync(pipeServer, shutdownMsg, CancellationToken.None)
                .Wait(timeout);
        }

        // -------------------------------------------------------------------------

        private static TestIsolationArgWire SerializeTaskArg(object? a)
        {
            if (a is Type t)
                return new TestIsolationArgWire { TypeName = TestIsolationArgWire.SentinelType, Json = t.AssemblyQualifiedName };

            if (a is IMessageSink)
                return new TestIsolationArgWire { TypeName = TestIsolationArgWire.SentinelMessageSink };

            if (a is IProgressMonitor)
                return new TestIsolationArgWire { TypeName = TestIsolationArgWire.SentinelProgressMonitor };

            // object[] (exact type — not string[] or other typed arrays): serialize elements individually
            // to preserve element types through the JSON round-trip.
            if (a != null && a.GetType() == typeof(object[]))
                return new TestIsolationArgWire
                {
                    TypeName = TestIsolationArgWire.SentinelObjectArray,
                    NestedArgs = ((object[])a).Select(SerializeTaskArg).ToList()
                };

            return new TestIsolationArgWire
            {
                TypeName = a?.GetType().AssemblyQualifiedName,
                Json = TestIsolationPipeProtocol.SerializeArg(a)
            };
        }

        private static IMessageSink? ExtractMessageSink(object[]? args)
        {
            if (args == null) return null;
            foreach (var a in args)
                if (a is IMessageSink sink)
                    return sink;
            return null;
        }

        private async Task AcceptAndReceiveAsync(CancellationToken ct)
        {
            try
            {
                pipeServer = new NamedPipeServerStream(
                    ipcPortName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync(ct);
                connectedTcs.TrySetResult(true);

                while (!ct.IsCancellationRequested)
                {
                    var msg = await TestIsolationPipeProtocol.ReadAsync(pipeServer, TimeSpan.FromHours(1), ct);
                    if (msg == null)
                        break; // pipe closed or cancelled

                    switch (msg.Kind)
                    {
                        case "TaskFinished":
                            ProcessTaskFinished(msg);
                            break;
                        case "MessageSinkPublish":
                            ProcessMessageSinkPublish(msg);
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                connectedTcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                connectedTcs.TrySetException(ex);
            }
        }

        private void ProcessMessageSinkPublish(TestIsolationWireMsg msg)
        {
            var sink = currentMessageSink;
            if (sink == null || msg.MessageTypeName == null || msg.MessageData == null)
                return;

            try
            {
                var message = TestIsolationPipeProtocol.DeserializeMessage(msg.MessageTypeName, msg.MessageData);
                if (message != null)
                    sink.Publish(message);
            }
            catch
            {
                // Ignore deserialization/publish errors; don't crash the receive loop.
            }
        }

        private void ProcessTaskFinished(TestIsolationWireMsg msg)
        {
            if (!Guid.TryParse(msg.Id, out Guid id))
                return;

            ExceptionData? exception = null;
            if (msg.ExceptionTypeName != null)
            {
                exception = new ExceptionData(
                    msg.ExceptionTypeName,
                    msg.ExceptionMessage ?? "",
                    msg.ExceptionStackTrace ?? "",
                    ExceptionData.NoProperties,
                    null);
            }

            object? result = null;
            if (msg.ResultTypeName != null && msg.ResultJson != null)
            {
                try
                {
                    Type? resultType = Type.GetType(msg.ResultTypeName);
                    if (resultType != null)
                        result = JsonSerializer.Deserialize(msg.ResultJson, resultType);
                }
                catch
                {
                    // Ignore; result stays null.
                }
            }

            lock (activeTasks)
            {
                if (activeTasks.TryGetValue(id, out var state))
                    state.Finished(result, exception);
            }
        }

        // -------------------------------------------------------------------------

        private sealed class IsolatedTaskState
        {
            private object? result;
            private ExceptionData? exception;
            private readonly ManualResetEvent finish = new(false);

            public object WaitForCompletion()
            {
                finish.WaitOne();
                if (exception != null)
                    throw new TestIsolationException(
                        string.Format("The isolated task threw an exception: {0}", exception));
                return result!;
            }

            public void Finished(object? result, ExceptionData? exception)
            {
                this.result = result;
                this.exception = exception;
                finish.Set();
            }
        }
    }
}
