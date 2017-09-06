// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    public class RequestData : IRequestData
    {
        private MetricsCollector metricsCollector;

        public RequestData(MetricsCollector metricsCollector)
        {
            this.metricsCollector = metricsCollector;
        }

        /// <inheritdoc/>
        public IMetricsCollector MetricsCollector => throw new NotImplementedException();
    }
}
