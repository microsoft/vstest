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
            this.testableTestRunCache = new TestableTestRunCache();
            this.testRecorder = new TestExecutionRecorder(null,this.testableTestRunCache);

            this.testCase = new TestCase("A.C.M", new Uri("executor://dummy"), "A");
            this.testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(this.testCase);

            this.mockTestCaseEventsHandler = new Mock<ITestCaseEventsHandler>();
            testRecorderWithTestEventsHandler = new TestExecutionRecorder(this.mockTestCaseEventsHandler.Object, this.testableTestRunCache);

        }

        [TestMethod]
        public void AttachmentsShouldReturnEmptyListByDefault()
        {
            var attachments = this.testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            Assert.AreEqual(0, attachments.Count);
        }

        [TestMethod]
        public void RecordStartShouldUpdateTestRunCache()
        {
            this.testRecorder.RecordStart(this.testCase);
            Assert.IsTrue(this.testableTestRunCache.TestStartedList.Contains(this.testCase));
        }
       
        [TestMethod]
        public void RecordResultShouldUpdateTestRunCache()
        {
            this.testRecorder.RecordResult(this.testResult);
            Assert.IsTrue(this.testableTestRunCache.TestResultList.Contains(this.testResult));
        }        

        [TestMethod]
        public void RecordEndShouldUpdateTestRunCache()
        {
            this.testRecorder.RecordEnd(this.testCase, TestOutcome.Passed);
            Assert.IsTrue(this.testableTestRunCache.TestCompletedList.Contains(this.testCase));
        }

        [TestMethod]
        public void RecordAttachmentsShouldAddToAttachmentSet()
        {
            var attachmentSet = new List<AttachmentSet> { new AttachmentSet(new Uri("attachment://dummy"), "attachment") };

            this.testRecorder.RecordAttachments(attachmentSet);

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

            this.testRecorder.RecordAttachments(attachmentSet);

            var attachments = this.testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            CollectionAssert.AreEqual(attachmentSet, attachments);

            var newAttachmentSet = new AttachmentSet(new Uri("attachment://median"), "mid");
            attachmentSet.Add(newAttachmentSet);

            this.testRecorder.RecordAttachments(new List<AttachmentSet> { newAttachmentSet });

            attachments = this.testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            CollectionAssert.AreEqual(attachmentSet, attachments);
        }

        #region TestCaseResult caching tests.
        [TestMethod]
        public void RecordStartShouldInvokeSendTestCaseStart()
        {
            this.testRecorderWithTestEventsHandler.RecordStart(this.testCase);

            this.mockTestCaseEventsHandler.Verify(x => x.SendTestCaseStart(this.testCase), Times.Once);
        }

        [TestMethod]
        public void SendTestCaseEndShouldInovkeTestCaseEndEventIfTestCaseStartWasCalledBefore()
        {
            this.testRecorderWithTestEventsHandler.RecordEnd(this.testCase,TestOutcome.Passed);

            this.mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(this.testCase, TestOutcome.Passed), Times.Once);
        }

        [TestMethod]
        public void SendTestCaseEndShouldNotInvokeTestCaseEndEventInCaseOfAMissingTestCaseStartInDataDrivenScenario()
        {
            this.testRecorderWithTestEventsHandler.RecordStart(this.testCase);
            this.testRecorderWithTestEventsHandler.RecordEnd(this.testCase,TestOutcome.Passed);
            this.testRecorderWithTestEventsHandler.RecordEnd(this.testCase,TestOutcome.Failed);

            this.mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(this.testCase, TestOutcome.Passed), Times.Once);
        }

        [TestMethod]
        public void RecordEndShouldInvokeSendTestCaseEndMultipleTimesInDataDrivenScenario()
        {
            this.testRecorderWithTestEventsHandler.RecordStart(this.testCase);
            this.testRecorderWithTestEventsHandler.RecordEnd(this.testCase,TestOutcome.Passed);
            this.testRecorderWithTestEventsHandler.RecordStart(this.testCase);
            this.testRecorderWithTestEventsHandler.RecordEnd(this.testCase,TestOutcome.Failed);

            this.mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(this.testCase, TestOutcome.Passed), Times.Once);
        }

        [TestMethod]
        public void RecordResultShouldPublishTestCaseResultEventIfTestCaseStartAndTestCaseEndEventsAreNotPublished()
        {
            this.testRecorderWithTestEventsHandler.RecordResult(this.testResult);

            this.mockTestCaseEventsHandler.Verify(x => x.SendTestResult(this.testResult), Times.Never);
        }

        [TestMethod]
        public void RecordResultShouldPublishTestCaseResultEventIfTestCaseStartAndTestCaseEndEventsArePublished()
        {
            this.testRecorderWithTestEventsHandler.RecordStart(this.testCase);
            this.testRecorderWithTestEventsHandler.RecordEnd(this.testCase, TestOutcome.Passed);
            this.testRecorderWithTestEventsHandler.RecordResult(this.testResult);

            this.mockTestCaseEventsHandler.Verify(x => x.SendTestResult(testResult), Times.Once);
        }

        [TestMethod]
        public void RecordResultShouldFlushIfTestCaseEndWasCalledBefore()
        {
            this.testRecorderWithTestEventsHandler.RecordStart(this.testCase);
            this.testRecorderWithTestEventsHandler.RecordEnd(this.testCase, TestOutcome.Passed);
            this.testRecorderWithTestEventsHandler.RecordResult(this.testResult);

            this.mockTestCaseEventsHandler.Verify(x => x.SendTestCaseEnd(this.testCase,TestOutcome.Passed), Times.Once);
            Assert.IsTrue(this.testableTestRunCache.TestResultList.Contains(this.testResult));
        }

        [TestMethod]
        public void RecordResultShouldNotFlushIfTestCaseEndWasNotCalledBefore()
        {
            this.testRecorderWithTestEventsHandler.RecordResult(this.testResult);

            Assert.IsFalse(this.testableTestRunCache.TestResultList.Contains(this.testResult));
        }
        #endregion
    }
}