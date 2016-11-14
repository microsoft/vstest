// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using Client.Execution;

    using CrossPlatEngine.Execution;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using ObjectModel;
    using ObjectModel.Client;
    using ObjectModel.Logging;

    [TestClass]
    public class TestRunResultAggregatorTests
    {
        TestRunResultAggregator resultAggregator = TestRunResultAggregator.Instance;
        Mock<ITestRunRequest> mockTestRunRequest;

        [TestInitialize]
        public void TestInit()
        {
            resultAggregator.Reset();
            mockTestRunRequest = new Mock<ITestRunRequest>();
            resultAggregator.RegisterTestRunEvents(mockTestRunRequest.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            resultAggregator.UnregisterTestRunEvents(mockTestRunRequest.Object);
        }

        [TestMethod]
        public void DefaultOutcomeIsPassed()
        {
            Assert.AreEqual(TestOutcome.Passed, resultAggregator.Outcome);
        }

        [TestMethod]
        public void MarkTestRunFailedSetsOutcomeToFailed()
        {
            resultAggregator.MarkTestRunFailed();
            Assert.AreEqual(TestOutcome.Failed, resultAggregator.Outcome);
        }

        [TestMethod]
        public void TestRunMessageHandlerForMessageLevelErrorSetsOutcomeToFailed()
        {
            var messageArgs = new TestRunMessageEventArgs(TestMessageLevel.Error, "bad stuff");
            mockTestRunRequest.Raise(tr => tr.TestRunMessage += null, messageArgs);
            Assert.AreEqual(TestOutcome.Failed, resultAggregator.Outcome);
        }

        [TestMethod]
        public void TestRunCompletionHandlerForTestRunStatisticsNullSetsOutcomeToFailed()
        {
            var messageArgs = new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan());
            mockTestRunRequest.Raise(tr => tr.OnRunCompletion += null, messageArgs);
            Assert.AreEqual(TestOutcome.Failed, resultAggregator.Outcome);
        }

        [TestMethod]
        public void TestRunCompletionHandlerForTestRunStatsWithOneOrMoreFailingTestsSetsOutcomeToFailed()
        {
            var testOutcomeDict = new System.Collections.Generic.Dictionary<TestOutcome, long>();
            testOutcomeDict.Add(TestOutcome.Failed, 1);
            var stats = new TestableTestRunStats(testOutcomeDict);
            
            var messageArgs = new TestRunCompleteEventArgs(stats, false, false, null, null, new TimeSpan());
            this.mockTestRunRequest.Raise(tr => tr.OnRunCompletion += null, messageArgs);
            Assert.AreEqual(TestOutcome.Failed, resultAggregator.Outcome);
        }

        #region implementation

        private class TestableTestRunStats : ITestRunStatistics
        {
            public TestableTestRunStats(Dictionary<TestOutcome, long> stats)
            {
                this.Stats = stats;
            }

            public long ExecutedTests { get; set; }

            /// <summary>
            /// Gets the test stats which is the test outcome versus its state.
            /// </summary>
            [DataMember]
            public IDictionary<TestOutcome, long> Stats { get; private set; }

            /// <summary>
            /// Gets the number of tests with a specified outcome.
            /// </summary>
            /// <param name="testOutcome"> The test outcome. </param>
            /// <returns> The number of tests with this outcome. </returns>
            public long this[TestOutcome testOutcome]
            {
                get
                {
                    long count;
                    if (this.Stats.TryGetValue(testOutcome, out count))
                    {
                        return count;
                    }

                    return 0;
                }
            }
        }

        #endregion
    }
}
