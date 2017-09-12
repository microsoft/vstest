// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common
{
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    public class RequestData : IRequestData
    {
        private IMetricsCollection metricsCollection;

        public RequestData(IMetricsCollection metricsCollection)
        {
            Debug.Assert(metricsCollection != null, "metrics collection is null");

            this.metricsCollection = metricsCollection;
        }

        /// <inheritdoc/>
        public IMetricsCollection MetricsCollection => this.metricsCollection;
    }
}
