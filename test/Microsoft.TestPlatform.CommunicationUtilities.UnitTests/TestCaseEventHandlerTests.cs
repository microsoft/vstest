// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestCaseDataCollectionRequestHandlerTests
    {
        private Mock<ICommunicationManager> mockCommunicationManager;

        public TestCaseDataCollectionRequestHandlerTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
        }

        [TestMethod]
        public void InitializeShouldInitializeConnection()
        {
            this.mockCommunicationManager.Setup(x => x.HostServer()).Returns(1);
            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object);

            requestHandler.InitializeCommunication();

            this.mockCommunicationManager.Verify(x => x.HostServer(), Times.Once);
            this.mockCommunicationManager.Verify(x => x.AcceptClientAsync(), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfExceptionIsThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.HostServer()).Throws<Exception>();
            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object);
            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.InitializeCommunication();
            });
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldWaitForConnectionToBeCompleted()
        {
            this.mockCommunicationManager.Setup(x => x.WaitForClientConnection(It.IsAny<int>())).Returns(true);
            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object);

            var result = requestHandler.WaitForRequestHandlerConnection(10);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldThrowExceptionIfThrownByConnectionManager()
        {
            this.mockCommunicationManager.Setup(x => x.WaitForClientConnection(It.IsAny<int>())).Throws<Exception>();
            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object);

            Assert.ThrowsException<Exception>(() =>
            {
                requestHandler.WaitForRequestHandlerConnection(10);
            });
        }

        [TestMethod]
        public void CloseShouldStopServer()
        {
            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object);

            requestHandler.Close();

            this.mockCommunicationManager.Verify(x => x.StopServer(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.StopServer()).Throws<Exception>();
            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object);

            Assert.ThrowsException<Exception>(
                () =>
                {
                    requestHandler.Close();
                });
        }

        [TestMethod]
        public void CloseShouldNotThrowExceptionIfCommunicationManagerIsNull()
        {
            var requestHandler = new DataCollectionTestCaseEventHandler(null);

            requestHandler.Close();

            this.mockCommunicationManager.Verify(x => x.StopServer(), Times.Never);
        }

        [TestMethod]
        public void DisposeShouldStopServer()
        {
            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object);

            requestHandler.Dispose();

            this.mockCommunicationManager.Verify(x => x.StopServer(), Times.Once);
        }

        [TestMethod]
        public void DisposehouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.StopServer()).Throws<Exception>();
            var requestHandler = new DataCollectionTestCaseEventHandler(this.mockCommunicationManager.Object);

            Assert.ThrowsException<Exception>(
                () =>
                {
                    requestHandler.Dispose();
                });
        }

        [TestMethod]
        public void DisposeShouldNotThrowExceptionIfCommunicationManagerIsNull()
        {
            var requestHandler = new DataCollectionTestCaseEventHandler(null);

            requestHandler.Close();

            this.mockCommunicationManager.Verify(x => x.StopServer(), Times.Never);
        }
    }
}
