// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

using Common.Telemetry;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using static Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.IParallelProxyDiscoveryManager;

/// <summary>
/// ParallelDiscoveryDataAggregator aggregates discovery data from parallel discovery managers
/// </summary>
internal class ParallelDiscoveryDataAggregator
{
    #region PrivateFields

    private readonly object _dataUpdateSyncObject = new();
    private readonly ConcurrentDictionary<string, object> _metricsAggregator;

    #endregion

    public ParallelDiscoveryDataAggregator()
    {
        IsAborted = false;
        TotalTests = 0;
        _metricsAggregator = new ConcurrentDictionary<string, object>();
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

    #region Internal Properties

    /// <summary>
    /// Dictionary which stores source with corresponding discoveryStatus
    /// </summary>
    internal ConcurrentDictionary<string, DiscoveryStatus> SourcesWithDiscoveryStatus = new();

    /// <summary>
    /// Indicates if discovery complete payload already sent back to IDE
    /// </summary>
    internal bool IsMessageSent { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the Aggregated Metrics.
    /// </summary>
    /// <returns></returns>
    public IDictionary<string, object> GetAggregatedDiscoveryDataMetrics()
    {
        if (_metricsAggregator == null || _metricsAggregator.Count == 0)
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
    internal void AggregateTheSourcesWithDiscoveryStatus(IEnumerable<string> sources, DiscoveryStatus status)
    {
        if (sources == null || sources.Count() == 0) return;

        foreach (var source in sources)
        {
            if (status == DiscoveryStatus.NotDiscovered) SourcesWithDiscoveryStatus[source] = status;

            if (!SourcesWithDiscoveryStatus.ContainsKey(source))
            {
                EqtTrace.Warning("ParallelDiscoveryDataAggregator.AggregateTheSourcesWithDiscoveryStatus: " +
                                 $"{source} is not present in SourcesWithDiscoveryStatus dictionary.");
            }
            else
            {
                SourcesWithDiscoveryStatus[source] = status;

                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("ParallelDiscoveryDataAggregator.AggregateTheSourcesWithDiscoveryStatus: " +
                                     $"{source} is marked with {status} status.");
                }
            }
        }
    }

    /// <summary>
    /// Aggregates the value indicating if we already sent message to IDE.
    /// </summary>
    /// <param name="isMessageSent">Boolean value if we already sent message to IDE</param>
    internal void AggregateIsMessageSent(bool isMessageSent)
    {
        IsMessageSent = IsMessageSent || isMessageSent;
    }

    /// <summary>
    /// Returns sources with particular discovery status.
    /// </summary>
    /// <param name="status">Status to filter</param>
    /// <returns></returns>
    internal ICollection<string> GetSourcesWithStatus(DiscoveryStatus status)
    {
        if (SourcesWithDiscoveryStatus == null || SourcesWithDiscoveryStatus.IsEmpty) return new List<string>();

        return SourcesWithDiscoveryStatus.Where(source => source.Value == status)
                                         .Select(source => source.Key).ToList();
    }

    #endregion
}
