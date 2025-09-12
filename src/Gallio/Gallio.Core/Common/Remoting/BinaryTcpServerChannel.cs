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
    /// A server channel based on an <see cref="TcpServerChannel" /> that uses a
    /// <see cref="BinaryServerFormatterSinkProvider" />.
    /// </summary>
    public class BinaryTcpServerChannel : BaseServerChannel
    {
        private readonly string host;
        private readonly int port;
        private TcpListener listener;
        private TcpClient client;

        public BinaryTcpServerChannel(string host, int port)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.port = port;
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            listener = new TcpListener(IPAddress.Parse(host), port);
            listener.Start();
            client = await listener.AcceptTcpClientAsync(cancellationToken);
        }

        public override Task RegisterServiceAsync<TService>(string serviceName, TService service)
        {
            // Placeholder: attach service to TCP listener logic
            return Task.CompletedTask;
        }

        public NetworkStream Stream => client?.GetStream();

        public override Task StopAsync(CancellationToken cancellationToken = default)
        {
            client?.Close();
            listener?.Stop();
            client = null;
            listener = null;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                client?.Dispose();
                listener = null;
                client = null;
            }
        }
    }
}
