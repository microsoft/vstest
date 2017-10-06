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
        private DataCollectionTestCaseEventSender dataCollectionTestCaseEventSender;
        private Mock<ICommunicationManager> mockCommunicationManager;
        private TestCase testCase = new TestCase("hello", new Uri("world://how"), "1.dll");

        public DataCollectionTestCaseEventSenderTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.dataCollectionTestCaseEventSender = new TestableDataCollectionTestCaseEventSender(this.mockCommunicationManager.Object, JsonDataSerializer.Instance);
        }

        [TestMethod]
        public void InitializeShouldInitializeCommunicationManager()
        {
            this.dataCollectionTestCaseEventSender.InitializeCommunication(123);

            this.mockCommunicationManager.Verify(x => x.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, 123)), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.SetupClientAsync(It.IsAny<IPEndPoint>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
                {
                    this.dataCollectionTestCaseEventSender.InitializeCommunication(123);
                });
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldInvokeWaitForServerConnection()
        {
            this.dataCollectionTestCaseEventSender.WaitForRequestSenderConnection(123);

            this.mockCommunicationManager.Verify(x => x.WaitForServerConnection(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.WaitForServerConnection(It.IsAny<int>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
                {
                    this.dataCollectionTestCaseEventSender.WaitForRequestSenderConnection(123);
                });
        }

        [TestMethod]
        public void CloseShouldDisposeCommunicationManager()
        {
            this.dataCollectionTestCaseEventSender.Close();

            this.mockCommunicationManager.Verify(x => x.StopClient(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.StopClient()).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
                {
                    this.dataCollectionTestCaseEventSender.Close();
                });
        }

        [TestMethod]
        public void SendTestCaseStartShouldSendMessageThroughCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.DataCollectionTestStartAck });
            var testcaseStartEventArgs = new TestCaseStartEventArgs(this.testCase);
            this.dataCollectionTestCaseEventSender.SendTestCaseStart(testcaseStartEventArgs);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionTestStart, testcaseStartEventArgs), Times.Once);
            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionTestStart, testcaseStartEventArgs), Times.Once);
        }

        [TestMethod]
        public void SendTestCaseStartShouldThrowExceptionIfThrownByCommunicationManager()
        {
            var testcaseStartEventArgs = new TestCaseStartEventArgs(this.testCase);
            this.mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionTestStart, testcaseStartEventArgs)).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
                {
                    this.dataCollectionTestCaseEventSender.SendTestCaseStart(testcaseStartEventArgs);
                });
        }

        [TestMethod]
        public void SendTestCaseEndShouldReturnAttachments()
        {
            var testCaseEndEventArgs = new TestCaseEndEventArgs();

            var attachmentSet = new AttachmentSet(new Uri("my://attachment"), "displayname");
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.DataCollectionTestEndResult, Payload = JToken.FromObject(new Collection<AttachmentSet>() { attachmentSet }) });
            var attachments = this.dataCollectionTestCaseEventSender.SendTestCaseEnd(testCaseEndEventArgs);

            Assert.AreEqual(attachments[0].Uri, attachmentSet.Uri);
            Assert.AreEqual(attachments[0].DisplayName, attachmentSet.DisplayName);
        }

        [TestMethod]
        public void SendTestCaseCompletedShouldThrowExceptionIfThrownByCommunicationManager()
        {
            var testCaseEndEventArgs = new TestCaseEndEventArgs();

            this.mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionTestEnd, It.IsAny<TestCaseEndEventArgs>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
                {
                    this.dataCollectionTestCaseEventSender.SendTestCaseEnd(testCaseEndEventArgs);
                });
        }
    }
}