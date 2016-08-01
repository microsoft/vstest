// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CommunicationUtilities.UnitTests.ObjectModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionTestRunEventsHandlerTests
    {
        private Mock<ITestRunEventsHandler> baseTestRunEventsHandler;
        private DataCollectionTestRunEventsHandler testRunEventHandler;
        private Mock<IProxyDataCollectionManager> proxyDataCollectionManager;

        [TestInitialize]
        public void InitializeTests()
        {
            this.baseTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            this.proxyDataCollectionManager = new Mock<IProxyDataCollectionManager>();
            this.testRunEventHandler = new DataCollectionTestRunEventsHandler(this.baseTestRunEventsHandler.Object, this.proxyDataCollectionManager.Object);
        }

        [TestMethod]
        public void HandleLogMessageShouldSendMessageToBaseTestRunEventsHandler()
        {
            this.testRunEventHandler.HandleLogMessage(TestMessageLevel.Informational, null);
            this.baseTestRunEventsHandler.Verify(th => th.HandleLogMessage(0, null), Times.AtLeast(1));
        }

        [TestMethod]
        public void HandleRawMessageShouldSendMessageToBaseTestRunEventsHandler()
        {
            this.testRunEventHandler.HandleRawMessage(null);
            this.baseTestRunEventsHandler.Verify(th => th.HandleRawMessage(null), Times.AtLeast(1));
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldAfterTestRunEndToGetAttachments()
        {
            this.testRunEventHandler.HandleTestRunComplete(null, null, null, null);
            this.baseTestRunEventsHandler.Verify(th => th.HandleTestRunComplete(null, null, null, null), Times.AtLeast(1));
            this.proxyDataCollectionManager.Verify(dcm => dcm.AfterTestRunEnd(false, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        #region Get Combined Attachments
        [TestMethod]
        public void GetCombinedAttachmentSetsShouldReturnCombinedAttachments()
        {
            Collection<AttachmentSet> Attachments1 = new Collection<AttachmentSet>();
            AttachmentSet attachmentset1 = new AttachmentSet(new Uri("DataCollection://Attachment/v1"), "AttachmentV1");
            attachmentset1.Attachments.Add(new UriDataAttachment(new Uri("DataCollection://Attachment/v11"), "AttachmentV1-Attachment1"));
            Attachments1.Add(attachmentset1);

            Collection<AttachmentSet> Attachments2 = new Collection<AttachmentSet>();
            AttachmentSet attachmentset2 = new AttachmentSet(new Uri("DataCollection://Attachment/v1"), "AttachmentV1");
            attachmentset2.Attachments.Add(new UriDataAttachment(new Uri("DataCollection://Attachment/v12"), "AttachmentV1-Attachment2"));

            Attachments2.Add(attachmentset2);

            var result = DataCollectionTestRunEventsHandler.GetCombinedAttachmentSets(Attachments1, Attachments2);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(2, result.First().Attachments.Count);
        }

        [TestMethod]
        public void GetCombinedAttachmentSetsShouldReturnFirstArgumentIfSecondArgumentIsNull()
        {
            Collection<AttachmentSet> Attachments1 = new Collection<AttachmentSet>();
            AttachmentSet attachmentset1 = new AttachmentSet(new Uri("DataCollection://Attachment/v1"), "AttachmentV1");
            attachmentset1.Attachments.Add(new UriDataAttachment(new Uri("DataCollection://Attachment/v11"), "AttachmentV1-Attachment1"));
            Attachments1.Add(attachmentset1);

            var result = DataCollectionTestRunEventsHandler.GetCombinedAttachmentSets(Attachments1, null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result.First().Attachments.Count);
        }

        [TestMethod]
        public void GetCombinedAttachmentSetsShouldReturnNullIfFirstArgumentIsNull()
        {
            var result = DataCollectionTestRunEventsHandler.GetCombinedAttachmentSets(null, null);
            Assert.IsNull(result);
        }

        #endregion
    }
}