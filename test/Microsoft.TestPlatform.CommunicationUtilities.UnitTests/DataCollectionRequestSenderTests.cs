// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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
        public void SendAfterTestRunEndAndGetResultShouldReturnAttachments()
        {
            var datacollectorUri = new Uri("my://custom/datacollector");
            var attachmentUri = new Uri("my://filename.txt");
            var displayName = "CustomDataCollector";
            var attachment = new AttachmentSet(datacollectorUri, displayName);
            attachment.Attachments.Add(new UriDataAttachment(attachmentUri, "filename.txt"));

            this.mockDataSerializer.Setup(x => x.DeserializePayload<AfterTestRunEndResult>(It.IsAny<Message>())).Returns(
                new AfterTestRunEndResult(new Collection<AttachmentSet>() { attachment }, new Dictionary<string, object>()));
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.AfterTestRunEndResult, Payload = null });

            var result = this.requestSender.SendAfterTestRunEndAndGetResult(null, false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AttachmentSets);
            Assert.IsNotNull(result.Metrics);
            Assert.AreEqual(1, result.AttachmentSets.Count);
            Assert.AreEqual(0, result.Metrics.Count);
            Assert.IsNotNull(result.AttachmentSets[0]);
            Assert.AreEqual(displayName, result.AttachmentSets[0].DisplayName);
            Assert.AreEqual(datacollectorUri, result.AttachmentSets[0].Uri);
            Assert.AreEqual(attachmentUri, result.AttachmentSets[0].Attachments[0].Uri);
        }

        [TestMethod]
        public void SendAfterTestRunEndAndGetResultShouldNotReturnAttachmentsWhenRequestCancelled()
        {
            var attachmentSets = this.requestSender.SendAfterTestRunEndAndGetResult(null, true);

            Assert.IsNull(attachmentSets);
        }

        [TestMethod]
        public void SendBeforeTestRunStartAndGetResultShouldSendBeforeTestRunStartMessageAndPayload()
        {
            var testSources = new List<string>() { "test1.dll" };
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.BeforeTestRunStartResult, Payload = null });
            this.requestSender.SendBeforeTestRunStartAndGetResult(string.Empty, testSources, true, null);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.BeforeTestRunStart, It.Is<BeforeTestRunStartPayload>(p => p.SettingsXml == string.Empty && p.IsTelemetryOptedIn)));
        }
    }
}
