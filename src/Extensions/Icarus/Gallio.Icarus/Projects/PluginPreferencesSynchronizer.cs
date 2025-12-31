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
using System.Reflection;
using Gallio.Common.Policies;
using Gallio.Icarus.Controllers;
using Gallio.Icarus.Events;
using Gallio.Icarus.Models;
using Gallio.Runner.Projects;
using Gallio.UI.Events;
using Gallio.Runtime;

namespace Gallio.Icarus.Projects
{
    /// <summary>
    /// Synchronizes plugin preferences (like AutoCAD) with test projects using reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class uses reflection to detect and invoke extension methods on TestProject
    /// for saving/loading plugin preferences. This allows plugins to extend TestProject
    /// without Icarus having a direct dependency on them.
    /// </para>
    /// </remarks>
    public class PluginPreferencesSynchronizer : Handles<SavingProject>, Handles<ProjectLoaded>
    {
        private readonly IProjectTreeModel projectTreeModel;

        /// <summary>
        /// Creates a new <see cref="PluginPreferencesSynchronizer"/>.
        /// </summary>
        /// <param name="projectTreeModel">The project tree model.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="projectTreeModel"/> is null.</exception>
        public PluginPreferencesSynchronizer(IProjectTreeModel projectTreeModel)
        {
            if (projectTreeModel == null)
                throw new ArgumentNullException("projectTreeModel");

            this.projectTreeModel = projectTreeModel;
        }

        /// <summary>
        /// Handles the SavingProject event by invoking plugin-specific save methods.
        /// </summary>
        /// <param name="event">The saving project event.</param>
        public void Handle(SavingProject @event)
        {
            if (projectTreeModel.TestProject == null)
                return;

            TryInvokePluginMethod("SaveAutoCADPreferences", "Gallio.AutoCAD.Preferences.IAcadPreferenceManager");
            // Add more plugins here as needed
        }

        /// <summary>
        /// Handles the ProjectLoaded event by invoking plugin-specific load methods.
        /// </summary>
        /// <param name="event">The project loaded event.</param>
        public void Handle(ProjectLoaded @event)
        {
            if (projectTreeModel.TestProject == null)
                return;

            TryInvokePluginMethod("LoadAutoCADPreferences", "Gallio.AutoCAD.Preferences.IAcadPreferenceManager");
            // Add more plugins here as needed
        }

        private void TryInvokePluginMethod(string methodName, string preferenceManagerTypeName)
        {
            try
            {
                // Try to find the extension method in loaded assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // Look for extension method in Gallio.AutoCAD.Projects namespace
                    var extensionType = assembly.GetType("Gallio.AutoCAD.Projects.TestProjectExtensions", false);
                    if (extensionType == null)
                        continue;

                    // Try to resolve the preference manager type from the same assembly
                    var preferenceManagerType = assembly.GetType(preferenceManagerTypeName, false);
                    if (preferenceManagerType == null)
                        continue;

                    // Find the extension method with the correct signature
                    var method = extensionType.GetMethod(methodName, 
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new[] { typeof(TestProject), preferenceManagerType },
                        null);

                    if (method == null)
                        continue;

                    // Try to get the preference manager from the service locator
                    object preferenceManager = null;
                    try
                    {
                        var serviceLocator = RuntimeAccessor.ServiceLocator;
                        var resolveMethod = serviceLocator.GetType().GetMethod("Resolve", new Type[] { typeof(Type) });
                        preferenceManager = resolveMethod.Invoke(serviceLocator, new object[] { preferenceManagerType });
                    }
                    catch
                    {
                        // Preference manager not registered - plugin not loaded
                        continue;
                    }

                    if (preferenceManager == null)
                        continue;

                    // Invoke the extension method
                    method.Invoke(null, new[] { projectTreeModel.TestProject, preferenceManager });
                    return;
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash if plugin preference synchronization fails
                UnhandledExceptionPolicy.Report("Error synchronizing plugin preferences", ex);
            }
        }
    }
}
