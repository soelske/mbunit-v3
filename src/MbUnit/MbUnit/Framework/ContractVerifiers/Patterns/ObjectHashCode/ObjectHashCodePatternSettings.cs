﻿// Copyright 2005-2008 Gallio Project - http://www.gallio.org/
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

namespace MbUnit.Framework.ContractVerifiers.Patterns.ObjectHashCode
{
    /// <summary>
    /// Data container which exposes necessary data required to
    /// run the test pattern <see cref="ObjectHashCodePattern"/>.
    /// </summary>
    internal class ObjectHashCodePatternSettings
    {
        /// <summary>
        /// Gets the target evaluated type.
        /// </summary>
        public Type TargetType
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructs the data container which exposes necessary data required to
        /// run the test pattern <see cref="ObjectHashCodePattern"/>.
        /// </summary>
        /// <param name="targetType">The target evaluated type.</param>
        public ObjectHashCodePatternSettings(Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException("targetType");
            }

            this.TargetType = targetType;
        }
    }
}