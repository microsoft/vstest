// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Generic;

    /// <summary>
    /// This Interface Provides API's to Collect Metrics.
    /// </summary>
    public interface IMetricsCollector
    { 
        /// <summary>
        /// Add Metric in the Metrics Cache
        /// </summary>
        /// <param name="metric">Metirc Message</param>
        /// <param name="value">Value associated with Metric</param>
        void Add(string metric, string value);

        /// <summary>
        /// Get Metrics
        /// </summary>
        /// <returns>Returns the Telemetry Data Points</returns>
        IDictionary<string, string> Metrics();

        /// <summary>
        /// Clear the Metrics
        /// </summary>
        void Clear();
    }
}