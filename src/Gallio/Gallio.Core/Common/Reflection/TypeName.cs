// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Gallio.Common.Security;
using Gallio.Runtime.Extensibility;

namespace Gallio.Common.Reflection
{
    /// <summary>
    /// Describes the name of a type and allows partial type names to be compared with one another.
    /// </summary>
    [Serializable]
    public sealed class TypeName : IEquatable<TypeName>
    {
        private readonly string fullName;
        private readonly AssemblyName assemblyName;

        /// <summary>
        /// Creates a type name from a type.
        /// </summary>
        public TypeName(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            fullName = type.FullName;
            if (fullName == null)
                throw new ArgumentException("The type must have a valid FullName.", "type");

            assemblyName = type.Assembly.GetName();
        }

        /// <summary>
        /// Creates a type name from type info.
        /// </summary>
        public TypeName(ITypeInfo typeInfo)
        {
            if (typeInfo == null)
                throw new ArgumentNullException("typeInfo");

            fullName = typeInfo.FullName;
            if (fullName == null)
                throw new ArgumentException("The type must have a valid FullName.", "typeInfo");

            assemblyName = typeInfo.Assembly.GetName();
        }

        /// <summary>
        /// Creates a type name from its assembly-qualified name.
        /// </summary>
        public TypeName(string assemblyQualifiedName)
        {
            if (assemblyQualifiedName == null)
                throw new ArgumentNullException("assemblyQualifiedName");

            int lastBracketPos = assemblyQualifiedName.LastIndexOf(']');
            int commaPos = assemblyQualifiedName.IndexOf(',', lastBracketPos + 1);
            if (commaPos < 0)
                throw new ArgumentException("The assembly qualified name must include the assembly name.", "assemblyQualifiedName");

            fullName = assemblyQualifiedName.Substring(0, commaPos);
            assemblyName = new AssemblyName(assemblyQualifiedName.Substring(commaPos + 1));
        }

        /// <summary>
        /// Creates a type name from its full name and assembly name.
        /// </summary>
        public TypeName(string fullName, AssemblyName assemblyName)
        {
            if (fullName == null)
                throw new ArgumentNullException("fullName");
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");

            this.fullName = fullName;
            this.assemblyName = assemblyName;
        }

        /// <summary>
        /// Gets the assembly-qualified name of the type, including its namespace and assembly.
        /// </summary>
        public string AssemblyQualifiedName
        {
            get { return string.Concat(fullName, ", ", assemblyName.FullName); }
        }

        /// <summary>
        /// Gets the full name of the type, including its namespace.
        /// </summary>
        public string FullName
        {
            get { return fullName; }
        }

        /// <summary>
        /// Gets the full or partial name of the assembly that contains the type.
        /// </summary>
        public AssemblyName AssemblyName
        {
            get { return assemblyName; }
        }

        /// <inheritdoc />
        public bool Equals(TypeName other)
        {
            return other != null
                && fullName == other.fullName
                && assemblyName.FullName == other.assemblyName.FullName;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as TypeName);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return new FnvHasher(22739)
                .Add(fullName)
                .Add(assemblyName.FullName)
                .ToValue();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return AssemblyQualifiedName;
        }

        /// <summary>
        /// Resolves the type named by this instance.
        /// </summary>
        /// <returns>The type.</returns>
        /// <exception cref="ReflectionResolveException">Thrown if the type could not be resolved.</exception>
        public Type Resolve()
        {
            string assemblyQualifiedName = AssemblyQualifiedName;
            try
            {
                Type type = Type.GetType(assemblyQualifiedName);
                if (type != null)
                    return type;
            }
            catch (Exception ex)
            {
                throw new ReflectionResolveException(string.Format("Could not resolve type '{0}'.", assemblyQualifiedName), ex);
            }

#if NET8_0_OR_GREATER
            // Breaking change in .NET Core/.NET 8: Type.GetType() with a partial assembly name
            // (no version/PKT) does NOT trigger assembly loading — it only searches already-loaded
            // assemblies. Explicitly load the assembly via Assembly.Load() which fires the
            // AssemblyResolve event (handled by DefaultAssemblyLoader), then retry.
            Exception net8LoadException = null;
            try
            {
                Assembly asm = Assembly.Load(assemblyName.Name);
                if (asm != null)
                {
                    Type type = asm.GetType(fullName);
                    if (type != null)
                        return type;
                }
            }
            catch (Exception ex) { net8LoadException = ex; }

            // Fallback: search all currently loaded assemblies by short name.
            // Catches cases where the assembly was loaded under a different identity
            // (e.g., signed vs unsigned) but is functionally the same.
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(asm.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Type type = asm.GetType(fullName);
                    if (type != null)
                        return type;
                }
            }
#endif

            if (net8LoadException != null)
                throw new ReflectionResolveException(string.Format("Could not resolve type '{0}'. Assembly.Load('{1}') failed: {2}", assemblyQualifiedName, assemblyName.Name, net8LoadException.Message), net8LoadException);

            throw new ReflectionResolveException(string.Format("Could not resolve type '{0}'.", assemblyQualifiedName));
        }

        /// <summary>
        /// Returns true if the associated <see cref="AssemblyName"/> is a partial name only.
        /// </summary>
        public bool HasPartialAssemblyName
        {
            get { return assemblyName.FullName == assemblyName.Name; }
        }

        /// <summary>
        /// Returns a type name that has a partial assembly name instead of the full assembly name.
        /// </summary>
        public TypeName ConvertToPartialAssemblyName()
        {
            return HasPartialAssemblyName ? this : new TypeName(fullName, new AssemblyName(assemblyName.Name));
        }
    }
}
