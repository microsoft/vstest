// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionRequestSenderTests
    {
        private Mock<ICommunicationManager> mockCommunicationManager;
        private DataCollectionRequestSender requestSender;
        private Mock<IDataSerializer> mockDataSerializer;

        public DataCollectionRequestSenderTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.requestSender = new DataCollectionRequestSender(this.mockCommunicationManager.Object, this.mockDataSerializer.Object);
        }

        [TestMethod]
        public void SendAfterTestRunStartAndGetResultShouldReturnAttachments()
        {
            var datacollectorUri = new Uri("my://custom/datacollector");
            var attachmentUri = new Uri("my://filename.txt");
            var displayName = "CustomDataCollector";
            var attachment = new AttachmentSet(datacollectorUri, displayName);
            attachment.Attachments.Add(new UriDataAttachment(attachmentUri, "filename.txt"));

            this.mockDataSerializer.Setup(x => x.DeserializePayload<Collection<AttachmentSet>>(It.IsAny<Message>())).Returns(new Collection<AttachmentSet>() { attachment });
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.AfterTestRunEndResult, Payload = null });

            var attachmentSets = this.requestSender.SendAfterTestRunStartAndGetResult(null, false);

            Assert.IsNotNull(attachmentSets);
            Assert.AreEqual(attachmentSets.Count, 1);
            Assert.IsNotNull(attachmentSets[0]);
            Assert.AreEqual(attachmentSets[0].DisplayName, displayName);
            Assert.AreEqual(datacollectorUri, attachmentSets[0].Uri);
            Assert.AreEqual(attachmentUri, attachmentSets[0].Attachments[0].Uri);
        }

        [TestMethod]
        public void SendAfterTestRunStartAndGetResultShouldNotReturnAttachmentsWhenRequestCancelled()
        {
            var attachmentSets = this.requestSender.SendAfterTestRunStartAndGetResult(null, true);

            Assert.IsNull(attachmentSets);
        }

        [TestMethod]
        public void SendBeforeTestRunStartAndGetResultShouldSendBeforeTestRunStartMessageAndPayload()
        {
            var testSources = new List<string>() { "test1.dll" };
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.BeforeTestRunStartResult, Payload = null });
            this.requestSender.SendBeforeTestRunStartAndGetResult(string.Empty, testSources, null);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.BeforeTestRunStart, It.IsAny<BeforeTestRunStartPayload>()));
        }
    }
}
