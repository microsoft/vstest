// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyOutOfProcDataCollectionManagerTests
    {
        private Mock<ITestCaseEventsHandler> mockTestCaseEventsHandler;
        private Mock<IDataCollectionTestCaseEventSender> mockDataCollectionTestCaseEventSender;
        private Collection<AttachmentSet> attachmentSets;
        private TestCase testcase;
        private VisualStudio.TestPlatform.ObjectModel.TestResult testResult;

        private ProxyOutOfProcDataCollectionManager proxyOutOfProcDataCollectionManager;
        public ProxyOutOfProcDataCollectionManagerTests()
        {
            this.mockTestCaseEventsHandler = new Mock<ITestCaseEventsHandler>();
            this.mockDataCollectionTestCaseEventSender = new Mock<IDataCollectionTestCaseEventSender>();
            this.proxyOutOfProcDataCollectionManager = new ProxyOutOfProcDataCollectionManager(this.mockDataCollectionTestCaseEventSender.Object, this.mockTestCaseEventsHandler.Object);

            var attachmentSet = new AttachmentSet(new Uri("my://datacollector"), "mydatacollector");
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("my://attachment.txt"), string.Empty));
            this.attachmentSets = new Collection<AttachmentSet>();
            attachmentSets.Add(attachmentSet);

            this.testcase = new TestCase();
            testcase.Id = Guid.NewGuid();
            this.mockDataCollectionTestCaseEventSender.Setup(x => x.SendTestCaseEnd(It.IsAny<TestCaseEndEventArgs>())).Returns(attachmentSets);
            this.mockTestCaseEventsHandler.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));
            this.testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testcase);
        }

        [TestMethod]
        public void TriggerTestCaseEndShouldReturnCacheAttachmentsAndAssociateWithTestResultWhenTriggerSendTestResultIsInvoked()
        {
            this.mockTestCaseEventsHandler.Raise(x => x.TestResult += null, new TestResultEventArgs(testResult));

            Assert.AreEqual(1, testResult.Attachments.Count);
            Assert.IsTrue(testResult.Attachments[0].Attachments[0].Uri.OriginalString.Contains("attachment.txt"));
        }

        [TestMethod]
        public void TriggerSendTestResultShouldDeleteTheAttachmentsFromCache()
        {
            this.mockTestCaseEventsHandler.Raise(x => x.TestResult += null, new TestResultEventArgs(this.testResult));

            this.testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(this.testcase);
            this.mockTestCaseEventsHandler.Raise(x => x.TestResult += null, new TestResultEventArgs(this.testResult));

            Assert.AreEqual(0, this.testResult.Attachments.Count);
        }
    }
}