// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher
{
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// This class will be initialized if Telemetry is opted out.
    /// </summary>
    public class NoOpMetricsPublisher : IMetricsPublisher
    {
        /// <summary>
        /// Will do NO-OP.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="metrics"></param>
        public void PublishMetrics(string eventName, IDictionary<string, object> metrics)
        {
            // Log to Text File
            var logEnabled = Environment.GetEnvironmentVariable("VSTEST_LOGTELEMETRY");
            if (!string.IsNullOrEmpty(logEnabled) && logEnabled.Equals("1", StringComparison.Ordinal))
            {
                this.LogToFile(eventName, metrics, new FileHelper());
            }
        }

        /// <summary>
        /// Will do NO-OP
        /// </summary>
        public void Dispose()
        {
            // No operation
        }

        /// <summary>
        /// Log the telemetry to file.
        /// For Testing purposes.
        /// </summary>
        /// <param name="eventName">
        /// The event Name.
        /// </param>
        /// <param name="metrics">
        /// Metrics
        /// </param>
        /// <param name="fileHelper">
        /// The file Helper.
        /// </param>
        internal void LogToFile(string eventName, IDictionary<string, object> metrics, IFileHelper fileHelper)
        {
            string resultDirectory = Path.GetTempPath() + "TelemetryLogs";
            string resultFileName = Guid.NewGuid().ToString();
            string path = Path.Combine(resultDirectory, resultFileName);

            if (!fileHelper.DirectoryExists(resultDirectory))
            {
                fileHelper.CreateDirectory(resultDirectory);
            }

            var telemetryData = string.Join(";", metrics.Select(x => x.Key + "=" + x.Value));
            var finalData = string.Concat(eventName, ";", telemetryData);

            fileHelper.WriteAllTextToFile(path, finalData);
        }
    }
}
