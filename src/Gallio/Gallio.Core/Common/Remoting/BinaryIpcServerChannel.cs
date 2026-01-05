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

using System.IO.Pipes;

namespace Gallio.Common.Remoting
{
    public class BinaryIpcServerChannel : BaseServerChannel, IServerChannel
    {
        private readonly string pipeName;
        private NamedPipeServerStream pipeServer;
        private readonly ServiceDispatcher serviceDispatcher;
        private readonly MessageChannel messageChannel;
        private Task listenerTask;
        private CancellationTokenSource cancellationTokenSource;

        public BinaryIpcServerChannel(string pipeName)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            this.serviceDispatcher = new ServiceDispatcher();
            this.messageChannel = new MessageChannel();
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await pipeServer.WaitForConnectionAsync(cancellationToken);

            // Start message processing loop
            cancellationTokenSource = new CancellationTokenSource();
            listenerTask = Task.Run(() => ProcessMessagesAsync(cancellationTokenSource.Token));
        }

        public override Task RegisterServiceAsync<TService>(string serviceName, TService service)
        {
            if (serviceName == null)
                throw new ArgumentNullException(nameof(serviceName));
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            serviceDispatcher.RegisterService(serviceName, service);
            return Task.CompletedTask;
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && pipeServer.IsConnected)
                {
                    var request = await messageChannel.ReadMessageAsync(pipeServer, cancellationToken);
                    if (request == null)
                        break;

                    if (request.MessageType == RpcMessageType.Request)
                    {
                        var response = await serviceDispatcher.DispatchAsync(request, cancellationToken);
                        await messageChannel.WriteMessageAsync(pipeServer, response, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception)
            {
                // Log or handle connection errors
            }
        }

        public Stream Stream => pipeServer;

        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationTokenSource?.Cancel();
            if (listenerTask != null)
            {
                await listenerTask;
            }
            pipeServer?.Dispose();
            pipeServer = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource?.Cancel();
                listenerTask?.Wait(TimeSpan.FromSeconds(2));
                cancellationTokenSource?.Dispose();
                pipeServer?.Dispose();
                pipeServer = null;
            }
        }

        // IServerChannel implementation for compatibility
        public void RegisterService(string serviceName, MarshalByRefObject component)
        {
            RegisterServiceAsync(serviceName, component).GetAwaiter().GetResult();
        }
    }
}