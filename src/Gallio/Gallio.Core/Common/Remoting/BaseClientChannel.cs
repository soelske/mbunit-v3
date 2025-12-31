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

namespace Gallio.Common.Remoting
{
    /// <summary>
    /// Base class for client channels with a local service registry.
    /// </summary>
    public abstract class BaseClientChannel : BaseChannel
    {
        private readonly ConcurrentDictionary<string, object> services = new();

        public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

        public void RegisterService(string serviceName, object service)
        {
            if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));
            if (service == null) throw new ArgumentNullException(nameof(service));

            services[serviceName] = service;
        }

        public object GetService(Type serviceType, string serviceName)
        {
            if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            if (services.TryGetValue(serviceName, out var service) &&
                serviceType.IsInstanceOfType(service))
            {
                return service;
            }

            return null;
        }

        // Optional generic overload
        public T GetService<T>(string serviceName) where T : class
        {
            return GetService(typeof(T), serviceName) as T;
        }
    }
}