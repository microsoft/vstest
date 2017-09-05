// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Generic;

    /// <summary>
    /// This Interface Provides API's to Collect Metrics in TestHost and DataCollector Processes.
    /// </summary>
    public interface IMetricsCollector
    { 
        /// <summary>
        /// Add Metric in the Metrics Cache
        /// </summary>
        /// <param name="message">Metirc Message</param>
        /// <param name="value">Value associated with Metric</param>
        void AddMetric(string message, string value);

        /// <summary>
        /// Get Metrics
        /// </summary>
        /// <returns>Returns the Telemetry Data Points</returns>
        IDictionary<string, string> GetMetrics();

        /// <summary>
        /// Flush the Metrics
        /// </summary>
        void FlushMetrics();
    }
}