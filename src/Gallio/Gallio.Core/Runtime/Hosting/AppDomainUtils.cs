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

using System.Runtime.Loader;

namespace Gallio.Runtime.Hosting
{
    /// <summary>
    /// Utilities for working with isolated AssemblyLoadContexts as a replacement for AppDomains.
    /// </summary>
    public static class AppDomainUtils
    {
        /// <summary>
        /// Wrapper for an isolated AssemblyLoadContext to emulate AppDomain usage.
        /// </summary>
        public class IsolatedLoadContextWrapper
        {
            public AssemblyLoadContext LoadContext { get; }

            public IsolatedLoadContextWrapper(AssemblyLoadContext loadContext)
            {
                LoadContext = loadContext ?? throw new ArgumentNullException(nameof(loadContext));
            }
        }

        /// <summary>
        /// Creates an isolated AssemblyLoadContext and loads an assembly from a folder,
        /// automatically resolving other assemblies in the same folder.
        /// </summary>
        /// <param name="name">A unique name for the load context.</param>
        /// <param name="assemblyPath">Path to the main assembly to load.</param>
        /// <param name="enableUnload">If true, allows unloading the context later.</param>
        /// <returns>The isolated load context wrapper.</returns>
        public static IsolatedLoadContextWrapper CreateIsolatedLoadContext(string name, string assemblyPath, bool enableUnload = true)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (!File.Exists(assemblyPath)) throw new FileNotFoundException("Assembly not found.", assemblyPath);

            string folder = Path.GetDirectoryName(assemblyPath);

            var context = new AssemblyLoadContext(name, isCollectible: enableUnload);

            context.Resolving += (alc, assemblyName) =>
            {
                string candidatePath = Path.Combine(folder, assemblyName.Name + ".dll");
                if (File.Exists(candidatePath))
                    return alc.LoadFromAssemblyPath(candidatePath);

                return null; // fallback to default
            };

            context.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
            return new IsolatedLoadContextWrapper(context);
        }

        /// <summary>
        /// Creates an instance of a type from an assembly loaded in an isolated context.
        /// </summary>
        /// <param name="contextWrapper">The isolated load context wrapper.</param>
        /// <param name="typeFullName">The full name of the type to instantiate.</param>
        /// <param name="args">Constructor arguments.</param>
        /// <returns>The created object.</returns>
        public static object CreateRemoteInstance(IsolatedLoadContextWrapper contextWrapper, string typeFullName, params object[] args)
        {
            if (contextWrapper == null) throw new ArgumentNullException(nameof(contextWrapper));
            if (string.IsNullOrEmpty(typeFullName)) throw new ArgumentNullException(nameof(typeFullName));

            var alc = contextWrapper.LoadContext;

            foreach (var asm in alc.Assemblies)
            {
                Type type = asm.GetType(typeFullName, throwOnError: false);
                if (type != null)
                    return Activator.CreateInstance(type, args);
            }

            throw new InvalidOperationException($"Type '{typeFullName}' is not found in the given isolated context.");
        }

        /// <summary>
        /// Unloads an isolated load context.
        /// </summary>
        /// <param name="contextWrapper">The load context wrapper to unload.</param>
        public static void UnloadContext(IsolatedLoadContextWrapper contextWrapper)
        {
            if (contextWrapper == null) throw new ArgumentNullException(nameof(contextWrapper));
            contextWrapper.LoadContext.Unload();
        }
    }
}