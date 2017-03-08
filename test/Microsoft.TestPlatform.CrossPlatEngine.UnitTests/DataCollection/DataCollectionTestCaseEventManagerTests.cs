// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionTestCaseEventManagerTests
    {
        private DataCollectionTestCaseEventManager dataCollectionTestCaseEventManager;
        private Mock<ITestRunCache> mockTestRunCache;
        private int testCaseStartCalled;
        private int testCaseEndCalled;
        private int testResultCalled;
        private TestCase testCase;

        public DataCollectionTestCaseEventManagerTests()
        {
            this.mockTestRunCache = new Mock<ITestRunCache>();
            this.dataCollectionTestCaseEventManager = new DataCollectionTestCaseEventManager(this.mockTestRunCache.Object);
            this.testCaseStartCalled = 0;
            this.testCaseEndCalled = 0;
            this.testResultCalled = 0;
            this.testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");
            // random guid
            this.testCase.Id = new Guid("3871B3B0-2853-406B-BB61-1FE1764116FD");
            this.dataCollectionTestCaseEventManager.TestCaseStart += this.TriggerTestCaseStart;
            this.dataCollectionTestCaseEventManager.TestCaseEnd += this.TriggerTestCaseEnd;
            this.dataCollectionTestCaseEventManager.TestResult += this.TriggerTestResult;
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.dataCollectionTestCaseEventManager.TestCaseStart -= this.TriggerTestCaseStart;
            this.dataCollectionTestCaseEventManager.TestCaseEnd -= this.TriggerTestCaseEnd;
            this.dataCollectionTestCaseEventManager.TestResult -= this.TriggerTestResult;
        }

        [TestMethod]
        public void RaiseTestCaseStartShouldPublishTestCaseStartEvent()
        {
            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(this.testCase));
            Assert.AreEqual(1, this.testCaseStartCalled);
        }

        [TestMethod]
        public void RaiseTestCaseEndShouldPublishTestCaseEndEventIfTestCaseStartWasCalledBefore()
        {
            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(this.testCase));
            this.dataCollectionTestCaseEventManager.RaiseTestCaseEnd(new TestCaseEndEventArgs(this.testCase, TestOutcome.Passed));

            Assert.AreEqual(1, this.testCaseEndCalled);
        }

        [TestMethod]
        public void RaiseTestCaseEndShouldNotRaiseTestCaseEndEventInCaseOfAMissingTestCaseStartInDataDrivenScenario()
        {
            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(this.testCase));
            this.dataCollectionTestCaseEventManager.RaiseTestCaseEnd(new TestCaseEndEventArgs(this.testCase, TestOutcome.Passed));
            this.dataCollectionTestCaseEventManager.RaiseTestCaseEnd(new TestCaseEndEventArgs(this.testCase, TestOutcome.Failed));

            Assert.AreEqual(1, this.testCaseEndCalled);
        }

        [TestMethod]
        public void RaiseTestCaseEndShouldtInvokeTestCaseEndMultipleTimesInDataDrivenScenario()
        {
            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(this.testCase));
            this.dataCollectionTestCaseEventManager.RaiseTestCaseEnd(new TestCaseEndEventArgs(this.testCase, TestOutcome.Passed));
            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(this.testCase));
            this.dataCollectionTestCaseEventManager.RaiseTestCaseEnd(new TestCaseEndEventArgs(this.testCase, TestOutcome.Failed));

            Assert.IsTrue(this.testCaseEndCalled == 2, "TestCaseStart must only be called once");
        }

        [TestMethod]
        public void RaiseTestCaseResultShouldPublishTestCaseResultEventIfTestCaseStartAndTestCaseEndEventsAreNotPublished()
        {
            this.dataCollectionTestCaseEventManager.RaiseTestResult(new TestResultEventArgs(new VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase)));
            Assert.AreEqual(0, this.testResultCalled);
        }

        [TestMethod]
        public void RaiseTestCaseResultShouldPublishTestCaseResultEventIfTestCaseStartAndTestCaseEndEventsArePublished()
        {
            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(this.testCase));
            this.dataCollectionTestCaseEventManager.RaiseTestCaseEnd(new TestCaseEndEventArgs(this.testCase, TestOutcome.Passed));
            this.dataCollectionTestCaseEventManager.RaiseTestResult(new TestResultEventArgs(new VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase)));
            Assert.AreEqual(1, this.testResultCalled);
        }

        [TestMethod]
        public void RaiseTestResultShouldFlushIfTestCaseEndWasCalledBefore()
        {
            var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase);

            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(this.testCase));
            this.dataCollectionTestCaseEventManager.RaiseTestCaseEnd(new TestCaseEndEventArgs(testCase, TestOutcome.Passed));
            this.dataCollectionTestCaseEventManager.RaiseTestResult(new TestResultEventArgs(testResult));

            var allowFlush = testResult.GetPropertyValue<bool>(DataCollectionTestCaseEventManager.FlushResultTestResultPoperty, false);

            Assert.AreEqual(1, this.testCaseEndCalled);
            Assert.IsTrue(allowFlush, "TestResult must be flushed");
        }

        [TestMethod]
        public void RaiseTestResultShouldNotFlushIfTestCaseEndWasNotCalledBefore()
        {
            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase);
            this.dataCollectionTestCaseEventManager.RaiseTestResult(new TestResultEventArgs(testResult));

            var allowFlush = testResult.GetPropertyValue<bool>(DataCollectionTestCaseEventManager.FlushResultTestResultPoperty, true);

            Assert.IsFalse(allowFlush, "TestResult must not be flushed");
        }

        [TestMethod]
        public void FlushLastChunkResultsShouldPutTestResultsinTestRunCache()
        {
            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(this.testCase));
            this.dataCollectionTestCaseEventManager.RaiseTestResult(new TestResultEventArgs(new VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase)));

            this.dataCollectionTestCaseEventManager.FlushLastChunkResults();
            this.mockTestRunCache.Verify(x => x.OnNewTestResult(It.IsAny<VisualStudio.TestPlatform.ObjectModel.TestResult>()), Times.Once);
        }

        private void TriggerTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            this.testCaseStartCalled++;
        }

        private void TriggerTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            this.testCaseEndCalled++;
        }

        private void TriggerTestResult(object sender, TestResultEventArgs e)
        {
            this.testResultCalled++;
        }
    }
}
