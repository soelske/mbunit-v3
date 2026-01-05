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

using System.Text.Json;

namespace Gallio.Common.Remoting
{
    /// <summary>
    /// Represents a remote procedure call message.
    /// </summary>
    public class RpcMessage
    {
        /// <summary>
        /// Unique identifier for this message.
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The type of message (Request or Response).
        /// </summary>
        public RpcMessageType MessageType { get; set; }

        /// <summary>
        /// The name of the service being called.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// The name of the method being called.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// The serialized method arguments.
        /// </summary>
        public JsonElement[]? Arguments { get; set; }

        /// <summary>
        /// The serialized return value (for responses).
        /// </summary>
        public JsonElement? ReturnValue { get; set; }

        /// <summary>
        /// Indicates if the call was successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error message if the call failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error type name if the call failed.
        /// </summary>
        public string? ErrorType { get; set; }
    }

    /// <summary>
    /// Message type enumeration.
    /// </summary>
    public enum RpcMessageType
    {
        Request,
        Response
    }
}
