// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using CommunicationUtilitiesResources = VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;

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

        private readonly List<string> pathToAdditionalExtensions = new() { "Hello", "World" };
        private readonly Mock<ITestDiscoveryEventsHandler2> mockDiscoveryEventsHandler;
        private readonly Mock<ITestRunEventsHandler> mockExecutionEventsHandler;
        private readonly TestRunCriteriaWithSources testRunCriteriaWithSources;
        private TestHostConnectionInfo connectionInfo;
        private readonly ITestRequestSender testRequestSender;
        private ConnectedEventArgs connectedEventArgs;

        public TestRequestSenderTests()
        {
            connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":123",
                Role = ConnectionRole.Client,
                Transport = Transport.Sockets
            };
            mockChannel = new Mock<ICommunicationChannel>();
            mockServer = new Mock<ICommunicationEndPoint>();
            mockDataSerializer = new Mock<IDataSerializer>();
            testRequestSender = new TestableTestRequestSender(mockServer.Object, connectionInfo, mockDataSerializer.Object, new ProtocolConfig { Version = DUMMYPROTOCOLVERSION });

            connectedEventArgs = new ConnectedEventArgs(mockChannel.Object);
            mockDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            mockExecutionEventsHandler = new Mock<ITestRunEventsHandler>();
            testRunCriteriaWithSources = new TestRunCriteriaWithSources(new Dictionary<string, IEnumerable<string>>(), "runsettings", null, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, string.Empty);
        }

        [TestMethod]
        public void InitializeCommunicationShouldHostServerAndAcceptClient()
        {
            var port = SetupFakeCommunicationChannel();

            Assert.AreEqual("123", port, "Correct port must be returned.");
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldWaitForClientToConnect()
        {
            SetupFakeCommunicationChannel();

            var connected = testRequestSender.WaitForRequestHandlerConnection(1, It.IsAny<CancellationToken>());

            Assert.IsTrue(connected);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldNotConnectIfExceptionWasThrownByTcpLayer()
        {
            connectedEventArgs = new ConnectedEventArgs(new SocketException());
            SetupFakeCommunicationChannel();

            var connected = testRequestSender.WaitForRequestHandlerConnection(1, It.IsAny<CancellationToken>());

            Assert.IsFalse(connected);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionWithTimeoutShouldReturnImmediatelyWhenCancellationRequested()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var connectionTimeout = 5000;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var connected = testRequestSender.WaitForRequestHandlerConnection(connectionTimeout, cancellationTokenSource.Token);
            watch.Stop();

            Assert.IsFalse(connected);
            Assert.IsTrue(watch.ElapsedMilliseconds < connectionTimeout);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionWithTimeoutShouldReturnImmediatelyIfHostExitedUnexpectedly()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            testRequestSender.OnClientProcessExit("DummyError");

            var connectionTimeout = 5000;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var connected = testRequestSender.WaitForRequestHandlerConnection(connectionTimeout, cancellationTokenSource.Token);
            watch.Stop();

            Assert.IsFalse(connected);
            Assert.IsTrue(watch.ElapsedMilliseconds < connectionTimeout);
        }

        [TestMethod]
        public void CloseShouldCallStopServerOnCommunicationManager()
        {
            testRequestSender.Close();

            mockServer.Verify(mc => mc.Stop(), Times.Once);
        }

        [TestMethod]
        public void DisposeShouldCallStopServerOnCommunicationManager()
        {
            testRequestSender.Dispose();

            mockServer.Verify(mc => mc.Stop(), Times.Once);
        }

        [TestMethod]
        public void EndSessionShouldSendSessionEndMessage()
        {
            SetupFakeCommunicationChannel();

            testRequestSender.EndSession();

            mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.SessionEnd), Times.Once);
            mockChannel.Verify(c => c.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void EndSessionShouldNotSendSessionEndMessageIfClientDisconnected()
        {
            SetupFakeCommunicationChannel();
            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);
            RaiseClientDisconnectedEvent();

            testRequestSender.EndSession();

            mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.SessionEnd), Times.Never);
        }

        [TestMethod]
        public void EndSessionShouldNotSendSessionEndMessageIfTestHostProcessExited()
        {
            SetupFakeCommunicationChannel();
            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);
            testRequestSender.OnClientProcessExit("Dummy Message");

            testRequestSender.EndSession();

            mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.SessionEnd), Times.Once);
        }

        [TestMethod]
        public void EndSessionShouldNotSendTestRunCancelMessageIfClientDisconnected()
        {
            SetupFakeCommunicationChannel();
            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);
            RaiseClientDisconnectedEvent();

            testRequestSender.SendTestRunCancel();

            mockChannel.Verify(mockChannel => mockChannel.Send(MessageType.CancelTestRun), Times.Never);
        }

        [TestMethod]
        public void EndSessionShouldNotSendTestRunAbortMessageIfClientDisconnected()
        {
            SetupFakeCommunicationChannel();
            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);
            RaiseClientDisconnectedEvent();

            testRequestSender.SendTestRunAbort();

            mockChannel.Verify(mockChannel => mockChannel.Send(MessageType.CancelTestRun), Times.Never);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        public void OnClientProcessExitShouldSendErrorMessageIfStdErrIsEmpty(string stderr)
        {
            SetupFakeCommunicationChannel();
            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            testRequestSender.OnClientProcessExit(stderr);

            var expectedErrorMessage = "Reason: Test host process crashed";
            RaiseClientDisconnectedEvent();
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.EndsWith(expectedErrorMessage))), Times.Once);
        }

        [TestMethod]
        public void OnClientProcessExitShouldNotSendErrorMessageIfOperationNotStarted()
        {
            SetupFakeCommunicationChannel();
            testRequestSender.OnClientProcessExit("Dummy Stderr");

            RaiseClientDisconnectedEvent();
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains("Dummy Stderr"))), Times.Never);
        }

        [TestMethod]
        public void OnClientProcessExitShouldNotSendRawMessageIfOperationNotStarted()
        {
            SetupFakeCommunicationChannel();

            testRequestSender.OnClientProcessExit("Dummy Stderr");

            RaiseClientDisconnectedEvent();
            mockDataSerializer.Verify(ds => ds.SerializePayload(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.Message.Contains("Dummy Stderr"))), Times.Never);
            mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage(It.IsAny<string>()), Times.Never);
        }

        #region Version Check Tests

        [TestMethod]
        public void CheckVersionWithTestHostShouldSendHighestSupportedVersion()
        {
            SetupDeserializeMessage(MessageType.VersionCheck, 99);
            SetupRaiseMessageReceivedOnCheckVersion();
            SetupFakeCommunicationChannel();

            testRequestSender.CheckVersionWithTestHost();

            mockDataSerializer.Verify(ds => ds.SerializePayload(MessageType.VersionCheck, DUMMYPROTOCOLVERSION), Times.Once);
            mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void CheckVersionWithTestHostShouldThrowIfTestHostVersionDoesNotMatch()
        {
            SetupDeserializeMessage(MessageType.ProtocolError, string.Empty);
            SetupRaiseMessageReceivedOnCheckVersion();
            SetupFakeCommunicationChannel();

            Assert.ThrowsException<TestPlatformException>(() => testRequestSender.CheckVersionWithTestHost());
        }

        [TestMethod]
        public void CheckVersionWithTestHostShouldThrowIfUnexpectedResponseIsReceived()
        {
            SetupDeserializeMessage(MessageType.TestCasesFound, string.Empty);
            SetupRaiseMessageReceivedOnCheckVersion();
            SetupFakeCommunicationChannel();

            Assert.ThrowsException<TestPlatformException>(() => testRequestSender.CheckVersionWithTestHost());
        }

        [TestMethod]
        public void CheckVersionWithTestHostShouldThrowIfProtocolNegotiationTimeouts()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "0");

            SetupFakeCommunicationChannel();

            var message = Assert.ThrowsException<TestPlatformException>(() => testRequestSender.CheckVersionWithTestHost()).Message;

            Assert.AreEqual(message, TimoutErrorMessage);
        }

        #endregion

        #region Discovery Protocol Tests
        [TestMethod]
        public void InitializeDiscoveryShouldSendCommunicationMessageWithCorrectParameters()
        {
            SetupFakeCommunicationChannel();

            testRequestSender.InitializeDiscovery(pathToAdditionalExtensions);

            mockDataSerializer.Verify(d => d.SerializePayload(MessageType.DiscoveryInitialize, pathToAdditionalExtensions, 1), Times.Once);
            mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDiscoveryShouldSendCommunicationMessageWithCorrectParametersWithVersion()
        {
            SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            testRequestSender.InitializeDiscovery(pathToAdditionalExtensions);

            mockDataSerializer.Verify(d => d.SerializePayload(MessageType.DiscoveryInitialize, pathToAdditionalExtensions, DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldSendStartDiscoveryMessageOnChannel()
        {
            SetupFakeCommunicationChannel();

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            mockDataSerializer.Verify(
                s => s.SerializePayload(MessageType.StartDiscovery, It.IsAny<DiscoveryCriteria>(), DEFAULTPROTOCOLVERSION),
                Times.Once);
            mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldSendStartDiscoveryMessageOnChannelWithVersion()
        {
            SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            mockDataSerializer.Verify(
                s => s.SerializePayload(MessageType.StartDiscovery, It.IsAny<DiscoveryCriteria>(), DUMMYNEGOTIATEDPROTOCOLVERSION),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotifyRawMessageOnMessageReceived()
        {
            SetupDeserializeMessage(MessageType.TestMessage, new TestMessagePayload());
            SetupFakeCommunicationChannel();

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleRawMessage("DummyData"), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotifyDiscoveredTestsOnTestCasesFoundMessageReceived()
        {
            SetupDeserializeMessage<IEnumerable<TestCase>>(MessageType.TestCasesFound, new TestCase[2]);
            SetupFakeCommunicationChannel();

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveredTests(It.Is<IEnumerable<TestCase>>(t => t.Count() == 2)));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotifyDiscoveryCompleteOnCompleteMessageReceived()
        {
            var completePayload = new DiscoveryCompletePayload { TotalTests = 10, IsAborted = false };
            SetupDeserializeMessage(MessageType.DiscoveryComplete, completePayload);
            SetupFakeCommunicationChannel();

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveryComplete(It.Is<DiscoveryCompleteEventArgs>(dc => dc.IsAborted == false && dc.TotalCount == 10), null));
        }

        [TestMethod]
        public void DiscoverTestsShouldStopServerOnCompleteMessageReceived()
        {
            var completePayload = new DiscoveryCompletePayload { TotalTests = 10, IsAborted = false };
            SetupDeserializeMessage(MessageType.DiscoveryComplete, completePayload);
            SetupFakeCommunicationChannel();

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseMessageReceivedEvent();

            mockServer.Verify(ms => ms.Stop());
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyLogMessageOnTestMessageReceived()
        {
            var message = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = "Message1" };
            SetupDeserializeMessage(MessageType.TestMessage, message);
            SetupFakeCommunicationChannel();

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, "Message1"));
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyLogMessageIfExceptionThrownOnMessageReceived()
        {
            SetupExceptionOnMessageReceived();
            SetupFakeCommunicationChannel();

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains("Dummy Message"))));
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedMessage"), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyDiscoveryCompleteIfExceptionThrownOnMessageReceived()
        {
            SetupExceptionOnMessageReceived();
            SetupFakeCommunicationChannel();

            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveryComplete(It.Is<DiscoveryCompleteEventArgs>(dc => dc.IsAborted == true && dc.TotalCount == -1), null));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotAbortDiscoveryIfClientDisconnectedAndOperationIsComplete()
        {
            var completePayload = new DiscoveryCompletePayload { TotalTests = 10, IsAborted = false };
            SetupDeserializeMessage(MessageType.DiscoveryComplete, completePayload);
            SetupFakeCommunicationChannel();
            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);
            RaiseMessageReceivedEvent();   // Raise discovery complete

            RaiseClientDisconnectedEvent();

            mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Never);
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(-1, true), null), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyLogMessageIfClientDisconnected()
        {
            // Expect default error message since we've not set any client exit message
            var expectedErrorMessage = "Reason: Unable to communicate";
            SetupFakeCommunicationChannel();
            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.Message.Contains(expectedErrorMessage))))
                .Returns("Serialized error");
            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseClientDisconnectedEvent();

            mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains(expectedErrorMessage))));
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleRawMessage(It.Is<string>(s => !string.IsNullOrEmpty(s) && s.Equals("Serialized error"))), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyLogMessageIfClientDisconnectedWithClientExit()
        {
            SetupFakeCommunicationChannel();
            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.Message.Contains("Dummy Stderr"))))
                .Returns("Serialized Stderr");
            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);
            testRequestSender.OnClientProcessExit("Dummy Stderr");

            RaiseClientDisconnectedEvent();

            mockDiscoveryEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains("Dummy Stderr"))), Times.Once);
            mockDiscoveryEventsHandler.Verify(eh => eh.HandleRawMessage(It.Is<string>(s => !string.IsNullOrEmpty(s) && s.Equals("Serialized Stderr"))), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestShouldNotifyDiscoveryCompleteIfClientDisconnected()
        {
            SetupFakeCommunicationChannel();
            testRequestSender.DiscoverTests(new DiscoveryCriteria(), mockDiscoveryEventsHandler.Object);

            RaiseClientDisconnectedEvent();

            mockDiscoveryEventsHandler.Verify(eh => eh.HandleDiscoveryComplete(It.Is<DiscoveryCompleteEventArgs>(dc => dc.IsAborted == true && dc.TotalCount == -1), null));
        }

        #endregion

        #region Execution Protocol Tests

        [TestMethod]
        public void InitializeExecutionShouldSendCommunicationMessageWithCorrectParameters()
        {
            SetupFakeCommunicationChannel();

            testRequestSender.InitializeExecution(pathToAdditionalExtensions);

            mockDataSerializer.Verify(d => d.SerializePayload(MessageType.ExecutionInitialize, pathToAdditionalExtensions, 1), Times.Once);
            mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void InitializeExecutionShouldSendCommunicationMessageWithCorrectParametersWithVersion()
        {
            SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            testRequestSender.InitializeExecution(pathToAdditionalExtensions);

            mockDataSerializer.Verify(d => d.SerializePayload(MessageType.ExecutionInitialize, pathToAdditionalExtensions, DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldSendStartTestExecutionWithSourcesOnChannel()
        {
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            mockDataSerializer.Verify(d => d.SerializePayload(MessageType.StartTestExecutionWithSources, testRunCriteriaWithSources, DEFAULTPROTOCOLVERSION), Times.Once);
            mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldSendStartTestExecutionWithSourcesOnChannelWithVersion()
        {
            SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            mockDataSerializer.Verify(d => d.SerializePayload(MessageType.StartTestExecutionWithSources, testRunCriteriaWithSources, DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void StartTestRunWithTestsShouldSendStartTestExecutionWithTestsOnChannel()
        {
            var runCriteria = new TestRunCriteriaWithTests(new TestCase[2], "runsettings", null, null);
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(runCriteria, mockExecutionEventsHandler.Object);

            mockDataSerializer.Verify(d => d.SerializePayload(MessageType.StartTestExecutionWithTests, runCriteria, DEFAULTPROTOCOLVERSION), Times.Once);
            mockChannel.Verify(mc => mc.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunWithTestsShouldSendStartTestExecutionWithTestsOnChannelWithVersion()
        {
            var runCriteria = new TestRunCriteriaWithTests(new TestCase[2], "runsettings", null, null);
            SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);

            testRequestSender.StartTestRun(runCriteria, mockExecutionEventsHandler.Object);

            mockDataSerializer.Verify(d => d.SerializePayload(MessageType.StartTestExecutionWithTests, runCriteria, DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyRawMessageOnMessageReceived()
        {
            SetupDeserializeMessage(MessageType.TestMessage, new TestMessagePayload());
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("DummyData"), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyTestRunStatsChangeOnRunStatsMessageReceived()
        {
            var testRunChangedArgs = new TestRunChangedEventArgs(
                null,
                new VisualStudio.TestPlatform.ObjectModel.TestResult[2],
                new TestCase[2]);
            SetupDeserializeMessage(MessageType.TestRunStatsChange, testRunChangedArgs);
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockExecutionEventsHandler.Verify(eh => eh.HandleTestRunStatsChange(testRunChangedArgs), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyExecutionCompleteOnRunCompleteMessageReceived()
        {
            var testRunCompletePayload = new TestRunCompletePayload
            {
                TestRunCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.MaxValue),
                LastRunTests = new TestRunChangedEventArgs(null, null, null),
                RunAttachments = new List<AttachmentSet>()
            };
            SetupDeserializeMessage(MessageType.ExecutionComplete, testRunCompletePayload);
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockExecutionEventsHandler.Verify(
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
                TestRunCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.MaxValue),
                LastRunTests = new TestRunChangedEventArgs(null, null, null),
                RunAttachments = new List<AttachmentSet>()
            };
            SetupDeserializeMessage(MessageType.ExecutionComplete, testRunCompletePayload);
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();

            mockServer.Verify(ms => ms.Stop());
        }

        [TestMethod]
        public void StartTestRunShouldNotifyLogMessageOnTestMessageReceived()
        {
            var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = "Dummy" };
            SetupDeserializeMessage(MessageType.TestMessage, testMessagePayload);
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockExecutionEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, "Dummy"), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyLaunchWithDebuggerOnMessageReceived()
        {
            var launchMessagePayload = new TestProcessStartInfo();
            SetupDeserializeMessage(MessageType.LaunchAdapterProcessWithDebuggerAttached, launchMessagePayload);
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockExecutionEventsHandler.Verify(eh => eh.LaunchProcessWithDebuggerAttached(launchMessagePayload), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldSendLaunchDebuggerAttachedCallbackOnMessageReceived()
        {
            var launchMessagePayload = new TestProcessStartInfo();
            SetupDeserializeMessage(MessageType.LaunchAdapterProcessWithDebuggerAttached, launchMessagePayload);
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockDataSerializer.Verify(ds => ds.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, It.IsAny<int>(), 1), Times.Once);
            mockChannel.Verify(c => c.Send(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void StartTestRunShouldSendLaunchDebuggerAttachedCallbackOnMessageReceivedWithVersion()
        {
            var launchMessagePayload = new TestProcessStartInfo();
            SetupFakeChannelWithVersionNegotiation(DUMMYNEGOTIATEDPROTOCOLVERSION);
            SetupDeserializeMessage(MessageType.LaunchAdapterProcessWithDebuggerAttached, launchMessagePayload);

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockDataSerializer.Verify(ds => ds.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, It.IsAny<int>(), DUMMYNEGOTIATEDPROTOCOLVERSION), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyLogMessageIfExceptionIsThrownOnMessageReceived()
        {
            SetupExceptionOnMessageReceived();
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockExecutionEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains("Dummy Message"))), Times.Once);
            mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedMessage"), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyExecutionCompleteIfExceptionIsThrownOnMessageReceived()
        {
            SetupExceptionOnMessageReceived();
            SetupFakeCommunicationChannel();

            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseMessageReceivedEvent();
            mockExecutionEventsHandler.Verify(eh => eh.HandleTestRunComplete(It.Is<TestRunCompleteEventArgs>(t => t.IsAborted), null, null, null), Times.Once);
            mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedAbortedPayload"), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotNotifyExecutionCompleteIfClientDisconnectedAndOperationComplete()
        {
            var testRunCompletePayload = new TestRunCompletePayload
            {
                TestRunCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.MaxValue),
                LastRunTests = new TestRunChangedEventArgs(null, null, null),
                RunAttachments = new List<AttachmentSet>()
            };
            SetupDeserializeMessage(MessageType.ExecutionComplete, testRunCompletePayload);
            SetupFakeCommunicationChannel();
            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);
            RaiseMessageReceivedEvent();   // Raise test run complete

            RaiseClientDisconnectedEvent();

            mockExecutionEventsHandler.Verify(eh => eh.HandleTestRunComplete(It.Is<TestRunCompleteEventArgs>(t => t.IsAborted), null, null, null), Times.Never);
            mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedAbortedPayload"), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyErrorLogMessageIfClientDisconnected()
        {
            SetupFakeCommunicationChannel();
            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseClientDisconnectedEvent();

            // Expect default error message since we've not set any client exit message
            var expectedErrorMessage = "Reason: Unable to communicate";
            mockExecutionEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains(expectedErrorMessage))), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyErrorLogMessageIfClientDisconnectedWithClientExit()
        {
            SetupFakeCommunicationChannel();
            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);
            testRequestSender.OnClientProcessExit("Dummy Stderr");

            RaiseClientDisconnectedEvent();

            var expectedErrorMessage = "Reason: Test host process crashed : Dummy Stderr";
            mockExecutionEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(s => s.Contains(expectedErrorMessage))), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotifyExecutionCompleteIfClientDisconnected()
        {
            SetupOperationAbortedPayload();
            SetupFakeCommunicationChannel();
            testRequestSender.StartTestRun(testRunCriteriaWithSources, mockExecutionEventsHandler.Object);

            RaiseClientDisconnectedEvent();

            mockExecutionEventsHandler.Verify(eh => eh.HandleTestRunComplete(It.Is<TestRunCompleteEventArgs>(t => t.IsAborted), null, null, null), Times.Once);
            mockExecutionEventsHandler.Verify(eh => eh.HandleRawMessage("SerializedAbortedPayload"), Times.Once);
        }

        [TestMethod]
        public void SendTestRunCancelShouldSendCancelTestRunMessage()
        {
            SetupFakeCommunicationChannel();

            testRequestSender.SendTestRunCancel();

            mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.CancelTestRun), Times.Once);
            mockChannel.Verify(c => c.Send(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SendTestRunAbortShouldSendAbortTestRunMessage()
        {
            SetupFakeCommunicationChannel();

            testRequestSender.SendTestRunAbort();

            mockDataSerializer.Verify(ds => ds.SerializeMessage(MessageType.AbortTestRun), Times.Once);
            mockChannel.Verify(c => c.Send(It.IsAny<string>()), Times.Once);
        }

        #endregion

        private string SetupFakeCommunicationChannel(string connectionArgs = "123")
        {
            connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":" + connectionArgs,
                Role = ConnectionRole.Client,
                Transport = Transport.Sockets
            };

            // Setup mock connected event and initialize communication channel
            mockServer.Setup(mc => mc.Start(connectionInfo.Endpoint))
                .Returns(connectionInfo.Endpoint)
                .Callback(() => mockServer.Raise(s => s.Connected += null, mockServer.Object, connectedEventArgs));

            return testRequestSender.InitializeCommunication().ToString();
        }

        private void SetupFakeChannelWithVersionNegotiation(int protocolVersion)
        {
            // Sends a check version message to setup the negotiated protocol version.
            // This method is only required in specific tests.
            SetupDeserializeMessage(MessageType.VersionCheck, DUMMYNEGOTIATEDPROTOCOLVERSION);
            SetupRaiseMessageReceivedOnCheckVersion();
            SetupFakeCommunicationChannel();
            testRequestSender.CheckVersionWithTestHost();
            ResetRaiseMessageReceivedOnCheckVersion();
        }

        private void RaiseMessageReceivedEvent()
        {
            mockChannel.Raise(
                c => c.MessageReceived += null,
                mockChannel.Object,
                new MessageReceivedEventArgs { Data = "DummyData" });
        }

        private void RaiseClientDisconnectedEvent()
        {
            mockServer.Raise(
                s => s.Disconnected += null,
                mockServer.Object,
                new DisconnectedEventArgs { Error = new Exception("Dummy Message") });
        }

        private void SetupDeserializeMessage<TPayload>(string messageType, TPayload payload)
        {
            mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message { MessageType = messageType });
            mockDataSerializer.Setup(ds => ds.DeserializePayload<TPayload>(It.IsAny<Message>()))
                .Returns(payload);
        }

        private void SetupExceptionMessageSerialize()
        {
            // Serialize the exception message
            mockDataSerializer
                .Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.Message.Contains("Dummy Message"))))
                .Returns("SerializedMessage");
        }

        private void SetupOperationAbortedPayload()
        {
            // Serialize the execution aborted
            mockDataSerializer
                .Setup(ds => ds.SerializePayload(MessageType.ExecutionComplete, It.Is<TestRunCompletePayload>(p => p.TestRunCompleteArgs.IsAborted)))
                .Returns("SerializedAbortedPayload");
        }

        private void SetupExceptionOnMessageReceived()
        {
            SetupExceptionMessageSerialize();
            SetupOperationAbortedPayload();

            mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>()))
                .Callback(() => throw new Exception("Dummy Message"));
        }

        private void SetupRaiseMessageReceivedOnCheckVersion()
        {
            mockChannel.Setup(mc => mc.Send(It.IsAny<string>())).Callback(RaiseMessageReceivedEvent);
        }

        private void ResetRaiseMessageReceivedOnCheckVersion()
        {
            mockChannel.Reset();
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
