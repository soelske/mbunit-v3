// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
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

using System;
using Gallio.AutoCAD.Preferences;
using Gallio.Runner.Projects;

namespace Gallio.AutoCAD.Projects
{
    /// <summary>
    /// Extension methods for <see cref="TestProject"/> to support AutoCAD preferences.
    /// </summary>
    public static class TestProjectExtensions
    {
        /// <summary>
        /// Loads AutoCAD preferences from a test project into a preference manager.
        /// </summary>
        /// <param name="testProject">The test project to load from.</param>
        /// <param name="preferenceManager">The preference manager to update.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testProject"/> or <paramref name="preferenceManager"/> is null.</exception>
        public static void LoadAutoCADPreferences(this TestProject testProject, IAcadPreferenceManager preferenceManager)
        {
            if (testProject == null)
                throw new ArgumentNullException("testProject");
            if (preferenceManager == null)
                throw new ArgumentNullException("preferenceManager");

            if (testProject.AutoCADCommandLineArguments != null)
                preferenceManager.CommandLineArguments = testProject.AutoCADCommandLineArguments;

            if (testProject.AutoCADStartupAction.HasValue)
                preferenceManager.StartupAction = (StartupAction)testProject.AutoCADStartupAction.Value;

            if (testProject.AutoCADUserSpecifiedExecutable != null)
                preferenceManager.UserSpecifiedExecutable = testProject.AutoCADUserSpecifiedExecutable;

            if (testProject.AutoCADWorkingDirectory != null)
                preferenceManager.WorkingDirectory = testProject.AutoCADWorkingDirectory;
        }

        /// <summary>
        /// Saves AutoCAD preferences from a preference manager into a test project.
        /// </summary>
        /// <param name="testProject">The test project to save to.</param>
        /// <param name="preferenceManager">The preference manager to read from.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testProject"/> or <paramref name="preferenceManager"/> is null.</exception>
        public static void SaveAutoCADPreferences(this TestProject testProject, IAcadPreferenceManager preferenceManager)
        {
            if (testProject == null)
                throw new ArgumentNullException("testProject");
            if (preferenceManager == null)
                throw new ArgumentNullException("preferenceManager");

            testProject.AutoCADCommandLineArguments = preferenceManager.CommandLineArguments;
            testProject.AutoCADStartupAction = (int)preferenceManager.StartupAction;
            testProject.AutoCADUserSpecifiedExecutable = preferenceManager.UserSpecifiedExecutable;
            testProject.AutoCADWorkingDirectory = preferenceManager.WorkingDirectory;
        }

        /// <summary>
        /// Gets a value indicating whether the test project has any AutoCAD preferences set.
        /// </summary>
        /// <param name="testProject">The test project to check.</param>
        /// <returns>True if any AutoCAD preferences are set; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testProject"/> is null.</exception>
        public static bool HasAutoCADPreferences(this TestProject testProject)
        {
            if (testProject == null)
                throw new ArgumentNullException("testProject");

            return testProject.AutoCADCommandLineArguments != null
                || testProject.AutoCADStartupAction.HasValue
                || testProject.AutoCADUserSpecifiedExecutable != null
                || testProject.AutoCADWorkingDirectory != null;
        }
    }
}
