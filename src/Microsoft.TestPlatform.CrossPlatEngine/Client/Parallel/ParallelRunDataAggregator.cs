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

        private List<string> executorUris;

        private List<ITestRunStatistics> testRunStatsList;

        private ConcurrentDictionary<string, object> metricsAggregator;

        private object dataUpdateSyncObject = new object();

        #endregion

        public ParallelRunDataAggregator()
        {
            ElapsedTime = TimeSpan.Zero;
            RunContextAttachments = new Collection<AttachmentSet>();
            RunCompleteArgsAttachments = new List<AttachmentSet>();
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

        public List<Exception> Exceptions { get; }

        public HashSet<string> ExecutorUris => new HashSet<string>(executorUris);

        public bool IsAborted { get; private set; }

        public bool IsCanceled { get; private set; }

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
            if (this.metricsAggregator == null || this.metricsAggregator.Count == 0)
            {
                return new ConcurrentDictionary<string, object>();
            }

            var adapterUsedCount = this.metricsAggregator.Count(metrics =>
                metrics.Key.Contains(TelemetryDataConstants.TotalTestsRanByAdapter));

            var adaptersDiscoveredCount = this.metricsAggregator.Count(metrics =>
                metrics.Key.Contains(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter));

            // Aggregating Total Adapter Used Count
            this.metricsAggregator.TryAdd(TelemetryDataConstants.NumberOfAdapterUsedToRunTests, adapterUsedCount);

            // Aggregating Total Adapters Discovered Count
            this.metricsAggregator.TryAdd(
                TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution,
                adaptersDiscoveredCount);

            return this.metricsAggregator;
        }

        public Exception GetAggregatedException()
        {
            if (Exceptions == null || Exceptions.Count < 1) return null;

            return new AggregateException(Exceptions);
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
             Collection<AttachmentSet> runCompleteArgsAttachments)
        {
            lock (dataUpdateSyncObject)
            {
                this.IsAborted = this.IsAborted || isAborted;
                this.IsCanceled = this.IsCanceled || isCanceled;

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
            }
        }

        /// <summary>
        /// Aggregates Run Data Metrics from each Test Host Process
        /// </summary>
        /// <param name="metrics"></param>
        public void AggregateRunDataMetrics(IDictionary<string, object> metrics)
        {
            if (metrics == null || metrics.Count == 0 || this.metricsAggregator == null)
            {
                return;
            }

            foreach (var metric in metrics)
            {
                if (metric.Key.Contains(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter) || metric.Key.Contains(TelemetryDataConstants.TimeTakenByAllAdaptersInSec) || (metric.Key.Contains(TelemetryDataConstants.TotalTestsRun) || metric.Key.Contains(TelemetryDataConstants.TotalTestsRanByAdapter)))
                {
                    var newValue = Convert.ToDouble(metric.Value);
                    object oldValue;

                    if (this.metricsAggregator.TryGetValue(metric.Key, out oldValue))
                    {
                        var oldDoubleValue = Convert.ToDouble(oldValue);
                        this.metricsAggregator[metric.Key] = newValue + oldDoubleValue;
                    }
                    else
                    {
                        this.metricsAggregator.TryAdd(metric.Key, newValue);
                    }
                }
            }
        }

        #endregion
    }
}
