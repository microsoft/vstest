// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.ObjectModel;

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
        private Mock<ICommunicationManager> mockCommunicationManager;
        private Mock<IDataCollectionManager> mockDataCollectionManager;
        private DataCollectionTestCaseEventHandler requestHandler;

        public DataCollectionTestCaseEventHandlerTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.mockDataCollectionManager = new Mock<IDataCollectionManager>();
            this.requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object, new Mock<IDataCollectionManager>().Object);
        }

        [TestMethod]
        public void InitializeShouldInitializeConnection()
        {
            this.mockCommunicationManager.Setup(x => x.HostServer()).Returns(1);
            this.requestHandler.InitializeCommunication();

            this.mockCommunicationManager.Verify(x => x.HostServer(), Times.Once);
            this.mockCommunicationManager.Verify(x => x.AcceptClientAsync(), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfExceptionIsThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.HostServer()).Throws<Exception>();
            Assert.ThrowsException<Exception>(() =>
            {
                this.requestHandler.InitializeCommunication();
            });
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldWaitForConnectionToBeCompleted()
        {
            this.mockCommunicationManager.Setup(x => x.WaitForClientConnection(It.IsAny<int>())).Returns(true);

            var result = this.requestHandler.WaitForRequestHandlerConnection(10);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldThrowExceptionIfThrownByConnectionManager()
        {
            this.mockCommunicationManager.Setup(x => x.WaitForClientConnection(It.IsAny<int>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.requestHandler.WaitForRequestHandlerConnection(10);
            });
        }

        [TestMethod]
        public void CloseShouldStopServer()
        {
            this.requestHandler.Close();

            this.mockCommunicationManager.Verify(x => x.StopServer(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.StopServer()).Throws<Exception>();

            Assert.ThrowsException<Exception>(
                () =>
                {
                    this.requestHandler.Close();
                });
        }

        [TestMethod]
        public void CloseShouldNotThrowExceptionIfCommunicationManagerIsNull()
        {
            var requestHandler = new DataCollectionTestCaseEventHandler(null, new Mock<IDataCollectionManager>().Object);

            requestHandler.Close();

            this.mockCommunicationManager.Verify(x => x.StopServer(), Times.Never);
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessBeforeTestCaseStartEvent()
        {
            var message = new Message();
            message.MessageType = MessageType.DataCollectionTestStart;
            message.Payload = JToken.FromObject(new TestCaseEndEventArgs());

            this.mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(new Message() { MessageType = MessageType.SessionEnd, Payload = "false" });

            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object, this.mockDataCollectionManager.Object);

            requestHandler.ProcessRequests();

            this.mockDataCollectionManager.Verify(x => x.TestCaseStarted(It.IsAny<TestCaseStartEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessAfterTestCaseCompleteEvent()
        {
            var message = new Message();
            message.MessageType = MessageType.DataCollectionTestEnd;
            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            message.Payload = JToken.FromObject(new TestResultEventArgs(new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase)));

            this.mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(new Message() { MessageType = MessageType.SessionEnd, Payload = "false" });

            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object, this.mockDataCollectionManager.Object);

            requestHandler.ProcessRequests();

            this.mockDataCollectionManager.Verify(x => x.TestCaseEnded(It.IsAny<TestCaseEndEventArgs>()), Times.Once);
            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionTestEndResult, It.IsAny<Collection<AttachmentSet>>()));
        }

        [TestMethod]
        public void ProcessRequestsShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => { this.requestHandler.ProcessRequests(); });
        }
    }
}
