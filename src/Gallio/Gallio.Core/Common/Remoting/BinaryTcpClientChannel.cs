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

using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;

namespace Gallio.Common.Remoting
{
    /// <summary>
    /// A client channel based on TCP sockets with binary message protocol.
    /// </summary>
    public class BinaryTcpClientChannel : BaseClientChannel
    {
        private readonly string host;
        private readonly int port;
        private TcpClient tcpClient;
        private readonly ConcurrentDictionary<string, object> serviceProxies = new();

        public BinaryTcpClientChannel(string host, int port)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.port = port;
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, cancellationToken);
        }

        public override Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return StartAsync(cancellationToken);
        }

        public NetworkStream Stream => tcpClient?.GetStream();

        public override Task StopAsync(CancellationToken cancellationToken = default)
        {
            // Dispose all proxies
            foreach (var proxy in serviceProxies.Values)
            {
                if (proxy is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            serviceProxies.Clear();

            tcpClient?.Close();
            tcpClient = null;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all proxies
                foreach (var proxy in serviceProxies.Values)
                {
                    if (proxy is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                serviceProxies.Clear();

                tcpClient?.Dispose();
                tcpClient = null;
            }
        }

        // GetService implementation for IClientChannel compatibility
        public override object GetService(Type serviceType, string serviceName)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (serviceName == null)
                throw new ArgumentNullException(nameof(serviceName));

            if (tcpClient == null || !tcpClient.Connected)
                throw new InvalidOperationException("Channel is not connected. Call StartAsync first.");

            // Return cached proxy if available
            var cacheKey = $"{serviceName}:{serviceType.FullName}";
            if (serviceProxies.TryGetValue(cacheKey, out var cachedProxy))
            {
                return cachedProxy;
            }

            // Create new proxy using reflection
            var proxyType = typeof(RemoteServiceProxy<>).MakeGenericType(serviceType);
            var createMethod = proxyType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            
            if (createMethod == null)
                throw new InvalidOperationException($"Could not find Create method on {proxyType.Name}");

            var proxy = createMethod.Invoke(null, new object[] { tcpClient.GetStream(), serviceName });
            
            // Cache the proxy
            serviceProxies[cacheKey] = proxy;

            return proxy;
        }
    }
}
