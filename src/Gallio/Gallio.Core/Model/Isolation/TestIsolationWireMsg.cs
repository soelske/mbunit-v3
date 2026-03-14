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

using System.Collections.Generic;

namespace Gallio.Model.Isolation
{
    /// <summary>
    /// Wire-format DTO for the isolation protocol between Icarus and AutoCAD.
    /// Serialized as length-prefixed JSON over a named pipe.
    /// </summary>
    public class TestIsolationWireMsg
    {
        /// <summary>Message kind: "RunTask", "Shutdown", or "TaskFinished".</summary>
        public string Kind { get; set; } = "";

        /// <summary>Unique task id (Guid string).</summary>
        public string Id { get; set; } = "";

        // --- RunTask fields ---

        /// <summary>Assembly-qualified type name of the isolated task.</summary>
        public string? TaskTypeName { get; set; }

        /// <summary>Serialized task arguments, or null.</summary>
        public List<TestIsolationArgWire>? Args { get; set; }

        // --- TaskFinished fields ---

        /// <summary>Assembly-qualified type name of the result, or null.</summary>
        public string? ResultTypeName { get; set; }

        /// <summary>JSON-serialized result value, or null.</summary>
        public string? ResultJson { get; set; }

        /// <summary>Exception type full name if the task threw, or null.</summary>
        public string? ExceptionTypeName { get; set; }

        /// <summary>Exception message if the task threw, or null.</summary>
        public string? ExceptionMessage { get; set; }

        /// <summary>Exception stack trace if the task threw, or null.</summary>
        public string? ExceptionStackTrace { get; set; }

        // --- MessageSinkPublish fields ---

        /// <summary>Assembly-qualified type name of the published Message subclass, or null.</summary>
        public string? MessageTypeName { get; set; }

        /// <summary>Serialized Message body (DataContractSerializer XML), or null.</summary>
        public string? MessageData { get; set; }
    }

    /// <summary>
    /// Wire-format for a single serialized task argument.
    /// </summary>
    public class TestIsolationArgWire
    {
        // Sentinel TypeName values for non-JSON-serializable argument types.
        // Recognized on both the server (Gallio.Core) and the client (AutoCAD plugin).

        /// <summary>Sentinel: argument is a <see cref="System.Type"/>; <see cref="Json"/> holds the assembly-qualified name.</summary>
        public const string SentinelType = "~Type";

        /// <summary>Sentinel: argument is an <c>IMessageSink</c>; client substitutes a pipe-back sink.</summary>
        public const string SentinelMessageSink = "~IMessageSink";

        /// <summary>Sentinel: argument is an <c>IProgressMonitor</c>; client substitutes a null monitor.</summary>
        public const string SentinelProgressMonitor = "~IProgressMonitor";

        /// <summary>Sentinel: argument is an <c>object[]</c> with mixed types; elements are in <see cref="NestedArgs"/>.</summary>
        public const string SentinelObjectArray = "~object[]";

        /// <summary>Assembly-qualified type name, or null if the argument is null.</summary>
        public string? TypeName { get; set; }

        /// <summary>JSON-serialized value, or null if not serializable or null argument.</summary>
        public string? Json { get; set; }

        /// <summary>For <see cref="SentinelObjectArray"/>: the recursively serialized elements of the object[].</summary>
        public List<TestIsolationArgWire>? NestedArgs { get; set; }
    }
}
