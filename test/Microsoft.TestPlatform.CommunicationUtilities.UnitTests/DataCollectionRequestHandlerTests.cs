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
        private Mock<IDataCollectionManagerFactory> mockDataCollectionManagerFactory;
        private Mock<IDataCollectionTestCaseEventManagerFactory> mockTestCaseDataCollectionCommunicationFactory;

        public DataCollectionRequestHandlerTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.mockMessageSink = new Mock<IMessageSink>();
            this.mockDataCollectionManagerFactory = new Mock<IDataCollectionManagerFactory>();
            this.mockTestCaseDataCollectionCommunicationFactory = new Mock<IDataCollectionTestCaseEventManagerFactory>();
        }

        [TestMethod]
        public void CreateInstanceShouldThrowExceptinIfInstanceIsNullAndNullIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                DataCollectionRequestHandler.CreateInstance(null, null);
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
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

            requestHandler.InitializeCommunication(123);

            this.mockCommunicationManager.Verify(x => x.SetupClientAsync(123), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.SetupClientAsync(It.IsAny<int>())).Throws<Exception>();
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.InitializeCommunication(123);
            });
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldInvokeCommunicationManager()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

            requestHandler.WaitForRequestSenderConnection(0);

            this.mockCommunicationManager.Verify(x => x.WaitForServerConnection(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.WaitForServerConnection(It.IsAny<int>())).Throws<Exception>();
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.WaitForRequestSenderConnection(0);
            });
        }

        [TestMethod]
        public void SendDataCollectionMessageShouldSendMessageToCommunicationManager()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);
            var message = new DataCollectionMessageEventArgs(TestMessageLevel.Error, "message");

            requestHandler.SendDataCollectionMessage(message);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionMessage, message), Times.Once);
        }

        [TestMethod]
        public void SendDataCollectionMessageShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionMessage, It.IsAny<DataCollectionMessageEventArgs>())).Throws<Exception>();
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);
            var message = new DataCollectionMessageEventArgs(TestMessageLevel.Error, "message");

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.SendDataCollectionMessage(message);
            });
        }

        [TestMethod]
        public void CloseShouldCloseCommunicationChannel()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

            requestHandler.Close();

            this.mockCommunicationManager.Verify(x => x.StopClient(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.StopClient()).Throws<Exception>();
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.Close();
            });
        }

        [TestMethod]
        public void DisposeShouldCloseCommunicationChannel()
        {
            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

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

            var mockDataCollectorManager = new Mock<IDataCollectionManager>();
            mockDataCollectorManager.Setup(x => x.SessionStarted()).Returns(true);
            this.mockDataCollectionManagerFactory.Setup(x => x.Create(this.mockMessageSink.Object)).Returns(mockDataCollectorManager.Object);

            var mockTestCaseDataCollectionRequestHandler = new Mock<IDataCollectionTestCaseEventHandler>();
            this.mockTestCaseDataCollectionCommunicationFactory.Setup(x => x.GetTestCaseDataCollectionRequestHandler()).Returns(mockTestCaseDataCollectionRequestHandler.Object);

            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

            requestHandler.ProcessRequests();

            mockTestCaseDataCollectionRequestHandler.Verify(x => x.InitializeCommunication(), Times.Once);
            mockTestCaseDataCollectionRequestHandler.Verify(x => x.WaitForRequestHandlerConnection(It.IsAny<int>()), Times.Once);
            mockTestCaseDataCollectionRequestHandler.Verify(x => x.ProcessRequests(), Times.Once);

            mockDataCollectorManager.Verify(x => x.SessionStarted(), Times.Once);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.BeforeTestRunStartResult, It.IsAny<BeforeTestRunStartResult>()), Times.Once);

            // Verify AfterTestRun events.
            mockDataCollectorManager.Verify(x => x.SessionEnded(It.IsAny<bool>()), Times.Once);
            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.AfterTestRunEndResult, It.IsAny<Collection<AttachmentSet>>()), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Throws<Exception>();

            var requestHandler = new DataCollectionRequestHandler(this.mockCommunicationManager.Object, this.mockMessageSink.Object, this.mockDataCollectionManagerFactory.Object, this.mockTestCaseDataCollectionCommunicationFactory.Object);

            Assert.ThrowsException<Exception>(() => { requestHandler.ProcessRequests(); });
        }
    }
}
