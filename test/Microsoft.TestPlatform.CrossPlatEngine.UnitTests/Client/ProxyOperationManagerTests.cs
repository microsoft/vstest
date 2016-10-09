// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyOperationManagerTests
    {
        private readonly ProxyOperationManager testOperationManager;

        private readonly Mock<ITestHostManager> mockTestHostManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int connectionTimeout = 400;

        public ProxyOperationManagerTests()
        {
            this.mockTestHostManager = new Mock<ITestHostManager>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);
            this.testOperationManager = new TestableProxyOperationManager(this.mockRequestSender.Object, this.mockTestHostManager.Object, this.connectionTimeout);
        }

        [TestMethod]
        public void SetupChannelShouldLaunchTestHost()
        {
            var expectedStartInfo = new TestProcessStartInfo();
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            this.mockTestHostManager.Setup(
                    th => th.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(expectedStartInfo);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            this.mockTestHostManager.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(si => si == expectedStartInfo)), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldCreateTimestampedLogFileForHost()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            EqtTrace.InitializeVerboseTrace("log.txt");
            
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            this.mockTestHostManager.Verify(
                th =>
                    th.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        null,
                        It.Is<TestRunnerConnectionInfo>(t => t.LogFile.Contains("log.host." + DateTime.Now.ToString("yyMMdd")))));
            EqtTrace.TraceLevel = TraceLevel.Off;
        }

        [TestMethod]
        public void SetupChannelShouldAddRunnerProcessIdForTestHost()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            this.mockTestHostManager.Verify(
                th =>
                    th.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        null,
                        It.Is<TestRunnerConnectionInfo>(t => t.RunnerProcessId.Equals(Process.GetCurrentProcess().Id))));
        }

        [TestMethod]
        public void SetupChannelShouldSetupServerForCommunication()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldNotInitializeIfConnectionIsAlreadyInitialized()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldWaitForTestHostConnection()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldWaitForTestHostConnectionEvenIfConnectionIsInitialized()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout), Times.Exactly(2));
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfWaitForTestHostConnectionTimesOut()
        {
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(false);

            Assert.ThrowsException<TestPlatformException>(() => this.testOperationManager.SetupChannel(Enumerable.Empty<string>()));
        }

        [TestMethod]
        public void CloseShouldEndSession()
        {
            this.testOperationManager.Close();

            this.mockRequestSender.Verify(rs => rs.EndSession(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldResetChannelInitialization()
        {
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            this.testOperationManager.Close();

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());
            this.mockTestHostManager.Verify(th => th.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Exactly(2));
        }

        private class TestableProxyOperationManager : ProxyOperationManager
        {
            public TestableProxyOperationManager(
                ITestRequestSender requestSender,
                ITestHostManager testHostManager,
                int clientConnectionTimeout) : base(requestSender, testHostManager, clientConnectionTimeout)
            {
            }
        }
    }
}
