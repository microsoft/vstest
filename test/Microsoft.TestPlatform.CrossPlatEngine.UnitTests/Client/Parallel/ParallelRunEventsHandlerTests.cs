// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
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

    [TestClass]
    public class ParallelRunEventsHandlerTests
    {
        private ParallelRunEventsHandler parallelRunEventsHandler;

        private Mock<IProxyExecutionManager> mockProxyExecutionManager;

        private Mock<ITestRunEventsHandler> mockTestRunEventsHandler;

        private Mock<IParallelProxyExecutionManager> mockParallelProxyExecutionManager;

        private Mock<IDataSerializer> mockDataSerializer;

        private Mock<IRequestData> mockRequestData;

        [TestInitialize]
        public void TestInit()
        {
            mockProxyExecutionManager = new Mock<IProxyExecutionManager>();
            mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            mockParallelProxyExecutionManager = new Mock<IParallelProxyExecutionManager>();
            mockDataSerializer = new Mock<IDataSerializer>();
            mockRequestData = new Mock<IRequestData>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());

            parallelRunEventsHandler = new ParallelRunEventsHandler(mockRequestData.Object, mockProxyExecutionManager.Object,
                mockTestRunEventsHandler.Object, mockParallelProxyExecutionManager.Object,
                new ParallelRunDataAggregator(Constants.EmptyRunSettings), mockDataSerializer.Object);
        }

        [TestMethod]
        public void HandleRawMessageShouldSendStatsChangeRawMessageToRunEventsHandler()
        {
            string payload = "RunStats";
            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestRunStatsChange, Payload = payload });

            parallelRunEventsHandler.HandleRawMessage(payload);

            mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldSendLoggerRawMessageToRunEventsHandler()
        {
            string payload = "LogMessage";
            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestMessage, Payload = payload });

            parallelRunEventsHandler.HandleRawMessage(payload);

            mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldNotSendRunCompleteEventRawMessageToRunEventsHandler()
        {
            string payload = "ExecComplete";
            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.ExecutionComplete, Payload = payload });

            parallelRunEventsHandler.HandleRawMessage(payload);

            mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HandleLogMessageShouldJustPassOnTheEventToRunEventsHandler()
        {
            string log = "Hello";
            parallelRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, log);

            mockTestRunEventsHandler.Verify(mt =>
                mt.HandleLogMessage(TestMessageLevel.Error, log), Times.Once);
        }

        [TestMethod]
        public void HandleRunStatsChangeShouldJustPassOnTheEventToRunEventsHandler()
        {
            var eventArgs = new TestRunChangedEventArgs(null, null, null);
            parallelRunEventsHandler.HandleTestRunStatsChange(eventArgs);

            mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(eventArgs), Times.Once);
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldJustPassOnTheEventToRunEventsHandler()
        {
            var testProcessStartInfo = new TestProcessStartInfo();
            parallelRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

            mockTestRunEventsHandler.Verify(mt => mt.LaunchProcessWithDebuggerAttached(testProcessStartInfo), Times.Once);
        }

        [TestMethod]
        public void HandleRunCompleteShouldNotCallLastChunkResultsIfNotPresent()
        {
            var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero);

             mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
                    mockProxyExecutionManager.Object, completeArgs, null, null, null)).Returns(false);

            parallelRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);

            // Raw message must be sent
            mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);

            mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(null), Times.Never);

            mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
                mockProxyExecutionManager.Object, completeArgs, null, null, null), Times.Once);
        }

        [TestMethod]
        public void HandleRunCompleteShouldCallLastChunkResultsIfPresent()
        {
            string payload = "RunStats";
            var lastChunk = new TestRunChangedEventArgs(null, null, null);
            var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero);

            mockDataSerializer.Setup(mds => mds.SerializePayload(MessageType.TestRunStatsChange, lastChunk))
                .Returns(payload);

            mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
                    mockProxyExecutionManager.Object, completeArgs, null, null, null)).Returns(false);

            parallelRunEventsHandler.HandleTestRunComplete(completeArgs, lastChunk, null, null);

            // Raw message must be sent
            mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);

            mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(lastChunk), Times.Once);

            mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
                mockProxyExecutionManager.Object, completeArgs, null, null, null), Times.Once);
        }

        [TestMethod]
        public void HandleRunCompleteShouldCallTestRunCompleteOnActualHandlerIfParallelMaangerReturnsCompleteAsTrue()
        {
            string payload = "ExecComplete";
            var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero);

            mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
                    mockProxyExecutionManager.Object, completeArgs, null, null, null)).Returns(true);

            mockDataSerializer.Setup(mds => mds.SerializeMessage(MessageType.ExecutionComplete)).Returns(payload);

            parallelRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);

            mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(null), Times.Never);

            mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
                mockProxyExecutionManager.Object, completeArgs, null, null, null), Times.Once);

            mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Once);

            mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>()), Times.Once);
        }

        [TestMethod]
        public void HandleRunCompleteShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero);

            mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
                mockProxyExecutionManager.Object, completeArgs, null, null, null)).Returns(true);

            // Act
            parallelRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.RunState, It.IsAny<string>()), Times.Once);
        }
    }
}
