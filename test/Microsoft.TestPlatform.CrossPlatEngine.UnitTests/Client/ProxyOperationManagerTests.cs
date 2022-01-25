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

    [TestClass]
    public class ProxyOperationManagerTests : ProxyBaseManagerTests
    {
        private const int CLIENTPROCESSEXITWAIT = 10 * 1000;

        private readonly ProxyOperationManager testOperationManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        private Mock<IProcessHelper> mockProcessHelper;

        private Mock<IRunSettingsHelper> mockRunsettingHelper;

        private Mock<IWindowsRegistryHelper> mockWindowsRegistry;

        private Mock<IEnvironmentVariableHelper> mockEnvironmentVariableHelper;

        private Mock<IFileHelper> mockFileHelper;

        private Mock<IEnvironment> mockEnvironment;

        private readonly Mock<IRequestData> mockRequestData;

        private readonly int connectionTimeout = EnvironmentHelper.DefaultConnectionTimeout * 1000;

        private readonly string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

        private static readonly string TimoutErrorMessage =
            "vstest.console process failed to connect to testhost process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.";

        public ProxyOperationManagerTests()
        {
            mockRequestSender = new Mock<ITestRequestSender>();
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);
            mockRequestData = new Mock<IRequestData>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new Mock<IMetricsCollection>().Object);
            testOperationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object);
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
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            mockTestHostManager.Setup(
                    th => th.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(expectedStartInfo);

            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.Is<TestProcessStartInfo>(si => si == expectedStartInfo), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldCreateTimestampedLogFileForHost()
        {
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            EqtTrace.InitializeTrace("log.txt", PlatformTraceLevel.Verbose);

            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockTestHostManager.Verify(
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
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockTestHostManager.Verify(
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

            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockTestHostManager.Verify(
                th =>
                    th.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.Is<TestRunnerConnectionInfo>(t => t.TraceLevel == (int)PlatformTraceLevel.Info)));
        }

        [TestMethod]
        public void SetupChannelShouldSetupServerForCommunication()
        {
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
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
            ProtocolConfig protocolConfig = new() { Version = 2 };
            var mockCommunicationServer = new Mock<ICommunicationEndPoint>();

            mockCommunicationServer.Setup(mc => mc.Start(connectionInfo.Endpoint)).Returns(IPAddress.Loopback + ":123").Callback(
                () => mockCommunicationServer.Raise(s => s.Connected += null, mockCommunicationServer.Object, new ConnectedEventArgs(mockChannel.Object)));

            var testRequestSender = new TestRequestSender(mockCommunicationServer.Object, connectionInfo, mockDataSerializer.Object, protocolConfig, CLIENTPROCESSEXITWAIT);
            SetupChannelMessage(MessageType.VersionCheck, MessageType.VersionCheck, protocolConfig.Version);

            mockTestHostManager.Setup(thm => thm.GetTestHostConnectionInfo()).Returns(connectionInfo);

            var localTestOperationManager = new TestableProxyOperationManager(mockRequestData.Object, testRequestSender, mockTestHostManager.Object);

            localTestOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

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
            ProtocolConfig protocolConfig = new() { Version = 2 };
            var mockCommunicationEndpoint = new Mock<ICommunicationEndPoint>();
            mockCommunicationEndpoint.Setup(mc => mc.Start(connectionInfo.Endpoint)).Returns(connectionInfo.Endpoint).Callback(() => mockCommunicationEndpoint.Raise(
                    s => s.Connected += null,
                    mockCommunicationEndpoint.Object,
                    new ConnectedEventArgs(mockChannel.Object)));

            SetupChannelMessage(MessageType.VersionCheck, MessageType.VersionCheck, protocolConfig.Version);
            var testRequestSender = new TestRequestSender(mockCommunicationEndpoint.Object, connectionInfo, mockDataSerializer.Object, new ProtocolConfig { Version = 2 }, CLIENTPROCESSEXITWAIT);

            mockTestHostManager.Setup(thm => thm.GetTestHostConnectionInfo()).Returns(connectionInfo);

            var localTestOperationManager = new TestableProxyOperationManager(mockRequestData.Object, testRequestSender, mockTestHostManager.Object);

            localTestOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockCommunicationEndpoint.Verify(s => s.Start(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldNotInitializeIfConnectionIsAlreadyInitialized()
        {
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldWaitForTestHostConnection()
        {
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldNotWaitForTestHostConnectionIfConnectionIsInitialized()
        {
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [TestMethod]
        public void SetupChannelShouldHonorTimeOutSetByUser()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "100");

            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(100000, It.IsAny<CancellationToken>())).Returns(true);
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockRequestSender.Verify(rs => rs.WaitForRequestHandlerConnection(100000, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfWaitForTestHostConnectionTimesOut()
        {
            SetupTestHostLaunched(true);
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(false);

            var operationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object);

            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings)).Message;
            Assert.AreEqual(message, TimoutErrorMessage);
        }

        [TestMethod]
        public void SetupChannelShouldThrowTestPlatformExceptionIfRequestCancelled()
        {
            SetupTestHostLaunched(true);
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(false);

            var cancellationTokenSource = new CancellationTokenSource();
            var operationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object, cancellationTokenSource);

            cancellationTokenSource.Cancel();
            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings)).Message;
            Equals("Canceling the operation as requested.", message);
        }

        [TestMethod]
        public void SetupChannelShouldThrowTestPlatformExceptionIfRequestCancelledDuringLaunchOfTestHost()
        {
            SetupTestHostLaunched(true);
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(false);

            mockTestHostManager.Setup(rs => rs.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Callback(() => Task.Run(() => throw new OperationCanceledException()));

            var cancellationTokenSource = new CancellationTokenSource();
            var operationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object, cancellationTokenSource);

            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings)).Message;
            Equals("Canceling the operation as requested.", message);
        }

        [TestMethod]
        public void SetupChannelShouldThrowTestPlatformExceptionIfRequestCancelledPostHostLaunchDuringWaitForHandlerConnection()
        {
            SetupTestHostLaunched(true);
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(false);

            var cancellationTokenSource = new CancellationTokenSource();
            mockTestHostManager.Setup(rs => rs.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Callback(() => cancellationTokenSource.Cancel());
            var operationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object, cancellationTokenSource);

            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings)).Message;
            Equals("Canceling the operation as requested.", message);
        }

        [TestMethod]
        public void SetupChannelShouldThrowIfLaunchTestHostFails()
        {
            SetupTestHostLaunched(false);
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);

            var operationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object);

            var message = Assert.ThrowsException<TestPlatformException>(() => operationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings)).Message;
            Assert.AreEqual(message, string.Format(CultureInfo.CurrentUICulture, Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources.InitializationFailed));
        }

        [TestMethod]
        public void SetupChannelShouldCheckVersionWithTestHost()
        {
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);
            mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldThrowExceptionIfVersionCheckFails()
        {
            // Make the version check fail
            mockRequestSender.Setup(rs => rs.CheckVersionWithTestHost()).Throws(new TestPlatformException("Version check failed"));
            Assert.ThrowsException<TestPlatformException>(() => testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings));
        }

        [TestMethod]
        public void SetupChannelForDotnetHostManagerWithIsVersionCheckRequiredFalseShouldNotCheckVersionWithTestHost()
        {
            SetUpMocksForDotNetTestHost();
            var testHostManager = new TestableDotnetTestHostManager(false, mockProcessHelper.Object, mockFileHelper.Object, mockEnvironment.Object, mockRunsettingHelper.Object, mockWindowsRegistry.Object, mockEnvironmentVariableHelper.Object);
            var operationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, testHostManager);

            operationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Never);
        }

        [TestMethod]
        public void SetupChannelForDotnetHostManagerWithIsVersionCheckRequiredTrueShouldCheckVersionWithTestHost()
        {
            SetUpMocksForDotNetTestHost();
            var testHostManager = new TestableDotnetTestHostManager(true, mockProcessHelper.Object, mockFileHelper.Object, mockEnvironment.Object, mockRunsettingHelper.Object, mockWindowsRegistry.Object, mockEnvironmentVariableHelper.Object);
            var operationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, testHostManager);

            operationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            mockRequestSender.Verify(rs => rs.CheckVersionWithTestHost(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldEndSessionIfHostWasLaunched()
        {
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            testOperationManager.Close();

            mockRequestSender.Verify(rs => rs.EndSession(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldNotEndSessionIfHostLaucnhedFailed()
        {
            testOperationManager.Close();

            mockRequestSender.Verify(rs => rs.EndSession(), Times.Never);
        }

        [TestMethod]
        public void CloseShouldAlwaysCleanTestHost()
        {
            testOperationManager.Close();

            mockTestHostManager.Verify(th => th.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void CloseShouldResetChannelInitialization()
        {
            SetupWaitForTestHostExit();
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            testOperationManager.Close();

            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);
            mockTestHostManager.Verify(th => th.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public void CloseShouldTerminateTesthostProcessIfWaitTimesout()
        {
            // Ensure testhost start returns a dummy process id
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(connectionTimeout, It.IsAny<CancellationToken>())).Returns(true);
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            testOperationManager.Close();

            mockTestHostManager.Verify(th => th.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void CloseShouldNotThrowIfEndSessionFails()
        {
            mockRequestSender.Setup(rs => rs.EndSession()).Throws<Exception>();

            testOperationManager.Close();
        }

        private void SetupWaitForTestHostExit()
        {
            // Raise host exited when end session is called
            mockRequestSender.Setup(rs => rs.EndSession())
                .Callback(() => mockTestHostManager.Raise(t => t.HostExited += null, new HostProviderEventArgs(string.Empty)));
        }

        private void SetupTestHostLaunched(bool launchStatus)
        {
            // Raise host exited when end session is called
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockTestHostManager.Raise(t => t.HostLaunched += null, new HostProviderEventArgs(string.Empty)))
                .Returns(Task.FromResult(launchStatus));
        }

        [TestMethod]
        public void UpdateTestProcessStartInfoShouldUpdateTelemetryOptedInArgTrueIfTelemetryOptedIn()
        {
            TestProcessStartInfo receivedTestProcessInfo = new();
            var mockRequestData = new Mock<IRequestData>();

            mockRequestData.Setup(rd => rd.IsTelemetryOptedIn).Returns(true);

            var testOperationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object);

            mockTestHostManager
                .Setup(tm => tm.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback<TestProcessStartInfo, CancellationToken>(
                    (testProcessStartInfo, cancellationToken) => receivedTestProcessInfo = testProcessStartInfo)
                        .Returns(Task.FromResult(true));

            // Act.
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            // Verify.
            Assert.IsTrue(receivedTestProcessInfo.Arguments.Contains("--telemetryoptedin true"));
        }

        [TestMethod]
        public void UpdateTestProcessStartInfoShouldUpdateTelemetryOptedInArgFalseIfTelemetryOptedOut()
        {
            TestProcessStartInfo receivedTestProcessInfo = new();
            var mockRequestData = new Mock<IRequestData>();

            mockRequestData.Setup(rd => rd.IsTelemetryOptedIn).Returns(false);

            var testOperationManager = new TestableProxyOperationManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object);

            mockTestHostManager
                .Setup(tm => tm.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback<TestProcessStartInfo, CancellationToken>(
                    (testProcessStartInfo, cancellationToken) => receivedTestProcessInfo = testProcessStartInfo)
                .Returns(Task.FromResult(true));

            // Act.
            testOperationManager.SetupChannel(Enumerable.Empty<string>(), defaultRunSettings);

            // Verify.
            Assert.IsTrue(receivedTestProcessInfo.Arguments.Contains("--telemetryoptedin false"));
        }

        private void SetUpMocksForDotNetTestHost()
        {
            mockProcessHelper = new Mock<IProcessHelper>();
            mockFileHelper = new Mock<IFileHelper>();
            mockEnvironment = new Mock<IEnvironment>();
            mockRunsettingHelper = new Mock<IRunSettingsHelper>();
            mockWindowsRegistry = new Mock<IWindowsRegistryHelper>();
            mockEnvironmentVariableHelper = new Mock<IEnvironmentVariableHelper>();

            mockRunsettingHelper.SetupGet(r => r.IsDefaultTargetArchitecture).Returns(true);
            mockProcessHelper.Setup(
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
                CancellationTokenSource = cancellationTokenSource;
            }
        }

        private class TestableDotnetTestHostManager : DotnetTestHostManager
        {
            private readonly bool isVersionCheckRequired;

            public TestableDotnetTestHostManager(
                bool checkRequired,
                IProcessHelper processHelper,
                IFileHelper fileHelper,
                IEnvironment environment,
                IRunSettingsHelper runsettingHelper,
                IWindowsRegistryHelper windowsRegistryHelper,
                IEnvironmentVariableHelper environmentVariableHelper) : base(
                    processHelper,
                    fileHelper,
                    new DotnetHostHelper(fileHelper, environment, windowsRegistryHelper, environmentVariableHelper, processHelper),
                    environment,
                    runsettingHelper,
                    windowsRegistryHelper,
                    environmentVariableHelper)
            {
                isVersionCheckRequired = checkRequired;
            }

            internal override bool IsVersionCheckRequired => isVersionCheckRequired;

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
