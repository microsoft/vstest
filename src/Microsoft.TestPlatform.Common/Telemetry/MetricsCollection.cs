// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <inheritdoc />
    /// <summary>
    /// This Class will collect Metrics.
    /// </summary>
    public class MetricsCollection : IMetricsCollection
    {
        private Dictionary<string, object> metricDictionary;

        /// <summary>
        /// The Metrics Collection
        /// </summary>
        public MetricsCollection()
        {
            this.metricDictionary = new Dictionary<string, object>();
        }

        /// <summary>
        /// Add The Metrics
        /// </summary>
        /// <param name="metric"></param>
        /// <param name="value"></param>
        public void Add(string metric, object value)
        {
            this.metricDictionary[metric] = value;
        }

        /// <summary>
        /// Returns the Metrics
        /// </summary>
        public IDictionary<string, object> Metrics => this.metricDictionary;

        /// <summary>
        /// Clears the Metrics
        /// </summary>
        public void Clear()
        {
            this.metricDictionary?.Clear();
        }
    }
}
