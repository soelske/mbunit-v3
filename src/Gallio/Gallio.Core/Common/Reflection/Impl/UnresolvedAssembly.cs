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

using Gallio.Common.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Gallio.Common.Reflection.Impl
{
    /// <summary>
    /// Placeholder for an assembly that could not be resolved.
    /// Modern .NET 8 compatible version (no longer inherits from System.Reflection.Assembly).
    /// </summary>
    internal sealed partial class UnresolvedAssembly : IUnresolvedCodeElement, IEquatable<UnresolvedAssembly>
    {
        private readonly IAssemblyInfo adapter;

        internal UnresolvedAssembly(IAssemblyInfo adapter)
        {
            this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public IAssemblyInfo Adapter => adapter;

        ICodeElementInfo IUnresolvedCodeElement.Adapter => adapter;

        public string CodeBase => new Uri(adapter.Path).ToString();

        public string FullName => adapter.FullName;

        public string Location => adapter.Path;

        public string ImageRuntimeVersion => RuntimeEnvironment.GetSystemVersion();

        public AssemblyName GetName() => adapter.GetName();

        public AssemblyName[] GetReferencedAssemblies() => adapter.GetReferencedAssemblies().ToArray();

        public Type[] GetTypes()
        {
            return GenericCollectionUtils.ConvertAllToArray(adapter.GetTypes(),
                type => type.Resolve(false));
        }

        public Type[] GetExportedTypes()
        {
            return GenericCollectionUtils.ConvertAllToArray(adapter.GetExportedTypes(),
                type => type.Resolve(false));
        }

        public Type? GetType(string name, bool throwOnError = false, bool ignoreCase = false)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            foreach (ITypeInfo type in adapter.GetTypes())
            {
                if (string.Equals(type.Name, name,
                    ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    return type.Resolve(false);
                }
            }

            if (throwOnError)
                throw new TypeLoadException($"Cannot find type '{name}'.");

            return null;
        }

        // These used to exist on Assembly, but we provide stubs here.
        public Stream? GetManifestResourceStream(string name) => null;
        public string[] GetManifestResourceNames() => Array.Empty<string>();

        public override bool Equals(object? obj) => Equals(obj as UnresolvedAssembly);

        public bool Equals(UnresolvedAssembly? other) =>
            other != null && adapter.Equals(other.adapter);

        public override int GetHashCode() => adapter.GetHashCode();

        public override string ToString() => $"UnresolvedAssembly: {FullName}";
    }
}