// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace testhost.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.TestHost.EventHandlers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestCaseEventsHandlerTests
    {
        private TestCaseEventsHandler testCaseEventsHandler;
        private Mock<ITestRunCache> mockTestRunCache;
        private int testCaseStartCalled;
        private int testCaseEndCalled;
        private int testResultCalled;
        private TestCase testCase;

        public TestCaseEventsHandlerTests()
        {
            this.testCaseEventsHandler = new TestCaseEventsHandler();
            this.mockTestRunCache = new Mock<ITestRunCache>();
            this.testCaseEventsHandler.Initialize(this.mockTestRunCache.Object);
            this.testCaseStartCalled = 0;
            this.testCaseEndCalled = 0;
            this.testResultCalled = 0;
            this.testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");

            // random guid
            this.testCase.Id = new Guid("3871B3B0-2853-406B-BB61-1FE1764116FD");
            this.testCaseEventsHandler.TestCaseStart += this.TriggerTestCaseStart;
            this.testCaseEventsHandler.TestCaseEnd += this.TriggerTestCaseEnd;
            this.testCaseEventsHandler.TestResult += this.TriggerTestResult;
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.testCaseEventsHandler.TestCaseStart -= this.TriggerTestCaseStart;
            this.testCaseEventsHandler.TestCaseEnd -= this.TriggerTestCaseEnd;
            this.testCaseEventsHandler.TestResult -= this.TriggerTestResult;
        }

        [TestMethod]
        public void SendTestCaseStartShouldPublishTestCaseStartEvent()
        {
            this.testCaseEventsHandler.SendTestCaseStart(this.testCase);
            Assert.AreEqual(1, this.testCaseStartCalled);
        }

        [TestMethod]
        public void SendTestCaseEndShouldPublishTestCaseEndEventIfTestCaseStartWasCalledBefore()
        {
            this.testCaseEventsHandler.SendTestCaseStart(this.testCase);
            this.testCaseEventsHandler.SendTestCaseEnd(this.testCase, TestOutcome.Passed);

            Assert.AreEqual(1, this.testCaseEndCalled);
        }

        [TestMethod]
        public void SendTestCaseEndShouldNotSendTestCaseEndEventInCaseOfAMissingTestCaseStartInDataDrivenScenario()
        {
            this.testCaseEventsHandler.SendTestCaseStart(this.testCase);
            this.testCaseEventsHandler.SendTestCaseEnd(this.testCase, TestOutcome.Passed);
            this.testCaseEventsHandler.SendTestCaseEnd(this.testCase, TestOutcome.Failed);

            Assert.AreEqual(1, this.testCaseEndCalled);
        }

        [TestMethod]
        public void SendTestCaseEndShouldtInvokeTestCaseEndMultipleTimesInDataDrivenScenario()
        {
            this.testCaseEventsHandler.SendTestCaseStart(this.testCase);
            this.testCaseEventsHandler.SendTestCaseEnd(this.testCase, TestOutcome.Passed);
            this.testCaseEventsHandler.SendTestCaseStart(this.testCase);
            this.testCaseEventsHandler.SendTestCaseEnd(this.testCase, TestOutcome.Failed);

            Assert.IsTrue(this.testCaseEndCalled == 2, "TestCaseStart must only be called once");
        }

        [TestMethod]
        public void SendTestResultShouldPublishTestCaseResultEventIfTestCaseStartAndTestCaseEndEventsAreNotPublished()
        {
            this.testCaseEventsHandler.SendTestResult(new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase));
            Assert.AreEqual(0, this.testResultCalled);
        }

        [TestMethod]
        public void SendTestResultShouldPublishTestCaseResultEventIfTestCaseStartAndTestCaseEndEventsArePublished()
        {
            this.testCaseEventsHandler.SendTestCaseStart(this.testCase);
            this.testCaseEventsHandler.SendTestCaseEnd(this.testCase, TestOutcome.Passed);
            this.testCaseEventsHandler.SendTestResult(new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase));
            Assert.AreEqual(1, this.testResultCalled);
        }

        [TestMethod]
        public void RaiseTestResultShouldFlushIfTestCaseEndWasCalledBefore()
        {
            var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase);

            this.testCaseEventsHandler.SendTestCaseStart(this.testCase);
            this.testCaseEventsHandler.SendTestCaseEnd(this.testCase, TestOutcome.Passed);
            var allowFlush = this.testCaseEventsHandler.SendTestResult(testResult);

            Assert.AreEqual(1, this.testCaseEndCalled);
            Assert.IsTrue(allowFlush, "TestResult must be flushed");
        }

        [TestMethod]
        public void RaiseTestResultShouldNotFlushIfTestCaseEndWasNotCalledBefore()
        {
            var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase);
            var allowFlush = this.testCaseEventsHandler.SendTestResult(testResult);

            Assert.IsFalse(allowFlush, "TestResult must not be flushed");
        }

        [TestMethod]
        public void FlushLastChunkResultsShouldPutTestResultsinTestRunCache()
        {
            this.testCaseEventsHandler.SendTestCaseStart(this.testCase);
            this.testCaseEventsHandler.SendTestResult(new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase));

            this.testCaseEventsHandler.FlushLastChunkResults();
            this.mockTestRunCache.Verify(x => x.OnNewTestResult(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult>()), Times.Once);
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