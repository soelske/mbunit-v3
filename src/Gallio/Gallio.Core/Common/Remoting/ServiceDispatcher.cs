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
using System.Reflection;
using System.Text.Json;

namespace Gallio.Common.Remoting
{
    /// <summary>
    /// Dispatches RPC messages to registered services.
    /// </summary>
    public class ServiceDispatcher
    {
        private readonly ConcurrentDictionary<string, object> _services = new();
        private readonly ConcurrentDictionary<string, MethodInfo[]> _methodCache = new();

        /// <summary>
        /// Registers a service with the dispatcher.
        /// </summary>
        public void RegisterService(string serviceName, object service)
        {
            if (serviceName == null)
                throw new ArgumentNullException(nameof(serviceName));
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            _services[serviceName] = service;

            // Cache methods
            var methods = service.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            _methodCache[serviceName] = methods;
        }

        /// <summary>
        /// Unregisters a service from the dispatcher.
        /// </summary>
        public void UnregisterService(string serviceName)
        {
            if (serviceName == null)
                throw new ArgumentNullException(nameof(serviceName));

            _services.TryRemove(serviceName, out _);
            _methodCache.TryRemove(serviceName, out _);
        }

        /// <summary>
        /// Dispatches an RPC request to the appropriate service method.
        /// </summary>
        public async Task<RpcMessage> DispatchAsync(RpcMessage request, CancellationToken cancellationToken = default)
        {
            var response = new RpcMessage
            {
                MessageId = request.MessageId,
                MessageType = RpcMessageType.Response,
                ServiceName = request.ServiceName,
                MethodName = request.MethodName
            };

            try
            {
                // Find service
                if (!_services.TryGetValue(request.ServiceName, out var service))
                {
                    response.Success = false;
                    response.ErrorType = "ServiceNotFoundException";
                    response.ErrorMessage = $"Service '{request.ServiceName}' not found";
                    return response;
                }

                // Find method
                if (!_methodCache.TryGetValue(request.ServiceName, out var methods))
                {
                    response.Success = false;
                    response.ErrorType = "ServiceNotFoundException";
                    response.ErrorMessage = $"Service '{request.ServiceName}' has no cached methods";
                    return response;
                }

                var method = methods.FirstOrDefault(m => m.Name == request.MethodName);
                if (method == null)
                {
                    response.Success = false;
                    response.ErrorType = "MethodNotFoundException";
                    response.ErrorMessage = $"Method '{request.MethodName}' not found on service '{request.ServiceName}'";
                    return response;
                }

                // Deserialize arguments
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];

                if (request.Arguments != null)
                {
                    for (int i = 0; i < parameters.Length && i < request.Arguments.Length; i++)
                    {
                        var paramType = parameters[i].ParameterType;
                        args[i] = request.Arguments[i].Deserialize(paramType);
                    }
                }

                // Invoke method
                object result;
                var returnValue = method.Invoke(service, args);

                // Handle async methods
                if (returnValue is Task task)
                {
                    await task;

                    // Get result from Task<T>
                    if (task.GetType().IsGenericType)
                    {
                        var resultProperty = task.GetType().GetProperty("Result");
                        result = resultProperty?.GetValue(task);
                    }
                    else
                    {
                        result = null; // Task without return value
                    }
                }
                else
                {
                    result = returnValue;
                }

                // Serialize result
                if (result != null)
                {
                    var resultJson = JsonSerializer.Serialize(result);
                    response.ReturnValue = JsonSerializer.Deserialize<JsonElement>(resultJson);
                }

                response.Success = true;
            }
            catch (TargetInvocationException ex)
            {
                response.Success = false;
                response.ErrorType = ex.InnerException?.GetType().FullName ?? "InvocationException";
                response.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorType = ex.GetType().FullName;
                response.ErrorMessage = ex.Message;
            }

            return response;
        }
    }
}
