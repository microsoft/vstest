// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionRequestHandlerTests
    {
        private Mock<ICommunicationManager> mockCommunicationManager;
        private Mock<IMessageSink> mockMessageSink;
        private Mock<IDataCollectionManager> mockDataCollectionManager;
        private Mock<IDataCollectionTestCaseEventHandler> mockDataCollectionTestCaseEventHandler;

        public DataCollectionRequestHandlerTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.mockMessageSink = new Mock<IMessageSink>();
            this.mockDataCollectionManager = new Mock<IDataCollectionManager>();
            this.mockDataCollectionTestCaseEventHandler = new Mock<IDataCollectionTestCaseEventHandler>();
        }

        [TestMethod]
        public void CreateInstanceShouldThrowExceptionIfInstanceCommunicationManagerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                DataCollectionRequestHandler.CreateInstance(null, this.mockMessageSink.Object);
            });
        }

        [TestMethod]
        public void CreateInstanceShouldThrowExceptinIfInstanceMessageSinkIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                DataCollectionRequestHandler.CreateInstance(this.mockCommunicationManager.Object, null);
            });
        }

        [TestMethod]
        public void CreateInstanceShouldCreateInstance()
        {
            var result = DataCollectionRequestHandler.CreateInstance(this.mockCommunicationManager.Object, this.mockMessageSink.Object);

            Assert.AreEqual(result, DataCollectionRequestHandler.Instance);
        }

        [TestMethod]
        public void InitializeCommunicationShouldInitializeCommunication()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            requestHandler.InitializeCommunication(123);

            this.mockCommunicationManager.Verify(x => x.SetupClientAsync(123), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.SetupClientAsync(It.IsAny<int>())).Throws<Exception>();
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.InitializeCommunication(123);
            });
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldInvokeCommunicationManager()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            requestHandler.WaitForRequestSenderConnection(0);

            this.mockCommunicationManager.Verify(x => x.WaitForServerConnection(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.WaitForServerConnection(It.IsAny<int>())).Throws<Exception>();
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.WaitForRequestSenderConnection(0);
            });
        }

        [TestMethod]
        public void SendDataCollectionMessageShouldSendMessageToCommunicationManager()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);
            var message = new DataCollectionMessageEventArgs(TestMessageLevel.Error, "message");

            requestHandler.SendDataCollectionMessage(message);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionMessage, message), Times.Once);
        }

        [TestMethod]
        public void SendDataCollectionMessageShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionMessage, It.IsAny<DataCollectionMessageEventArgs>())).Throws<Exception>();
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);
            var message = new DataCollectionMessageEventArgs(TestMessageLevel.Error, "message");

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.SendDataCollectionMessage(message);
            });
        }

        [TestMethod]
        public void CloseShouldCloseCommunicationChannel()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            requestHandler.Close();

            this.mockCommunicationManager.Verify(x => x.StopClient(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.StopClient()).Throws<Exception>();
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.Close();
            });
        }

        [TestMethod]
        public void DisposeShouldCloseCommunicationChannel()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            requestHandler.Dispose();

            this.mockCommunicationManager.Verify(x => x.StopClient(), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessRequests()
        {
            var message = new Message();
            message.MessageType = MessageType.BeforeTestRunStart;
            message.Payload = "settingsXml";

            this.mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(new Message() { MessageType = MessageType.AfterTestRunEnd, Payload = "false" });

            this.mockDataCollectionManager.Setup(x => x.SessionStarted()).Returns(true);

            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            requestHandler.ProcessRequests();

            mockDataCollectionTestCaseEventHandler.Verify(x => x.InitializeCommunication(), Times.Once);
            mockDataCollectionTestCaseEventHandler.Verify(x => x.WaitForRequestHandlerConnection(It.IsAny<int>()), Times.Once);
            mockDataCollectionTestCaseEventHandler.Verify(x => x.ProcessRequests(), Times.Once);

            mockDataCollectionManager.Verify(x => x.SessionStarted(), Times.Once);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.BeforeTestRunStartResult, It.IsAny<BeforeTestRunStartResult>()), Times.Once);

            // Verify AfterTestRun events.
            mockDataCollectionManager.Verify(x => x.SessionEnded(It.IsAny<bool>()), Times.Once);
            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.AfterTestRunEndResult, It.IsAny<Collection<AttachmentSet>>()), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Throws<Exception>();

            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManager.Object, this.mockDataCollectionTestCaseEventHandler.Object);

            Assert.ThrowsException<Exception>(() => { requestHandler.ProcessRequests(); });
        }
    }
}
