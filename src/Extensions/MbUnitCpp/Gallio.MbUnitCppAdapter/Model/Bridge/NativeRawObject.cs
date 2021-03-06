﻿// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
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
using Gallio.Model;
using System.Runtime.InteropServices;

namespace Gallio.MbUnitCppAdapter.Model.Bridge
{
    /// <summary>
    /// Represents the raw native unmanaged object/variable.
    /// </summary>
    public struct NativeRawObject
    {
        private readonly string description;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="description">The description of the object.</param>
        public NativeRawObject(string description)
        {
            this.description = description;
        }

        /// <summary>
        /// Prints the address as an hexadecimal number.
        /// </summary>
        /// <returns>The text representation of the address</returns>
        public override string ToString()
        {
            return description;
        }
    }
}
