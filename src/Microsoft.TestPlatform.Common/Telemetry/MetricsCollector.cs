// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <inheritdoc />
    /// <summary>
    /// This Class will collect Metrics in TestHost and Data Collector Processes.
    /// </summary>
    public class MetricsCollector : IMetricsCollector
    {
        private ConcurrentDictionary<string, string> metricDictionary;

        public MetricsCollector()
        {
            this.metricDictionary = new ConcurrentDictionary<string, string>();
        }

        public void AddMetric(string key, string value)
        {
            ValidateArg.NotNull(key, "key");
            ValidateArg.NotNull(value, "value");

            this.metricDictionary?.TryAdd(key, value);
        }

        public IDictionary<string, string> GetMetrics()
        {
            if (this.metricDictionary == null || this.metricDictionary.IsEmpty)
            {
                return new ConcurrentDictionary<string, string>();
            }
            else
            {
                return this.metricDictionary;
            }
        }

        public void FlushMetrics()
        {
            this.metricDictionary?.Clear();
        }
    }
}