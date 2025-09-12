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

        public BinaryIpcServerChannel(string pipeName)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await pipeServer.WaitForConnectionAsync(cancellationToken);
        }

        public override Task RegisterServiceAsync<TService>(string serviceName, TService service)
        {
            // placeholder
            return Task.CompletedTask;
        }

        public Stream Stream => pipeServer;

        public override Task StopAsync(CancellationToken cancellationToken = default)
        {
            pipeServer?.Dispose();
            pipeServer = null;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pipeServer?.Dispose();
                pipeServer = null;
            }
        }

        // Implementatie van IServerChannel
        public void RegisterService(string serviceName, MarshalByRefObject component)
        {
            if (serviceName == null)
                throw new ArgumentNullException(nameof(serviceName));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            // TODO: voeg je IPC logica hier toe om een service te registreren
        }
    }
}