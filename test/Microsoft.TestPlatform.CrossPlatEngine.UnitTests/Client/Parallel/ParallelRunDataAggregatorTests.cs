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

            Assert.AreEqual(0, aggregator.Exceptions.Count, "Exceptions List must be initialized as empty list.");
            Assert.AreEqual(0, aggregator.ExecutorUris.Count, "Exceptions List must be initialized as empty list.");
            Assert.AreEqual(0, aggregator.RunCompleteArgsAttachments.Count, "RunCompleteArgsAttachments List must be initialized as empty list.");
            Assert.AreEqual(0, aggregator.RunContextAttachments.Count, "RunContextAttachments List must be initialized as empty list");

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


            Assert.AreEqual(1, aggregator.RunCompleteArgsAttachments.Count, "RunCompleteArgsAttachments List must have data.");

            var attachmentSet2 = new Collection<AttachmentSet>();
            attachmentSet2.Add(new AttachmentSet(new Uri("x://hello2"), "hello2"));

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, null, attachmentSet2);

            Assert.AreEqual(2, aggregator.RunCompleteArgsAttachments.Count, "RunCompleteArgsAttachments List must have aggregated data.");
        }

        [TestMethod]
        public void AggregateShouldAggregateRunContextAttachmentsCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            var attachmentSet1 = new Collection<AttachmentSet>();
            attachmentSet1.Add(new AttachmentSet(new Uri("x://hello1"), "hello1"));

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, attachmentSet1, null);

            Assert.AreEqual(1, aggregator.RunContextAttachments.Count, "RunContextAttachments List must have data.");

            var attachmentSet2 = new Collection<AttachmentSet>();
            attachmentSet2.Add(new AttachmentSet(new Uri("x://hello2"), "hello2"));

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, attachmentSet2, null);

            Assert.AreEqual(2, aggregator.RunContextAttachments.Count, "RunContextAttachments List must have aggregated data.");
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

            Assert.AreEqual(TimeSpan.Zero, aggregator.ElapsedTime, "Timespan must be zero");

            aggregator.Aggregate(null, null, null, TimeSpan.FromMilliseconds(100), isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.AreEqual(TimeSpan.FromMilliseconds(100), aggregator.ElapsedTime, "Timespan must be 100ms");


            aggregator.Aggregate(null, null, null, TimeSpan.FromMilliseconds(200), isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            Assert.AreEqual(TimeSpan.FromMilliseconds(200), aggregator.ElapsedTime, "Timespan should be Max of all 200ms");

            aggregator.Aggregate(null, null, null, TimeSpan.FromMilliseconds(150), isAborted: false, isCanceled: false, runContextAttachments: null,
             runCompleteArgsAttachments: null);

            Assert.AreEqual(TimeSpan.FromMilliseconds(200), aggregator.ElapsedTime, "Timespan should be Max of all i.e. 200ms");
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
            Assert.AreEqual(1, aggregatedException.InnerExceptions.Count, "Inner exception list must have one element");
            Assert.AreEqual(exception1, aggregatedException.InnerExceptions[0], "Inner exception must be the one set.");

            var exception2 = new NotSupportedException();
            aggregator.Aggregate(null, null, exception: exception2, elapsedTime: TimeSpan.Zero, isAborted: false, isCanceled: false, runContextAttachments: null,
                runCompleteArgsAttachments: null);

            aggregatedException = aggregator.GetAggregatedException() as AggregateException;
            Assert.IsNotNull(aggregatedException, "Aggregated exception must NOT be null");
            Assert.IsNotNull(aggregatedException.InnerExceptions, "Inner exception list must NOT be null");
            Assert.AreEqual(2, aggregatedException.InnerExceptions.Count, "Inner exception list must have one element");
            Assert.AreEqual(exception2, aggregatedException.InnerExceptions[1], "Inner exception must be the one set.");
        }

        [TestMethod]
        public void AggregateShouldAggregateExecutorUrisCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, null, null);

            Assert.AreEqual(0, aggregator.ExecutorUris.Count, "ExecutorUris List must not have data.");

            var uri1 = "x://hello1";
            aggregator.Aggregate(null, new List<string>() { uri1 }, null, TimeSpan.Zero, false, false, null, null);

            Assert.AreEqual(1, aggregator.ExecutorUris.Count, "ExecutorUris List must have data.");
            Assert.IsTrue(aggregator.ExecutorUris.Contains(uri1), "ExecutorUris List must have correct data.");

            var uri2 = "x://hello2";
            aggregator.Aggregate(null, new List<string>() { uri2 }, null, TimeSpan.Zero, false, false, null, null);

            Assert.AreEqual(2, aggregator.ExecutorUris.Count, "ExecutorUris List must have aggregated data.");
            Assert.IsTrue(aggregator.ExecutorUris.Contains(uri2), "ExecutorUris List must have correct data.");
        }

        [TestMethod]
        public void AggregateShouldAggregateRunStatsCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.Aggregate(null, null, null, TimeSpan.Zero, false, false, null, null);

            var runStats = aggregator.GetAggregatedRunStats();
            Assert.AreEqual(0, runStats.ExecutedTests, "RunStats must not have data.");

            var stats1 = new Dictionary<TestOutcome, long>();
            stats1.Add(TestOutcome.Passed, 2);
            stats1.Add(TestOutcome.Failed, 3);
            stats1.Add(TestOutcome.Skipped, 1);
            stats1.Add(TestOutcome.NotFound, 4);
            stats1.Add(TestOutcome.None, 2);

            aggregator.Aggregate(new TestRunStatistics(12, stats1), null, null, TimeSpan.Zero, false, false, null, null);

            runStats = aggregator.GetAggregatedRunStats();
            Assert.AreEqual(12, runStats.ExecutedTests, "RunStats must have aggregated data.");
            Assert.AreEqual(2, runStats.Stats[TestOutcome.Passed], "RunStats must have aggregated data.");
            Assert.AreEqual(3, runStats.Stats[TestOutcome.Failed], "RunStats must have aggregated data.");
            Assert.AreEqual(1, runStats.Stats[TestOutcome.Skipped], "RunStats must have aggregated data.");
            Assert.AreEqual(4, runStats.Stats[TestOutcome.NotFound], "RunStats must have aggregated data.");
            Assert.AreEqual(2, runStats.Stats[TestOutcome.None], "RunStats must have aggregated data.");


            var stats2 = new Dictionary<TestOutcome, long>();
            stats2.Add(TestOutcome.Passed, 3);
            stats2.Add(TestOutcome.Failed, 2);
            stats2.Add(TestOutcome.Skipped, 2);
            stats2.Add(TestOutcome.NotFound, 1);
            stats2.Add(TestOutcome.None, 3);

            aggregator.Aggregate(new TestRunStatistics(11, stats2), null, null, TimeSpan.Zero, false, false, null, null);

            runStats = aggregator.GetAggregatedRunStats();
            Assert.AreEqual(23, runStats.ExecutedTests, "RunStats must have aggregated data.");
            Assert.AreEqual(5, runStats.Stats[TestOutcome.Passed], "RunStats must have aggregated data.");
            Assert.AreEqual(5, runStats.Stats[TestOutcome.Failed], "RunStats must have aggregated data.");
            Assert.AreEqual(3, runStats.Stats[TestOutcome.Skipped], "RunStats must have aggregated data.");
            Assert.AreEqual(5, runStats.Stats[TestOutcome.NotFound], "RunStats must have aggregated data.");
            Assert.AreEqual(5, runStats.Stats[TestOutcome.None], "RunStats must have aggregated data.");
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldAggregateMetricsCorrectly()
        {
            var aggregator = new ParallelRunDataAggregator();

            aggregator.AggregateRunDataMetrics(null);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();
            Assert.AreEqual(0, runMetrics.Count);
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldAddTotalTestsRun()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, object>();
            dict.Add(TelemetryDataConstants.TotalTestsRanByAdapter, 2);

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            object value;
            Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TotalTestsRanByAdapter, out value));
            Assert.AreEqual(4, Convert.ToInt32(value));
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldAddTimeTakenToRunTests()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, object>();
            dict.Add(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter, .02091);

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            object value;
            Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter, out value));
            Assert.AreEqual(.04182, value);
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldAddTimeTakenByAllAdapters()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, object>();
            dict.Add(TelemetryDataConstants.TimeTakenByAllAdaptersInSec, .02091);

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            object value;
            Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenByAllAdaptersInSec, out value));
            Assert.AreEqual(.04182, value);
        }

        [TestMethod]
        public void AggregateRunDataMetricsShouldNotAggregateRunState()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, object>();
            dict.Add(TelemetryDataConstants.RunState, "Completed");

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            object value;
            Assert.IsFalse(runMetrics.TryGetValue(TelemetryDataConstants.RunState, out value));
        }

        [TestMethod]
        public void GetAggregatedRunDataMetricsShouldReturnEmptyIfMetricAggregatorIsEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, object>();

            aggregator.AggregateRunDataMetrics(dict);
            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            Assert.AreEqual(0, runMetrics.Count);
        }

        [TestMethod]
        public void GetAggregatedRunDataMetricsShouldReturnEmptyIfMetricsIsNull()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, string>();

            aggregator.AggregateRunDataMetrics(null);
            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            Assert.AreEqual(0, runMetrics.Count);
        }

        [TestMethod]
        public void GetRunDataMetricsShouldAddTotalAdaptersUsedIfMetricsIsNotEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, object>();
            dict.Add(TelemetryDataConstants.TotalTestsRanByAdapter, 2);

            aggregator.AggregateRunDataMetrics(dict);
            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            object value;
            Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToRunTests, out value));
            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void GetRunDataMetricsShouldAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();

            var dict = new Dictionary<string, object>();
            dict.Add(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter + "executor:MSTestV1", .02091);
            dict.Add(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter + "executor:MSTestV2", .02091);

            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            object value;
            Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution, out value));
            Assert.AreEqual(2, value);
        }

        [TestMethod]
        public void GetRunDataMetricsShouldNotAddTotalAdaptersUsedIfMetricsIsEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();
            var dict = new Dictionary<string, object>();

            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            object value;
            Assert.IsFalse(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToRunTests, out value));
        }

        [TestMethod]
        public void GetRunDataMetricsShouldNotAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
        {
            var aggregator = new ParallelRunDataAggregator();
            var dict = new Dictionary<string, object>();

            aggregator.AggregateRunDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedRunDataMetrics();

            object value;
            Assert.IsFalse(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution, out value));
        }
    }
}
