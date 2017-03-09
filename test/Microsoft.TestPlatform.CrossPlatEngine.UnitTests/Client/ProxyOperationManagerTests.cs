// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyOperationManagerTests
    {
        private readonly ProxyOperationManager testOperationManager;

        private readonly Mock<ITestRuntimeProvider> mockTestHostManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int connectionTimeout = 400;

        public ProxyOperationManagerTests()
        {
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);
            this.testOperationManager = new TestableProxyOperationManager(this.mockRequestSender.Object, this.mockTestHostManager.Object, this.connectionTimeout);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockTestHostManager.Setup(m =>
                            m.GetTestHostProcessStartInfo(It.IsAny<IEnumerable<string>>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(new TestProcessStartInfo());
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

            this.mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.Is<TestProcessStartInfo>(si => si == expectedStartInfo)), Times.Once);
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
                        It.Is<TestRunnerConnectionInfo>(
                            t => t.LogFile.Contains("log.host." + DateTime.Now.ToString("yy-MM-dd"))
                                 && t.LogFile.Contains("_" + Thread.CurrentThread.ManagedThreadId + ".txt"))));
#if NET46
            EqtTrace.TraceLevel = TraceLevel.Off;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Off;
#endif
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
        [Ignore]
        //Not valid test anymore, since host providers now monitor test host themselves
        public void SetupChannelShouldAddExitCallbackToTestHostStartInfo()
        {
            TestProcessStartInfo startInfo = null;
            this.mockTestHostManager.Setup(m => m.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>()))
                .Callback<TestProcessStartInfo>(
                    (s) => { startInfo = s; });

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.IsNotNull(startInfo.ErrorReceivedCallback);
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
            this.mockTestHostManager.Verify(th => th.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>()), Times.Exactly(2));
        }

        private class TestableProxyOperationManager : ProxyOperationManager
        {
            public TestableProxyOperationManager(
                ITestRequestSender requestSender,
                ITestRuntimeProvider testHostManager,
                int clientConnectionTimeout) : base(requestSender, testHostManager, clientConnectionTimeout)
            {
            }
        }
    }
}
