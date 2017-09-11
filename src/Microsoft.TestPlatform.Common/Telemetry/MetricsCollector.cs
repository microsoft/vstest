// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <inheritdoc />
    /// <summary>
    /// This Class will collect Metrics.
    /// </summary>
    public class MetricsCollector : IMetricsCollector
    {
        private Dictionary<string, string> metricDictionary;

        public MetricsCollector()
        {
            this.metricDictionary = new Dictionary<string, string>();
        }

        public void Add(string metric, string value)
        {
            ValidateArg.NotNull(metric, "metric");
            ValidateArg.NotNull(value, "value");

            if (metricDictionary.ContainsKey(metric))
            {
                metricDictionary[metric] = value;
            }
            else
            {
                metricDictionary?.Add(metric, value);
            }
        }

        public IDictionary<string, string> Metrics()
        {
            if (this.metricDictionary == null || this.metricDictionary.Count == 0)
            {
                return new Dictionary<string, string>();
            }
            else
            {
                return this.metricDictionary;
            }
        }

        public void Clear()
        {
            this.metricDictionary?.Clear();
        }
    }
}
