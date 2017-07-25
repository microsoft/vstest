// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.TestRunnerConnectionInfo;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;

    [TestClass]
    public class TestRequestSenderTests
    {
        private ITestRequestSender testRequestSender;
        private Mock<ICommunicationManager> mockCommunicationManager;
        private Mock<IDataSerializer> mockDataSerializer;
        private ProtocolConfig protocolConfig = new ProtocolConfig { Version = 2 };
        private ConnectionInfo connectionInfo;

        [TestInitialize]
        public void TestInit()
        {
            this.connectionInfo = new ConnectionInfo
                                      {
                                          Endpoint = IPAddress.Loopback + ":0",
                                          Role = ConnectionRole.Client,
                                          Channel = TransportChannel.Sockets
                                      };
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.testRequestSender = new TestRequestSender(this.mockCommunicationManager.Object, this.connectionInfo, this.mockDataSerializer.Object, this.protocolConfig);
            this.CheckAndSetProtocolVersion();
        }

        [TestMethod]
        public void InitializeCommunicationShouldHostServerAndAcceptClient()
        {
            this.mockCommunicationManager.Setup(mc => mc.HostServer(IPAddress.Loopback + ":0")).Returns(123);

            var port = this.testRequestSender.InitializeCommunication();

            this.mockCommunicationManager.Verify(mc => mc.HostServer(IPAddress.Loopback + ":0"), Times.Once);
            this.mockCommunicationManager.Verify(mc => mc.AcceptClientAsync(), Times.Once);
            Assert.AreEqual(port, 123, "Correct port must be returned.");
        }

        [TestMethod]
        public void InitializeCommunicationShouldSetUpClientIfTestRunnerIsClient()
        {
            this.mockCommunicationManager.Setup(mc => mc.SetupClientAsync(IPAddress.Loopback + ":0"));

            // These settings are that of Test runtime(testhost)
            this.connectionInfo = new ConnectionInfo
                                      {
                                          Endpoint = IPAddress.Loopback + ":0",
                                          Role = ConnectionRole.Host,
                                          Channel = TransportChannel.Sockets
                                      };
            this.testRequestSender = new TestRequestSender(this.mockCommunicationManager.Object, this.connectionInfo, this.mockDataSerializer.Object, this.protocolConfig);
            this.CheckAndSetProtocolVersion();

            this.testRequestSender.InitializeCommunication();

            this.mockCommunicationManager.Verify(mc => mc.SetupClientAsync(IPAddress.Loopback + ":0"), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldCallWaitForClientConnection()
        {
            this.testRequestSender.WaitForRequestHandlerConnection(123);

            this.mockCommunicationManager.Verify(mc => mc.WaitForClientConnection(123), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldCallWaitForServerConnectionIfTestRunnerIsClient()
        {
            // These settings are that of Test runtime(testhost)
            this.connectionInfo = new ConnectionInfo
                                      {
                                          Endpoint = IPAddress.Loopback + ":0",
                                          Role = ConnectionRole.Host,
                                          Channel = TransportChannel.Sockets
                                      };

            this.testRequestSender = new TestRequestSender(this.mockCommunicationManager.Object, this.connectionInfo, this.mockDataSerializer.Object, this.protocolConfig);
            this.CheckAndSetProtocolVersion();

            this.testRequestSender.WaitForRequestHandlerConnection(123);

            this.mockCommunicationManager.Verify(mc => mc.WaitForServerConnection(123), Times.Once);
        }

        [TestMethod]
        public void CloseShouldCallStopServerOnCommunicationManager()
        {
            this.testRequestSender.Close();

            this.mockCommunicationManager.Verify(mc => mc.StopServer(), Times.Once);
        }

        [TestMethod]
        public void DisposeShouldCallStopServerOnCommunicationManager()
        {
            this.testRequestSender.Dispose();

            this.mockCommunicationManager.Verify(mc => mc.StopServer(), Times.Once);
        }

        [TestMethod]
        public void VersionCheckWithTestHostShouldCheckVersionIfVersionCheckPassesReturnTrue()
        {
            var mockCommunicationManager = new Mock<ICommunicationManager>();
            var message = new Message() { MessageType = MessageType.VersionCheck, Payload = this.protocolConfig.Version };
            mockCommunicationManager.Setup(mc => mc.ReceiveMessage()).Returns(message);
            var testRequestSender = new TestRequestSender(mockCommunicationManager.Object, default(ConnectionInfo),  this.mockDataSerializer.Object, this.protocolConfig);

            testRequestSender.CheckVersionWithTestHost();

            mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.VersionCheck, this.protocolConfig.Version), Times.Once);
        }

        [TestMethod]
        public void VersionCheckWithTestHostShouldBeAbleToReceiveProtocolErrorAndThrowException()
        {
            var mockCommunicationManager = new Mock<ICommunicationManager>();
            var message = new Message() { MessageType = MessageType.ProtocolError, Payload = null };
            mockCommunicationManager.Setup(mc => mc.ReceiveMessage()).Returns(message);
            var testRequestSender = new TestRequestSender(mockCommunicationManager.Object, default(ConnectionInfo),  this.mockDataSerializer.Object, this.protocolConfig);

            var ex = Assert.ThrowsException<TestPlatformException>(() => testRequestSender.CheckVersionWithTestHost());

            Assert.AreEqual("Protocol version check failed. Make sure test runner and host are compatible.", ex.Message);
        }

        [TestMethod]
        public void VersionCheckWithTestHostForInvalidMessageShouldThrowException()
        {
            var mockCommunicationManager = new Mock<ICommunicationManager>();
            var message = new Message() { MessageType = MessageType.TestCasesFound, Payload = null };
            mockCommunicationManager.Setup(mc => mc.ReceiveMessage()).Returns(message);
            var testRequestSender = new TestRequestSender(mockCommunicationManager.Object, default(ConnectionInfo),  this.mockDataSerializer.Object, this.protocolConfig);

            var ex = Assert.ThrowsException<TestPlatformException>(() => testRequestSender.CheckVersionWithTestHost());

            Assert.AreEqual("Unexpected message received. Expected MessageType : ProtocolVersion Actual MessageType: TestDiscovery.TestFound", ex.Message);
        }

        [TestMethod]
        public void InitializeDiscoveryShouldSendCommunicationMessageWithCorrectParameters()
        {
            var paths = new List<string>() { "Hello", "World" };
            this.CheckAndSetProtocolVersion();
            this.testRequestSender.InitializeDiscovery(paths, false);

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.DiscoveryInitialize, paths, this.protocolConfig.Version), Times.Once);
        }

        [TestMethod]
        public void InitializeExecutionShouldSendCommunicationMessageWithCorrectParameters()
        {
            var paths = new List<string>() { "Hello", "World" };
            this.testRequestSender.InitializeExecution(paths, true);

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.ExecutionInitialize, paths, this.protocolConfig.Version), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCallHandleDiscoveredTestsOnTestCaseEvent()
        {
            var sources = new List<string>() { "Hello", "World" };
            string settingsXml = "SettingsXml";
            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();
            var discoveryCriteria = new DiscoveryCriteria(sources, 100, settingsXml);

            var testCases = new List<TestCase>() { new TestCase("x.y.z", new Uri("x://y"), "x.dll") };
            var rawMessage = "OnDiscoveredTests";
            var message = new Message() { MessageType = MessageType.TestCasesFound, Payload = null };

            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<IEnumerable<TestCase>>(message)).Returns(testCases);

            var completePayload = new DiscoveryCompletePayload()
            {
                IsAborted = false,
                LastDiscoveredTests = null,
                TotalTests = 1
            };
            var completeMessage = new Message() { MessageType = MessageType.DiscoveryComplete, Payload = null };
            mockHandler.Setup(mh => mh.HandleDiscoveredTests(testCases)).Callback(
                () =>
                {
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<DiscoveryCompletePayload>(completeMessage)).Returns(completePayload);
                });

            this.testRequestSender.DiscoverTests(discoveryCriteria, mockHandler.Object);

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.StartDiscovery, discoveryCriteria, this.protocolConfig.Version), Times.Once);
            this.mockDataSerializer.Verify(ds => ds.DeserializeMessage(rawMessage), Times.Exactly(2));
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(testCases), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(rawMessage), Times.Exactly(2));
        }

        [TestMethod]
        public void DiscoverTestsShouldCallHandleLogMessageOnTestMessage()
        {
            var sources = new List<string>() { "Hello", "World" };
            string settingsXml = "SettingsXml";
            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();
            var discoveryCriteria = new DiscoveryCriteria(sources, 100, settingsXml);

            var rawMessage = "TestMessage";
            var messagePayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Error, Message = rawMessage };
            var message = new Message() { MessageType = MessageType.TestMessage, Payload = null };

            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestMessagePayload>(message)).Returns(messagePayload);

            var completePayload = new DiscoveryCompletePayload()
            {
                IsAborted = false,
                LastDiscoveredTests = null,
                TotalTests = 1
            };
            var completeMessage = new Message() { MessageType = MessageType.DiscoveryComplete, Payload = null };
            mockHandler.Setup(mh => mh.HandleLogMessage(TestMessageLevel.Error, rawMessage)).Callback(
                () =>
                {
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<DiscoveryCompletePayload>(completeMessage)).Returns(completePayload);
                });

            this.testRequestSender.DiscoverTests(discoveryCriteria, mockHandler.Object);

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.StartDiscovery, discoveryCriteria, this.protocolConfig.Version), Times.Once);
            this.mockDataSerializer.Verify(ds => ds.DeserializeMessage(rawMessage), Times.Exactly(2));
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, rawMessage), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(rawMessage), Times.Exactly(2));
        }

        [TestMethod]
        public void DiscoverTestsShouldCallHandleDiscoveryCompleteOnDiscoveryCompletion()
        {
            var sources = new List<string>() { "Hello", "World" };
            string settingsXml = "SettingsXml";
            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();
            var discoveryCriteria = new DiscoveryCriteria(sources, 100, settingsXml);

            var rawMessage = "RunComplete";
            var completePayload = new DiscoveryCompletePayload()
            {
                IsAborted = false,
                LastDiscoveredTests = null,
                TotalTests = 1
            };
            var message = new Message() { MessageType = MessageType.DiscoveryComplete, Payload = null };

            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<DiscoveryCompletePayload>(message)).Returns(completePayload);

            this.testRequestSender.DiscoverTests(discoveryCriteria, mockHandler.Object);

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.StartDiscovery, discoveryCriteria, this.protocolConfig.Version), Times.Once);
            this.mockDataSerializer.Verify(ds => ds.DeserializeMessage(rawMessage), Times.Once);
            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(1, null, false), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(rawMessage), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldHandleExceptionOnSendMessage()
        {
            var sources = new List<string>() { "Hello", "World" };
            string settingsXml = "SettingsXml";
            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();
            var discoveryCriteria = new DiscoveryCriteria(sources, 100, settingsXml);
            var exception = new Exception();
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.StartDiscovery, discoveryCriteria, this.protocolConfig.Version))
                .Throws(exception);

            this.testRequestSender.DiscoverTests(discoveryCriteria, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(-1, null, true), Times.Once);
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(It.IsAny<string>()), Times.Exactly(2));
        }

        [TestMethod]
        public void DiscoverTestsShouldHandleDiscoveryCompleteOnCommunicationFailure()
        {
            this.DiscoverTestsErrorScenarioTestTemplates(CommunicationUtilitiesResources.UnableToCommunicateToTestHost, (s) => { });
        }

        [TestMethod]
        public void DiscoverTestsShouldHandleDiscoveryCompleteOnProcessExit()
        {
            this.DiscoverTestsErrorScenarioTestTemplates("Error Message", (s) => this.testRequestSender.OnClientProcessExit(s));
        }

        public void DiscoverTestsErrorScenarioTestTemplates(string errorMessage, Action<string> exitCallback)
        {
            var sources = new List<string>() { "Hello", "World" };
            string settingsXml = "SettingsXml";
            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();
            var discoveryCriteria = new DiscoveryCriteria(sources, 100, settingsXml);
            this.mockCommunicationManager.Setup(cm => cm.ReceiveRawMessageAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((string)null))
                .Callback(() => exitCallback(errorMessage));

            this.testRequestSender.InitializeCommunication();
            this.testRequestSender.DiscoverTests(discoveryCriteria, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(-1, null, true), Times.Once);
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, string.Format(CommunicationUtilitiesResources.AbortedTestDiscovery, errorMessage)), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(It.IsAny<string>()), Times.Exactly(2));
        }

        [TestMethod]
        public void StartTestRunWithSourcesShouldCallHandleTestRunStatsChange()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var runCriteria = new TestRunCriteriaWithSources(null, null, null);

            var testRunChangedArgs = new TestRunChangedEventArgs(null, null, null);
            var rawMessage = "OnTestRunStatsChange";
            var message = new Message() { MessageType = MessageType.TestRunStatsChange, Payload = null };

            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunChangedEventArgs>(message)).Returns(testRunChangedArgs);

            var completePayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = null
            };
            var completeMessage = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(testRunChangedArgs)).Callback(
                () =>
                {
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(completeMessage)).Returns(completePayload);
                });

            var waitHandle = new AutoResetEvent(false);
            mockHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback(() => waitHandle.Set());

            this.testRequestSender.StartTestRun(runCriteria, mockHandler.Object);

            waitHandle.WaitOne();

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.StartTestExecutionWithSources, runCriteria, this.protocolConfig.Version), Times.Once);

            // One for run stats and another for runcomplete
            this.mockDataSerializer.Verify(ds => ds.DeserializeMessage(rawMessage), Times.Exactly(2));
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(testRunChangedArgs), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(rawMessage), Times.Exactly(2));
        }

        [TestMethod]
        public void StartTestRunWithTestsShouldCallHandleTestRunStatsChange()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var runCriteria = new TestRunCriteriaWithTests(null, null, null);

            var testRunChangedArgs = new TestRunChangedEventArgs(null, null, null);
            var rawMessage = "OnTestRunStatsChange";
            var message = new Message() { MessageType = MessageType.TestRunStatsChange, Payload = null };

            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunChangedEventArgs>(message)).Returns(testRunChangedArgs);

            var completePayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = null
            };
            var completeMessage = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            var waitHandle = new AutoResetEvent(false);
            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(testRunChangedArgs)).Callback(
                () =>
                {
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(completeMessage)).Returns(completePayload);
                    waitHandle.Set();
                });

            this.testRequestSender.StartTestRun(runCriteria, mockHandler.Object);

            waitHandle.WaitOne();
            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.StartTestExecutionWithTests, runCriteria, this.protocolConfig.Version), Times.Once);
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(testRunChangedArgs), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(rawMessage), Times.AtLeastOnce);
        }

        [TestMethod]
        public void StartTestRunShouldCallHandleLogMessageOnTestMessage()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var runCriteria = new TestRunCriteriaWithSources(null, null, null);

            var rawMessage = "OnLogMessage";
            var message = new Message() { MessageType = MessageType.TestMessage, Payload = null };
            var payload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Error, Message = rawMessage };

            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestMessagePayload>(message)).Returns(payload);

            var completePayload = new TestRunCompletePayload()
            {
                ExecutorUris = null, LastRunTests = null, RunAttachments = null, TestRunCompleteArgs = null
            };
            var completeMessage = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            var waitHandle = new AutoResetEvent(false);
            mockHandler.Setup(mh => mh.HandleLogMessage(TestMessageLevel.Error, rawMessage)).Callback(
                () =>
                {
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(completeMessage)).Returns(completePayload);
                    waitHandle.Set();
                });

            this.testRequestSender.StartTestRun(runCriteria, mockHandler.Object);
            waitHandle.WaitOne();

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.StartTestExecutionWithSources, runCriteria, this.protocolConfig.Version), Times.Once);
            this.mockDataSerializer.Verify(ds => ds.DeserializeMessage(It.IsAny<string>()), Times.Exactly(2));
            mockHandler.Verify(mh => mh.HandleLogMessage(payload.MessageLevel, payload.Message), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(rawMessage), Times.AtLeastOnce);
        }

        [TestMethod]
        public void StartTestRunShouldCallLaunchProcessWithDebuggerAndWaitForCallback()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var runCriteria = new TestRunCriteriaWithSources(null, null, null);

            var rawMessage = "LaunchProcessWithDebugger";
            var message = new Message() { MessageType = MessageType.LaunchAdapterProcessWithDebuggerAttached, Payload = null };
            var payload = new TestProcessStartInfo();

            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestProcessStartInfo>(message)).Returns(payload);

            var completePayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = null
            };
            var completeMessage = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            mockHandler.Setup(mh => mh.LaunchProcessWithDebuggerAttached(payload)).Callback(
                () =>
                {
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(completeMessage)).Returns(completePayload);
                });

            var waitHandle = new AutoResetEvent(false);
            mockHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback(() => waitHandle.Set());

            this.testRequestSender.StartTestRun(runCriteria, mockHandler.Object);

            waitHandle.WaitOne();

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.StartTestExecutionWithSources, runCriteria, this.protocolConfig.Version), Times.Once);

            this.mockDataSerializer.Verify(ds => ds.DeserializeMessage(It.IsAny<string>()), Times.Exactly(2));
            mockHandler.Verify(mh => mh.LaunchProcessWithDebuggerAttached(payload), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(rawMessage), Times.Exactly(2));
            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, It.IsAny<object>(), this.protocolConfig.Version), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCallHandleTestRunCompleteOnRunCompletion()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var runCriteria = new TestRunCriteriaWithTests(null, null, null);

            var rawMessage = "ExecComplete";
            var message = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            var payload = new TestRunCompletePayload() { ExecutorUris = null, LastRunTests = null, RunAttachments = null, TestRunCompleteArgs = null };

            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(message)).Returns(payload);

            var waitHandle = new AutoResetEvent(false);
            mockHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback(() => waitHandle.Set());

            this.testRequestSender.StartTestRun(runCriteria, mockHandler.Object);

            waitHandle.WaitOne();

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.StartTestExecutionWithTests, runCriteria, this.protocolConfig.Version), Times.Once);
            this.mockDataSerializer.Verify(ds => ds.DeserializeMessage(rawMessage), Times.Once);
            mockHandler.Verify(mh => mh.HandleTestRunComplete(null, null, null, null), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(rawMessage), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCallHandleTestRunCompleteAndHandleLogMessageOnConnectionBreak()
        {
            this.StartTestRunErrorTestsTemplate(CommunicationUtilitiesResources.UnableToCommunicateToTestHost, (s) => { });
        }

        [TestMethod]
        public void StartTestRunShouldCallHandleTestRunCompleteAndHandleLogMessageOnProcessExit()
        {
            this.StartTestRunErrorTestsTemplate("Error Message", (s) => this.testRequestSender.OnClientProcessExit(s));
        }

        [TestMethod]
        public void EndSessionShouldSendCorrectEventMessage()
        {
            this.testRequestSender.EndSession();

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.SessionEnd), Times.Once);
        }

        [TestMethod]
        public void CancelTestRunSessionShouldSendCorrectEventMessage()
        {
            this.testRequestSender.SendTestRunCancel();

            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.CancelTestRun), Times.Once);
        }

        private void CheckAndSetProtocolVersion()
        {
            var message = new Message() { MessageType = MessageType.VersionCheck, Payload = this.protocolConfig.Version };
            this.mockCommunicationManager.Setup(mc => mc.ReceiveMessage()).Returns(message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<int>(It.IsAny<Message>())).Returns(this.protocolConfig.Version);
            this.testRequestSender.CheckVersionWithTestHost();
        }

        private void SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(string rawMessage, Message message)
        {
            this.testRequestSender.InitializeCommunication();
            this.mockCommunicationManager.Setup(mc => mc.ReceiveRawMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(rawMessage));
            this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(rawMessage)).Returns(message);
        }

        private void StartTestRunErrorTestsTemplate(string errorMessage, Action<string> onClientProcessExitCallback)
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var runCriteria = new TestRunCriteriaWithSources(null, null, null);
            this.mockCommunicationManager.Setup(mc => mc.ReceiveRawMessageAsync(It.IsAny<CancellationToken>()))
                .Callback(() => onClientProcessExitCallback(errorMessage)).Returns(Task.FromResult((string)null));
            string testCompleteRawMessage =
                "{\"MessageType\":\"TestExecution.Completed\",\"Payload\":{\"TestRunCompleteArgs\":{\"TestRunStatistics\":null,\"IsCanceled\":false,\"IsAborted\":true,\"Error\":{\"ClassName\":\"System.IO.IOException\",\"Message\":\"Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.\",\"Data\":null,\"InnerException\":null},\"AttachmentSets\":null,\"ElapsedTimeInRunningTests\":\"00:00:00\"},\"LastRunTests\":null,\"RunAttachments\":null,\"ExecutorUris\":null}}";
            this.mockDataSerializer.Setup(
                    md => md.SerializePayload(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>()))
                .Returns(testCompleteRawMessage);
            var waitHandle = new AutoResetEvent(false);
            mockHandler.Setup(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null)).Callback(() => waitHandle.Set());

            this.testRequestSender.InitializeCommunication();
            this.testRequestSender.StartTestRun(runCriteria, mockHandler.Object);
            waitHandle.WaitOne();
            this.testRequestSender.EndSession();

            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, string.Format(CommunicationUtilitiesResources.AbortedTestRun, errorMessage)), Times.Once);
            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null), Times.Once);
            mockHandler.Verify(mh => mh.HandleRawMessage(testCompleteRawMessage), Times.Once);
            this.mockCommunicationManager.Verify(mc => mc.SendMessage(MessageType.SessionEnd), Times.Never);
        }
    }
}
