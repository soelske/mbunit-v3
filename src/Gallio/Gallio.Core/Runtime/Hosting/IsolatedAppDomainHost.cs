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
using Gallio.Common.Remoting;
using Gallio.Runtime.Debugging;
using Gallio.Runtime.Loader;
using Gallio.Runtime.Logging;
using Gallio.Common.Reflection;

namespace Gallio.Runtime.Hosting
{
    /// <summary>
    /// An isolated host that runs code within an isolated AssemblyLoadContext of this process.
    /// Communication with the isolated context occurs over a .NET remoting-like mechanism.
    /// </summary>
    public class IsolatedAppDomainHost : RemoteHost
    {
        private readonly IDebuggerManager debuggerManager;

        private AppDomainUtils.IsolatedLoadContextWrapper appDomainWrapper;
        private CurrentDirectorySwitcher currentDirectorySwitcher;
        private string temporaryConfigurationFilePath;

        public IsolatedAppDomainHost(HostSetup hostSetup, ILogger logger, IDebuggerManager debuggerManager)
            : base(hostSetup, logger, null)
        {
            this.debuggerManager = debuggerManager ?? throw new ArgumentNullException(nameof(debuggerManager));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                FreeResources();
        }

        protected override IRemoteHostService AcquireRemoteHostService()
        {
            try
            {
                SetWorkingDirectory();
                CreateTemporaryConfigurationFile();
                CreateIsolatedLoadContext();

                var endpoint = (IsolatedAppDomainEndpoint)AppDomainUtils.CreateRemoteInstance(
                    appDomainWrapper,
                    typeof(IsolatedAppDomainEndpoint).FullName
                );

                foreach (string hintDirectory in HostSetup.HintDirectories)
                    endpoint.AddHintDirectory(hintDirectory);

                return endpoint.CreateRemoteHostService(null);
            }
            catch (Exception)
            {
                FreeResources();
                throw;
            }
        }

        private void FreeResources()
        {
            ResetWorkingDirectory();
            UnloadIsolatedContext();
            DeleteTemporaryConfigurationFile();
        }

        private void SetWorkingDirectory()
        {
            if (!string.IsNullOrEmpty(HostSetup.WorkingDirectory))
                currentDirectorySwitcher = new CurrentDirectorySwitcher(HostSetup.WorkingDirectory);
        }

        private void CreateTemporaryConfigurationFile()
        {
            try
            {
                HostSetup patchedSetup = HostSetup.Copy();
                patchedSetup.Configuration.AddAssemblyBinding(new AssemblyBinding(typeof(IsolatedAppDomainHost).Assembly));
                temporaryConfigurationFilePath = patchedSetup.WriteTemporaryConfigurationFile();
            }
            catch (Exception ex)
            {
                throw new HostException("Could not write the temporary configuration file.", ex);
            }
        }

        private void CreateIsolatedLoadContext()
        {
            string mainAssemblyPath = typeof(IsolatedAppDomainHost).Assembly.Location;
            appDomainWrapper = AppDomainUtils.CreateIsolatedLoadContext(
                "IsolatedAppDomainHost",
                mainAssemblyPath,
                enableUnload: true
            );
        }

        private void UnloadIsolatedContext()
        {
            if (appDomainWrapper != null)
            {
                AppDomainUtils.UnloadContext(appDomainWrapper);
                appDomainWrapper = null;
            }
        }

        private void ResetWorkingDirectory()
        {
            try
            {
                currentDirectorySwitcher?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log(LogSeverity.Warning, "Could not reset working directory.", ex);
            }
            finally
            {
                currentDirectorySwitcher = null;
            }
        }

        private void DeleteTemporaryConfigurationFile()
        {
            try
            {
                if (temporaryConfigurationFilePath != null)
                    File.Delete(temporaryConfigurationFilePath);
            }
            catch (Exception ex)
            {
                Logger.Log(LogSeverity.Warning, "Could not delete temporary configuration file.", ex);
            }
            finally
            {
                temporaryConfigurationFilePath = null;
            }
        }

        internal class IsolatedAppDomainEndpoint : LongLivedMarshalByRefObject
        {
            private DefaultAssemblyLoader assemblyLoader;

            public void AddHintDirectory(string hintDirectory)
            {
                if (assemblyLoader == null)
                    assemblyLoader = new DefaultAssemblyLoader();

                assemblyLoader.AddHintDirectory(hintDirectory);
            }

            public IRemoteHostService CreateRemoteHostService(TimeSpan? watchdogTimeout)
            {
                return new RemoteHostService(watchdogTimeout);
            }
        }
    }
}