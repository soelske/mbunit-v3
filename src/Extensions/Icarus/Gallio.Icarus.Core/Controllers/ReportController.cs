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
using System.Globalization;
using System.IO;
using Gallio.Common.Policies;
using Gallio.Icarus.Controllers.Interfaces;
using Gallio.Icarus.Reports;
using Gallio.Icarus.Services.Interfaces;
using Gallio.Runner.Reports.Schema;
using Gallio.Runtime;
using Gallio.Runtime.Logging;
using Gallio.Runtime.ProgressMonitoring;

namespace Gallio.Icarus.Controllers
{
    internal class ReportController : IReportController
    {
        private readonly IReportService reportService;

        public ReportController(IReportService reportService)
        {
            this.reportService = reportService;
        }

        public System.Collections.Generic.IList<string> ReportTypes
        {
            get { return reportService.ReportTypes; }
        }

        public void GenerateReport(Report report, ReportOptions reportOptions, IProgressMonitor progressMonitor)
        {
            // Gallio.Reports.dll not loaded — skip report generation and log a warning.
            if (reportService.ReportTypes.Count == 0)
            {
                RuntimeAccessor.Logger.Log(LogSeverity.Warning,
                    "Report generation skipped: Gallio.Reports.dll is not loaded. " +
                    "Add Gallio.Reports.Core as a project reference to enable report formatting.");
                return;
            }

            string fileName = Path.Combine(reportOptions.ReportDirectory,
                GenerateReportName(report, reportOptions.ReportNameFormat));
            reportService.SaveReportAs(report, fileName, "xml", progressMonitor);
        }

        private static string GenerateReportName(Report report, string reportNameFormat)
        {
            DateTime reportTime = report.TestPackageRun != null ? report.TestPackageRun.StartTime : DateTime.Now;

            return String.Format(CultureInfo.InvariantCulture, reportNameFormat,
                reportTime.ToString(@"yyyyMMdd"),
                reportTime.ToString(@"HHmmss"));
        }

        public string ShowReport(Report report, string reportType, IProgressMonitor progressMonitor)
        {
            return reportService.SaveReportAs(report,
                SpecialPathPolicy.For<ReportController>().CreateTempDirectoryWithUniqueName().FullName,
                reportType, progressMonitor);
        }

        public string ConvertSavedReport(string fileName, string format, IProgressMonitor progressMonitor)
        {
            return reportService.ConvertSavedReport(fileName, format, progressMonitor);
        }
    }
}
