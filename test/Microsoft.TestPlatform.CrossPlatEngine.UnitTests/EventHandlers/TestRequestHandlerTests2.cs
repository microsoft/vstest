// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class TestRequestHandlerTests2
    {
        private readonly Mock<ICommunicationClient> mockCommunicationClient;
        private readonly Mock<ICommunicationChannel> mockChannel;
        private readonly Mock<IDataSerializer> mockDataSerializer;

        private readonly ITestRequestHandler requestHandler;

        public TestRequestHandlerTests2()
        {
            this.mockCommunicationClient = new Mock<ICommunicationClient>();
            this.mockChannel = new Mock<ICommunicationChannel>();
            this.mockDataSerializer = new Mock<IDataSerializer>();

            this.requestHandler = new TestableTestRequestHandler(this.mockCommunicationClient.Object, this.mockDataSerializer.Object);
        }

        [TestMethod]
        public void InitializeCommunicationShouldConnectToServerAsynchronously()
        {
            this.requestHandler.InitializeCommunication(123);

            this.mockCommunicationClient.Verify(c => c.Start("123"), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldThrowIfServerIsNotAccessible()
        {
            var rh = new TestableTestRequestHandler(new SocketClient(), this.mockDataSerializer.Object);

            ////Assert.ThrowsException<IOException>(() => { rh.InitializeCommunication(123); rh.WaitForRequestSenderConnection(1000); });
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldWaitUntilConnectionIsSetup()
        {
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldReturnFalseIfConnectionSetupTimesout()
        {
        }

        [TestMethod]
        public void ProcessRequestsShouldWaitForMessageReceivedOnChannel()
        {
        }

        #region Version Check Protocol
        // ProcessRequestsVersionCheckShouldAckMinimumOfGivenAndHighestSupportedVersion -- data test
        // ProcessRequestsVersionCheckShouldLogDiagnosticsFilePath
        // ProcessRequestsVersionCheckShouldLogErrorIfDiagnosticsEnableFails
        #endregion

        #region Discovery Protocol
        // ProcessRequestsDiscoveryInitializeShouldSetExtensionPaths
        // ProcessRequestsDiscoveryStartShouldStartDiscoveryWithGivenCriteria

        // DiscoveryCompleteShouldSendDiscoveryCompletePayloadOnChannel
        #endregion

        #region Execution Protocol
        // ProcessRequestsExecutionInitializeShouldSetExtensionPaths
        // ProcessRequestsExecutionStartShouldStartExecutionWithGivenSources
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
    }

    public class TestableTestRequestHandler : TestRequestHandler2
    {
        public TestableTestRequestHandler(ICommunicationClient communicationClient, IDataSerializer dataSerializer)
            : base(communicationClient, dataSerializer)
        {
        }
    }
}
