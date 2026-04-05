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

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Gallio.Common.Remoting
{
    /// <summary>
    /// Creates dynamic proxies for remote service calls using DispatchProxy.
    /// </summary>
    public class RemoteServiceProxy<T> : DispatchProxy where T : class
    {
        private Stream _stream;
        private MessageChannel _messageChannel;
        private string _serviceName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<RpcMessage>> _pendingRequests = new();
        private CancellationTokenSource _cancellationTokenSource;
        private Task _receiveTask;

        /// <summary>
        /// Creates a proxy instance for the specified service.
        /// </summary>
        public static T Create(Stream stream, string serviceName)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (serviceName == null)
                throw new ArgumentNullException(nameof(serviceName));

            var proxy = Create<T, RemoteServiceProxy<T>>() as RemoteServiceProxy<T>;
            proxy.Initialize(stream, serviceName);
            return proxy as T;
        }

        private void Initialize(Stream stream, string serviceName)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _messageChannel = new MessageChannel();
            _serviceName = serviceName;
            _cancellationTokenSource = new CancellationTokenSource();

            // Start listening for responses
            _receiveTask = Task.Run(async () => await ReceiveResponsesAsync(_cancellationTokenSource.Token));
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            // Create request message
            var request = new RpcMessage
            {
                MessageType = RpcMessageType.Request,
                ServiceName = _serviceName,
                MethodName = targetMethod.Name
            };

            // Serialize arguments
            if (args != null && args.Length > 0)
            {
                var argElements = new JsonElement[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] != null)
                    {
                        var json = JsonSerializer.Serialize(args[i]);
                        argElements[i] = JsonSerializer.Deserialize<JsonElement>(json);
                    }
                }
                request.Arguments = argElements;
            }

            // Send request and wait for response
            var tcs = new TaskCompletionSource<RpcMessage>();
            _pendingRequests[request.MessageId] = tcs;

            try
            {
                _messageChannel.WriteMessageAsync(_stream, request, _cancellationTokenSource.Token).Wait();

                // Wait for response (with timeout)
                if (!tcs.Task.Wait(TimeSpan.FromSeconds(30)))
                {
                    _pendingRequests.TryRemove(request.MessageId, out _);
                    throw new TimeoutException($"Remote call to {_serviceName}.{targetMethod.Name} timed out");
                }

                var response = tcs.Task.Result;

                if (!response.Success)
                {
                    var exceptionMessage = $"Remote call to {_serviceName}.{targetMethod.Name} failed: {response.ErrorMessage}";
                    throw new RpcException(exceptionMessage, response.ErrorType ?? "RemoteException");
                }

                // Deserialize return value
                if (response.ReturnValue.HasValue && targetMethod.ReturnType != typeof(void))
                {
                    var returnType = targetMethod.ReturnType;

                    // Handle Task<T> return types
                    if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        returnType = returnType.GetGenericArguments()[0];
                        var result = response.ReturnValue.Value.Deserialize(returnType);
                        return Task.FromResult(result);
                    }

                    // Handle Task return type (no value)
                    if (returnType == typeof(Task))
                    {
                        return Task.CompletedTask;
                    }

                    // Synchronous return
                    return response.ReturnValue.Value.Deserialize(returnType);
                }

                // Handle Task return type for void methods
                if (targetMethod.ReturnType == typeof(Task))
                {
                    return Task.CompletedTask;
                }

                return null;
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
            finally
            {
                _pendingRequests.TryRemove(request.MessageId, out _);
            }
        }

        private async Task ReceiveResponsesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var response = await _messageChannel.ReadMessageAsync(_stream, cancellationToken);
                    if (response == null)
                        break;

                    if (response.MessageType == RpcMessageType.Response &&
                        _pendingRequests.TryRemove(response.MessageId, out var tcs))
                    {
                        tcs.SetResult(response);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception)
            {
                // Connection error - complete all pending requests with error
                foreach (var kvp in _pendingRequests)
                {
                    kvp.Value.TrySetException(new IOException("Connection lost"));
                }
                _pendingRequests.Clear();
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _receiveTask?.Wait(TimeSpan.FromSeconds(1));
            _cancellationTokenSource?.Dispose();
        }
    }
}
