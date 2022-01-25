// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// ParallelRunDataAggregator aggregates test run data from execution managers running in parallel
    /// </summary>
    internal class ParallelRunDataAggregator
    {
        #region PrivateFields


        private readonly List<string> executorUris;

        private readonly List<ITestRunStatistics> testRunStatsList;

        private readonly ConcurrentDictionary<string, object> metricsAggregator;

        private readonly object dataUpdateSyncObject = new();

        #endregion

        public ParallelRunDataAggregator(string runSettingsXml)
        {
            RunSettings = runSettingsXml ?? throw new ArgumentNullException(nameof(runSettingsXml));
            ElapsedTime = TimeSpan.Zero;
            RunContextAttachments = new Collection<AttachmentSet>();
            RunCompleteArgsAttachments = new List<AttachmentSet>();
            InvokedDataCollectors = new Collection<InvokedDataCollector>();
            Exceptions = new List<Exception>();
            executorUris = new List<string>();
            testRunStatsList = new List<ITestRunStatistics>();

            metricsAggregator = new ConcurrentDictionary<string, object>();

            IsAborted = false;
            IsCanceled = false;
        }

        #region Public Properties

        public TimeSpan ElapsedTime { get; set; }

        public Collection<AttachmentSet> RunContextAttachments { get; set; }

        public List<AttachmentSet> RunCompleteArgsAttachments { get; }

        public Collection<InvokedDataCollector> InvokedDataCollectors { get; set; }

        public List<Exception> Exceptions { get; }

        public HashSet<string> ExecutorUris => new(executorUris);

        public bool IsAborted { get; private set; }

        public bool IsCanceled { get; private set; }

        public string RunSettings { get; private set; }

        #endregion

        #region Public Methods

        public ITestRunStatistics GetAggregatedRunStats()
        {
            var testOutcomeMap = new Dictionary<TestOutcome, long>();
            long totalTests = 0;
            if (testRunStatsList.Count > 0)
            {
                foreach (var runStats in testRunStatsList)
                {
                    foreach (var outcome in runStats.Stats.Keys)
                    {
                        if (!testOutcomeMap.ContainsKey(outcome))
                        {
                            testOutcomeMap.Add(outcome, 0);
                        }
                        testOutcomeMap[outcome] += runStats.Stats[outcome];
                    }
                    totalTests += runStats.ExecutedTests;
                }
            }

            var overallRunStats = new TestRunStatistics(testOutcomeMap);
            overallRunStats.ExecutedTests = totalTests;
            return overallRunStats;
        }

        /// <summary>
        /// Returns the Aggregated Run Data Metrics
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, object> GetAggregatedRunDataMetrics()
        {
            if (metricsAggregator == null || metricsAggregator.Count == 0)
            {
                return new ConcurrentDictionary<string, object>();
            }

            var adapterUsedCount = metricsAggregator.Count(metrics =>
                metrics.Key.Contains(TelemetryDataConstants.TotalTestsRanByAdapter));

            var adaptersDiscoveredCount = metricsAggregator.Count(metrics =>
                metrics.Key.Contains(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter));

            // Aggregating Total Adapter Used Count
            metricsAggregator.TryAdd(TelemetryDataConstants.NumberOfAdapterUsedToRunTests, adapterUsedCount);

            // Aggregating Total Adapters Discovered Count
            metricsAggregator.TryAdd(
                TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution,
                adaptersDiscoveredCount);

            return metricsAggregator;
        }

        public Exception GetAggregatedException()
        {
            return Exceptions == null || Exceptions.Count < 1 ? null : (Exception)new AggregateException(Exceptions);
        }

        /// <summary>
        /// Aggregate Run Data
        /// Must be thread-safe as this is expected to be called by parallel managers
        /// </summary>
        public void Aggregate(
             ITestRunStatistics testRunStats,
             ICollection<string> executorUris,
             Exception exception,
             TimeSpan elapsedTime,
             bool isAborted,
             bool isCanceled,
             ICollection<AttachmentSet> runContextAttachments,
             Collection<AttachmentSet> runCompleteArgsAttachments,
             Collection<InvokedDataCollector> invokedDataCollectors)
        {
            lock (dataUpdateSyncObject)
            {
                IsAborted = IsAborted || isAborted;
                IsCanceled = IsCanceled || isCanceled;

                ElapsedTime = TimeSpan.FromMilliseconds(Math.Max(ElapsedTime.TotalMilliseconds, elapsedTime.TotalMilliseconds));
                if (runContextAttachments != null)
                {
                    foreach (var attachmentSet in runContextAttachments)
                    {
                        RunContextAttachments.Add(attachmentSet);
                    }
                }

                if (runCompleteArgsAttachments != null) RunCompleteArgsAttachments.AddRange(runCompleteArgsAttachments);
                if (exception != null) Exceptions.Add(exception);
                if (executorUris != null) this.executorUris.AddRange(executorUris);
                if (testRunStats != null) testRunStatsList.Add(testRunStats);

                if (invokedDataCollectors?.Count > 0)
                {
                    foreach (var invokedDataCollector in invokedDataCollectors)
                    {
                        if (!InvokedDataCollectors.Contains(invokedDataCollector))
                        {
                            InvokedDataCollectors.Add(invokedDataCollector);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Aggregates Run Data Metrics from each Test Host Process
        /// </summary>
        /// <param name="metrics"></param>
        public void AggregateRunDataMetrics(IDictionary<string, object> metrics)
        {
            if (metrics == null || metrics.Count == 0 || metricsAggregator == null)
            {
                return;
            }

            foreach (var metric in metrics)
            {
                if (metric.Key.Contains(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter) || metric.Key.Contains(TelemetryDataConstants.TimeTakenByAllAdaptersInSec) || (metric.Key.Contains(TelemetryDataConstants.TotalTestsRun) || metric.Key.Contains(TelemetryDataConstants.TotalTestsRanByAdapter)))
                {
                    var newValue = Convert.ToDouble(metric.Value);

                    if (metricsAggregator.TryGetValue(metric.Key, out var oldValue))
                    {
                        var oldDoubleValue = Convert.ToDouble(oldValue);
                        metricsAggregator[metric.Key] = newValue + oldDoubleValue;
                    }
                    else
                    {
                        metricsAggregator.TryAdd(metric.Key, newValue);
                    }
                }
            }
        }

        #endregion
    }
}
