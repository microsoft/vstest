// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyOperationManagerTests
    {
        private readonly ProxyOperationManager testOperationManager;

        private readonly Mock<ITestRuntimeProvider> mockTestHostManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        private Mock<IProcessHelper> mockProcessHelper;

        private Mock<IFileHelper> mockFileHelper;

        private Mock<IEnvironment> mockEnvironment;

        private Mock<IRequestData> mockRequestData;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int connectionTimeout = 400;

        public ProxyOperationManagerTests()
        {
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new Mock<IMetricsCollection>().Object);
            this.testOperationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.connectionTimeout);

            this.mockTestHostManager.Setup(
                    m => m.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(new TestProcessStartInfo());

            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(() => this.mockTestHostManager.Raise(t => t.HostLaunched += null, new HostProviderEventArgs(string.Empty)))
                .Returns(Task.FromResult(true));
        }

        [TestMethod]
        public void SetupChannelShouldLaunchTestHost()
        {
            var expectedStartInfo = new TestProcessStartInfo();
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            this.mockTestHostManager.Setup(
                    th => th.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(expectedStartInfo);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.Is<TestProcessStartInfo>(si => si == expectedStartInfo), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldCreateTimestampedLogFileForHost()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            EqtTrace.InitializeVerboseTrace("log.txt");

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.mockTestHostManager.Verify(
                th =>
                    th.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        null,
                        It.Is<TestRunnerConnectionInfo>(
                            t => t.LogFile.Contains("log.host." + DateTime.Now.ToString("yy-MM-dd"))
                                 && t.LogFile.Contains("_" + Thread.CurrentThread.ManagedThreadId + ".txt"))));
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Off;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Off;
#endif
        }

        [TestMethod]
        public void SetupChannelShouldAddRunnerProcessIdForTestHost()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

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
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldCallHostServerIfRunnerIsServer()
        {
            var connectionInfo = new TestHostConnectionInfo
                                      {
                                          Endpoint = IPAddress.Loopback + ":0",
                                          Role = ConnectionRole.Client,
                                          Transport = Transport.Sockets
                                      };
            ProtocolConfig protocolConfig = new ProtocolConfig { Version = 2 };
            var mockCommunicationManager = new Mock<ICommunicationManager>();
            mockCommunicationManager.Setup(mc => mc.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, 123));
            mockCommunicationManager.Setup(mc => mc.WaitForClientConnection(It.IsAny<int>())).Returns(true);
            var mockDataSerializer = new Mock<IDataSerializer>();
            var testRequestSender = new TestRequestSender(mockCommunicationManager.Object, connectionInfo, mockDataSerializer.Object, protocolConfig);

            var message = new Message() { MessageType = MessageType.VersionCheck, Payload = protocolConfig.Version };
            mockCommunicationManager.Setup(mc => mc.ReceiveMessage()).Returns(message);
            mockDataSerializer.Setup(ds => ds.DeserializePayload<int>(It.IsAny<Message>())).Returns(protocolConfig.Version);

            this.mockTestHostManager.Setup(thm => thm.GetTestHostConnectionInfo()).Returns(connectionInfo);

            var localTestOperationManager = new TestableProxyOperationManager(this.mockRequestData.Object, testRequestSender, this.mockTestHostManager.Object, this.connectionTimeout);

            localTestOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            mockCommunicationManager.Verify(s => s.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            mockCommunicationManager.Verify(s => s.AcceptClientAsync(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldCallSetupClientIfRunnerIsClient()
        {
            var connectionInfo = new TestHostConnectionInfo
                                     {
                                         Endpoint = IPAddress.Loopback + ":0",
                                         Role = ConnectionRole.Host,
                                         Transport = Transport.Sockets
                                     };
            ProtocolConfig protocolConfig = new ProtocolConfig { Version = 2 };
            var mockCommunicationManager = new Mock<ICommunicationManager>();
            mockCommunicationManager.Setup(mc => mc.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, 0)));
            mockCommunicationManager.Setup(mc => mc.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            var mockDataSerializer = new Mock<IDataSerializer>();
            var testRequestSender = new TestRequestSender(mockCommunicationManager.Object, connectionInfo, mockDataSerializer.Object, new ProtocolConfig { Version = 2 });

            this.mockTestHostManager.Setup(thm => thm.GetTestHostConnectionInfo()).Returns(connectionInfo);
            var message = new Message() { MessageType = MessageType.VersionCheck, Payload = protocolConfig.Version };
            mockCommunicationManager.Setup(mc => mc.ReceiveMessage()).Returns(message);
            mockDataSerializer.Setup(ds => ds.DeserializePayload<int>(It.IsAny<Message>())).Returns(protocolConfig.Version);

            var localTestOperationManager = new TestableProxyOperationManager(this.mockRequestData.Object, testRequestSender, this.mockTestHostManager.Object, this.connectionTimeout);

            localTestOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            mockCommunicationManager.Verify(s => s.SetupClientAsync(It.IsAny<IPEndPoint>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldNotInitializeIfConnectionIsAlreadyInitialized()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldWaitForTestHostConnection()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldNotWaitForTestHostConnectionIfConnectionIsInitialized()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout), Times.Exactly(1));
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfWaitForTestHostConnectionTimesOut()
        {
            SetupTestHostLaunched(true);
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(false);

            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.connectionTimeout);

            Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None));
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfLaunchTestHostFails()
        {
            SetupTestHostLaunched(false);
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);

            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.connectionTimeout);

            Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None));
        }

        [TestMethod]
        public void SetupChannelShouldCheckVersionWithTestHost()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);
            this.mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldThrowExceptionIfVersionCheckFails()
        {
            // Make the version check fail
            this.mockRequestSender.Setup(rs => rs.CheckVersionWithTestHost()).Throws(new TestPlatformException("Version check failed"));
            Assert.ThrowsException<TestPlatformException>(() => this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None));
        }

        [TestMethod]
        public void SetupChannelForDotnetHostManagerWithIsVersionCheckRequiredFalseShouldNotCheckVersionWithTestHost()
        {
            this.SetUpMocksForDotNetTestHost();
            var testHostManager = new TestableDotnetTestHostManager(false, this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockEnvironment.Object);
            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, testHostManager, this.connectionTimeout);

            operationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Never);
        }

        [TestMethod]
        public void SetupChannelForDotnetHostManagerWithIsVersionCheckRequiredTrueShouldCheckVersionWithTestHost()
        {
            this.SetUpMocksForDotNetTestHost();
            var testHostManager = new TestableDotnetTestHostManager(true, this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockEnvironment.Object);
            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, testHostManager, this.connectionTimeout);

            operationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldEndSessionIfHostWasLaunched()
        {
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.testOperationManager.Close();

            this.mockRequestSender.Verify(rs => rs.EndSession(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldNotEndSessionIfHostLaucnhedFailed()
        {
            this.testOperationManager.Close();

            this.mockRequestSender.Verify(rs => rs.EndSession(), Times.Never);
        }

        [TestMethod]
        public void CloseShouldAlwaysCleanTestHost()
        {
            this.testOperationManager.Close();

            this.mockTestHostManager.Verify(th => th.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void CloseShouldResetChannelInitialization()
        {
            this.SetupWaitForTestHostExit();
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.testOperationManager.Close();

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);
            this.mockTestHostManager.Verify(th => th.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public void CloseShouldTerminateTesthostProcessIfWaitTimesout()
        {
            // Ensure testhost start returns a dummy process id
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            this.testOperationManager.Close();

            this.mockTestHostManager.Verify(th => th.CleanTestHostAsync(CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public void CloseShouldNotThrowIfEndSessionFails()
        {
            this.mockRequestSender.Setup(rs => rs.EndSession()).Throws<Exception>();

            this.testOperationManager.Close();
        }

        private void SetupWaitForTestHostExit()
        {
            // Raise host exited when end session is called
            this.mockRequestSender.Setup(rs => rs.EndSession())
                .Callback(() => this.mockTestHostManager.Raise(t => t.HostExited += null, new HostProviderEventArgs(string.Empty)));
        }

        private void SetupTestHostLaunched(bool launchStatus)
        {
            // Raise host exited when end session is called
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(() => this.mockTestHostManager.Raise(t => t.HostLaunched += null, new HostProviderEventArgs(string.Empty)))
                .Returns(Task.FromResult(launchStatus));
        }

        [TestMethod]
        public void UpdateTestProcessStartInfoShouldUpdateTelemetryOptedInArgTrueIfTelemetryOptedIn()
        {
            TestProcessStartInfo receivedTestProcessInfo = new TestProcessStartInfo();
            var mockRequestData = new Mock<IRequestData>();

            mockRequestData.Setup(rd => rd.IsTelemetryOptedOut).Returns(false);

            var testOperationManager = new TestableProxyOperationManager(mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.connectionTimeout);

            this.mockTestHostManager
                .Setup(tm => tm.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback<TestProcessStartInfo, CancellationToken>(
                    (testProcessStartInfo, cancellationToken) =>
                        {
                            receivedTestProcessInfo = testProcessStartInfo;
                        })
                        .Returns(Task.FromResult(true));

            // Act.
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            // Verify.
            Assert.IsTrue(receivedTestProcessInfo.Arguments.Contains("--telemetryoptedin true"));
        }

        [TestMethod]
        public void UpdateTestProcessStartInfoShouldUpdateTelemetryOptedInArgFalseIfTelemetryOptedOut()
        {
            TestProcessStartInfo receivedTestProcessInfo = new TestProcessStartInfo();
            var mockRequestData = new Mock<IRequestData>();

            mockRequestData.Setup(rd => rd.IsTelemetryOptedOut).Returns(true);

            var testOperationManager = new TestableProxyOperationManager(mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.connectionTimeout);

            this.mockTestHostManager
                .Setup(tm => tm.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback<TestProcessStartInfo, CancellationToken>(
                    (testProcessStartInfo, cancellationToken) =>
                        {
                            receivedTestProcessInfo = testProcessStartInfo;
                        })
                .Returns(Task.FromResult(true));

            // Act.
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), CancellationToken.None);

            // Verify.
            Assert.IsTrue(receivedTestProcessInfo.Arguments.Contains("--telemetryoptedin false"));
        }

        private void SetUpMocksForDotNetTestHost()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockEnvironment = new Mock<IEnvironment>();

            this.mockProcessHelper.Setup(
                    ph =>
                        ph.LaunchProcess(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<Action<object, string>>(),
                            It.IsAny<Action<object>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback) =>
                        {
                            var process = Process.GetCurrentProcess();

                            errorCallback(process, string.Empty);
                            exitCallback(process);
                        }).Returns(Process.GetCurrentProcess());
        }

        private class TestableProxyOperationManager : ProxyOperationManager
        {
            public TestableProxyOperationManager(
                IRequestData requestData,
                ITestRequestSender requestSender,
                ITestRuntimeProvider testHostManager,
                int clientConnectionTimeout) : base(requestData, requestSender, testHostManager, clientConnectionTimeout)
            {
            }
        }

        private class TestableDotnetTestHostManager : DotnetTestHostManager
        {
            private bool isVersionCheckRequired;

            public TestableDotnetTestHostManager(
                bool checkRequired,
                IProcessHelper processHelper,
                IFileHelper fileHelper,
                IEnvironment environment) : base(processHelper, fileHelper, new DotnetHostHelper(fileHelper, environment))
            {
                this.isVersionCheckRequired = checkRequired;
            }

            internal override bool IsVersionCheckRequired => this.isVersionCheckRequired;

            public override TestProcessStartInfo GetTestHostProcessStartInfo(
                IEnumerable<string> sources,
                IDictionary<string, string> environmentVariables,
                TestRunnerConnectionInfo connectionInfo)
            {
                return new TestProcessStartInfo();
            }
        }
    }
}
