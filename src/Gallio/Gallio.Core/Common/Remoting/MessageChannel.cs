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

using System.Text;
using System.Text.Json;

namespace Gallio.Common.Remoting
{
    /// <summary>
    /// Handles reading and writing RPC messages over a stream.
    /// </summary>
    public class MessageChannel
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = false
        };

        /// <summary>
        /// Reads an RPC message from the stream.
        /// </summary>
        public async Task<RpcMessage?> ReadMessageAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream == null || !stream.CanRead)
                return null;

            try
            {
                // Read message length (4 bytes)
                var lengthBuffer = new byte[4];
                var bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                if (bytesRead != 4)
                    return null;

                var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0 || messageLength > 10 * 1024 * 1024) // Max 10MB
                    throw new InvalidOperationException($"Invalid message length: {messageLength}");

                // Read message body
                var messageBuffer = new byte[messageLength];
                var totalRead = 0;
                while (totalRead < messageLength)
                {
                    bytesRead = await stream.ReadAsync(messageBuffer, totalRead, messageLength - totalRead, cancellationToken);
                    if (bytesRead == 0)
                        throw new IOException("Connection closed while reading message");
                    totalRead += bytesRead;
                }

                // Deserialize
                var json = Encoding.UTF8.GetString(messageBuffer);
                return JsonSerializer.Deserialize<RpcMessage>(json, JsonOptions);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log or handle read errors
                return null;
            }
        }

        /// <summary>
        /// Writes an RPC message to the stream.
        /// </summary>
        public async Task WriteMessageAsync(Stream stream, RpcMessage message, CancellationToken cancellationToken = default)
        {
            if (stream == null || !stream.CanWrite)
                throw new InvalidOperationException("Stream is not writable");

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Serialize message
            var json = JsonSerializer.Serialize(message, JsonOptions);
            var messageBytes = Encoding.UTF8.GetBytes(json);

            // Write length prefix
            var lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            await stream.WriteAsync(lengthBytes, 0, 4, cancellationToken);

            // Write message body
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
    }
}
