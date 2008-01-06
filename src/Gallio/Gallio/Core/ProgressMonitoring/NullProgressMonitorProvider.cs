// Copyright 2008 MbUnit Project - http://www.mbunit.com/
// Portions Copyright 2000-2004 Jonathan De Halleux, Jamie Cansdale
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

namespace Gallio.Core.ProgressMonitoring
{
    /// <summary>
    /// Runs tasks without reporting any progress.  Argument validation and state
    /// changes are still noted but they do not have any outward effect.
    /// </summary>
    public sealed class NullProgressMonitorProvider : BaseProgressMonitorProvider
    {
        private static readonly NullProgressMonitorProvider instance = new NullProgressMonitorProvider();

        /// <summary>
        /// Gets the singleton instance of the provider.
        /// </summary>
        public static NullProgressMonitorProvider Instance
        {
            get { return instance; }
        }

        private NullProgressMonitorProvider()
        {
        }

        /// <inheritdoc />
        protected override IProgressMonitorPresenter GetPresenter()
        {
            return NullProgressMonitorPresenter.Instance;
        }
    }
}