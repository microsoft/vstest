// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.DesignMode
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class DesignModeClientTests
    {
        private const int Timeout = 15 * 1000;

        private const int PortNumber = 123;

        private readonly Mock<ITestRequestManager> mockTestRequestManager;

        private readonly Mock<ICommunicationManager> mockCommunicationManager;

        private readonly DesignModeClient designModeClient;

        private readonly int protocolVersion = 3;

        private readonly AutoResetEvent complateEvent;

        private readonly Mock<IEnvironment> mockPlatformEnvrironment;

        public DesignModeClientTests()
        {
            this.mockTestRequestManager = new Mock<ITestRequestManager>();
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.mockPlatformEnvrironment = new Mock<IEnvironment>();
            this.designModeClient = new DesignModeClient(this.mockCommunicationManager.Object, JsonDataSerializer.Instance, this.mockPlatformEnvrironment.Object);
            this.complateEvent = new AutoResetEvent(false);
        }

        [TestMethod]
        public void DesignModeClientBeforeConnectInstanceShouldReturnNull()
        {
            Assert.IsNull(DesignModeClient.Instance);
        }

        [TestMethod]
        public void DesignModeClientInitializeShouldInstantiateClassAndCreateClient()
        {
            DesignModeClient.Initialize();
            Assert.IsNotNull(DesignModeClient.Instance);
        }

        [TestMethod]
        public void TestRunMessageHandlerShouldCallCommmunicationManagerIfMessageisError()
        {
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>()));

            this.designModeClient.TestRunMessageHandler(new object(), new TestRunMessageEventArgs(TestMessageLevel.Error, "message"));

            this.mockCommunicationManager.Verify(cm => cm.SendMessage(It.IsAny<string>(),It.IsAny<TestMessagePayload>()), Times.Once());
        }

        [TestMethod]
        public void TestRunMessageHandlerShouldCallCommmunicationManagerIfMessageisWarning()
        {
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>()));

            this.designModeClient.TestRunMessageHandler(new object(), new TestRunMessageEventArgs(TestMessageLevel.Warning, "message"));

            this.mockCommunicationManager.Verify(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<TestMessagePayload>()), Times.Once());
        }

        [TestMethod]
        public void TestRunMessageHandlerShouldNotCallCommmunicationManagerIfMessageisInformational()
        {
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>()));

            this.designModeClient.TestRunMessageHandler(new object(), new TestRunMessageEventArgs(TestMessageLevel.Informational, "message"));

            this.mockCommunicationManager.Verify(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<TestMessagePayload>()), Times.Never());
        }

        [TestMethod]
        public void DesignModeClientConnectShouldSetupChannel()
        {
            var verCheck = new Message { MessageType = MessageType.VersionCheck, Payload = this.protocolVersion };
            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(verCheck).Returns(sessionEnd);

            this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object);

            this.mockCommunicationManager.Verify(cm => cm.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, PortNumber)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForServerConnection(It.IsAny<int>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.SessionConnected), Times.Once());
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once());
        }

        [TestMethod]
        public void DesignModeClientConnectShouldNotSendConnectedIfServerConnectionTimesOut()
        {
            var verCheck = new Message { MessageType = MessageType.VersionCheck, Payload = this.protocolVersion };
            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(false);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(verCheck).Returns(sessionEnd);

            Assert.ThrowsException<TimeoutException>(() => this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object));

            this.mockCommunicationManager.Verify(cm => cm.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, PortNumber)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForServerConnection(It.IsAny<int>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.SessionConnected), Times.Never);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void DesignModeClientDuringConnectShouldHighestCommonVersionWhenReceivedVersionIsGreaterThanSupportedVersion()
        {
            var verCheck = new Message { MessageType = MessageType.VersionCheck, Payload = 3 };
            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(verCheck).Returns(sessionEnd);

            this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object);

            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once());
        }

        [TestMethod]
        public void DesignModeClientDuringConnectShouldHighestCommonVersionWhenReceivedVersionIsSmallerThanSupportedVersion()
        {
            var verCheck = new Message { MessageType = MessageType.VersionCheck, Payload = 1 };
            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(verCheck).Returns(sessionEnd);

            this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object);

            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, 1), Times.Once());
        }

        [TestMethod]
        public void DesignModeClientWithGetTestRunnerProcessStartInfoShouldDeserializeTestsWithTraitsCorrectly()
        {
            // Arrange.
            var testCase = new TestCase("A.C.M", new Uri("d:\\executor"), "A.dll");
            testCase.Traits.Add(new Trait("foo", "bar"));

            var testList = new System.Collections.Generic.List<TestCase> { testCase };
            var testRunPayload = new TestRunRequestPayload { RunSettings = null, TestCases = testList };

            var getProcessStartInfoMessage = new Message
                                                 {
                                                     MessageType = MessageType.GetTestRunnerProcessStartInfoForRunSelected,
                                                     Payload = JToken.FromObject("random")
                                                 };

            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            TestRunRequestPayload receivedTestRunPayload = null;
            var allTasksComplete = new ManualResetEvent(false);

            // Setup mocks.
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.Setup(cm => cm.DeserializePayload<TestRunRequestPayload>(getProcessStartInfoMessage))
                .Returns(testRunPayload);

            this.mockTestRequestManager.Setup(
                    trm =>
                        trm.RunTests(
                            It.IsAny<TestRunRequestPayload>(),
                            It.IsAny<ITestHostLauncher>(),
                            It.IsAny<ITestRunEventsRegistrar>(),
                            It.IsAny<ProtocolConfig>()))
                .Callback(
                    (TestRunRequestPayload trp,
                     ITestHostLauncher testHostManager,
                     ITestRunEventsRegistrar testRunEventsRegistrar,
                     ProtocolConfig config) =>
                        {
                            receivedTestRunPayload = trp;
                            allTasksComplete.Set();
                        });

            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage())
                .Returns(getProcessStartInfoMessage)
                .Returns(sessionEnd);

            // Act.
            this.designModeClient.ConnectToClientAndProcessRequests(0, this.mockTestRequestManager.Object);

            // wait for the internal spawned of tasks to complete.
            Assert.IsTrue(allTasksComplete.WaitOne(1000), "Timed out waiting for mock request manager.");

            // Assert.
            Assert.IsNotNull(receivedTestRunPayload);
            Assert.IsNotNull(receivedTestRunPayload.TestCases);
            Assert.AreEqual(1, receivedTestRunPayload.TestCases.Count);

            // Validate traits
            var traits = receivedTestRunPayload.TestCases.ToArray()[0].Traits;
            Assert.AreEqual("foo", traits.ToArray()[0].Name);
            Assert.AreEqual("bar", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public void DesignModeClientWithRunSelectedTestCasesShouldDeserializeTestsWithTraitsCorrectly()
        {
            // Arrange.
            var testCase = new TestCase("A.C.M", new Uri("d:\\executor"), "A.dll");
            testCase.Traits.Add(new Trait("foo", "bar"));

            var testList = new System.Collections.Generic.List<TestCase> { testCase };
            var testRunPayload = new TestRunRequestPayload { RunSettings = null, TestCases = testList };

            var getProcessStartInfoMessage = new Message
                                                 {
                                                     MessageType = MessageType.TestRunSelectedTestCasesDefaultHost,
                                                     Payload = JToken.FromObject("random")
                                                 };

            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            TestRunRequestPayload receivedTestRunPayload = null;
            var allTasksComplete = new ManualResetEvent(false);

            // Setup mocks.
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.Setup(cm => cm.DeserializePayload<TestRunRequestPayload>(getProcessStartInfoMessage))
                .Returns(testRunPayload);
            this.mockTestRequestManager.Setup(
                trm =>
                trm.RunTests(
                    It.IsAny<TestRunRequestPayload>(),
                    It.IsAny<ITestHostLauncher>(),
                    It.IsAny<ITestRunEventsRegistrar>(),
                    It.IsAny<ProtocolConfig>()))
                .Callback(
                    (TestRunRequestPayload trp,
                     ITestHostLauncher testHostManager,
                     ITestRunEventsRegistrar testRunEventsRegistrar,
                     ProtocolConfig config) =>
                    {
                        receivedTestRunPayload = trp;
                        allTasksComplete.Set();
                    });
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage())
                .Returns(getProcessStartInfoMessage)
                .Returns(sessionEnd);

            // Act.
            this.designModeClient.ConnectToClientAndProcessRequests(0, this.mockTestRequestManager.Object);

            // wait for the internal spawned of tasks to complete.
            allTasksComplete.WaitOne(1000);

            // Assert.
            Assert.IsNotNull(receivedTestRunPayload);
            Assert.IsNotNull(receivedTestRunPayload.TestCases);
            Assert.AreEqual(1, receivedTestRunPayload.TestCases.Count);

            // Validate traits
            var traits = receivedTestRunPayload.TestCases.ToArray()[0].Traits;
            Assert.AreEqual("foo", traits.ToArray()[0].Name);
            Assert.AreEqual("bar", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public void DesignModeClientOnBadConnectionShouldStopServerAndThrowTimeoutException()
        {
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(false);

            var ex = Assert.ThrowsException<TimeoutException>(() => this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object));
            Assert.AreEqual("vstest.console process failed to connect to translation layer process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.", ex.Message);

            this.mockCommunicationManager.Verify(cm => cm.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, PortNumber)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForServerConnection(It.IsAny<int>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.StopClient(), Times.Once);
        }

        [TestMethod]
        public void DesignModeClientShouldStopCommunicationOnParentProcessExit()
        {
            this.mockPlatformEnvrironment.Setup(pe => pe.Exit(It.IsAny<int>()));
            this.designModeClient.HandleParentProcessExit();

            this.mockCommunicationManager.Verify(cm => cm.StopClient(), Times.Once);
        }

        [TestMethod]
        public void DesignModeClientLaunchCustomHostMustReturnIfAckComes()
        {
            var testableDesignModeClient = new TestableDesignModeClient(this.mockCommunicationManager.Object, JsonDataSerializer.Instance, this.mockPlatformEnvrironment.Object);

            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);

            var expectedProcessId = 1234;
            Action sendMessageAction = () =>
            {
                testableDesignModeClient.InvokeCustomHostLaunchAckCallback(expectedProcessId, null);
            };

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.CustomTestHostLaunch, It.IsAny<object>())).
                Callback(() => Task.Run(sendMessageAction));

            var info = new TestProcessStartInfo();
            var processId = testableDesignModeClient.LaunchCustomHost(info, CancellationToken.None);

            Assert.AreEqual(expectedProcessId, processId);
        }

        [TestMethod]
        [ExpectedException(typeof(TestPlatformException))]
        public void DesignModeClientLaunchCustomHostMustThrowIfInvalidAckComes()
        {
            var testableDesignModeClient = new TestableDesignModeClient(this.mockCommunicationManager.Object, JsonDataSerializer.Instance, this.mockPlatformEnvrironment.Object);

            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);

            var expectedProcessId = -1;
            Action sendMessageAction = () =>
            {
                testableDesignModeClient.InvokeCustomHostLaunchAckCallback(expectedProcessId, "Dummy");
            };

            this.mockCommunicationManager
                .Setup(cm => cm.SendMessage(MessageType.CustomTestHostLaunch, It.IsAny<object>()))
                .Callback(() => Task.Run(sendMessageAction));

            var info = new TestProcessStartInfo();
            testableDesignModeClient.LaunchCustomHost(info, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(TestPlatformException))]
        public void DesignModeClientLaunchCustomHostMustThrowIfCancellationOccursBeforeHostLaunch()
        {
            var testableDesignModeClient = new TestableDesignModeClient(this.mockCommunicationManager.Object, JsonDataSerializer.Instance, this.mockPlatformEnvrironment.Object);

            var info = new TestProcessStartInfo();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            testableDesignModeClient.LaunchCustomHost(info, cancellationTokenSource.Token);
        }

        [TestMethod]
        public void DesignModeClientConnectShouldSendTestMessageAndDiscoverCompleteOnExceptionInDiscovery()
        {
            var payload = new DiscoveryRequestPayload();
            var startDiscovery = new Message { MessageType = MessageType.StartDiscovery, Payload = JToken.FromObject(payload) };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(startDiscovery);
            this.mockCommunicationManager
                .Setup(cm => cm.SendMessage(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>()))
                .Callback(() => complateEvent.Set());
            this.mockTestRequestManager.Setup(
                    rm => rm.DiscoverTests(
                        It.IsAny<DiscoveryRequestPayload>(),
                        It.IsAny<ITestDiscoveryEventsRegistrar>(),
                        It.IsAny<ProtocolConfig>()))
                .Throws(new Exception());

            this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object);

            Assert.IsTrue(this.complateEvent.WaitOne(Timeout), "Discovery not completed.");
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestMessage, It.IsAny<TestMessagePayload>()), Times.Once());
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>()), Times.Once());
        }

        [TestMethod]
        public void DesignModeClientConnectShouldSendTestMessageAndDiscoverCompleteOnTestPlatformExceptionInDiscovery()
        {
            var payload = new DiscoveryRequestPayload();
            var startDiscovery = new Message { MessageType = MessageType.StartDiscovery, Payload = JToken.FromObject(payload) };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(startDiscovery);
            this.mockCommunicationManager
                .Setup(cm => cm.SendMessage(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>()))
                .Callback(() => complateEvent.Set());
            this.mockTestRequestManager.Setup(
                    rm => rm.DiscoverTests(
                        It.IsAny<DiscoveryRequestPayload>(),
                        It.IsAny<ITestDiscoveryEventsRegistrar>(),
                        It.IsAny<ProtocolConfig>()))
                .Throws(new TestPlatformException("Hello world"));

            this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object);

            Assert.IsTrue(this.complateEvent.WaitOne(Timeout), "Discovery not completed.");
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestMessage, It.IsAny<TestMessagePayload>()), Times.Once());
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>()), Times.Once());
        }

        [TestMethod]
        public void DesignModeClientConnectShouldSendTestMessageAndExecutionCompleteOnExceptionInTestRun()
        {
            var payload = new TestRunRequestPayload();
            var testRunAll = new Message { MessageType = MessageType.TestRunAllSourcesWithDefaultHost, Payload = JToken.FromObject(payload) };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(testRunAll);
            this.mockCommunicationManager
                .Setup(cm => cm.SendMessage(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>()))
                .Callback(() => this.complateEvent.Set());
            this.mockTestRequestManager.Setup(
                rm => rm.RunTests(
                    It.IsAny<TestRunRequestPayload>(),
                    null,
                    It.IsAny<DesignModeTestEventsRegistrar>(),
                It.IsAny<ProtocolConfig>())).Throws(new Exception());

            this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object);

            Assert.IsTrue(this.complateEvent.WaitOne(Timeout), "Execution not completed.");
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestMessage, It.IsAny<TestMessagePayload>()), Times.Once());
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>()), Times.Once());
        }

        [TestMethod]
        public void DesignModeClientConnectShouldSendTestMessageAndExecutionCompleteOnTestPlatformExceptionInTestRun()
        {
            var payload = new TestRunRequestPayload();
            var testRunAll = new Message { MessageType = MessageType.TestRunAllSourcesWithDefaultHost, Payload = JToken.FromObject(payload) };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(testRunAll);
            this.mockCommunicationManager
                .Setup(cm => cm.SendMessage(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>()))
                .Callback(() => this.complateEvent.Set());
            this.mockTestRequestManager.Setup(
                rm => rm.RunTests(
                    It.IsAny<TestRunRequestPayload>(),
                    null,
                    It.IsAny<DesignModeTestEventsRegistrar>(),
                It.IsAny<ProtocolConfig>())).Throws(new TestPlatformException("Hello world"));

            this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object);

            Assert.IsTrue(this.complateEvent.WaitOne(Timeout), "Execution not completed.");
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestMessage, It.IsAny<TestMessagePayload>()), Times.Once());
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>()), Times.Once());
        }

        [TestMethod]
        public void DesignModeClientSendTestMessageShouldSendTestMessage()
        {
            var testPayload = new TestMessagePayload { MessageLevel = ObjectModel.Logging.TestMessageLevel.Error, Message = "DummyMessage" };

            this.designModeClient.SendTestMessage(testPayload.MessageLevel, testPayload.Message);

            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestMessage, It.IsAny<TestMessagePayload>()), Times.Once());
        }

        private class TestableDesignModeClient : DesignModeClient
        {
            internal TestableDesignModeClient(
                ICommunicationManager communicationManager,
                IDataSerializer dataSerializer,
                IEnvironment platformEnvironment)
                : base(communicationManager, dataSerializer, platformEnvironment)
            {
            }

            public void InvokeCustomHostLaunchAckCallback(int processId, string errorMessage)
            {
                var payload = new CustomHostLaunchAckPayload()
                {
                    HostProcessId = processId,
                    ErrorMessage = errorMessage
                };
                this.onCustomTestHostLaunchAckReceived?.Invoke(
                    new Message() { MessageType = MessageType.CustomTestHostLaunchCallback, Payload = JToken.FromObject(payload) });
            }
        }
    }
}
