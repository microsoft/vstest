// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyOperationManagerTests
    {
        private ProxyOperationManager testOperationManager;

        private Mock<ITestHostManager> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int connectionTimeout = 400;

        public ProxyOperationManagerTests()
        {
            this.mockTestHostManager = new Mock<ITestHostManager>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);
            this.testOperationManager = new ProxyOperationManager(this.mockRequestSender.Object, null, this.connectionTimeout);
        }

        [TestMethod]
        public void SetupChannelShouldLaunchTestHost()
        {
            var expectedStartInfo = new TestProcessStartInfo();
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            this.mockTestHostManager.Setup(
                    th => th.GetTestHostProcessStartInfo(null, It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(expectedStartInfo);

            this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);

            this.mockTestHostManager.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(si => si == expectedStartInfo)), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldSetupServerForCommunication()
        {
            this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldNotInitializeIfConnectionIsAlreadyInitialized()
        {
            this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);
            this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfObjectIsDisposed()
        {
            using (this.testOperationManager)
            {
                this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);
            }

            Assert.ThrowsException<ObjectDisposedException>(() => this.testOperationManager.SetupChannel(this.mockTestHostManager.Object));
        }

        [TestMethod]
        public void SetupChannelShouldWaitForTestHostConnection()
        {
            this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldWaitForTestHostConnectionEvenIfConnectionIsInitialized()
        {
            this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);
            this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout), Times.Exactly(2));
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfWaitForTestHostConnectionTimesOut()
        {
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(false);

            Assert.ThrowsException<TestPlatformException>(() => this.testOperationManager.SetupChannel(this.mockTestHostManager.Object));
        }

        [TestMethod]
        public void AbortShouldThrowIfObjectIsDisposed()
        {
            using (this.testOperationManager)
            {
                this.testOperationManager.SetupChannel(this.mockTestHostManager.Object);
            }

            Assert.ThrowsException<ObjectDisposedException>(() => this.testOperationManager.SetupChannel(this.mockTestHostManager.Object));
        }
    }
}
