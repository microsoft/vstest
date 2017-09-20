// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
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
        /// <param name="eventName"></param>
        /// <param name="metrics"></param>
        void PublishMetrics(string eventName, IDictionary<string, string> metrics);
    }
}
