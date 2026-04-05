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

using Gallio.Common.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Gallio.Common.Reflection.Impl
{
    /// <summary>
    /// Data structure that maps every method in a PE assembly with its <see cref="CodeLocation"/>.
    /// Uses System.Reflection.Metadata for .NET 8 compatibility.
    /// </summary>
    public class CciModuleCache
    {
        private readonly IFileSystem fileSystem;
        private readonly string assemblyPath;
        private readonly IDictionary<uint, CodeLocation> map = new Dictionary<uint, CodeLocation>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileSystem">File system wrapper.</param>
        /// <param name="assemblyPath">The path of the assembly to scan.</param>
        public CciModuleCache(IFileSystem fileSystem, string assemblyPath)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.assemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));

            FeedMap();
        }

        /// <summary>
        /// Returns the code location of the method with the specified metadata token.
        /// </summary>
        /// <param name="methodToken">The searched metadata token.</param>
        /// <returns>The resulting code location, or an unknown location if the method was not found.</returns>
        [CLSCompliant(false)]
        public CodeLocation GetMethodLocation(uint methodToken)
        {
            return map.TryGetValue(methodToken, out var result)
                ? result
                : CodeLocation.Unknown;
        }

        private void FeedMap()
        {
            using var stream = fileSystem.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);

            var metadataReader = peReader.GetMetadataReader();

            // Open the associated PDB (portable)
            using var pdbStream = OpenPdbStream();
            using var pdbProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = pdbProvider.GetMetadataReader();

            foreach (var methodHandle in metadataReader.MethodDefinitions)
            {
                var token = (uint)MetadataTokens.GetToken(methodHandle);

                var methodDebugInfo = pdbReader.GetMethodDebugInformation(methodHandle);
                var sequencePoints = methodDebugInfo.GetSequencePoints();

                foreach (var sp in sequencePoints)
                {
                    if (!sp.IsHidden)
                    {
                        var doc = pdbReader.GetDocument(sp.Document);
                        var fileName = pdbReader.GetString(doc.Name);

                        map[token] = new CodeLocation(fileName, sp.StartLine, 0);
                        break; // only take the first visible location
                    }
                }
            }
        }

        private Stream OpenPdbStream()
        {
            var pdbFileName = Path.ChangeExtension(assemblyPath, ".pdb");

            try
            {
                return fileSystem.OpenRead(pdbFileName);
            }
            catch (FileNotFoundException ex)
            {
                throw new InvalidOperationException(
                    $"Cannot open or read the program database '{pdbFileName}'.", ex);
            }
        }
    }
}