// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    /// <summary>
    /// ParallelDiscoveryDataAggregator aggregates discovery data from parallel discovery managers
    /// </summary>
    internal class ParallelDiscoveryDataAggregator
    {
        #region PrivateFields
                
        private object dataUpdateSyncObject = new object();
        private ConcurrentDictionary<string, string> metricsAggregator;

        #endregion

        public ParallelDiscoveryDataAggregator()
        {
            IsAborted = false;
            TotalTests = 0;
            this.metricsAggregator = new ConcurrentDictionary<string, string>();
        }

        #region Public Properties

        /// <summary>
        /// Set to true if any of the request is aborted
        /// </summary>
        public bool IsAborted { get; private set; }

        /// <summary>
        /// Aggregate total test count
        /// </summary>
        public long TotalTests { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the Aggregated Metrcis.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetAggregatedDiscoveryDataMetrics()
        {
            if (this.metricsAggregator == null || this.metricsAggregator.Count == 0)
            {
                return new ConcurrentDictionary<string, string>();
            }

            var adapterUsedCount = this.metricsAggregator.Count(metrics =>
                metrics.Key.Contains(TelemetryDataConstants.TotalTestsByAdapter));

            var adaptersDiscoveredCount = this.metricsAggregator.Count(metrics =>
                metrics.Key.Contains(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter));

            // Aggregating Total Adapter Used Count
            this.metricsAggregator.TryAdd(
                TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests,
                adapterUsedCount.ToString());

            // Aggregating Total Adapters Discovered Count
            this.metricsAggregator.TryAdd(
                TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery,
                adaptersDiscoveredCount.ToString());

            return this.metricsAggregator;
        }

        /// <summary>
        /// Aggregate discovery data 
        /// Must be thread-safe as this is expected to be called by parallel managers
        /// </summary>
        public void Aggregate(long totalTests, bool isAborted)
        {
            lock (dataUpdateSyncObject)
            {
                this.IsAborted = this.IsAborted || isAborted;

                if (this.IsAborted)
                {
                    // Do not aggregate tests count if test discovery is aborted. It is mandated by
                    // platform that tests count is negative for discovery abort event.
                    // See `DiscoveryCompleteEventArgs`.
                    this.TotalTests = -1;
                    return;
                }

                this.TotalTests = this.TotalTests + totalTests;
            }
        }

        /// <summary>
        /// Aggregates the metrics from Test Host Process.
        /// </summary>
        /// <param name="metrics"></param>
        public void AggregateDiscoveryDataMetrics(IDictionary<string, string> metrics)
        {
            if (metrics == null || metrics.Count == 0 || metricsAggregator == null)
            {
                return;
            }

            foreach (var metric in metrics)
            {
                if (metric.Key.Contains(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter) || metric.Key.Contains(TelemetryDataConstants.TimeTakenInSecByAllAdapters) || (metric.Key.Contains(TelemetryDataConstants.TotalTestsDiscovered) || metric.Key.Contains(TelemetryDataConstants.TotalTestsByAdapter) || metric.Key.Contains(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec)))
                {
                    var newValue = Double.Parse(metric.Value);
                    string oldValue;

                    if (this.metricsAggregator.TryGetValue(metric.Key, out oldValue))
                    {
                        this.metricsAggregator[metric.Key] = (newValue + Double.Parse(oldValue)).ToString();
                    }

                    else
                    {
                        this.metricsAggregator.TryAdd(metric.Key, newValue.ToString());
                    }
                }
            }
        }

        #endregion
    }
}
