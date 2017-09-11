// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client.Parallel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ParallelRunDataAggregatorTests
    {
        [TestMethod]
        public void ParallelRunDataAggregatorConstructorShouldInitializeAggregatorVars()
        {
            var aggregator = new ParallelRunDataAggregator();

            Assert.AreEqual(aggregator.ElapsedTime, TimeSpan.Zero, "Timespan must be initialized to zero.");

            Assert.IsNotNull(aggregator.Exceptions, "Exceptions list must not be null");
            Assert.IsNotNull(aggregator.ExecutorUris, "ExecutorUris list must not be null");
            Assert.IsNotNull(aggregator.RunCompleteArgsAttachments, "RunCompleteArgsAttachments list must not be null");
            Assert.IsNotNull(aggregator.RunContextAttachments, "RunContextAttachments list must not be null");

            Assert.AreEqual(aggregator.Exceptions.Count, 0, "Exceptions List must be initialized as empty list.");
            Assert.AreEqual(aggregator.ExecutorUris.Count, 0, "Exceptions List must be initialized as empty list.");
            Assert.AreEqual(aggregator.RunCompleteArgsAttachments.Count, 0, "RunCompleteArgsAttachments List must be initialized as empty list.");
            Assert.AreEqual(aggregator.RunContextAttachments.Count, 0, "RunContextAttachments List must be initialized as empty list");

            Assert.IsFalse(aggregator.IsAborted, "Aborted must be false by default");

            Assert.IsFalse(aggregator.IsCanceled, "Canceled must be false by default");
        }

        [TestMethod]
        public void AggregateShouldAggregateRunCompleteAttachmentsCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            var attachmentSet1 = new Collection<AttachmentSet>();
            attachmentSet1.Add(new AttachmentSet(new Uri("x://hello1"), "hello1"));

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, null, attachmentSet1);


            Assert.AreEqual(aggregator.RunCompleteArgsAttachments.Count, 1, "RunCompleteArgsAttachments List must have data.");

            var attachmentSet2 = new Collection<AttachmentSet>();
            attachmentSet2.Add(new AttachmentSet(new Uri("x://hello2"), "hello2"));

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, null, attachmentSet2);

            Assert.AreEqual(aggregator.RunCompleteArgsAttachments.Count, 2, "RunCompleteArgsAttachments List must have aggregated data.");
        }

        [TestMethod]
        public void AggregateShouldAggregateRunContextAttachmentsCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            var attachmentSet1 = new Collection<AttachmentSet>();
            attachmentSet1.Add(new AttachmentSet(new Uri("x://hello1"), "hello1"));

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, attachmentSet1, null);

            Assert.AreEqual(aggregator.RunContextAttachments.Count, 1, "RunContextAttachments List must have data.");

            var attachmentSet2 = new Collection<AttachmentSet>();
            attachmentSet2.Add(new AttachmentSet(new Uri("x://hello2"), "hello2"));

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, attachmentSet2, null);

            Assert.AreEqual(aggregator.RunContextAttachments.Count, 2, "RunContextAttachments List must have aggregated data.");
        }


        [TestMethod]
        public void AggregateShouldAggregateAbortedAndCanceledCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.IsFalse(aggregator.IsAborted, "Aborted must be false");

            Assert.IsFalse(aggregator.IsCanceled, "Canceled must be false");

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, isAborted: true, isCanceled: false, runContextAttachments: null,
                 runCompleteArgsAttachments: null);

            Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");

            Assert.IsFalse(aggregator.IsCanceled, "Canceled must still be false");

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, isAborted: false, isCanceled: true, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.IsTrue(aggregator.IsAborted, "Aborted must continue be true");

            Assert.IsTrue(aggregator.IsCanceled, "Canceled must be true");

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.IsTrue(aggregator.IsAborted, "Aborted must continue be true");

            Assert.IsTrue(aggregator.IsCanceled, "Canceled must continue be true");

        }

        [TestMethod]
        public void AggregateShouldAggregateTimeSpanCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.AreEqual(aggregator.ElapsedTime, TimeSpan.Zero, "Timespan must be zero");

            aggregator.Aggregate(null, null, null, TimeSpan.FromMilliseconds(100), isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.AreEqual(aggregator.ElapsedTime, TimeSpan.FromMilliseconds(100), "Timespan must be 100ms");


            aggregator.Aggregate(null, null, null, TimeSpan.FromMilliseconds(200), isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.AreEqual(aggregator.ElapsedTime, TimeSpan.FromMilliseconds(200), "Timespan should be Max of all 200ms");

            aggregator.Aggregate(null, null, null, TimeSpan.FromMilliseconds(150), isAborted: false, isCanceled: false, runContextAttachments: null,
             runCompleteArgsAttachments: null);

            Assert.AreEqual(aggregator.ElapsedTime, TimeSpan.FromMilliseconds(200), "Timespan should be Max of all i.e. 200ms");
        }

        [TestMethod]
        public void AggregateShouldAggregateExceptionsCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.Aggregate(null, null, exception: null, elapsedTime: TimeSpan.Zero, isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.IsNull(aggregator.GetAggregatedException(), "Aggregated exception must be null");

            var exception1 = new NotImplementedException();
            aggregator.Aggregate(null, null, exception: exception1, elapsedTime: TimeSpan.Zero, isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            var aggregatedException = aggregator.GetAggregatedException() as AggregateException;
            Assert.IsNotNull(aggregatedException, "Aggregated exception must NOT be null");
            Assert.IsNotNull(aggregatedException.InnerExceptions, "Inner exception list must NOT be null");
            Assert.AreEqual(aggregatedException.InnerExceptions.Count, 1, "Inner exception lsit must have one element");
            Assert.AreEqual(aggregatedException.InnerExceptions[0], exception1, "Inner exception must be the one set.");

            var exception2 = new NotSupportedException();
            aggregator.Aggregate(null, null, exception: exception2, elapsedTime: TimeSpan.Zero, isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            aggregatedException = aggregator.GetAggregatedException() as AggregateException;
            Assert.IsNotNull(aggregatedException, "Aggregated exception must NOT be null");
            Assert.IsNotNull(aggregatedException.InnerExceptions, "Inner exception list must NOT be null");
            Assert.AreEqual(aggregatedException.InnerExceptions.Count, 2, "Inner exception lsit must have one element");
            Assert.AreEqual(aggregatedException.InnerExceptions[1], exception2, "Inner exception must be the one set.");
        }

        [TestMethod]
        public void AggregateShouldAggregateExecutorUrisCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, null, null);

            Assert.AreEqual(aggregator.ExecutorUris.Count, 0, "ExecutorUris List must not have data.");

            var uri1 = "x://hello1";
            aggregator.Aggregate(null, new List<string>() { uri1 }, null, TimeSpan.Zero, false, false, null, null);

            Assert.AreEqual(aggregator.ExecutorUris.Count, 1, "ExecutorUris List must have data.");
            Assert.IsTrue(aggregator.ExecutorUris.Contains(uri1), "ExecutorUris List must have correct data.");

            var uri2 = "x://hello2";
            aggregator.Aggregate(null, new List<string>() { uri2 }, null, TimeSpan.Zero, false, false, null, null);

            Assert.AreEqual(aggregator.ExecutorUris.Count, 2, "ExecutorUris List must have aggregated data.");
            Assert.IsTrue(aggregator.ExecutorUris.Contains(uri2), "ExecutorUris List must have correct data.");
        }

        [TestMethod]
        public void AggregateShouldAggregateRunStatsCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, null, null);

            var runStats = aggregator.GetAggregatedRunStats();
            Assert.AreEqual(runStats.ExecutedTests, 0, "RunStats must not have data.");

            var stats1 = new Dictionary<TestOutcome, long>();
            stats1.Add(TestOutcome.Passed, 2);
            stats1.Add(TestOutcome.Failed, 3);
            stats1.Add(TestOutcome.Skipped, 1);
            stats1.Add(TestOutcome.NotFound, 4);
            stats1.Add(TestOutcome.None, 2);

            aggregator.Aggregate(new TestRunStatistics(12, stats1), null, null, TimeSpan.Zero, false, false, null, null);

            runStats = aggregator.GetAggregatedRunStats();
            Assert.AreEqual(runStats.ExecutedTests, 12, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.Passed], 2, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.Failed], 3, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.Skipped], 1, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.NotFound], 4, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.None], 2, "RunStats must have aggregated data.");


            var stats2 = new Dictionary<TestOutcome, long>();
            stats2.Add(TestOutcome.Passed, 3);
            stats2.Add(TestOutcome.Failed, 2);
            stats2.Add(TestOutcome.Skipped, 2);
            stats2.Add(TestOutcome.NotFound, 1);
            stats2.Add(TestOutcome.None, 3);

            aggregator.Aggregate(new TestRunStatistics(11, stats2), null, null, TimeSpan.Zero, false, false, null, null);

            runStats = aggregator.GetAggregatedRunStats();
            Assert.AreEqual(runStats.ExecutedTests, 23, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.Passed], 5, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.Failed], 5, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.Skipped], 3, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.NotFound], 5, "RunStats must have aggregated data.");
            Assert.AreEqual(runStats.Stats[TestOutcome.None], 5, "RunStats must have aggregated data.");
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldAggregateMetricsCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.AggregateRunDataMetrics(null);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();
            Assert.AreEqual(runMetrics.Count, 0);
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldAddTotalTestsRun()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TotalTestsRanByAdapter, "2");

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.TotalTestsRanByAdapter, out value), true);
            Assert.AreEqual(value, "4");
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldAddTimeTakenToRunTests()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter, ".02091");

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter, out value), true);
            Assert.AreEqual(value, (.04182).ToString());
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldAddTimeTakenByAllAdapters()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TimeTakenByAllAdaptersInSec, ".02091");

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenByAllAdaptersInSec, out value), true);
            Assert.AreEqual(value, (.04182).ToString());
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldNotAggregateRunState()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.RunState, "Completed");

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.RunState, out value), false);
        }

        [TestMethod]
        public void GetAggregatedRunDataMetricsShouldReturnEmptyIfMetricAggregatorIsEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();

            aggregator.AggregateRunDataMetrics(dict);
            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            Assert.AreEqual(runMetrics.Count, 0);
        }

        [TestMethod]
        public void GetAggregatedRunDataMetricsShouldReturnEmptyIfMetricsIsNull()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();

            aggregator.AggregateRunDataMetrics(null);
            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            Assert.AreEqual(runMetrics.Count, 0);
        }

        [TestMethod]
        public void GetRunDataMetricsShouldAddTotalAdaptersUsedIfMetricsIsNotEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TotalTestsRanByAdapter, "2");

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToRunTests, out value), true);
            Assert.AreEqual(value, "1");
        }

        [TestMethod]
        public void GetRunDataMetricsShouldAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter + "executor:MSTestV1", ".02091");
            dict.Add(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter + "executor:MSTestV2", ".02091");

            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution, out value), true);
            Assert.AreEqual(value, "2");
        }

        [TestMethod]
        public void GetRunDataMetricsShouldNotAddTotalAdaptersUsedIfMetricsIsEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();
            var dict = new Dictionary<string, string>();

            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToRunTests, out value), false);
        }


        [TestMethod]
        public void GetRunDataMetricsShouldNotAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();
            var dict = new Dictionary<string, string>();

            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution, out value), false);
        }
    }
}
