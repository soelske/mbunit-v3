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
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Gallio.Common;
using Gallio.Model.Schema;
using Gallio.Runner.Reports.Schema;
using Gallio.UI.Common.Synchronization;

namespace Gallio.UI.Reports
{
    /// <summary>
    /// Displays a summary of a set of test step runs.
    /// When Gallio.Reports plugin resources are unavailable the viewer stays blank
    /// instead of throwing a RuntimeException.
    /// </summary>
    public partial class TestStepRunViewer : UserControl
    {
        private HtmlTestStepRunFormatter formatter;
        // null means the formatter could not be created (plugin resources missing).
        private bool formatterUnavailable;
        private volatile FileInfo htmlFile;

        /// <summary>
        /// Creates a test step run viewer.
        /// </summary>
        public TestStepRunViewer()
        {
            InitializeComponent();
            Disposed += HandleDisposed;
        }

        /// <inheritdoc />
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateAsync();
        }

        /// <summary>
        /// Clears the contents of the report viewer and discards all cached content.
        /// </summary>
        public void Clear()
        {
            ClearNoUpdate();
            UpdateAsync();
        }

        private void ClearNoUpdate()
        {
            if (formatter != null)
                formatter.Clear();

            htmlFile = null;
        }

        /// <summary>
        /// Displays information about a set of test step runs.
        /// </summary>
        public void Show(ICollection<TestStepRun> testStepRuns)
        {
            Show(testStepRuns, true);
        }

        /// <summary>
        /// Displays information about a set of test step runs.
        /// </summary>
        public void Show(ICollection<TestStepRun> testStepRuns, bool recurse)
        {
            Show(testStepRuns, null, recurse);
        }

        /// <summary>
        /// Displays information about a set of test step runs, using additional
        /// information from the test model when available.
        /// </summary>
        public void Show(ICollection<TestStepRun> testStepRuns, TestModelData testModelData, bool recurse)
        {
            if (testStepRuns == null || testStepRuns.Contains(null))
                throw new ArgumentNullException("testStepRuns");

            if (testStepRuns.Count == 0)
            {
                ClearNoUpdate();
            }
            else
            {
                // When the Gallio.Reports plugin resources are not registered,
                // EnsureFormatter sets formatterUnavailable = true and leaves
                // formatter == null. In that case we skip rendering silently.
                EnsureFormatter();
                if (formatter != null)
                    htmlFile = formatter.Format(testStepRuns, testModelData, recurse);
            }

            UpdateAsync();
        }

        private void HandleDisposed(object sender, EventArgs e)
        {
            ClearNoUpdate();
        }

        private void UpdateAsync()
        {
            DoAsync(() =>
            {
                try
                {
                    // webBrowser.IsBusy calls IHTMLLocation.GetHref() via COM; when the
                    // control transitions between cross-origin URLs (file:// → null) MSHTML
                    // returns E_ACCESSDENIED (0x80070005). Catch it here so the viewer
                    // stays silent instead of surfacing an unhandled exception.
                    if (!webBrowser.IsDisposed && webBrowser.IsBusy)
                        webBrowser.Stop();

                    var cachedHtmlFile = htmlFile;

                    if (cachedHtmlFile != null)
                    {
                        webBrowser.Url = new Uri(cachedHtmlFile.FullName);
                        webBrowser.Show();
                    }
                    else
                    {
                        webBrowser.Url = null;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // COM access denied during navigation transition — ignore and let the
                    // next Show()/Clear() call update the browser.
                }
            });
        }

        private void DoAsync(GallioAction action)
        {
            if (InvokeRequired)
                SyncContext.Post(cb => action(), null);
            else
                action();
        }

        private void EnsureFormatter()
        {
            lock (this)
            {
                if (formatter != null || formatterUnavailable)
                    return;

                try
                {
                    formatter = new HtmlTestStepRunFormatter();
                }
                catch
                {
                    // Gallio.Reports plugin resources not registered — execution log
                    // stays blank. Will be fixed when Gallio.Reports.Core is added.
                    formatterUnavailable = true;
                }
            }
        }
    }
}
