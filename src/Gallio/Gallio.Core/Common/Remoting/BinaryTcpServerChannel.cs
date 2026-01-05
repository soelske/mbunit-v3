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

using System.Net;
using System.Net.Sockets;

namespace Gallio.Common.Remoting
{
    /// <summary>
    /// A server channel based on TCP sockets with binary message protocol.
    /// </summary>
    public class BinaryTcpServerChannel : BaseServerChannel
    {
        private readonly string host;
        private readonly int port;
        private TcpListener listener;
        private TcpClient client;
        private readonly ServiceDispatcher serviceDispatcher;
        private readonly MessageChannel messageChannel;
        private Task listenerTask;
        private CancellationTokenSource cancellationTokenSource;

        public BinaryTcpServerChannel(string host, int port)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.port = port;
            this.serviceDispatcher = new ServiceDispatcher();
            this.messageChannel = new MessageChannel();
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var ipAddress = IPAddress.Parse(host);
            listener = new TcpListener(ipAddress, port);
            listener.Start();
            
            client = await listener.AcceptTcpClientAsync(cancellationToken);

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
                var stream = client.GetStream();
                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    var request = await messageChannel.ReadMessageAsync(stream, cancellationToken);
                    if (request == null)
                        break;

                    if (request.MessageType == RpcMessageType.Request)
                    {
                        var response = await serviceDispatcher.DispatchAsync(request, cancellationToken);
                        await messageChannel.WriteMessageAsync(stream, response, cancellationToken);
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

        public NetworkStream Stream => client?.GetStream();

        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationTokenSource?.Cancel();
            if (listenerTask != null)
            {
                await listenerTask;
            }
            client?.Close();
            listener?.Stop();
            client = null;
            listener = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource?.Cancel();
                listenerTask?.Wait(TimeSpan.FromSeconds(2));
                cancellationTokenSource?.Dispose();
                client?.Dispose();
                listener = null;
                client = null;
            }
        }
    }
}
