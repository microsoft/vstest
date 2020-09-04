// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
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
    using Constants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;

    [TestClass]
    public class ProxyOperationManagerTests : ProxyBaseManagerTests
    {
        private const int CLIENTPROCESSEXITWAIT = 10 * 1000;

        private readonly ProxyOperationManager testOperationManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        private Mock<IProcessHelper> mockProcessHelper;

        private Mock<IFileHelper> mockFileHelper;

        private Mock<IEnvironment> mockEnvironment;

        private Mock<IRequestData> mockRequestData;

        private int connectionTimeout = EnvironmentHelper.DefaultConnectionTimeout * 1000;

        private string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

        private static readonly string TimoutErrorMessage =
            "vstest.console process failed to connect to testhost process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.";

        public ProxyOperationManagerTests()
        {
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new Mock<IMetricsCollection>().Object);
            this.testOperationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, string.Empty);
        }

        [TestMethod]
        public void SetupChannelShouldLaunchTestHost()
        {
            var expectedStartInfo = new TestProcessStartInfo();
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            this.mockTestHostManager.Setup(
                    th => th.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(expectedStartInfo);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.Is<TestProcessStartInfo>(si => si == expectedStartInfo), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldCreateTimestampedLogFileForHost()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            EqtTrace.InitializeTrace("log.txt", PlatformTraceLevel.Verbose);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockTestHostManager.Verify(
                th =>
                    th.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.Is<TestRunnerConnectionInfo>(
                            t => t.LogFile.Contains("log.host." + DateTime.Now.ToString("yy-MM-dd"))
                                 && t.LogFile.Contains("_" + Thread.CurrentThread.ManagedThreadId + ".txt"))));
#if NETFRAMEWORK
            EqtTrace.TraceLevel = TraceLevel.Off;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Off;
#endif
        }

        [TestMethod]
        public void SetupChannelShouldAddRunnerProcessIdForTestHost()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockTestHostManager.Verify(
                th =>
                    th.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.Is<TestRunnerConnectionInfo>(t => t.RunnerProcessId.Equals(Process.GetCurrentProcess().Id))));
        }

        [TestMethod]
        public void SetupChannelShouldAddCorrectTraceLevelForTestHost()
        {
#if NETFRAMEWORK
            EqtTrace.TraceLevel = TraceLevel.Info;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Info;
#endif

            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockTestHostManager.Verify(
                th =>
                    th.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.Is<TestRunnerConnectionInfo>(t => t.TraceLevel == (int)PlatformTraceLevel.Info)));
        }

        [TestMethod]
        public void SetupChannelShouldSetupServerForCommunication()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldCallHostServerIfRunnerIsServer()
        {
            var connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":0",
                Role = ConnectionRole.Host,
                Transport = Transport.Sockets
            };
            ProtocolConfig protocolConfig = new ProtocolConfig { Version = 2 };
            var mockCommunicationServer = new Mock<ICommunicationEndPoint>();

            mockCommunicationServer.Setup(mc => mc.Start(connectionInfo.Endpoint)).Returns(IPAddress.Loopback + ":123").Callback(
                () => { mockCommunicationServer.Raise(s => s.Connected += null, mockCommunicationServer.Object, new ConnectedEventArgs(this.mockChannel.Object)); });

            var testRequestSender = new TestRequestSender(mockCommunicationServer.Object, connectionInfo, mockDataSerializer.Object, protocolConfig, CLIENTPROCESSEXITWAIT);
            this.SetupChannelMessage(MessageType.VersionCheck, MessageType.VersionCheck, protocolConfig.Version);

            this.mockTestHostManager.Setup(thm => thm.GetTestHostConnectionInfo()).Returns(connectionInfo);

            var localTestOperationManager = new TestableProxyOperationManager(this.mockRequestData.Object, testRequestSender, this.mockTestHostManager.Object);

            localTestOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            mockCommunicationServer.Verify(s => s.Start(IPAddress.Loopback.ToString() + ":0"), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldCallSetupClientIfRunnerIsClient()
        {
            var connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":124",
                Role = ConnectionRole.Host,
                Transport = Transport.Sockets
            };
            ProtocolConfig protocolConfig = new ProtocolConfig { Version = 2 };
            var mockCommunicationEndpoint = new Mock<ICommunicationEndPoint>();
            mockCommunicationEndpoint.Setup(mc => mc.Start(connectionInfo.Endpoint)).Returns(connectionInfo.Endpoint).Callback(() =>
            {
                mockCommunicationEndpoint.Raise(
                    s => s.Connected += null,
                    mockCommunicationEndpoint.Object,
                    new ConnectedEventArgs(this.mockChannel.Object));
            });

            this.SetupChannelMessage(MessageType.VersionCheck, MessageType.VersionCheck, protocolConfig.Version);
            var testRequestSender = new TestRequestSender(mockCommunicationEndpoint.Object, connectionInfo, mockDataSerializer.Object, new ProtocolConfig { Version = 2 }, CLIENTPROCESSEXITWAIT);

            this.mockTestHostManager.Setup(thm => thm.GetTestHostConnectionInfo()).Returns(connectionInfo);

            var localTestOperationManager = new TestableProxyOperationManager(this.mockRequestData.Object, testRequestSender, this.mockTestHostManager.Object);

            localTestOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            mockCommunicationEndpoint.Verify(s => s.Start(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldNotInitializeIfConnectionIsAlreadyInitialized()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldWaitForTestHostConnection()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldNotWaitForTestHostConnectionIfConnectionIsInitialized()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [TestMethod]
        public void SetupChannelShouldHonorTimeOutSetByUser()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "100");

            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(100000, It.IsAny<CancellationToken>())).Returns(true);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(100000, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfWaitForTestHostConnectionTimesOut()
        {
            SetupTestHostLaunched(true);
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>())).Returns(false);

            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object);

            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings)).Message;
            Assert.AreEqual(message, ProxyOperationManagerTests.TimoutErrorMessage);
        }

        [TestMethod]
        public void SetupChannelShouldThrowTestPlatformExceptionIfRequestCancelled()
        {
            SetupTestHostLaunched(true);
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>())).Returns(false);

            var cancellationTokenSource = new CancellationTokenSource();
            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, cancellationTokenSource);

            cancellationTokenSource.Cancel();
            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings)).Message;
            StringAssert.Equals("Canceling the operation as requested.", message);
        }

        [TestMethod]
        public void SetupChannelShouldThrowTestPlatformExceptionIfRequestCancelledDuringLaunchOfTestHost()
        {
            SetupTestHostLaunched(true);
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>())).Returns(false);

            this.mockTestHostManager.Setup(rs => rs.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Callback(() => Task.Run(() => { throw new OperationCanceledException(); }));

            var cancellationTokenSource = new CancellationTokenSource();
            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, cancellationTokenSource);

            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings)).Message;
            StringAssert.Equals("Canceling the operation as requested.", message);
        }

        [TestMethod]
        public void SetupChannelShouldThrowTestPlatformExceptionIfRequestCancelledPostHostLaunchDuringWaitForHandlerConnection()
        {
            SetupTestHostLaunched(true);
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>())).Returns(false);

            var cancellationTokenSource = new CancellationTokenSource();
            this.mockTestHostManager.Setup(rs => rs.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Callback(() => cancellationTokenSource.Cancel());
            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, cancellationTokenSource);

            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings)).Message;
            StringAssert.Equals("Canceling the operation as requested.", message);
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfLaunchTestHostFails()
        {
            SetupTestHostLaunched(false);
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);

            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object);

            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings)).Message;
            Assert.AreEqual(message, string.Format(CultureInfo.CurrentUICulture, Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources.InitializationFailed));
        }

        [TestMethod]
        public void SetupChannelShouldCheckVersionWithTestHost()
        {
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);
            this.mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldThrowExceptionIfVersionCheckFails()
        {
            // Make the version check fail
            this.mockRequestSender.Setup(rs => rs.CheckVersionWithTestHost()).Throws(new TestPlatformException("Version check failed"));
            Assert.ThrowsException<TestPlatformException>(() => this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings));
        }

        [TestMethod]
        public void SetupChannelForDotnetHostManagerWithIsVersionCheckRequiredFalseShouldNotCheckVersionWithTestHost()
        {
            this.SetUpMocksForDotNetTestHost();
            var testHostManager = new TestableDotnetTestHostManager(false, this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockEnvironment.Object);
            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, testHostManager);

            operationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Never);
        }

        [TestMethod]
        public void SetupChannelForDotnetHostManagerWithIsVersionCheckRequiredTrueShouldCheckVersionWithTestHost()
        {
            this.SetUpMocksForDotNetTestHost();
            var testHostManager = new TestableDotnetTestHostManager(true, this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockEnvironment.Object);
            var operationManager = new TestableProxyOperationManager(this.mockRequestData.Object, this.mockRequestSender.Object, testHostManager);

            operationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldEndSessionIfHostWasLaunched()
        {
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

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
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.testOperationManager.Close();

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);
            this.mockTestHostManager.Verify(th => th.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public void CloseShouldTerminateTesthostProcessIfWaitTimesout()
        {
            // Ensure testhost start returns a dummy process id
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);
            this.testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            this.testOperationManager.Close();

            this.mockTestHostManager.Verify(th => th.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Once);
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

            mockRequestData.Setup(rd => rd.IsTelemetryOptedIn).Returns(true);

            var testOperationManager = new TestableProxyOperationManager(mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object);

            this.mockTestHostManager
                .Setup(tm => tm.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback<TestProcessStartInfo, CancellationToken>(
                    (testProcessStartInfo, cancellationToken) =>
                        {
                            receivedTestProcessInfo = testProcessStartInfo;
                        })
                        .Returns(Task.FromResult(true));

            // Act.
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

            // Verify.
            Assert.IsTrue(receivedTestProcessInfo.Arguments.Contains("--telemetryoptedin true"));
        }

        [TestMethod]
        public void UpdateTestProcessStartInfoShouldUpdateTelemetryOptedInArgFalseIfTelemetryOptedOut()
        {
            TestProcessStartInfo receivedTestProcessInfo = new TestProcessStartInfo();
            var mockRequestData = new Mock<IRequestData>();

            mockRequestData.Setup(rd => rd.IsTelemetryOptedIn).Returns(false);

            var testOperationManager = new TestableProxyOperationManager(mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object);

            this.mockTestHostManager
                .Setup(tm => tm.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback<TestProcessStartInfo, CancellationToken>(
                    (testProcessStartInfo, cancellationToken) =>
                        {
                            receivedTestProcessInfo = testProcessStartInfo;
                        })
                .Returns(Task.FromResult(true));

            // Act.
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), this.defaultRunSettings);

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
                            It.IsAny<Action<object>>(),
                            It.IsAny<Action<object, string>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>, Action<object, string>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback, outputCallback) =>
                        {
                            var process = Process.GetCurrentProcess();

                            errorCallback(process, string.Empty);
                            exitCallback(process);
                        }).Returns(Process.GetCurrentProcess());
        }

        private class TestableProxyOperationManager : ProxyOperationManager
        {
            public TestableProxyOperationManager(IRequestData requestData,
                ITestRequestSender requestSender,
                ITestRuntimeProvider testHostManager) : base(requestData, requestSender, testHostManager)
            {
            }

            public TestableProxyOperationManager(
                IRequestData requestData,
                ITestRequestSender requestSender,
                ITestRuntimeProvider testHostManager,
                CancellationTokenSource cancellationTokenSource) : base(requestData, requestSender, testHostManager)
            {
                this.CancellationTokenSource = cancellationTokenSource;
            }
        }

        private class TestableDotnetTestHostManager : DotnetTestHostManager
        {
            private bool isVersionCheckRequired;

            public TestableDotnetTestHostManager(
                bool checkRequired,
                IProcessHelper processHelper,
                IFileHelper fileHelper,
                IEnvironment environment) : base(processHelper, fileHelper, new DotnetHostHelper(fileHelper, environment), environment)
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
