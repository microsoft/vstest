// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Generic;

    // If Telemtry is opted out, this class will be initialized and will do No operation.
    class DummyMetricCollector : IMetricsCollector
    {
        public void AddMetric(string message, string value)
        {
            // No operation
        }

        public void FlushMetrics()
        {
            // No Operation
        }

        public IDictionary<string, string> GetMetrics()
        {
            return new Dictionary<string, string>();
        }
    }
}
