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
        private Mock<IDataCollectionTestCaseEventManager> mockDataCollectionTestCaseEventManager;
        private Mock<IDataCollectionTestCaseEventSender> mockDataCollectionTestCaseEventSender;
        private Collection<AttachmentSet> attachmentSets;
        private TestCase testcase;

        private ProxyOutOfProcDataCollectionManager proxyOutOfProcDataCollectionManager;
        public ProxyOutOfProcDataCollectionManagerTests()
        {
            this.mockDataCollectionTestCaseEventManager = new Mock<IDataCollectionTestCaseEventManager>();
            this.mockDataCollectionTestCaseEventSender = new Mock<IDataCollectionTestCaseEventSender>();
            this.proxyOutOfProcDataCollectionManager = new ProxyOutOfProcDataCollectionManager(this.mockDataCollectionTestCaseEventSender.Object, this.mockDataCollectionTestCaseEventManager.Object);

            var attachmentSet = new AttachmentSet(new Uri("my://datacollector"), "mydatacollector");
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("my://attachment.txt"), string.Empty));
            this.attachmentSets = new Collection<AttachmentSet>();
            attachmentSets.Add(attachmentSet);

            this.testcase = new TestCase();
            testcase.Id = Guid.NewGuid();
        }

        [TestMethod]
        public void TriggerTestCaseEndShouldReturnCacheAttachmentsAndAssociateWithTestResultWhenTriggerSendTestResultIsInvoked()
        {
            this.mockDataCollectionTestCaseEventSender.Setup(x => x.SendTestCaseComplete(It.IsAny<TestCaseEndEventArgs>())).Returns(attachmentSets);

            this.mockDataCollectionTestCaseEventManager.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));

            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testcase);
            this.mockDataCollectionTestCaseEventManager.Raise(x => x.TestResult += null, new TestResultEventArgs(testResult));

            Assert.AreEqual(1, testResult.Attachments.Count);
            Assert.IsTrue(testResult.Attachments[0].Attachments[0].Uri.OriginalString.Contains("attachment.txt"));
        }

        [TestMethod]
        public void TriggerSendTestResultShouldDeleteTheAttachmentsFromCache()
        {
            this.mockDataCollectionTestCaseEventSender.Setup(x => x.SendTestCaseComplete(It.IsAny<TestCaseEndEventArgs>())).Returns(attachmentSets);
            this.mockDataCollectionTestCaseEventManager.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));

            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testcase);
            this.mockDataCollectionTestCaseEventManager.Raise(x => x.TestResult += null, new TestResultEventArgs(testResult));

            testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testcase);
            this.mockDataCollectionTestCaseEventManager.Raise(x => x.TestResult += null, new TestResultEventArgs(testResult));

            Assert.AreEqual(0, testResult.Attachments.Count);
        }


    }
}
