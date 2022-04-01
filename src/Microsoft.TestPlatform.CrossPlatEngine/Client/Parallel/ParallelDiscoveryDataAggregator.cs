// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

/// <summary>
/// ParallelDiscoveryDataAggregator aggregates discovery data from parallel discovery managers
/// </summary>
internal class ParallelDiscoveryDataAggregator
{
    private readonly object _dataUpdateSyncObject = new();
    private readonly ConcurrentDictionary<string, object> _metricsAggregator = new();
    private readonly ConcurrentDictionary<string, DiscoveryStatus> _sourcesWithDiscoveryStatus = new();

    /// <summary>
    /// Set to true if any of the request is aborted
    /// </summary>
    public bool IsAborted { get; private set; }

    /// <summary>
    /// Aggregate total test count
    /// </summary>
    public long TotalTests { get; private set; }

    /// <summary>
    /// A collection of aggregated discovered extensions.
    /// </summary>
    public Dictionary<string, HashSet<string>> DiscoveredExtensions { get; private set; } = new();

    /// <summary>
    /// Indicates if discovery complete payload already sent back to IDE
    /// </summary>
    internal bool IsMessageSent { get; private set; }

    /// <summary>
    /// Returns the Aggregated Metrics.
    /// </summary>
    /// <returns></returns>
    public IDictionary<string, object> GetMetrics()
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
    public void Aggregate(DiscoveryCompleteEventArgs discoveryCompleteEventArgs)
    {
        lock (_dataUpdateSyncObject)
        {
            IsAborted = IsAborted || discoveryCompleteEventArgs.IsAborted;

            // Do not aggregate tests count if test discovery is aborted. It is mandated by
            // platform that tests count is negative for discovery abort event.
            // See `DiscoveryCompleteEventArgs`.
            TotalTests = IsAborted ? -1 : TotalTests + discoveryCompleteEventArgs.TotalCount;

            // Aggregate the discovered extensions.
            DiscoveredExtensions = TestExtensions.CreateMergedDictionary(DiscoveredExtensions, discoveryCompleteEventArgs.DiscoveredExtensions);
        }

        AggregateMetrics(discoveryCompleteEventArgs.Metrics);

        // Do not aggregate NotDiscovered, PartiallyDiscovered, FullyDiscovered sources here
        // because this aggregator is shared with proxies so the state is already up-to-date.
        //
        // The reason to share the aggregator is that we only get 1 notification for completed discovery
        // when discovery is done Instead of 1 notification per discovery manager. And we also don't get 
        // any notification for discovery of sources that did not start discovering yet. So we need the state
        // in the aggregator to start with all the sources that we were requested to discover in NotDiscovered
        // state. And then as we discover them, we need to update that directly. So when any discovery is cancelled
        // we can just take the current state in the aggregator and report it.
    }

    /// <summary>
    /// Aggregates the metrics from Test Host Process.
    /// </summary>
    /// <param name="metrics"></param>
    public void AggregateMetrics(IDictionary<string, object>? metrics)
    {
        if (metrics == null || metrics.Count == 0 || _metricsAggregator == null)
        {
            return;
        }

        foreach (var metric in metrics)
        {
            if (metric.Key.Contains(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter)
                || metric.Key.Contains(TelemetryDataConstants.TimeTakenInSecByAllAdapters)
                || metric.Key.Contains(TelemetryDataConstants.TotalTestsDiscovered)
                || metric.Key.Contains(TelemetryDataConstants.TotalTestsByAdapter)
                || metric.Key.Contains(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec))
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
    /// Aggregates the value indicating if we already sent message to IDE.
    /// </summary>
    /// <param name="isMessageSent">Boolean value if we already sent message to IDE</param>
    public void AggregateIsMessageSent(bool isMessageSent)
    {
        IsMessageSent = IsMessageSent || isMessageSent;
    }

    public List<string> GetSourcesWithStatus(DiscoveryStatus discoveryStatus)
        => _sourcesWithDiscoveryStatus.IsEmpty
            ? new List<string>()
            : _sourcesWithDiscoveryStatus
                .Where(source => source.Value == discoveryStatus)
                .Select(source => source.Key)
                .ToList();

    public void MarkSourcesWithStatus(IEnumerable<string?>? sources, DiscoveryStatus status)
    {
        if (sources is null)
        {
            return;
        }

        foreach (var source in sources)
        {
            if (source is null)
            {
                continue;
            }

            _sourcesWithDiscoveryStatus.AddOrUpdate(source,
                _ =>
                {
                    if (status != DiscoveryStatus.NotDiscovered)
                    {
                        EqtTrace.Warning($"ParallelDiscoveryDataAggregator.MarkSourcesWithStatus: Undiscovered {source}.");
                    }

                    return status;
                },
                (_, previousStatus) =>
                {
                    if (previousStatus == DiscoveryStatus.FullyDiscovered && status != DiscoveryStatus.FullyDiscovered
                        || previousStatus == DiscoveryStatus.PartiallyDiscovered && status == DiscoveryStatus.NotDiscovered)
                    {
                        EqtTrace.Warning($"ParallelDiscoveryDataAggregator.MarkSourcesWithStatus: Downgrading source status from {previousStatus} to {status}.");
                    }

                    EqtTrace.Info($"ParallelDiscoveryDataAggregator.MarkSourcesWithStatus: Marking {source} with {status} status.");
                    return status;
                });
        }
    }

    public void MarkSourcesBasedOnDiscoveredTestCases(IEnumerable<TestCase>? testCases, bool isComplete, ref string? previousSource)
    {
        // When discovery is complete (i.e.not aborted), then we can assume that all partially
        // discovered sources are now fully discovered. We should also mark all not discovered
        // sources as fully discovered because that means that the source is considered as tests
        // but contains no test so we never received the partially or fully discovered event.
        if (isComplete)
        {
            MarkSourcesWithStatus(testCases?.Select(x => x.Source), DiscoveryStatus.FullyDiscovered);
            MarkSourcesWithStatus(new[] { previousSource }, DiscoveryStatus.FullyDiscovered);
            // Reset last source (not mandatory but done for the sake of completness).
            previousSource = null;
            return;
        }

        // When all testcases count in source is dividable by chunk size (e.g. 100 tests and
        // chunk size of 10) then lastChunk is coming as empty. Otherwise, we receive the
        // remaining test cases to process.
        if (testCases is null)
        {
            return;
        }

        foreach (var testCase in testCases)
        {
            var currentSource = testCase.Source;

            // We rely on the fact that sources are processed in a sequential way, which
            // means that when we receive a different source than the previous, we can
            // assume that the previous source was fully discovered.
            if (previousSource is null || previousSource == currentSource)
            {
                MarkSourcesWithStatus(new[] { currentSource }, DiscoveryStatus.PartiallyDiscovered);
            }
            else if (currentSource != previousSource)
            {
                MarkSourcesWithStatus(new[] { previousSource }, DiscoveryStatus.FullyDiscovered);
                MarkSourcesWithStatus(new[] { currentSource }, DiscoveryStatus.PartiallyDiscovered);
            }

            previousSource = currentSource;
        }
    }
}
