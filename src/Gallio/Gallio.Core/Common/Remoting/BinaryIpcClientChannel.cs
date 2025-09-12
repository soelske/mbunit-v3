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
    public class BinaryIpcClientChannel : BaseClientChannel, IClientChannel
    {
        private readonly string pipeName;
        private NamedPipeClientStream pipeClient;

        public BinaryIpcClientChannel(string pipeName)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync(cancellationToken);
        }

        public override Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return StartAsync(cancellationToken);
        }

        public Stream Stream => pipeClient;

        public override Task StopAsync(CancellationToken cancellationToken = default)
        {
            pipeClient?.Dispose();
            pipeClient = null;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pipeClient?.Dispose();
                pipeClient = null;
            }
        }

        // Implementatie van IClientChannel
        public object GetService(Type serviceType, string serviceName)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (serviceName == null)
                throw new ArgumentNullException(nameof(serviceName));

            // TODO: voeg je IPC logica hier toe om een service proxy te verkrijgen
            return null; // voorlopig dummy return
        }
    }
}