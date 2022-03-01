// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

/// <summary>
/// ParallelDiscoveryDataAggregator aggregates discovery data from parallel discovery managers
/// </summary>
internal class ParallelDiscoveryDataAggregator
{
    private readonly object _dataUpdateSyncObject = new();
    private readonly ConcurrentDictionary<string, object> _metricsAggregator;

    public ParallelDiscoveryDataAggregator()
    {
        IsAborted = false;
        TotalTests = 0;
        _metricsAggregator = new ConcurrentDictionary<string, object>();
    }

    /// <summary>
    /// Set to true if any of the request is aborted
    /// </summary>
    public bool IsAborted { get; private set; }

    /// <summary>
    /// Aggregate total test count
    /// </summary>
    public long TotalTests { get; private set; }


    /// <summary>
    /// Dictionary which stores source with corresponding discoveryStatus
    /// </summary>
    private readonly ConcurrentDictionary<string, DiscoveryStatus> _sourcesWithDiscoveryStatus = new();

    /// <summary>
    /// Indicates if discovery complete payload already sent back to IDE
    /// </summary>
    internal bool IsMessageSent { get; private set; }

    /// <summary>
    /// Returns the Aggregated Metrics.
    /// </summary>
    /// <returns></returns>
    public IDictionary<string, object> GetAggregatedDiscoveryDataMetrics()
    {
        if (_metricsAggregator == null || _metricsAggregator.IsEmpty)
        {
            return new ConcurrentDictionary<string, object>();
        }

        var adapterUsedCount = _metricsAggregator.Count(metrics =>
            metrics.Key.Contains(TelemetryDataConstants.TotalTestsByAdapter));

        var adaptersDiscoveredCount = _metricsAggregator.Count(metrics =>
            metrics.Key.Contains(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter));

        // Aggregating Total Adapter Used Count
        _metricsAggregator.TryAdd(
            TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests,
            adapterUsedCount);

        // Aggregating Total Adapters Discovered Count
        _metricsAggregator.TryAdd(
            TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery,
            adaptersDiscoveredCount);

        return _metricsAggregator;
    }

    /// <summary>
    /// Aggregate discovery data
    /// Must be thread-safe as this is expected to be called by parallel managers
    /// </summary>
    public void Aggregate(long totalTests, bool isAborted)
    {
        lock (_dataUpdateSyncObject)
        {
            IsAborted = IsAborted || isAborted;

            if (IsAborted)
            {
                // Do not aggregate tests count if test discovery is aborted. It is mandated by
                // platform that tests count is negative for discovery abort event.
                // See `DiscoveryCompleteEventArgs`.
                TotalTests = -1;
                return;
            }

            TotalTests += totalTests;
        }
    }

    /// <summary>
    /// Aggregates the metrics from Test Host Process.
    /// </summary>
    /// <param name="metrics"></param>
    public void AggregateDiscoveryDataMetrics(IDictionary<string, object> metrics)
    {
        if (metrics == null || metrics.Count == 0 || _metricsAggregator == null)
        {
            return;
        }

        foreach (var metric in metrics)
        {
            if (metric.Key.Contains(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter) || metric.Key.Contains(TelemetryDataConstants.TimeTakenInSecByAllAdapters) || (metric.Key.Contains(TelemetryDataConstants.TotalTestsDiscovered) || metric.Key.Contains(TelemetryDataConstants.TotalTestsByAdapter) || metric.Key.Contains(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec)))
            {
                var newValue = Convert.ToDouble(metric.Value);

                if (_metricsAggregator.TryGetValue(metric.Key, out object oldValue))
                {
                    double oldDoubleValue = Convert.ToDouble(oldValue);
                    _metricsAggregator[metric.Key] = newValue + oldDoubleValue;
                }
                else
                {
                    _metricsAggregator.TryAdd(metric.Key, newValue);
                }
            }
        }
    }

    /// <summary>
    /// Aggregate the source as fully discovered
    /// </summary>
    /// <param name="sorce">Fully discovered source</param>
    public void MarkSourcesWithStatus(ICollection<string> sources, DiscoveryStatus status)
        => DiscoveryManager.MarkSourcesWithStatus(sources, status, _sourcesWithDiscoveryStatus);

    /// <summary>
    /// Aggregates the value indicating if we already sent message to IDE.
    /// </summary>
    /// <param name="isMessageSent">Boolean value if we already sent message to IDE</param>
    public void AggregateIsMessageSent(bool isMessageSent)
    {
        IsMessageSent = IsMessageSent || isMessageSent;
    }

    /// <summary>
    /// Returns sources with particular discovery status.
    /// </summary>
    /// <param name="status">Status to filter</param>
    /// <returns></returns>
    public List<string> GetSourcesWithStatus(DiscoveryStatus status)
        => DiscoveryManager.GetSourcesWithStatus(status, _sourcesWithDiscoveryStatus);
}
