// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class DataCollectionTestCaseEventHandlerTests
    {
        private readonly Mock<ICommunicationManager> mockCommunicationManager;
        private readonly Mock<IDataCollectionManager> mockDataCollectionManager;
        private readonly DataCollectionTestCaseEventHandler requestHandler;
        private readonly Mock<IDataSerializer> dataSerializer;

        public DataCollectionTestCaseEventHandlerTests()
        {
            mockCommunicationManager = new Mock<ICommunicationManager>();
            mockDataCollectionManager = new Mock<IDataCollectionManager>();
            dataSerializer = new Mock<IDataSerializer>();
            requestHandler = new DataCollectionTestCaseEventHandler(mockCommunicationManager.Object, new Mock<IDataCollectionManager>().Object, dataSerializer.Object);
        }

        [TestMethod]
        public void InitializeShouldInitializeConnection()
        {
            mockCommunicationManager.Setup(x => x.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, 1));
            requestHandler.InitializeCommunication();

            mockCommunicationManager.Verify(x => x.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            mockCommunicationManager.Verify(x => x.AcceptClientAsync(), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfExceptionIsThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Throws<Exception>();
            Assert.ThrowsException<Exception>(() => requestHandler.InitializeCommunication());
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldWaitForConnectionToBeCompleted()
        {
            mockCommunicationManager.Setup(x => x.WaitForClientConnection(It.IsAny<int>())).Returns(true);

            var result = requestHandler.WaitForRequestHandlerConnection(10);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldThrowExceptionIfThrownByConnectionManager()
        {
            mockCommunicationManager.Setup(x => x.WaitForClientConnection(It.IsAny<int>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => requestHandler.WaitForRequestHandlerConnection(10));
        }

        [TestMethod]
        public void CloseShouldStopServer()
        {
            requestHandler.Close();

            mockCommunicationManager.Verify(x => x.StopServer(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.StopServer()).Throws<Exception>();

            Assert.ThrowsException<Exception>(
                () => requestHandler.Close());
        }

        [TestMethod]
        public void CloseShouldNotThrowExceptionIfCommunicationManagerIsNull()
        {
            var requestHandler = new DataCollectionTestCaseEventHandler(null, new Mock<IDataCollectionManager>().Object, dataSerializer.Object);

            requestHandler.Close();

            mockCommunicationManager.Verify(x => x.StopServer(), Times.Never);
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessBeforeTestCaseStartEvent()
        {
            var message = new Message();
            message.MessageType = MessageType.DataCollectionTestStart;
            message.Payload = JToken.FromObject(new TestCaseEndEventArgs());

            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(new Message() { MessageType = MessageType.SessionEnd, Payload = "false" });

            var requestHandler = new DataCollectionTestCaseEventHandler(mockCommunicationManager.Object, mockDataCollectionManager.Object, dataSerializer.Object);

            requestHandler.ProcessRequests();

            mockDataCollectionManager.Verify(x => x.TestCaseStarted(It.IsAny<TestCaseStartEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessAfterTestCaseCompleteEvent()
        {
            var message = new Message();
            message.MessageType = MessageType.DataCollectionTestEnd;
            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            message.Payload = JToken.FromObject(new TestResultEventArgs(new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase)));

            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(new Message() { MessageType = MessageType.SessionEnd, Payload = "false" });

            var requestHandler = new DataCollectionTestCaseEventHandler(mockCommunicationManager.Object, mockDataCollectionManager.Object, dataSerializer.Object);

            requestHandler.ProcessRequests();

            mockDataCollectionManager.Verify(x => x.TestCaseEnded(It.IsAny<TestCaseEndEventArgs>()), Times.Once);
            mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionTestEndResult, It.IsAny<Collection<AttachmentSet>>()));
        }

        [TestMethod]
        public void ProcessRequestsShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.ReceiveMessage()).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => requestHandler.ProcessRequests());
        }
    }
}
