// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    [TestClass]
    public class ParallelRunEventsHandlerTests
    {
        private ParallelRunEventsHandler parallelRunEventsHandler;

        private Mock<IProxyExecutionManager> mockProxyExecutionManager;

        private Mock<ITestRunEventsHandler> mockTestRunEventsHandler;

        private Mock<IParallelProxyExecutionManager> mockParallelProxyExecutionManager;

        private Mock<IDataSerializer> mockDataSerializer;

        [TestInitialize]
        public void TestInit()
        {
            this.mockProxyExecutionManager = new Mock<IProxyExecutionManager>();
            this.mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            this.mockParallelProxyExecutionManager = new Mock<IParallelProxyExecutionManager>();
            this.mockDataSerializer = new Mock<IDataSerializer>();

            this.parallelRunEventsHandler = new ParallelRunEventsHandler(this.mockProxyExecutionManager.Object,
                this.mockTestRunEventsHandler.Object, this.mockParallelProxyExecutionManager.Object,
                new ParallelRunDataAggregator(), this.mockDataSerializer.Object);
        }

        [TestMethod]
        public void HandleRawMessageShouldSendStatsChangeRawMessageToRunEventsHandler()
        {
            string payload = "RunStats";
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestRunStatsChange, Payload = payload });

            this.parallelRunEventsHandler.HandleRawMessage(payload);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldSendLoggerRawMessageToRunEventsHandler()
        {
            string payload = "LogMessage";
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestMessage, Payload = payload });

            this.parallelRunEventsHandler.HandleRawMessage(payload);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldNotSendRunCompleteEventRawMessageToRunEventsHandler()
        {
            string payload = "ExecComplete";
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.ExecutionComplete, Payload = payload });

            this.parallelRunEventsHandler.HandleRawMessage(payload);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HandleLogMessageShouldJustPassOnTheEventToRunEventsHandler()
        {
            string log = "Hello";
            this.parallelRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, log);

            this.mockTestRunEventsHandler.Verify(mt =>
                mt.HandleLogMessage(TestMessageLevel.Error, log), Times.Once);
        }

        [TestMethod]
        public void HandleRunStatsChangeShouldJustPassOnTheEventToRunEventsHandler()
        {
            var eventArgs = new TestRunChangedEventArgs(null, null, null);
            this.parallelRunEventsHandler.HandleTestRunStatsChange(eventArgs);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(eventArgs), Times.Once);
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldJustPassOnTheEventToRunEventsHandler()
        {
            var testProcessStartInfo = new TestProcessStartInfo();
            this.parallelRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

            this.mockTestRunEventsHandler.Verify(mt => mt.LaunchProcessWithDebuggerAttached(testProcessStartInfo), Times.Once);
        }

        [TestMethod]
        public void HandleRunCompleteShouldNotCallLastChunkResultsIfNotPresent()
        {
            var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.Zero);

            this.mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
                   this.mockProxyExecutionManager.Object, completeArgs, null, null)).Returns(false);

            this.parallelRunEventsHandler.HandleTestRunComplete(completeArgs, null, null);

            // Raw message must be sent 
            this.mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(null), Times.Never);

            this.mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
                this.mockProxyExecutionManager.Object, completeArgs, null, null), Times.Once);
        }

        [TestMethod]
        public void HandleRunCompleteShouldCallLastChunkResultsIfPresent()
        {
            string payload = "RunStats";
            var lastChunk = new TestRunChangedEventArgs(null, null, null);
            var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.Zero);

            this.mockDataSerializer.Setup(mds => mds.SerializePayload(MessageType.TestRunStatsChange, lastChunk))
                .Returns(payload);

            this.mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
                    this.mockProxyExecutionManager.Object, completeArgs, null, null)).Returns(false);

            this.parallelRunEventsHandler.HandleTestRunComplete(completeArgs, lastChunk, null);

            // Raw message must be sent 
            this.mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(lastChunk), Times.Once);

            this.mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
                this.mockProxyExecutionManager.Object, completeArgs, null, null), Times.Once);
        }

        [TestMethod]
        public void HandleRunCompleteShouldCallTestRunCompleteOnActualHandlerIfParallelMaangerReturnsCompleteAsTrue()
        {
            string payload = "ExecComplete";
            var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.Zero);

            this.mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
                    this.mockProxyExecutionManager.Object, completeArgs, null, null)).Returns(true);

            this.mockDataSerializer.Setup(mds => mds.SerializeMessage(MessageType.ExecutionComplete)).Returns(payload);

            this.parallelRunEventsHandler.HandleTestRunComplete(completeArgs, null, null);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(null), Times.Never);

            this.mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
                this.mockProxyExecutionManager.Object, completeArgs, null, null), Times.Once);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Once);

            this.mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<string>>()), Times.Once);
        }
    }
}
