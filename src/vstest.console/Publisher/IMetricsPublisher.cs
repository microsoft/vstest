// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher
{
    using System.Collections.Generic;

    /// <summary>
    /// Publish the metrics
    /// </summary>
    public interface IMetricsPublisher
    {
        /// <summary>
        /// Publish the Metrics
        /// </summary>
        /// <param name="eventName">The event Name</param>
        /// <param name="metrics">Key/Value pair of Properties and Values</param>
        void PublishMetrics(string eventName, IDictionary<string, string> metrics);

        /// <summary>
        /// Dispose the Telemetry Session
        /// </summary>
        void Dispose();
    }
}
