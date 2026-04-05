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
    /// Synchronizes plugin preferences with test projects using reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class uses reflection to automatically detect and invoke extension methods
    /// for saving/loading plugin preferences. Plugins can extend TestProject by following
    /// the naming convention:
    /// - Load{PluginName}Preferences(this TestProject, I{PluginName}PreferenceManager)
    /// - Save{PluginName}Preferences(this TestProject, I{PluginName}PreferenceManager)
    /// </para>
    /// <para>
    /// The synchronizer will automatically discover all matching extension methods in
    /// loaded assemblies without requiring Icarus to have a dependency on the plugins.
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
        /// Handles the SavingProject event by invoking all plugin-specific save methods.
        /// </summary>
        /// <param name="event">The saving project event.</param>
        public void Handle(SavingProject @event)
        {
            if (projectTreeModel.TestProject == null)
                return;

            InvokePluginMethods("Save");
        }

        /// <summary>
        /// Handles the ProjectLoaded event by invoking all plugin-specific load methods.
        /// </summary>
        /// <param name="event">The project loaded event.</param>
        public void Handle(ProjectLoaded @event)
        {
            if (projectTreeModel.TestProject == null)
                return;

            InvokePluginMethods("Load");
        }

        private void InvokePluginMethods(string methodPrefix)
        {
            try
            {
                // Force-load plugin assemblies that may contain preference methods.
                // The Gallio runtime uses lazy activation: plugin assemblies are not
                // loaded into the AppDomain until a component is first resolved.
                // Calling ResolveServiceType() on matching services triggers the load
                // so that AppDomain.GetAssemblies() can find the extension methods below.
                PreloadPreferenceManagerAssemblies();

                // Search all loaded assemblies for matching extension methods
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // Skip system assemblies for performance
                    if (IsSystemAssembly(assembly))
                        continue;

                    foreach (var type in GetTypesFromAssembly(assembly))
                    {
                        // Look for static classes with extension methods
                        if (!type.IsClass || !type.IsSealed || !type.IsAbstract)
                            continue;

                        // Find all methods matching the pattern: {methodPrefix}*Preferences
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                        {
                            if (!IsPreferenceMethod(method, methodPrefix))
                                continue;

                            TryInvokePreferenceMethod(method);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash if plugin preference synchronization fails
                UnhandledExceptionPolicy.Report("Error synchronizing plugin preferences", ex);
            }
        }

        private void PreloadPreferenceManagerAssemblies()
        {
            try
            {
                foreach (var service in RuntimeAccessor.Registry.Services)
                {
                    if (service.IsDisabled)
                        continue;

                    if (!service.ServiceId.EndsWith("PreferenceManager"))
                        continue;

                    try
                    {
                        // Loading the service type forces the plugin assembly into the AppDomain.
                        service.ResolveServiceType();
                    }
                    catch
                    {
                        // Plugin may be unavailable; skip it.
                    }
                }
            }
            catch
            {
                // Registry may not be available; ignore.
            }
        }

        private bool IsSystemAssembly(Assembly assembly)
        {
            var name = assembly.FullName;
            return name.StartsWith("System") ||
                   name.StartsWith("Microsoft") ||
                   name.StartsWith("mscorlib") ||
                   name.StartsWith("netstandard");
        }

        private Type[] GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                // Some assemblies may fail to load all types
                return new Type[0];
            }
        }

        private bool IsPreferenceMethod(MethodInfo method, string methodPrefix)
        {
            // Check naming convention: {methodPrefix}*Preferences
            if (!method.Name.StartsWith(methodPrefix) || !method.Name.EndsWith("Preferences"))
                return false;

            // Check signature: (TestProject, I*PreferenceManager)
            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                return false;

            // First parameter must be TestProject
            if (parameters[0].ParameterType != typeof(TestProject))
                return false;

            // Second parameter must be an interface ending with PreferenceManager
            var preferenceManagerType = parameters[1].ParameterType;
            if (!preferenceManagerType.IsInterface)
                return false;

            if (!preferenceManagerType.Name.EndsWith("PreferenceManager"))
                return false;

            return true;
        }

        private void TryInvokePreferenceMethod(MethodInfo method)
        {
            try
            {
                var parameters = method.GetParameters();
                var preferenceManagerType = parameters[1].ParameterType;

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
                    return;
                }

                if (preferenceManager == null)
                    return;

                // Invoke the extension method
                method.Invoke(null, new[] { projectTreeModel.TestProject, preferenceManager });
            }
            catch (Exception ex)
            {
                // Log but don't crash if individual plugin fails
                UnhandledExceptionPolicy.Report(
                    string.Format("Error invoking plugin preference method: {0}", method.Name),
                    ex);
            }
        }
    }
}
