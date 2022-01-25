// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Adapter
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations;

    [TestClass]
    public class TestExecutionRecorderTests
    {
        private TestableTestRunCache testableTestRunCache;
        private Mock<ITestCaseEventsHandler> mockTestCaseEventsHandler;
        private TestExecutionRecorder testRecorder, testRecorderWithTestEventsHandler;
        private TestCase testCase;
        private Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult testResult;

        [TestInitialize]
        public void TestInit()
        {
            testableTestRunCache = new TestableTestRunCache();
            testRecorder = new TestExecutionRecorder(null, testableTestRunCache);

            testCase = new TestCase("A.C.M", new Uri("executor://dummy"), "A");
            testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);

            mockTestCaseEventsHandler = new Mock<ITestCaseEventsHandler>();
            testRecorderWithTestEventsHandler = new TestExecutionRecorder(mockTestCaseEventsHandler.Object, testableTestRunCache);

        }

        [TestMethod]
        public void AttachmentsShouldReturnEmptyListByDefault()
        {
            var attachments = testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            Assert.AreEqual(0, attachments.Count);
        }

        [TestMethod]
        public void RecordStartShouldUpdateTestRunCache()
        {
            testRecorder.RecordStart(testCase);
            Assert.IsTrue(testableTestRunCache.TestStartedList.Contains(testCase));
        }

        [TestMethod]
        public void RecordResultShouldUpdateTestRunCache()
        {
            testRecorder.RecordResult(testResult);
            Assert.IsTrue(testableTestRunCache.TestResultList.Contains(testResult));
        }

        [TestMethod]
        public void RecordEndShouldUpdateTestRunCache()
        {
            testRecorder.RecordEnd(testCase, TestOutcome.Passed);
            Assert.IsTrue(testableTestRunCache.TestCompletedList.Contains(testCase));
        }

        [TestMethod]
        public void RecordAttachmentsShouldAddToAttachmentSet()
        {
            var attachmentSet = new List<AttachmentSet> { new AttachmentSet(new Uri("attachment://dummy"), "attachment") };

            testRecorder.RecordAttachments(attachmentSet);

            var attachments = testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            CollectionAssert.AreEqual(attachmentSet, attachments);
        }

        [TestMethod]
        public void RecordAttachmentsShouldAddToAttachmentSetForMultipleAttachments()
        {
            var attachmentSet = new List<AttachmentSet>
                                    {
                                        new AttachmentSet(new Uri("attachment://dummy"), "attachment"),
                                        new AttachmentSet(new Uri("attachment://infinite"), "infinity")
                                    };

            testRecorder.RecordAttachments(attachmentSet);

            var attachments = testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            CollectionAssert.AreEqual(attachmentSet, attachments);

            var newAttachmentSet = new AttachmentSet(new Uri("attachment://median"), "mid");
            attachmentSet.Add(newAttachmentSet);

            testRecorder.RecordAttachments(new List<AttachmentSet> { newAttachmentSet });

            attachments = testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            CollectionAssert.AreEqual(attachmentSet, attachments);
        }

        #region TestCaseResult caching tests.
        [TestMethod]
        public void RecordStartShouldInvokeSendTestCaseStart()
        {
            testRecorderWithTestEventsHandler.RecordStart(testCase);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseStart(testCase), Times.Once);
        }

        [TestMethod]
        public void RecordEndShouldInovkeTestCaseEndEventOnlyIfTestCaseStartWasCalledBefore()
        {
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Once);
        }

        [TestMethod]
        public void RecordEndShouldNotInovkeTestCaseEndEventIfTestCaseStartWasNotCalledBefore()
        {
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Never);
        }

        [TestMethod]
        public void RecordEndShouldNotInvokeTestCaseEndEventInCaseOfAMissingTestCaseStartInDataDrivenScenario()
        {
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Failed);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Once);
            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Failed), Times.Never);
        }

        [TestMethod]
        public void RecordEndShouldInvokeSendTestCaseEndMultipleTimesInDataDrivenScenario()
        {
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Exactly(2));
        }

        [TestMethod]
        public void RecordStartAndRecordEndShouldIgnoreRedundantTestCaseStartAndTestCaseEnd()
        {
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseStart(testCase), Times.Exactly(1));
            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Exactly(1));
        }

        [TestMethod]
        public void RecordResultShouldPublishTestResultIfRecordStartAndRecordEndEventsAreNotPublished()
        {
            testRecorderWithTestEventsHandler.RecordResult(testResult);

            mockTestCaseEventsHandler.Verify(x => x.SendTestResult(testResult), Times.Once);
        }

        [TestMethod]
        public void RecordResultShouldPublishTestCaseResultEventIfTestCaseStartAndTestCaseEndEventsArePublished()
        {
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);
            testRecorderWithTestEventsHandler.RecordResult(testResult);

            mockTestCaseEventsHandler.Verify(x => x.SendTestResult(testResult), Times.Once);
        }

        [TestMethod]
        public void RecordResultShouldFlushIfRecordEndWasCalledBefore()
        {
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, TestOutcome.Passed);
            testRecorderWithTestEventsHandler.RecordResult(testResult);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Once);
            Assert.IsTrue(testableTestRunCache.TestResultList.Contains(testResult));
        }

        [TestMethod]
        public void RecordResultShouldSendTestCaseEndEventAndFlushIfRecordEndWasCalledAfterRecordResult()
        {
            testResult.Outcome = TestOutcome.Passed;
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordResult(testResult);
            testRecorderWithTestEventsHandler.RecordEnd(testCase, testResult.Outcome);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Once);
            Assert.IsTrue(testableTestRunCache.TestResultList.Contains(testResult));
        }

        [TestMethod]
        public void RecordResultShouldSendTestCaseEndEventIfRecordEndWasNotCalled()
        {
            testResult.Outcome = TestOutcome.Passed;
            testRecorderWithTestEventsHandler.RecordStart(testCase);
            testRecorderWithTestEventsHandler.RecordResult(testResult);

            mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Once);
            Assert.IsTrue(testableTestRunCache.TestResultList.Contains(testResult));
        }

        #endregion
    }
}