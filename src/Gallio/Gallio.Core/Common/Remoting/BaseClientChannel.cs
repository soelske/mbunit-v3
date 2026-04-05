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

namespace Gallio.Common.Remoting
{
    /// <summary>
    /// Base class for client channels.
    /// </summary>
    public abstract class BaseClientChannel : BaseChannel
    {
        /// <summary>
        /// Connects to the remote endpoint asynchronously.
        /// </summary>
        public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a remote service proxy by type and name.
        /// This must be implemented by derived classes to provide the actual proxy mechanism.
        /// </summary>
        /// <param name="serviceType">The type of the service interface.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>A proxy object that implements the service interface.</returns>
        public abstract object GetService(Type serviceType, string serviceName);

        /// <summary>
        /// Generic helper method to get a strongly-typed service proxy.
        /// </summary>
        public T GetService<T>(string serviceName) where T : class
        {
            return (T)GetService(typeof(T), serviceName);
        }
    }
}
