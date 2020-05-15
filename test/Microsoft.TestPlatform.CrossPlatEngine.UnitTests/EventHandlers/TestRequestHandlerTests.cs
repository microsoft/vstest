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
            this.mockCommunicationClient = new Mock<ICommunicationEndPoint>();
            this.mockCommunicationEndpointFactory = new Mock<ICommunicationEndpointFactory>();
            this.mockChannel = new Mock<ICommunicationChannel>();
            this.dataSerializer = JsonDataSerializer.Instance;
            this.testHostConnectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":123",
                Role = ConnectionRole.Client
            };

            this.jobQueue = new JobQueue<Action>(
                action => { action(); },
                "TestHostOperationQueue",
                500,
                25000000,
                true,
                message => EqtTrace.Error(message));

            // Setup mock discovery and execution managers
            this.mockTestHostManagerFactory = new Mock<ITestHostManagerFactory>();
            this.mockDiscoveryManager = new Mock<IDiscoveryManager>();
            this.mockExecutionManager = new Mock<IExecutionManager>();
            this.mockTestHostManagerFactory.Setup(mf => mf.GetDiscoveryManager()).Returns(this.mockDiscoveryManager.Object);
            this.mockTestHostManagerFactory.Setup(mf => mf.GetExecutionManager()).Returns(this.mockExecutionManager.Object);
            this.mockCommunicationEndpointFactory.Setup(f => f.Create(ConnectionRole.Client))
                .Returns(this.mockCommunicationClient.Object);

            this.requestHandler = new TestableTestRequestHandler(
                this.testHostConnectionInfo,
                this.mockCommunicationEndpointFactory.Object,
                JsonDataSerializer.Instance,
                jobQueue);
            this.requestHandler.InitializeCommunication();
            this.mockCommunicationClient.Raise(e => e.Connected += null, new ConnectedEventArgs(this.mockChannel.Object));
        }

        [TestMethod]
        public void InitializeCommunicationShouldConnectToServerAsynchronously()
        {
            this.mockCommunicationClient.Verify(c => c.Start(this.testHostConnectionInfo.Endpoint), Times.Once);
        }

        [TestMethod]
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
            this.mockCommunicationEndpointFactory.Setup(f => f.Create(ConnectionRole.Client))
                .Returns(socketClient);
            var rh = new TestableTestRequestHandler(connectionInfo, this.mockCommunicationEndpointFactory.Object, this.dataSerializer, this.jobQueue);

            rh.InitializeCommunication();
            this.requestHandler.WaitForRequestSenderConnection(1000);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldWaitUntilConnectionIsSetup()
        {
            Assert.IsTrue(this.requestHandler.WaitForRequestSenderConnection(1000));
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldReturnFalseIfConnectionSetupTimesout()
        {
            this.requestHandler = new TestableTestRequestHandler(
                this.testHostConnectionInfo,
                this.mockCommunicationEndpointFactory.Object,
                JsonDataSerializer.Instance,
                jobQueue);
            this.requestHandler.InitializeCommunication();

            Assert.IsFalse(this.requestHandler.WaitForRequestSenderConnection(1));
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessMessagesUntilSessionCompleted()
        {
            var task = this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendSessionEnd();
            Assert.IsTrue(task.Wait(2000));
        }

        #region Version Check Protocol

        [TestMethod]
        public void ProcessRequestsVersionCheckShouldAckMinimumOfGivenAndHighestSupportedVersion()
        {
            var message = new Message { MessageType = MessageType.VersionCheck, Payload = 1 };
            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);

            this.VerifyResponseMessageEquals(this.Serialize(message));
            this.SendSessionEnd();
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
            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);

            this.VerifyResponseMessageContains(EqtTrace.ErrorOnInitialization);
            this.SendSessionEnd();
        }

        #endregion

        #region Discovery Protocol

        [TestMethod]
        public void ProcessRequestsDiscoveryInitializeShouldSetExtensionPaths()
        {
            var message = this.dataSerializer.SerializePayload(MessageType.DiscoveryInitialize, new[] { "testadapter.dll" });
            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            this.mockDiscoveryManager.Verify(d => d.Initialize(It.Is<IEnumerable<string>>(paths => paths.Any(p => p.Equals("testadapter.dll"))), It.IsAny<ITestDiscoveryEventsHandler2>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsDiscoveryStartShouldStartDiscoveryWithGivenCriteria()
        {
            var message = this.dataSerializer.SerializePayload(MessageType.StartDiscovery, new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty));
            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            this.mockDiscoveryManager.Verify(d => d.DiscoverTests(It.Is<DiscoveryCriteria>(dc => dc.Sources.Contains("test.dll")), It.IsAny<ITestDiscoveryEventsHandler2>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void DiscoveryCompleteShouldSendDiscoveryCompletePayloadOnChannel()
        {
            var discoveryComplete = new DiscoveryCompletePayload { TotalTests = 1, LastDiscoveredTests = Enumerable.Empty<TestCase>(), IsAborted = false };
            var message = this.dataSerializer.SerializePayload(MessageType.DiscoveryComplete, discoveryComplete);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.requestHandler.DiscoveryComplete(new DiscoveryCompleteEventArgs(discoveryComplete.TotalTests, discoveryComplete.IsAborted), discoveryComplete.LastDiscoveredTests);

            this.VerifyResponseMessageEquals(message);
            this.SendSessionEnd();
        }

        #endregion

        #region Execution Protocol

        [TestMethod]
        public void ProcessRequestsExecutionInitializeShouldSetExtensionPaths()
        {
            var message = this.dataSerializer.SerializePayload(MessageType.ExecutionInitialize, new[] { "testadapter.dll" });
            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            this.mockExecutionManager.Verify(e => e.Initialize(It.Is<IEnumerable<string>>(paths => paths.Any(p => p.Equals("testadapter.dll"))), It.IsAny<ITestMessageEventHandler>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionStartShouldStartExecutionWithGivenSources()
        {
            var asm = new Dictionary<string, IEnumerable<string>>();
            asm["mstestv2"] = new[] {"test1.dll", "test2.dll"};
            var testRunCriteriaWithSources = new TestRunCriteriaWithSources(asm, "runsettings", null, null);
            var message = this.dataSerializer.SerializePayload(MessageType.StartTestExecutionWithSources, testRunCriteriaWithSources);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            mockExecutionManager.Verify(e =>
                e.StartTestRun(
                    It.Is<Dictionary<string, IEnumerable<string>>>(d => d.ContainsKey("mstestv2")), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TestExecutionContext>(), It.IsAny<ITestCaseEventsHandler>(),
                    It.IsAny<ITestRunEventsHandler>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionStartShouldStartExecutionWithGivenTests()
        {
            var t1 = new TestCase("N.C.M1", new Uri("executor://mstest/v2"), "test1.dll");
            var t2 = new TestCase("N.C.M2", new Uri("executor://mstest/v2"), "test1.dll");
            var testCases = new [] { t1, t2 };
            var testRunCriteriaWithTests = new TestRunCriteriaWithTests(testCases, "runsettings", null, null);
            var message = this.dataSerializer.SerializePayload(MessageType.StartTestExecutionWithTests, testRunCriteriaWithTests);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            mockExecutionManager.Verify(e =>
                e.StartTestRun(
                    It.Is<IEnumerable<TestCase>>(tcs =>
                        tcs.Any(t => t.FullyQualifiedName.Equals("N.C.M1")) &&
                        tcs.Any(t => t.FullyQualifiedName.Equals("N.C.M2"))), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TestExecutionContext>(), It.IsAny<ITestCaseEventsHandler>(),
                    It.IsAny<ITestRunEventsHandler>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionCancelShouldCancelTestRun()
        {
            var message = this.dataSerializer.SerializePayload(MessageType.CancelTestRun, string.Empty);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);
            this.SendMessageOnChannel(message);

            mockExecutionManager.Verify(e => e.Cancel(It.IsAny<ITestRunEventsHandler>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionLaunchAdapterProcessWithDebuggerShouldSendAckMessage()
        {
            var message = this.dataSerializer.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, string.Empty);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);
            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionAbortShouldStopTestRun()
        {
            var message = this.dataSerializer.SerializePayload(MessageType.AbortTestRun, string.Empty);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);
            this.SendMessageOnChannel(message);

            mockExecutionManager.Verify(e => e.Abort(It.IsAny<ITestRunEventsHandler>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void SendExecutionCompleteShouldSendTestRunCompletePayloadOnChannel()
        {
            var t1 = new TestCase("N.C.M1", new Uri("executor://mstest/v2"), "test1.dll");
            var t2 = new TestCase("N.C.M2", new Uri("executor://mstest/v2"), "test1.dll");
            var testCases = new[] { t1, t2 };
            var testRunCriteriaWithTests = new TestRunCriteriaWithTests(testCases, "runsettings", null, null);
            var message = this.dataSerializer.SerializePayload(MessageType.StartTestExecutionWithTests, testRunCriteriaWithTests);
            this.mockExecutionManager.Setup(em => em.StartTestRun(It.IsAny<IEnumerable<TestCase>>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestExecutionContext>(), It.IsAny<ITestCaseEventsHandler>(),
                It.IsAny<ITestRunEventsHandler>())).Callback(() =>
            {
                this.requestHandler.SendExecutionComplete(It.IsAny<TestRunCompleteEventArgs>(), It.IsAny<TestRunChangedEventArgs>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<ICollection<string>>());
            });

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);
            this.mockChannel.Setup(mc => mc.Send(It.Is<string>(d => d.Contains(MessageType.ExecutionComplete))))
                .Callback<string>(
                    (d) =>
                    {
                        var msg = this.dataSerializer.DeserializeMessage(d);
                        var payload = this.dataSerializer.DeserializePayload<TestRunCompletePayload>(msg);
                        Assert.IsNotNull(payload);
                    });
            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            mockExecutionManager.Verify(e =>
                e.StartTestRun(
                    It.Is<IEnumerable<TestCase>>(tcs =>
                        tcs.Any(t => t.FullyQualifiedName.Equals("N.C.M1")) &&
                        tcs.Any(t => t.FullyQualifiedName.Equals("N.C.M2"))), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TestExecutionContext>(), It.IsAny<ITestCaseEventsHandler>(),
                    It.IsAny<ITestRunEventsHandler>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldSendProcessInformationOnChannel()
        {
            var message = this.dataSerializer.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, "123");

            this.mockChannel.Setup(mc => mc.Send(It.Is<string>(d => d.Contains(MessageType.LaunchAdapterProcessWithDebuggerAttached))))
                .Callback<string>(
                    (d) =>
                    {
                        var msg = this.dataSerializer.DeserializeMessage(d);
                        var payload = this.dataSerializer.DeserializePayload<TestProcessStartInfo>(msg);
                        Assert.IsNotNull(payload);
                        this.SendMessageOnChannel(message);
                        this.SendSessionEnd();
                    });

            var task = Task.Run(() => this.requestHandler.LaunchProcessWithDebuggerAttached(new TestProcessStartInfo()));
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldWaitForProcessIdFromRunner()
        {
            var message = dataSerializer.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, "123");

            this.mockChannel.Setup(mc => mc.Send(It.Is<string>(d => d.Contains(MessageType.LaunchAdapterProcessWithDebuggerAttached))))
                .Callback<string>(
                    (d) =>
                    {
                        var msg = this.dataSerializer.DeserializeMessage(d);
                        var payload = this.dataSerializer.DeserializePayload<TestProcessStartInfo>(msg);
                        Assert.IsNotNull(payload);
                        this.SendMessageOnChannel(message);
                        this.SendSessionEnd();
                    });

            var task = Task.Run(() => this.requestHandler.LaunchProcessWithDebuggerAttached(new TestProcessStartInfo()));

            Assert.AreEqual(123, task.Result);
        }
        #endregion

        #region Logging Protocol
        [TestMethod]
        public void SendLogShouldSendTestMessageWithLevelOnChannel()
        {
            var logMsg = "Testing log message on channel";

            this.mockChannel.Setup(mc => mc.Send(It.Is<string>(d => d.Contains(MessageType.TestMessage))))
                .Callback<string>(
                    (d) =>
                    {
                        var msg = this.dataSerializer.DeserializeMessage(d);
                        var payload = this.dataSerializer.DeserializePayload<TestMessagePayload>(msg);
                        Assert.IsNotNull(payload);
                        Assert.AreEqual(payload.Message, logMsg);
                    });

            this.requestHandler.SendLog(TestMessageLevel.Informational, "Testing log message on channel");

            this.SendSessionEnd();
        }
        #endregion

        [TestMethod]
        public void ProcessRequestsEndSessionShouldCloseRequestHandler()
        {

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);
            this.SendSessionEnd();

            this.mockCommunicationClient.Verify(mc=>mc.Stop(), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsAbortSessionShouldBeNoOp()
        {
            var message = dataSerializer.SerializePayload(MessageType.SessionAbort, string.Empty);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);
            this.SendMessageOnChannel(message);

            // Session abort should not close the client
            this.mockCommunicationClient.Verify(mc => mc.Stop(), Times.Never);
        }

        [TestMethod]
        public void ProcessRequestsInvalidMessageTypeShouldNotThrow()
        {
            var message = dataSerializer.SerializePayload("DummyMessage", string.Empty);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);
            this.SendMessageOnChannel(message);

            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsInvalidMessageTypeShouldProcessFutureMessages()
        {
            var message = dataSerializer.SerializePayload("DummyMessage", string.Empty);

            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);
            this.SendMessageOnChannel(message);

            // Should process this message, after the invalid message
            this.SendSessionEnd();
            this.mockCommunicationClient.Verify(mc => mc.Stop(), Times.Once);
        }

        [TestMethod]
        public void DisposeShouldStopCommunicationChannel()
        {
            var testRequestHandler = this.requestHandler as TestRequestHandler;
            Assert.IsNotNull(testRequestHandler);

            testRequestHandler.Dispose();

            this.mockCommunicationClient.Verify(mc => mc.Stop(), Times.Once);
        }

        private void SendMessageOnChannel(Message message)
        {
            // Setup message to be returned on deserialization of data
            var data = this.Serialize(message);
            this.SendMessageOnChannel(data);
        }

        private void SendMessageOnChannel(string data)
        {
            this.mockChannel.Raise(c => c.MessageReceived += null, new MessageReceivedEventArgs { Data = data });
        }

        private void SendSessionEnd()
        {
            this.SendMessageOnChannel(new Message { MessageType = MessageType.SessionEnd, Payload = string.Empty });
        }

        private Task ProcessRequestsAsync()
        {
            return Task.Run(() => this.requestHandler.ProcessRequests(new Mock<ITestHostManagerFactory>().Object));
        }

        private Task ProcessRequestsAsync(ITestHostManagerFactory testHostManagerFactory)
        {
            return Task.Run(() => this.requestHandler.ProcessRequests(testHostManagerFactory));
        }

        private string Serialize(Message message)
        {
            return this.dataSerializer.SerializePayload(message.MessageType, message.Payload);
        }

        private void VerifyResponseMessageEquals(string message)
        {
            this.mockChannel.Verify(mc => mc.Send(It.Is<string>(s => s.Equals(message))));
        }

        private void VerifyResponseMessageContains(string message)
        {
            this.mockChannel.Verify(mc => mc.Send(It.Is<string>(s => s.Contains(message))));
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
