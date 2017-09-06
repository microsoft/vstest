// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common
{
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    public class RequestData : IRequestData
    {
        private IMetricsCollector metricsCollector;

        public RequestData(IMetricsCollector metricsCollector)
        {
            this.metricsCollector =
                metricsCollector ?? throw new System.ArgumentNullException(nameof(metricsCollector));
        }

        /// <inheritdoc/>
        public IMetricsCollector MetricsCollector => this.metricsCollector;
    }
}
