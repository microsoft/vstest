// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using System;
    using System.Collections.ObjectModel;

    [TestClass]
    public class ProxyOutOfProcDataCollectionManagerTests
    {
        private Mock<ITestEventsPublisher> mockTestEventsPublisher;
        private Mock<IDataCollectionTestCaseEventSender> mockDataCollectionTestCaseEventSender;
        private Collection<AttachmentSet> attachmentSets;
        private TestCase testcase;
        private VisualStudio.TestPlatform.ObjectModel.TestResult testResult;

        private ProxyOutOfProcDataCollectionManager proxyOutOfProcDataCollectionManager;
        public ProxyOutOfProcDataCollectionManagerTests()
        {
            this.mockTestEventsPublisher = new Mock<ITestEventsPublisher>();
            this.mockDataCollectionTestCaseEventSender = new Mock<IDataCollectionTestCaseEventSender>();
            this.proxyOutOfProcDataCollectionManager = new ProxyOutOfProcDataCollectionManager(this.mockDataCollectionTestCaseEventSender.Object, this.mockTestEventsPublisher.Object);

            var attachmentSet = new AttachmentSet(new Uri("my://datacollector"), "mydatacollector");
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("my://attachment.txt"), string.Empty));
            this.attachmentSets = new Collection<AttachmentSet>();
            this.attachmentSets.Add(attachmentSet);

            this.testcase = new TestCase();
            this.testcase.Id = Guid.NewGuid();
            this.mockDataCollectionTestCaseEventSender.Setup(x => x.SendTestCaseEnd(It.IsAny<TestCaseEndEventArgs>())).Returns(this.attachmentSets);
            this.mockTestEventsPublisher.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(this.testcase, TestOutcome.Passed));
            this.testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(this.testcase);
        }

        [TestMethod]
        public void TriggerTestCaseEndShouldReturnCacheAttachmentsAndAssociateWithTestResultWhenTriggerSendTestResultIsInvoked()
        {
            this.mockTestEventsPublisher.Raise(x => x.TestResult += null, new TestResultEventArgs(this.testResult));

            Assert.AreEqual(1, this.testResult.Attachments.Count);
            Assert.IsTrue(this.testResult.Attachments[0].Attachments[0].Uri.OriginalString.Contains("attachment.txt"));
        }

        [TestMethod]
        public void TriggerSendTestResultShouldDeleteTheAttachmentsFromCache()
        {
            this.mockTestEventsPublisher.Raise(x => x.TestResult += null, new TestResultEventArgs(this.testResult));

            this.testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(this.testcase);
            this.mockTestEventsPublisher.Raise(x => x.TestResult += null, new TestResultEventArgs(this.testResult));

            Assert.AreEqual(0, this.testResult.Attachments.Count);
        }
    }
}
