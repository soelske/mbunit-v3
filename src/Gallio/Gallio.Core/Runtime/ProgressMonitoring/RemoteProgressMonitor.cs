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

using Gallio.Common.Policies;

namespace Gallio.Runtime.ProgressMonitoring
{
    /// <summary>
    /// A progress monitor wrapper for .NET Core (no remoting).
    /// </summary>
    public sealed class RemoteProgressMonitor : CancelableProgressMonitor, IDisposable
    {
        private readonly IProgressMonitor innerMonitor;
        private readonly AsyncLocal<Dispatcher> dispatcherLocal = new AsyncLocal<Dispatcher>();

        public RemoteProgressMonitor(IProgressMonitor progressMonitor)
        {
            innerMonitor = progressMonitor ?? throw new ArgumentNullException(nameof(progressMonitor));
            RegisterDispatcher();
        }

        private void RegisterDispatcher()
        {
            var dispatcher = new Dispatcher(this);
            dispatcherLocal.Value = dispatcher;

            innerMonitor.Canceled += (sender, e) =>
            {
                try
                {
                    dispatcher.Cancel();
                }
                catch (Exception ex)
                {
                    UnhandledExceptionPolicy.Report(
                        "Could not locally dispatch cancelation event.", ex);
                }
            };
        }

        public override ProgressMonitorTaskCookie BeginTask(string taskName, double totalWorkUnits)
        {
            innerMonitor.BeginTask(taskName, totalWorkUnits);
            return new ProgressMonitorTaskCookie(this);
        }

        public override void SetStatus(string status) => innerMonitor.SetStatus(status);

        public override void Worked(double workUnits) => innerMonitor.Worked(workUnits);

        public override void Done() => innerMonitor.Done();

        public override IProgressMonitor CreateSubProgressMonitor(double parentWorkUnits)
        {
            var subMonitor = innerMonitor.CreateSubProgressMonitor(parentWorkUnits);
            return new RemoteProgressMonitor(subMonitor);
        }

        protected override void OnCancel() => innerMonitor.Cancel();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerMonitor.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class Dispatcher
        {
            private readonly RemoteProgressMonitor monitor;

            public Dispatcher(RemoteProgressMonitor monitor)
            {
                this.monitor = monitor;
            }

            public void Cancel()
            {
                try
                {
                    monitor.NotifyCanceled();
                }
                catch (Exception ex)
                {
                    UnhandledExceptionPolicy.Report(
                        "Could not locally dispatch cancelation event.", ex);
                }
            }
        }
    }
}