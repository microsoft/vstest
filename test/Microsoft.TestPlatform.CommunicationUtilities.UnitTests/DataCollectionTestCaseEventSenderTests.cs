// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net;

    using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.TestDoubles;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class DataCollectionTestCaseEventSenderTests
    {
        private readonly DataCollectionTestCaseEventSender dataCollectionTestCaseEventSender;
        private readonly Mock<ICommunicationManager> mockCommunicationManager;
        private readonly TestCase testCase = new("hello", new Uri("world://how"), "1.dll");

        public DataCollectionTestCaseEventSenderTests()
        {
            mockCommunicationManager = new Mock<ICommunicationManager>();
            dataCollectionTestCaseEventSender = new TestableDataCollectionTestCaseEventSender(mockCommunicationManager.Object, JsonDataSerializer.Instance);
        }

        [TestMethod]
        public void InitializeShouldInitializeCommunicationManager()
        {
            dataCollectionTestCaseEventSender.InitializeCommunication(123);

            mockCommunicationManager.Verify(x => x.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, 123)), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.SetupClientAsync(It.IsAny<IPEndPoint>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => dataCollectionTestCaseEventSender.InitializeCommunication(123));
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldInvokeWaitForServerConnection()
        {
            dataCollectionTestCaseEventSender.WaitForRequestSenderConnection(123);

            mockCommunicationManager.Verify(x => x.WaitForServerConnection(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.WaitForServerConnection(It.IsAny<int>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => dataCollectionTestCaseEventSender.WaitForRequestSenderConnection(123));
        }

        [TestMethod]
        public void CloseShouldDisposeCommunicationManager()
        {
            dataCollectionTestCaseEventSender.Close();

            mockCommunicationManager.Verify(x => x.StopClient(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.StopClient()).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => dataCollectionTestCaseEventSender.Close());
        }

        [TestMethod]
        public void SendTestCaseStartShouldSendMessageThroughCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.DataCollectionTestStartAck });
            var testcaseStartEventArgs = new TestCaseStartEventArgs(testCase);
            dataCollectionTestCaseEventSender.SendTestCaseStart(testcaseStartEventArgs);

            mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionTestStart, testcaseStartEventArgs), Times.Once);
            mockCommunicationManager.Verify(x => x.ReceiveMessage(), Times.Once);
        }

        [TestMethod]
        public void SendTestCaseStartShouldThrowExceptionIfThrownByCommunicationManager()
        {
            var testcaseStartEventArgs = new TestCaseStartEventArgs(testCase);
            mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionTestStart, testcaseStartEventArgs)).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => dataCollectionTestCaseEventSender.SendTestCaseStart(testcaseStartEventArgs));
        }

        [TestMethod]
        public void SendTestCaseEndShouldReturnAttachments()
        {
            var testCaseEndEventArgs = new TestCaseEndEventArgs();

            var attachmentSet = new AttachmentSet(new Uri("my://attachment"), "displayname");
            mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.DataCollectionTestEndResult, Payload = JToken.FromObject(new Collection<AttachmentSet>() { attachmentSet }) });
            var attachments = dataCollectionTestCaseEventSender.SendTestCaseEnd(testCaseEndEventArgs);

            Assert.AreEqual(attachments[0].Uri, attachmentSet.Uri);
            Assert.AreEqual(attachments[0].DisplayName, attachmentSet.DisplayName);
        }

        [TestMethod]
        public void SendTestCaseCompletedShouldThrowExceptionIfThrownByCommunicationManager()
        {
            var testCaseEndEventArgs = new TestCaseEndEventArgs();

            mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionTestEnd, It.IsAny<TestCaseEndEventArgs>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => dataCollectionTestCaseEventSender.SendTestCaseEnd(testCaseEndEventArgs));
        }
    }
}