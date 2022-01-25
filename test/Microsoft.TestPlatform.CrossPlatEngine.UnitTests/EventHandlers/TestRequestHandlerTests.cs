// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    [TestClass]
    public class TestRequestHandlerTests
    {
        private readonly Mock<ICommunicationEndPoint> mockCommunicationClient;
        private readonly Mock<ICommunicationEndpointFactory> mockCommunicationEndpointFactory;
        private readonly Mock<ICommunicationChannel> mockChannel;
        private readonly Mock<ITestHostManagerFactory> mockTestHostManagerFactory;
        private readonly Mock<IDiscoveryManager> mockDiscoveryManager;
        private readonly Mock<IExecutionManager> mockExecutionManager;

        private readonly JsonDataSerializer dataSerializer;
        private ITestRequestHandler requestHandler;
        private readonly TestHostConnectionInfo testHostConnectionInfo;
        private readonly JobQueue<Action> jobQueue;

        public TestRequestHandlerTests()
        {
            mockCommunicationClient = new Mock<ICommunicationEndPoint>();
            mockCommunicationEndpointFactory = new Mock<ICommunicationEndpointFactory>();
            mockChannel = new Mock<ICommunicationChannel>();
            dataSerializer = JsonDataSerializer.Instance;
            testHostConnectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":123",
                Role = ConnectionRole.Client
            };

            jobQueue = new JobQueue<Action>(
                action => action(),
                "TestHostOperationQueue",
                500,
                25000000,
                true,
                message => EqtTrace.Error(message));

            // Setup mock discovery and execution managers
            mockTestHostManagerFactory = new Mock<ITestHostManagerFactory>();
            mockDiscoveryManager = new Mock<IDiscoveryManager>();
            mockExecutionManager = new Mock<IExecutionManager>();
            mockTestHostManagerFactory.Setup(mf => mf.GetDiscoveryManager()).Returns(mockDiscoveryManager.Object);
            mockTestHostManagerFactory.Setup(mf => mf.GetExecutionManager()).Returns(mockExecutionManager.Object);
            mockCommunicationEndpointFactory.Setup(f => f.Create(ConnectionRole.Client))
                .Returns(mockCommunicationClient.Object);

            requestHandler = new TestableTestRequestHandler(
                testHostConnectionInfo,
                mockCommunicationEndpointFactory.Object,
                JsonDataSerializer.Instance,
                jobQueue);
            requestHandler.InitializeCommunication();
            mockCommunicationClient.Raise(e => e.Connected += null, new ConnectedEventArgs(mockChannel.Object));
        }

        [TestMethod]
        public void InitializeCommunicationShouldConnectToServerAsynchronously()
        {
            mockCommunicationClient.Verify(c => c.Start(testHostConnectionInfo.Endpoint), Times.Once);
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void InitializeCommunicationShouldThrowIfServerIsNotAccessible()
        {
            var connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":123",
                Role = ConnectionRole.Client
            };
            var socketClient = new SocketClient();
            socketClient.Connected += (sender, connectedEventArgs) =>
            {
                Assert.IsFalse(connectedEventArgs.Connected);
                Assert.AreEqual(typeof(SocketException), connectedEventArgs.Fault.InnerException.GetType());
            };
            mockCommunicationEndpointFactory.Setup(f => f.Create(ConnectionRole.Client))
                .Returns(socketClient);
            var rh = new TestableTestRequestHandler(connectionInfo, mockCommunicationEndpointFactory.Object, dataSerializer, jobQueue);

            rh.InitializeCommunication();
            requestHandler.WaitForRequestSenderConnection(1000);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldWaitUntilConnectionIsSetup()
        {
            Assert.IsTrue(requestHandler.WaitForRequestSenderConnection(1000));
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldReturnFalseIfConnectionSetupTimesout()
        {
            requestHandler = new TestableTestRequestHandler(
                testHostConnectionInfo,
                mockCommunicationEndpointFactory.Object,
                JsonDataSerializer.Instance,
                jobQueue);
            requestHandler.InitializeCommunication();

            Assert.IsFalse(requestHandler.WaitForRequestSenderConnection(1));
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessMessagesUntilSessionCompleted()
        {
            var task = ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            SendSessionEnd();
            Assert.IsTrue(task.Wait(2000));
        }

        #region Version Check Protocol

        [TestMethod]
        public void ProcessRequestsVersionCheckShouldAckMinimumOfGivenAndHighestSupportedVersion()
        {
            var message = new Message { MessageType = MessageType.VersionCheck, Payload = 1 };
            ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            SendMessageOnChannel(message);

            VerifyResponseMessageEquals(Serialize(message));
            SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsVersionCheckShouldLogErrorIfDiagnosticsEnableFails()
        {
            if (!string.IsNullOrEmpty(EqtTrace.LogFile))
            {
                // This test is no-op if diagnostics session is enabled
                return;
            }
            EqtTrace.ErrorOnInitialization = "non-existent-error";
            var message = new Message { MessageType = MessageType.VersionCheck, Payload = 1 };
            ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            SendMessageOnChannel(message);

            VerifyResponseMessageContains(EqtTrace.ErrorOnInitialization);
            SendSessionEnd();
        }

        #endregion

        #region Discovery Protocol

        [TestMethod]
        public void ProcessRequestsDiscoveryInitializeShouldSetExtensionPaths()
        {
            var message = dataSerializer.SerializePayload(MessageType.DiscoveryInitialize, new[] { "testadapter.dll" });
            ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            SendMessageOnChannel(message);
            jobQueue.Flush();

            mockDiscoveryManager.Verify(d => d.Initialize(It.Is<IEnumerable<string>>(paths => paths.Any(p => p.Equals("testadapter.dll"))), It.IsAny<ITestDiscoveryEventsHandler2>()));
            SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsDiscoveryStartShouldStartDiscoveryWithGivenCriteria()
        {
            var message = dataSerializer.SerializePayload(MessageType.StartDiscovery, new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty));
            ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            SendMessageOnChannel(message);
            jobQueue.Flush();

            mockDiscoveryManager.Verify(d => d.DiscoverTests(It.Is<DiscoveryCriteria>(dc => dc.Sources.Contains("test.dll")), It.IsAny<ITestDiscoveryEventsHandler2>()));
            SendSessionEnd();
        }

        [TestMethod]
        public void DiscoveryCompleteShouldSendDiscoveryCompletePayloadOnChannel()
        {
            var discoveryComplete = new DiscoveryCompletePayload { TotalTests = 1, LastDiscoveredTests = Enumerable.Empty<TestCase>(), IsAborted = false };
            var message = dataSerializer.SerializePayload(MessageType.DiscoveryComplete, discoveryComplete);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            requestHandler.DiscoveryComplete(new DiscoveryCompleteEventArgs(discoveryComplete.TotalTests, discoveryComplete.IsAborted), discoveryComplete.LastDiscoveredTests);

            VerifyResponseMessageEquals(message);
            SendSessionEnd();
        }

        #endregion

        #region Execution Protocol

        [TestMethod]
        public void ProcessRequestsExecutionInitializeShouldSetExtensionPaths()
        {
            var message = dataSerializer.SerializePayload(MessageType.ExecutionInitialize, new[] { "testadapter.dll" });
            ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            SendMessageOnChannel(message);
            jobQueue.Flush();

            mockExecutionManager.Verify(e => e.Initialize(It.Is<IEnumerable<string>>(paths => paths.Any(p => p.Equals("testadapter.dll"))), It.IsAny<ITestMessageEventHandler>()));
            SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionStartShouldStartExecutionWithGivenSources()
        {
            var asm = new Dictionary<string, IEnumerable<string>>
            {
                ["mstestv2"] = new[] { "test1.dll", "test2.dll" }
            };
            var testRunCriteriaWithSources = new TestRunCriteriaWithSources(asm, "runsettings", null, null);
            var message = dataSerializer.SerializePayload(MessageType.StartTestExecutionWithSources, testRunCriteriaWithSources);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            SendMessageOnChannel(message);
            jobQueue.Flush();

            mockExecutionManager.Verify(e =>
                e.StartTestRun(
                    It.Is<Dictionary<string, IEnumerable<string>>>(d => d.ContainsKey("mstestv2")), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TestExecutionContext>(), It.IsAny<ITestCaseEventsHandler>(),
                    It.IsAny<ITestRunEventsHandler>()));
            SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionStartShouldStartExecutionWithGivenTests()
        {
            var t1 = new TestCase("N.C.M1", new Uri("executor://mstest/v2"), "test1.dll");
            var t2 = new TestCase("N.C.M2", new Uri("executor://mstest/v2"), "test1.dll");
            var testCases = new [] { t1, t2 };
            var testRunCriteriaWithTests = new TestRunCriteriaWithTests(testCases, "runsettings", null, null);
            var message = dataSerializer.SerializePayload(MessageType.StartTestExecutionWithTests, testRunCriteriaWithTests);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);

            SendMessageOnChannel(message);
            jobQueue.Flush();

            mockExecutionManager.Verify(e =>
                e.StartTestRun(
                    It.Is<IEnumerable<TestCase>>(tcs =>
                        tcs.Any(t => t.FullyQualifiedName.Equals("N.C.M1")) &&
                        tcs.Any(t => t.FullyQualifiedName.Equals("N.C.M2"))), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TestExecutionContext>(), It.IsAny<ITestCaseEventsHandler>(),
                    It.IsAny<ITestRunEventsHandler>()));
            SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionCancelShouldCancelTestRun()
        {
            var message = dataSerializer.SerializePayload(MessageType.CancelTestRun, string.Empty);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);
            SendMessageOnChannel(message);

            mockExecutionManager.Verify(e => e.Cancel(It.IsAny<ITestRunEventsHandler>()));
            SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionLaunchAdapterProcessWithDebuggerShouldSendAckMessage()
        {
            var message = dataSerializer.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, string.Empty);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);
            SendMessageOnChannel(message);
            jobQueue.Flush();

            SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionAbortShouldStopTestRun()
        {
            var message = dataSerializer.SerializePayload(MessageType.AbortTestRun, string.Empty);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);
            SendMessageOnChannel(message);

            mockExecutionManager.Verify(e => e.Abort(It.IsAny<ITestRunEventsHandler>()));
            SendSessionEnd();
        }

        [TestMethod]
        public void SendExecutionCompleteShouldSendTestRunCompletePayloadOnChannel()
        {
            var t1 = new TestCase("N.C.M1", new Uri("executor://mstest/v2"), "test1.dll");
            var t2 = new TestCase("N.C.M2", new Uri("executor://mstest/v2"), "test1.dll");
            var testCases = new[] { t1, t2 };
            var testRunCriteriaWithTests = new TestRunCriteriaWithTests(testCases, "runsettings", null, null);
            var message = dataSerializer.SerializePayload(MessageType.StartTestExecutionWithTests, testRunCriteriaWithTests);
            mockExecutionManager.Setup(em => em.StartTestRun(It.IsAny<IEnumerable<TestCase>>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestExecutionContext>(), It.IsAny<ITestCaseEventsHandler>(),
                It.IsAny<ITestRunEventsHandler>())).Callback(() => requestHandler.SendExecutionComplete(It.IsAny<TestRunCompleteEventArgs>(), It.IsAny<TestRunChangedEventArgs>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<ICollection<string>>()));

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);
            mockChannel.Setup(mc => mc.Send(It.Is<string>(d => d.Contains(MessageType.ExecutionComplete))))
                .Callback<string>(
                    (d) =>
                    {
                        var msg = dataSerializer.DeserializeMessage(d);
                        var payload = dataSerializer.DeserializePayload<TestRunCompletePayload>(msg);
                        Assert.IsNotNull(payload);
                    });
            SendMessageOnChannel(message);
            jobQueue.Flush();

            mockExecutionManager.Verify(e =>
                e.StartTestRun(
                    It.Is<IEnumerable<TestCase>>(tcs =>
                        tcs.Any(t => t.FullyQualifiedName.Equals("N.C.M1")) &&
                        tcs.Any(t => t.FullyQualifiedName.Equals("N.C.M2"))), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TestExecutionContext>(), It.IsAny<ITestCaseEventsHandler>(),
                    It.IsAny<ITestRunEventsHandler>()));
            SendSessionEnd();
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldSendProcessInformationOnChannel()
        {
            var message = dataSerializer.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, "123");

            mockChannel.Setup(mc => mc.Send(It.Is<string>(d => d.Contains(MessageType.LaunchAdapterProcessWithDebuggerAttached))))
                .Callback<string>(
                    (d) =>
                    {
                        var msg = dataSerializer.DeserializeMessage(d);
                        var payload = dataSerializer.DeserializePayload<TestProcessStartInfo>(msg);
                        Assert.IsNotNull(payload);
                        SendMessageOnChannel(message);
                        SendSessionEnd();
                    });

            var task = Task.Run(() => requestHandler.LaunchProcessWithDebuggerAttached(new TestProcessStartInfo()));
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldWaitForProcessIdFromRunner()
        {
            var message = dataSerializer.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, "123");

            mockChannel.Setup(mc => mc.Send(It.Is<string>(d => d.Contains(MessageType.LaunchAdapterProcessWithDebuggerAttached))))
                .Callback<string>(
                    (d) =>
                    {
                        var msg = dataSerializer.DeserializeMessage(d);
                        var payload = dataSerializer.DeserializePayload<TestProcessStartInfo>(msg);
                        Assert.IsNotNull(payload);
                        SendMessageOnChannel(message);
                        SendSessionEnd();
                    });

            var task = Task.Run(() => requestHandler.LaunchProcessWithDebuggerAttached(new TestProcessStartInfo()));

            Assert.AreEqual(123, task.Result);
        }
        #endregion

        #region Logging Protocol
        [TestMethod]
        public void SendLogShouldSendTestMessageWithLevelOnChannel()
        {
            var logMsg = "Testing log message on channel";

            mockChannel.Setup(mc => mc.Send(It.Is<string>(d => d.Contains(MessageType.TestMessage))))
                .Callback<string>(
                    (d) =>
                    {
                        var msg = dataSerializer.DeserializeMessage(d);
                        var payload = dataSerializer.DeserializePayload<TestMessagePayload>(msg);
                        Assert.IsNotNull(payload);
                        Assert.AreEqual(payload.Message, logMsg);
                    });

            requestHandler.SendLog(TestMessageLevel.Informational, "Testing log message on channel");

            SendSessionEnd();
        }
        #endregion

        [TestMethod]
        public void ProcessRequestsEndSessionShouldCloseRequestHandler()
        {

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);
            SendSessionEnd();

            mockCommunicationClient.Verify(mc=>mc.Stop(), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsAbortSessionShouldBeNoOp()
        {
            var message = dataSerializer.SerializePayload(MessageType.SessionAbort, string.Empty);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);
            SendMessageOnChannel(message);

            // Session abort should not close the client
            mockCommunicationClient.Verify(mc => mc.Stop(), Times.Never);
        }

        [TestMethod]
        public void ProcessRequestsInvalidMessageTypeShouldNotThrow()
        {
            var message = dataSerializer.SerializePayload("DummyMessage", string.Empty);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);
            SendMessageOnChannel(message);

            SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsInvalidMessageTypeShouldProcessFutureMessages()
        {
            var message = dataSerializer.SerializePayload("DummyMessage", string.Empty);

            ProcessRequestsAsync(mockTestHostManagerFactory.Object);
            SendMessageOnChannel(message);

            // Should process this message, after the invalid message
            SendSessionEnd();
            mockCommunicationClient.Verify(mc => mc.Stop(), Times.Once);
        }

        [TestMethod]
        public void DisposeShouldStopCommunicationChannel()
        {
            var testRequestHandler = requestHandler as TestRequestHandler;
            Assert.IsNotNull(testRequestHandler);

            testRequestHandler.Dispose();

            mockCommunicationClient.Verify(mc => mc.Stop(), Times.Once);
        }

        private void SendMessageOnChannel(Message message)
        {
            // Setup message to be returned on deserialization of data
            var data = Serialize(message);
            SendMessageOnChannel(data);
        }

        private void SendMessageOnChannel(string data)
        {
            mockChannel.Raise(c => c.MessageReceived += null, new MessageReceivedEventArgs { Data = data });
        }

        private void SendSessionEnd()
        {
            SendMessageOnChannel(new Message { MessageType = MessageType.SessionEnd, Payload = string.Empty });
        }

        private Task ProcessRequestsAsync()
        {
            return Task.Run(() => requestHandler.ProcessRequests(new Mock<ITestHostManagerFactory>().Object));
        }

        private Task ProcessRequestsAsync(ITestHostManagerFactory testHostManagerFactory)
        {
            return Task.Run(() => requestHandler.ProcessRequests(testHostManagerFactory));
        }

        private string Serialize(Message message)
        {
            return dataSerializer.SerializePayload(message.MessageType, message.Payload);
        }

        private void VerifyResponseMessageEquals(string message)
        {
            mockChannel.Verify(mc => mc.Send(It.Is<string>(s => s.Equals(message))));
        }

        private void VerifyResponseMessageContains(string message)
        {
            mockChannel.Verify(mc => mc.Send(It.Is<string>(s => s.Contains(message))));
        }
    }

    public class TestableTestRequestHandler : TestRequestHandler
    {
        public TestableTestRequestHandler(
            TestHostConnectionInfo testHostConnectionInfo,
            ICommunicationEndpointFactory communicationEndpointFactory,
            IDataSerializer dataSerializer,
            JobQueue<Action> jobQueue)
            : base(
                  testHostConnectionInfo,
                  communicationEndpointFactory,
                  dataSerializer,
                  jobQueue,
                  OnLaunchAdapterProcessWithDebuggerAttachedAckReceived,
                  OnAttachDebuggerAckRecieved)
        {
        }

        private static void OnLaunchAdapterProcessWithDebuggerAttachedAckReceived(Message message)
        {
            Assert.AreEqual(message.MessageType, MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback);
        }

        private static void OnAttachDebuggerAckRecieved(Message message)
        {
            Assert.AreEqual(message.MessageType, MessageType.AttachDebuggerCallback);
        }
    }
}
