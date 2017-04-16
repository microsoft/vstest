// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// ParallelRunDataAggregator aggregates test run data from execution managers running in parallel
    /// </summary>
    internal class ParallelRunDataAggregator
    {
        #region PrivateFields

        private List<string> executorUris;

        private List<ITestRunStatistics> testRunStatsList;

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

            IsAborted = false;
            IsCanceled = false;
        }

        #region Public Properties

        public TimeSpan ElapsedTime { get; set; }

        public Collection<AttachmentSet> RunContextAttachments { get; }

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
             Collection<AttachmentSet> runCompleteArgsAttachments)
        {
            lock (dataUpdateSyncObject)
            {
                this.IsAborted = this.IsAborted || isAborted;
                this.IsCanceled = this.IsCanceled || isCanceled;

                ElapsedTime = TimeSpan.FromMilliseconds(Math.Max(ElapsedTime.TotalMilliseconds, elapsedTime.TotalMilliseconds));

                if (runCompleteArgsAttachments != null) RunCompleteArgsAttachments.AddRange(runCompleteArgsAttachments);
                if (exception != null) Exceptions.Add(exception);
                if (executorUris != null) this.executorUris.AddRange(executorUris);
                if (testRunStats != null) testRunStatsList.Add(testRunStats);
            }
        }

        #endregion
    }
}
