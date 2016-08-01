// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Adapter
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations;

    [TestClass]
    public class TestExecutionRecorderTests
    {
        private TestableTestRunCache testableTestRunCache;
        
        [TestInitialize]
        public void TestInit()
        {
            this.testableTestRunCache = new TestableTestRunCache();
        }

        [TestMethod]
        public void AttachmentsShouldReturnEmptyListByDefault()
        {
            var testRecorder = new TestExecutionRecorder(null, this.testableTestRunCache);

            var attachments = testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            Assert.AreEqual(0, attachments.Count);
        }

        [TestMethod]
        public void RecordStartShouldUpdateTestRunCache()
        {
            var testCase = new TestCase("A.C.M", new Uri("executor://dummy"), "A");

            var testRecorder = new TestExecutionRecorder(null, this.testableTestRunCache);

            testRecorder.RecordStart(testCase);
            Assert.IsTrue(this.testableTestRunCache.TestStartedList.Contains(testCase));
        }

        [TestMethod]
        public void RecordStartShouldSendTestCaseEvents()
        {
            var testCase = new TestCase("A.C.M", new Uri("executor://dummy"), "A");
            var mockTestCaseEventsHandler = new Mock<ITestCaseEventsHandler>();
            var testRecorder = new TestExecutionRecorder(mockTestCaseEventsHandler.Object, this.testableTestRunCache);

            testRecorder.RecordStart(testCase);

            mockTestCaseEventsHandler.Verify(tceh => tceh.SendTestCaseStart(testCase), Times.Once);
        }

        [TestMethod]
        public void RecordResultShouldUpdateTestRunCache()
        {
            var testCase = new TestCase("A.C.M", new Uri("executor://dummy"), "A");
            var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            var testRecorder = new TestExecutionRecorder(null, this.testableTestRunCache);

            testRecorder.RecordResult(testResult);
            Assert.IsTrue(this.testableTestRunCache.TestResultList.Contains(testResult));
        }

        [TestMethod]
        public void RecordResultShouldSendTestCaseEvents()
        {
            var testCase = new TestCase("A.C.M", new Uri("executor://dummy"), "A");
            var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            var mockTestCaseEventsHandler = new Mock<ITestCaseEventsHandler>();
            var testRecorder = new TestExecutionRecorder(mockTestCaseEventsHandler.Object, this.testableTestRunCache);

            testRecorder.RecordResult(testResult);

            mockTestCaseEventsHandler.Verify(tceh => tceh.SendTestResult(testResult), Times.Once);
        }

        [TestMethod]
        public void RecordEndShouldUpdateTestRunCache()
        {
            var testCase = new TestCase("A.C.M", new Uri("executor://dummy"), "A");

            var testRecorder = new TestExecutionRecorder(null, this.testableTestRunCache);

            testRecorder.RecordEnd(testCase, TestOutcome.Passed);
            Assert.IsTrue(this.testableTestRunCache.TestCompletedList.Contains(testCase));
        }

        [TestMethod]
        public void RecordEndShouldSendTestCaseEvents()
        {
            var testCase = new TestCase("A.C.M", new Uri("executor://dummy"), "A");
            var mockTestCaseEventsHandler = new Mock<ITestCaseEventsHandler>();
            var testRecorder = new TestExecutionRecorder(mockTestCaseEventsHandler.Object, this.testableTestRunCache);

            testRecorder.RecordEnd(testCase, TestOutcome.Passed);

            mockTestCaseEventsHandler.Verify(tceh => tceh.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Once);
        }

        [TestMethod]
        public void RecordAttachmentsShouldAddToAttachmentSet()
        {
            var testRecorder = new TestExecutionRecorder(null, this.testableTestRunCache);
            var attachmentSet = new List<AttachmentSet> { new AttachmentSet(new Uri("attachment://dummy"), "attachment") };

            testRecorder.RecordAttachments(attachmentSet);

            var attachments = testRecorder.Attachments;

            Assert.IsNotNull(attachments);
            CollectionAssert.AreEqual(attachmentSet, attachments);
        }

        [TestMethod]
        public void RecordAttachmentsShouldAddToAttachmentSetForMultipleAttachments()
        {
            var testRecorder = new TestExecutionRecorder(null, this.testableTestRunCache);
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
    }
}
