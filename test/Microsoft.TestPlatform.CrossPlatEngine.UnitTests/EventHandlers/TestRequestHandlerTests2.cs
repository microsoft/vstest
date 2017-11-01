// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
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
    public class TestRequestHandlerTests2
    {
        private readonly Mock<ICommunicationEndPoint> mockCommunicationClient;
        private readonly Mock<ICommunicationChannel> mockChannel;
        private readonly Mock<ITestHostManagerFactory> mockTestHostManagerFactory;
        private readonly Mock<IDiscoveryManager> mockDiscoveryManager;
        private readonly Mock<IExecutionManager> mockExecutionManager;

        private readonly JsonDataSerializer dataSerializer;
        private readonly ITestRequestHandler requestHandler;
        private readonly TestHostConnectionInfo testHostConnectionInfo;
        private readonly JobQueue<Action> jobQueue;

        public TestRequestHandlerTests2()
        {
            this.mockCommunicationClient = new Mock<ICommunicationEndPoint>();
            this.mockChannel = new Mock<ICommunicationChannel>();
            this.dataSerializer = JsonDataSerializer.Instance;
            this.testHostConnectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":123",
                Role = ConnectionRole.Host
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

            this.requestHandler = new TestableTestRequestHandler(
                this.testHostConnectionInfo,
                this.mockCommunicationClient.Object,
                JsonDataSerializer.Instance,
                jobQueue);
        }

        [TestMethod]
        public void InitializeCommunicationShouldConnectToServerAsynchronously()
        {
            this.requestHandler.InitializeCommunication();

            this.mockCommunicationClient.Verify(c => c.Start(this.testHostConnectionInfo.Endpoint), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldThrowIfServerIsNotAccessible()
        {
            var connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Any + ":123",
                Role = ConnectionRole.Host
            };

            var rh = new TestableTestRequestHandler(connectionInfo, new SocketClient(), this.dataSerializer, this.jobQueue);

            Assert.ThrowsException<SocketException>(() => { rh.InitializeCommunication(); this.requestHandler.WaitForRequestSenderConnection(5 * 1000); });
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldWaitUntilConnectionIsSetup()
        {
            this.SetupChannel();

            Assert.IsTrue(this.requestHandler.WaitForRequestSenderConnection(1000));
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldReturnFalseIfConnectionSetupTimesout()
        {
            this.requestHandler.InitializeCommunication();

            Assert.IsFalse(this.requestHandler.WaitForRequestSenderConnection(1));
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessMessagesUntilSessionCompleted()
        {
            this.SetupChannel();

            var task = this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendSessionEnd();
            Assert.IsTrue(task.Wait(5));
        }

        #region Version Check Protocol

        [TestMethod]
        public void ProcessRequestsVersionCheckShouldAckMinimumOfGivenAndHighestSupportedVersion()
        {
            var message = new Message { MessageType = MessageType.VersionCheck, Payload = 1 };
            this.SetupChannel();
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
            this.SetupChannel();
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
            this.SetupChannel();
            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            this.mockDiscoveryManager.Verify(d => d.Initialize(It.Is<IEnumerable<string>>(paths => paths.Any(p => p.Equals("testadapter.dll")))));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsDiscoveryStartShouldStartDiscoveryWithGivenCriteria()
        {
            var message = this.dataSerializer.SerializePayload(MessageType.StartDiscovery, new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty));
            this.SetupChannel();
            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);

            this.mockDiscoveryManager.Verify(d => d.DiscoverTests(It.Is<DiscoveryCriteria>(dc => dc.Sources.Contains("test.dll")), It.IsAny<ITestDiscoveryEventsHandler2>()));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void DiscoveryCompleteShouldSendDiscoveryCompletePayloadOnChannel()
        {
            var discoveryComplete = new DiscoveryCompletePayload { TotalTests = 1, LastDiscoveredTests = Enumerable.Empty<TestCase>(), IsAborted = false };
            var message = this.dataSerializer.SerializePayload(MessageType.DiscoveryComplete, discoveryComplete);
            this.SetupChannel();

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
            this.SetupChannel();

            //this.ProcessRequestsAsync();
            this.ProcessRequestsAsync(this.mockTestHostManagerFactory.Object);

            this.SendMessageOnChannel(message);
            this.jobQueue.Flush();

            this.mockExecutionManager.Verify(e => e.Initialize(It.Is<IEnumerable<string>>(paths => paths.Any(p => p.Equals("testadapter.dll")))));
            this.SendSessionEnd();
        }

        [TestMethod]
        public void ProcessRequestsExecutionStartShouldStartExecutionWithGivenSources()
        {
        }

        // ProcessRequestsExecutionStartShouldStartExecutionWithGivenTests
        // ProcessRequestsExecutionCancelShouldCancelTestRun
        // ProcessRequestsExecutionCancelShouldStopRequestProcessing
        // ProcessRequestsExecutionLaunchAdapterProcessWithDebuggerShouldSendAckMessage
        // ProcessRequestsExecutionAbortShouldStopTestRun
        // ProcessRequestsExecutionAbortShouldStopRequestProcessing

        // SendExecutionCompleteShouldSendTestRunCompletePayloadOnChannel
        // LaunchProcessWithDebuggerAttachedShouldSendProcessInformationOnChannel
        // LaunchProcessWithDebuggerAttachedShouldWaitForProcessIdFromRunner
        #endregion

        #region Logging Protocol
        // SendLogShouldSendTestMessageWithLevel
        #endregion

        // ProcessRequestsEndSessionShouldCloseRequestHandler
        // ProcessRequestsAbortSessionShouldBeNoOp
        // ProcessRequestsInvalidMessageTypeShouldNotThrow
        // ProcessRequestsInvalidMessageTypeShouldProcessFutureMessages

        // CloseShouldStopCommunicationChannel
        // DisposeShouldStopCommunicationChannel

        private void SetupChannel()
        {
            this.requestHandler.InitializeCommunication();
            this.mockCommunicationClient.Raise(e => e.Connected += null, new ConnectedEventArgs(this.mockChannel.Object));
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
        public TestableTestRequestHandler(TestHostConnectionInfo testHostConnectionInfo,ICommunicationEndPoint communicationClient, IDataSerializer dataSerializer, JobQueue<Action> jobQueue)
            : base(testHostConnectionInfo, communicationClient, dataSerializer, jobQueue)
        {
        }
    }
}
