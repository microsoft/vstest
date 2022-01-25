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
        private readonly Mock<ITestEventsPublisher> mockTestEventsPublisher;
        private readonly Mock<IDataCollectionTestCaseEventSender> mockDataCollectionTestCaseEventSender;
        private readonly Collection<AttachmentSet> attachmentSets;
        private readonly TestCase testcase;
        private VisualStudio.TestPlatform.ObjectModel.TestResult testResult;

        private readonly ProxyOutOfProcDataCollectionManager proxyOutOfProcDataCollectionManager;
        public ProxyOutOfProcDataCollectionManagerTests()
        {
            mockTestEventsPublisher = new Mock<ITestEventsPublisher>();
            mockDataCollectionTestCaseEventSender = new Mock<IDataCollectionTestCaseEventSender>();
            proxyOutOfProcDataCollectionManager = new ProxyOutOfProcDataCollectionManager(mockDataCollectionTestCaseEventSender.Object, mockTestEventsPublisher.Object);

            var attachmentSet = new AttachmentSet(new Uri("my://datacollector"), "mydatacollector");
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("my://attachment.txt"), string.Empty));
            attachmentSets = new Collection<AttachmentSet>
            {
                attachmentSet
            };

            testcase = new TestCase();
            testcase.Id = Guid.NewGuid();
            mockDataCollectionTestCaseEventSender.Setup(x => x.SendTestCaseEnd(It.IsAny<TestCaseEndEventArgs>())).Returns(attachmentSets);
            mockTestEventsPublisher.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));
            testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testcase);
        }

        [TestMethod]
        public void TriggerTestCaseEndShouldReturnCacheAttachmentsAndAssociateWithTestResultWhenTriggerSendTestResultIsInvoked()
        {
            mockTestEventsPublisher.Raise(x => x.TestResult += null, new TestResultEventArgs(testResult));

            Assert.AreEqual(1, testResult.Attachments.Count);
            Assert.IsTrue(testResult.Attachments[0].Attachments[0].Uri.OriginalString.Contains("attachment.txt"));
        }

        [TestMethod]
        public void TriggerSendTestResultShouldDeleteTheAttachmentsFromCache()
        {
            mockTestEventsPublisher.Raise(x => x.TestResult += null, new TestResultEventArgs(testResult));

            testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testcase);
            mockTestEventsPublisher.Raise(x => x.TestResult += null, new TestResultEventArgs(testResult));

            Assert.AreEqual(0, testResult.Attachments.Count);
        }
    }
}
