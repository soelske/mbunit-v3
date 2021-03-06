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
using System.Text;
using Gallio.Runtime.Extensibility;

namespace Gallio.Runtime.FileTypes
{
    /// <summary>
    /// Provides metadata about a <see cref="IFileTypeRecognizer" />.
    /// </summary>
    public class FileTypeRecognizerTraits : Traits
    {
        private readonly string id;
        private readonly string description;

        /// <summary>
        /// Creates traits for a file type recognizer.
        /// </summary>
        /// <param name="id">The file type id.</param>
        /// <param name="description">The file type description.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="id"/> or
        /// <paramref name="description"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="id"/> is empty.</exception>
        public FileTypeRecognizerTraits(string id, string description)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (id.Length == 0)
                throw new ArgumentException("The file type id must not be empty.", "id");
            if (description == null)
                throw new ArgumentNullException("description");

            this.id = id;
            this.description = description;
        }

        /// <summary>
        /// Gets the file type id.
        /// </summary>
        public string Id
        {
            get { return id; }
        }

        /// <summary>
        /// Gets the file type description.
        /// </summary>
        public string Description
        {
            get { return description; }
        }

        /// <summary>
        /// Gets or sets the id of the file type, or null if none.
        /// </summary>
        public string SuperTypeId { get; set; }

        /// <summary>
        /// Specifies a regular expression used to identify the type of a file by its name,
        /// or null if none.  The matching is performed case-insensitively.
        /// </summary>
        public string FileNameRegex { get; set; }

        /// <summary>
        /// Specifies a regular expression used to identify the type of a file by its contents,
        /// or null if none.  The matching is performed case-insensitively.
        /// </summary>
        public string ContentsRegex { get; set; }
    }
}
