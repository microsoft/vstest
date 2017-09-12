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
    public class MetricsCollection : IMetricsCollection
    {
        private Dictionary<string, string> metricDictionary;

        /// <summary>
        /// The Metrics Collector
        /// </summary>
        public MetricsCollection()
        {
            this.metricDictionary = new Dictionary<string, string>();
        }

        /// <summary>
        /// Add The Metrics
        /// </summary>
        /// <param name="metric"></param>
        /// <param name="value"></param>
        public void Add(string metric, string value)
        {
            ValidateArg.NotNull(metric, "metric");
            ValidateArg.NotNull(value, "value");

            this.metricDictionary[metric] = value;
        }

        /// <summary>
        /// Returns the Metrics
        /// </summary>
        public IDictionary<string, string> Metrics
        {
            get
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
        }

        /// <summary>
        /// Clears the Metrics
        /// </summary>
        public void Clear()
        {
            this.metricDictionary?.Clear();
        }
    }
}
