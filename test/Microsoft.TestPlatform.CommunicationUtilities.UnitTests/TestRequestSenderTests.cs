// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;

    [TestClass]
    public class TestRequestSenderTests
    {
        private const int DUMMYPROTOCOLVERSION = 42;
        private const int DEFAULTPROTOCOLVERSION = 1;
        private const int DUMMYNEGOTIATEDPROTOCOLVERSION = 41;
        private static readonly string TimoutErrorMessage = "Failed to negotiate protocol, waiting for response timed out after 0 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.";

        private readonly Mock<ICommunicationEndPoint> mockServer;
        private readonly Mock<IDataSerializer> mockDataSerializer;
        private readonly Mock<ICommunicationChannel> mockChannel;

        private readonly ConnectedEventArgs connectedEventArgs;
        private readonly List<string> pathToAdditionalExtensions = new List<string> { "Hello", "World" };
        private readonly Mock<ITestDiscoveryEventsHandler2> mockDiscoveryEventsHandler;
        private readonly Mock<ITestRunEventsHandler> mockExecutionEventsHandler;
        private readonly TestRunCriteriaWithSources testRunCriteriaWithSources;
        private TestHostConnectionInfo connectionInfo;
        private ITestRequestSender testRequestSender;

        public TestRequestSenderTests()
        {
            this.connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":123",
                Role = ConnectionRole.Client,
                Transport = Transport.Sockets
            };
            this.mockChannel = new Mock<ICommunicationChannel>();
            this.mockServer = new Mock<ICommunicationEndPoint>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.testRequestSender = new TestableTestRequestSender(this.mockServer.Object, this.connectionInfo, this.mockDataSerializer.Object, new ProtocolConfig { Version = DUMMYPROTOCOLVERSION });

            this.connectedEventArgs = new ConnectedEventArgs(this.mockChannel.Object);
            this.mockDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            this.mockExecutionEventsHandler = new Mock<ITestRunEventsHandler>();
            this.testRunCriteriaWithSources = new TestRunCriteriaWithSources(new Dictionary<string, IEnumerable<string>>(), "runsettings", null, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, string.Empty);
        }

        [TestMethod]
        public void InitializeCommunicationShouldHostServerAndAcceptClient()
        {
            var port = this.SetupFakeCommunicationChannel();

            Assert.AreEqual(port, "123", "Correct port must be returned.");
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldWaitForClientToConnect()
        {
            this.SetupFakeCommunicationChannel();

            var connected = this.testRequestSender.WaitForRequestHandlerConnection(1);

            Assert.IsTrue(connected);
        }

        [TestMethod]
        public void CloseShouldCallStopServerOnCommunicationManager()
        {
            this.testRequestSender.Close();

            this.mockServer.Verify(mc => mc.Stop(), Times.Once);
        }

        [TestMethod]
        public void DisposeShouldCallStopServerOnCommunicationManager()
        {
            this.testRequestSender.Dispose();

            this.mockServer.Verify(mc => mc.Stop(), Times.Once);
        }

        [TestMethod]
        public void EndSessionShouldSendSessionEndMessage()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.EndSession();

            this.mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.SessionEnd), Times.Once);
            this.mockChannel.Verify(c => c.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void EndSessionShouldNotSendSessionEndMessageIfClientDisconnected()
        {
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);
            this.RaiseClientDisconnectedEvent();

            this.testRequestSender.EndSession();

            this.mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.SessionEnd), Times.Never);
        }

        [TestMethod]
        public void EndSessionShouldNotSendSessionEndMessageIfTestHostProcessExited()
        {
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);
            this.testRequestSender.OnClientProcessExit("Dummy Message");

            this.testRequestSender.EndSession();

            this.mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.SessionEnd), Times.Once);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        public void OnClientProcessExitShouldSendErrorMessageIfStdErrIsEmpty(string stderr)
        {
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.testRequestSender.OnClientProcessExit(stderr);

            var expectedErrorMessage = "Reason: " + stderr;
            this.RaiseClientDisconnectedEvent();
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.EndsWith(expectedErrorMessage))), Times.Once);
        }

        [TestMethod]
        public void OnClientProcessExitShouldNotSendErrorMessageIfOperationNotStarted()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.OnClientProcessExit("Dummy Stderr");

            this.RaiseClientDisconnectedEvent();
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains("Dummy Stderr"))), Times.Never);
        }

        [TestMethod]
        public void OnClientProcessExitShouldNotSendRawMessageIfOperationNotStarted()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.OnClientProcessExit("Dummy Stderr");

            this.RaiseClientDisconnectedEvent();
            this.mockDataSerializer.Verify(ds => ds.SerializePayload(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.Message.Contains("Dummy Stderr"))), Times.Never);
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage(It.IsAny<string>()), Times.Never);
        }

        #region Version Check Tests

        [TestMethod]
        public void CheckVersionWithTestHostShouldSendHighestSupportedVersion()
        {
            this.SetupDeserializeMessage(MessageType.VersionCheck, 99);
            this.SetupRaiseMessageReceivedOnCheckVersion();
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.CheckVersionWithTestHost();

            this.mockDataSerializer.Verify(ds => ds.SerializePayload(MessageType.VersionCheck, DUMMYPROTOCOLVERSION), Times.Once);
            this.mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void CheckVersionWithTestHostShouldThrowIfTestHostVersionDoesNotMatch()
        {
            this.SetupDeserializeMessage(MessageType.ProtocolError, string.Empty);
            this.SetupRaiseMessageReceivedOnCheckVersion();
            this.SetupFakeCommunicationChannel();

            Assert.ThrowsException<TestPlatformException>(() => this.testRequestSender.CheckVersionWithTestHost());
        }

        [TestMethod]
        public void CheckVersionWithTestHostShouldThrowIfUnexpectedResponseIsReceived()
        {
            this.SetupDeserializeMessage(MessageType.TestCasesFound, string.Empty);
            this.SetupRaiseMessageReceivedOnCheckVersion();
            this.SetupFakeCommunicationChannel();

            Assert.ThrowsException<TestPlatformException>(() => this.testRequestSender.CheckVersionWithTestHost());
        }

        [TestMethod]
        public void CheckVersionWithTestHostShouldThrowIfProtocolNegotiationTimeouts()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "0");

            this.SetupFakeCommunicationChannel();

            var message = Assert.ThrowsException<TestPlatformException>(() => this.testRequestSender.CheckVersionWithTestHost()).Message;

            Assert.AreEqual(message, TestRequestSenderTests.TimoutErrorMessage);
        }

        #endregion

        #region Discovery Protocol Tests
        [TestMethod]
        public void InitializeDiscoveryShouldSendCommunicationMessageWithCorrectParameters()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.InitializeDiscovery(this.pathToAdditionalExtensions);

            this.mockDataSerializer.Verify(d => d.SerializePayload(MessageType.DiscoveryInitialize, this.pathToAdditionalExtensions, 1), Times.Once);
            this.mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDiscoveryShouldSendCommunicationMessageWithCorrectParametersWithVersion()
        {
            this.SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            this.testRequestSender.InitializeDiscovery(this.pathToAdditionalExtensions);

            this.mockDataSerializer.Verify(d => d.SerializePayload(MessageType.DiscoveryInitialize, this.pathToAdditionalExtensions, DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldSendStartDiscoveryMessageOnChannel()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.mockDataSerializer.Verify(
                s => s.SerializePayload(MessageType.StartDiscovery, It.IsAny<DiscoveryCriteria>(), DEFAULTPROTOCOLVERSION),
                Times.Once);
            this.mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldSendStartDiscoveryMessageOnChannelWithVersion()
        {
            this.SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.mockDataSerializer.Verify(
                s => s.SerializePayload(MessageType.StartDiscovery, It.IsAny<DiscoveryCriteria>(), DUMMYNEGOTIATEDPROTOCOLVERSION),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotifyRawMessageOnMessageReceived()
        {
            this.SetupDeserializeMessage(MessageType.TestMessage, new TestMessagePayload());
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleRawMessage("DummyData"), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotifyDiscoveredTestsOnTestCasesFoundMessageReceived()
        {
            this.SetupDeserializeMessage<IEnumerable<TestCase>>(MessageType.TestCasesFound, new TestCase[2]);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveredTests(It.Is<IEnumerable<TestCase>>(t => t.Count() == 2)));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotifyDiscoveryCompleteOnCompleteMessageReceived()
        {
            var completePayload = new DiscoveryCompletePayload { TotalTests = 10, IsAborted = false };
            this.SetupDeserializeMessage(MessageType.DiscoveryComplete, completePayload);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveryComplete(It.Is<DiscoveryCompleteEventArgs>(dc => dc.IsAborted == false && dc.TotalCount == 10), null));
        }

        [TestMethod]
        public void DiscoverTestsShouldStopServerOnCompleteMessageReceived()
        {
            var completePayload = new DiscoveryCompletePayload { TotalTests = 10, IsAborted = false };
            this.SetupDeserializeMessage(MessageType.DiscoveryComplete, completePayload);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseMessageReceivedEvent();

            this.mockServer.Verify(ms => ms.Stop());
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyLogMessageOnTestMessageReceived()
        {
            var message = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = "Message1" };
            this.SetupDeserializeMessage(MessageType.TestMessage, message);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, "Message1"));
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyLogMessageIfExceptionThrownOnMessageReceived()
        {
            this.SetupExceptionOnMessageReceived();
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains("Dummy Message"))));
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedMessage"), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyDiscoveryCompleteIfExceptionThrownOnMessageReceived()
        {
            this.SetupExceptionOnMessageReceived();
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveryComplete(It.Is<DiscoveryCompleteEventArgs>(dc => dc.IsAborted == true && dc.TotalCount == -1), null));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotAbortDiscoveryIfClientDisconnectedAndOperationIsComplete()
        {
            var completePayload = new DiscoveryCompletePayload { TotalTests = 10, IsAborted = false };
            this.SetupDeserializeMessage(MessageType.DiscoveryComplete, completePayload);
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);
            this.RaiseMessageReceivedEvent();   // Raise discovery complete

            this.RaiseClientDisconnectedEvent();

            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Never);
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(-1, true), null), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyLogMessageIfClientDisconnected()
        {
            // Expect default error message since we've not set any client exit message
            var expectedErrorMessage = "Reason: Unable to communicate";
            this.SetupFakeCommunicationChannel();
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.Message.Contains(expectedErrorMessage))))
                .Returns("Serialized error");
            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseClientDisconnectedEvent();

            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains(expectedErrorMessage))));
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleRawMessage(It.Is<string>(s => !string.IsNullOrEmpty(s) && s.Equals("Serialized error"))), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyLogMessageIfClientDisconnectedWithClientExit()
        {
            this.SetupFakeCommunicationChannel();
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.Message.Contains("Dummy Stderr"))))
                .Returns("Serialized Stderr");
            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);
            this.testRequestSender.OnClientProcessExit("Dummy Stderr");

            this.RaiseClientDisconnectedEvent();

            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains("Dummy Stderr"))), Times.Once);
            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleRawMessage(It.Is<string>(s => !string.IsNullOrEmpty(s) && s.Equals("Serialized Stderr"))), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyDiscoveryCompleteIfClientDisconnected()
        {
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.DiscoverTests(new DiscoveryCriteria(), this.mockDiscoveryEventsHandler.Object);

            this.RaiseClientDisconnectedEvent();

            this.mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveryComplete(It.Is<DiscoveryCompleteEventArgs>(dc => dc.IsAborted == true && dc.TotalCount == -1), null));
        }

        #endregion

        #region Execution Protocol Tests

        [TestMethod]
        public void InitializeExecutionShouldSendCommunicationMessageWithCorrectParameters()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.InitializeExecution(this.pathToAdditionalExtensions);

            this.mockDataSerializer.Verify(d => d.SerializePayload(MessageType.ExecutionInitialize, this.pathToAdditionalExtensions, 1), Times.Once);
            this.mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void InitializeExecutionShouldSendCommunicationMessageWithCorrectParametersWithVersion()
        {
            this.SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            this.testRequestSender.InitializeExecution(this.pathToAdditionalExtensions);

            this.mockDataSerializer.Verify(d => d.SerializePayload(MessageType.ExecutionInitialize, this.pathToAdditionalExtensions, DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldSendStartTestExecutionWithSourcesOnChannel()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.mockDataSerializer.Verify(d => d.SerializePayload(MessageType.StartTestExecutionWithSources, this.testRunCriteriaWithSources, DEFAULTPROTOCOLVERSION), Times.Once);
            this.mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldSendStartTestExecutionWithSourcesOnChannelWithVersion()
        {
            this.SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.mockDataSerializer.Verify(d => d.SerializePayload(MessageType.StartTestExecutionWithSources, this.testRunCriteriaWithSources, DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void StartTestRunWithTestsShouldSendStartTestExecutionWithTestsOnChannel()
        {
            var runCriteria = new TestRunCriteriaWithTests(new TestCase[2], "runsettings", null, null);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(runCriteria, this.mockExecutionEventsHandler.Object);

            this.mockDataSerializer.Verify(d => d.SerializePayload(MessageType.StartTestExecutionWithTests, runCriteria, DEFAULTPROTOCOLVERSION), Times.Once);
            this.mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunWithTestsShouldSendStartTestExecutionWithTestsOnChannelWithVersion()
        {
            var runCriteria = new TestRunCriteriaWithTests(new TestCase[2], "runsettings", null, null);
            this.SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            this.testRequestSender.StartTestRun(runCriteria, this.mockExecutionEventsHandler.Object);

            this.mockDataSerializer.Verify(d => d.SerializePayload(MessageType.StartTestExecutionWithTests, runCriteria, DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyRawMessageOnMessageReceived()
        {
            this.SetupDeserializeMessage(MessageType.TestMessage, new TestMessagePayload());
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("DummyData"), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyTestRunStatsChangeOnRunStatsMessageReceived()
        {
            var testRunChangedArgs = new TestRunChangedEventArgs(
                null,
                new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult[2],
                new TestCase[2]);
            this.SetupDeserializeMessage(MessageType.TestRunStatsChange, testRunChangedArgs);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleTestRunStatsChange(testRunChangedArgs), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyExecutionCompleteOnRunCompleteMessageReceived()
        {
            var testRunCompletePayload = new TestRunCompletePayload
            {
                TestRunCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.MaxValue),
                LastRunTests = new TestRunChangedEventArgs(null, null, null),
                RunAttachments = new List<AttachmentSet>()
            };
            this.SetupDeserializeMessage(MessageType.ExecutionComplete, testRunCompletePayload);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockExecutionEventsHandler.Verify(
                eh => eh.HandleTestRunComplete(
                    testRunCompletePayload.TestRunCompleteArgs,
                    testRunCompletePayload.LastRunTests,
                    testRunCompletePayload.RunAttachments,
                    It.IsAny<ICollection<string>>()),
                Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldStopServerOnRunCompleteMessageReceived()
        {
            var testRunCompletePayload = new TestRunCompletePayload
            {
                TestRunCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.MaxValue),
                LastRunTests = new TestRunChangedEventArgs(null, null, null),
                RunAttachments = new List<AttachmentSet>()
            };
            this.SetupDeserializeMessage(MessageType.ExecutionComplete, testRunCompletePayload);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();

            this.mockServer.Verify(ms => ms.Stop());
        }

        [TestMethod]
        public void StartTestRunShouldNotifyLogMessageOnTestMessageReceived()
        {
            var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = "Dummy" };
            this.SetupDeserializeMessage(MessageType.TestMessage, testMessagePayload);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, "Dummy"), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyLaunchWithDebuggerOnMessageReceived()
        {
            var launchMessagePayload = new TestProcessStartInfo();
            this.SetupDeserializeMessage(MessageType.LaunchAdapterProcessWithDebuggerAttached, launchMessagePayload);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockExecutionEventsHandler.Verify(eh => eh.LaunchProcessWithDebuggerAttached(launchMessagePayload), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldSendLaunchDebuggerAttachedCallbackOnMessageReceived()
        {
            var launchMessagePayload = new TestProcessStartInfo();
            this.SetupDeserializeMessage(MessageType.LaunchAdapterProcessWithDebuggerAttached, launchMessagePayload);
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockDataSerializer.Verify(ds => ds.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, It.IsAny<int>(), 1), Times.Once);
            this.mockChannel.Verify(c => c.Send(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void StartTestRunShouldSendLaunchDebuggerAttachedCallbackOnMessageReceivedWithVersion()
        {
            var launchMessagePayload = new TestProcessStartInfo();
            this.SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);
            this.SetupDeserializeMessage(MessageType.LaunchAdapterProcessWithDebuggerAttached, launchMessagePayload);

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockDataSerializer.Verify(ds => ds.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, It.IsAny<int>(), DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyLogMessageIfExceptionIsThrownOnMessageReceived()
        {
            this.SetupExceptionOnMessageReceived();
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains("Dummy Message"))), Times.Once);
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedMessage"), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyExecutionCompleteIfExceptionIsThrownOnMessageReceived()
        {
            this.SetupExceptionOnMessageReceived();
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseMessageReceivedEvent();
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleTestRunComplete(It.Is<TestRunCompleteEventArgs>(t => t.IsAborted), null, null, null), Times.Once);
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedAbortedPayload"), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotNotifyExecutionCompleteIfClientDisconnectedAndOperationComplete()
        {
            var testRunCompletePayload = new TestRunCompletePayload
            {
                TestRunCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.MaxValue),
                LastRunTests = new TestRunChangedEventArgs(null, null, null),
                RunAttachments = new List<AttachmentSet>()
            };
            this.SetupDeserializeMessage(MessageType.ExecutionComplete, testRunCompletePayload);
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);
            this.RaiseMessageReceivedEvent();   // Raise test run complete

            this.RaiseClientDisconnectedEvent();

            this.mockExecutionEventsHandler.Verify(eh => eh.HandleTestRunComplete(It.Is<TestRunCompleteEventArgs>(t => t.IsAborted), null, null, null), Times.Never);
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedAbortedPayload"), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyErrorLogMessageIfClientDisconnected()
        {
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseClientDisconnectedEvent();

            // Expect default error message since we've not set any client exit message
            var expectedErrorMessage = "Reason: Unable to communicate";
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains(expectedErrorMessage))), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyErrorLogMessageIfClientDisconnectedWithClientExit()
        {
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);
            this.testRequestSender.OnClientProcessExit("Dummy Stderr");

            this.RaiseClientDisconnectedEvent();

            var expectedErrorMessage = "Reason: Dummy Stderr";
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains(expectedErrorMessage))), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyExecutionCompleteIfClientDisconnected()
        {
            this.SetupOperationAbortedPayload();
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.StartTestRun(this.testRunCriteriaWithSources, this.mockExecutionEventsHandler.Object);

            this.RaiseClientDisconnectedEvent();

            this.mockExecutionEventsHandler.Verify(eh => eh.HandleTestRunComplete(It.Is<TestRunCompleteEventArgs>(t => t.IsAborted), null, null, null), Times.Once);
            this.mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedAbortedPayload"), Times.Once);
        }

        [TestMethod]
        public void SendTestRunCancelShouldSendCancelTestRunMessage()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.SendTestRunCancel();

            this.mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.CancelTestRun), Times.Once);
            this.mockChannel.Verify(c => c.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SendTestRunAbortShouldSendAbortTestRunMessage()
        {
            this.SetupFakeCommunicationChannel();

            this.testRequestSender.SendTestRunAbort();

            this.mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.AbortTestRun), Times.Once);
            this.mockChannel.Verify(c => c.Send(It.IsAny<string>()), Times.Once);
        }

        #endregion

        private string SetupFakeCommunicationChannel(string connectionArgs = "123")
        {
            this.connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":" + connectionArgs,
                Role = ConnectionRole.Client,
                Transport = Transport.Sockets
            };

            // Setup mock connected event and initialize communication channel
            this.mockServer.Setup(mc => mc.Start(this.connectionInfo.Endpoint))
                .Returns(this.connectionInfo.Endpoint)
                .Callback(() => this.mockServer.Raise(s => s.Connected += null, this.mockServer.Object, this.connectedEventArgs));

            return this.testRequestSender.InitializeCommunication().ToString();
        }

        private void SetupFakeChannelWithVersionNegotiation(int protocolVersion)
        {
            // Sends a check version message to setup the negotiated protocol version.
            // This method is only required in specific tests.
            this.SetupDeserializeMessage(MessageType.VersionCheck, DUMMYNEGOTIATEDPROTOCOLVERSION);
            this.SetupRaiseMessageReceivedOnCheckVersion();
            this.SetupFakeCommunicationChannel();
            this.testRequestSender.CheckVersionWithTestHost();
            this.ResetRaiseMessageReceivedOnCheckVersion();
        }

        private void RaiseMessageReceivedEvent()
        {
            this.mockChannel.Raise(
                c => c.MessageReceived += null,
                this.mockChannel.Object,
                new MessageReceivedEventArgs { Data = "DummyData" });
        }

        private void RaiseClientDisconnectedEvent()
        {
            this.mockServer.Raise(
                s => s.Disconnected += null,
                this.mockServer.Object,
                new DisconnectedEventArgs { Error = new Exception("Dummy Message") });
        }

        private void SetupDeserializeMessage<TPayload>(string messageType, TPayload payload)
        {
            this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message { MessageType = messageType });
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TPayload>(It.IsAny<Message>()))
                .Returns(payload);
        }

        private void SetupExceptionMessageSerialize()
        {
            // Serialize the exception message
            this.mockDataSerializer
                .Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.Message.Contains("Dummy Message"))))
                .Returns("SerializedMessage");
        }

        private void SetupOperationAbortedPayload()
        {
            // Serialize the execution aborted
            this.mockDataSerializer
                .Setup(ds => ds.SerializePayload(MessageType.ExecutionComplete, It.Is<TestRunCompletePayload>(p => p.TestRunCompleteArgs.IsAborted)))
                .Returns("SerializedAbortedPayload");
        }

        private void SetupExceptionOnMessageReceived()
        {
            this.SetupExceptionMessageSerialize();
            this.SetupOperationAbortedPayload();

            this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>()))
                .Callback(() => throw new Exception("Dummy Message"));
        }

        private void SetupRaiseMessageReceivedOnCheckVersion()
        {
            this.mockChannel.Setup(mc => mc.Send(It.IsAny<string>())).Callback(this.RaiseMessageReceivedEvent);
        }

        private void ResetRaiseMessageReceivedOnCheckVersion()
        {
            this.mockChannel.Reset();
        }

        private class TestableTestRequestSender : TestRequestSender
        {
            public TestableTestRequestSender(ICommunicationEndPoint commEndpoint, TestHostConnectionInfo connectionInfo, IDataSerializer serializer, ProtocolConfig protocolConfig)
                : base(commEndpoint, connectionInfo, serializer, protocolConfig, 0)
            {
            }
        }
    }
}
