// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client;

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
    private ParallelRunEventsHandler _parallelRunEventsHandler;

    private Mock<IProxyExecutionManager> _mockProxyExecutionManager;

    private Mock<ITestRunEventsHandler> _mockTestRunEventsHandler;

    private Mock<IParallelProxyExecutionManager> _mockParallelProxyExecutionManager;

    private Mock<IDataSerializer> _mockDataSerializer;

    private Mock<IRequestData> _mockRequestData;

    [TestInitialize]
    public void TestInit()
    {
        _mockProxyExecutionManager = new Mock<IProxyExecutionManager>();
        _mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
        _mockParallelProxyExecutionManager = new Mock<IParallelProxyExecutionManager>();
        _mockDataSerializer = new Mock<IDataSerializer>();
        _mockRequestData = new Mock<IRequestData>();
        _mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());

        _parallelRunEventsHandler = new ParallelRunEventsHandler(_mockRequestData.Object, _mockProxyExecutionManager.Object,
            _mockTestRunEventsHandler.Object, _mockParallelProxyExecutionManager.Object,
            new ParallelRunDataAggregator(Constants.EmptyRunSettings), _mockDataSerializer.Object);
    }

    [TestMethod]
    public void HandleRawMessageShouldSendStatsChangeRawMessageToRunEventsHandler()
    {
        string payload = "RunStats";
        _mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
            .Returns(new Message() { MessageType = MessageType.TestRunStatsChange, Payload = payload });

        _parallelRunEventsHandler.HandleRawMessage(payload);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
    }

    [TestMethod]
    public void HandleRawMessageShouldSendLoggerRawMessageToRunEventsHandler()
    {
        string payload = "LogMessage";
        _mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
            .Returns(new Message() { MessageType = MessageType.TestMessage, Payload = payload });

        _parallelRunEventsHandler.HandleRawMessage(payload);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
    }

    [TestMethod]
    public void HandleRawMessageShouldNotSendRunCompleteEventRawMessageToRunEventsHandler()
    {
        string payload = "ExecComplete";
        _mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
            .Returns(new Message() { MessageType = MessageType.ExecutionComplete, Payload = payload });

        _parallelRunEventsHandler.HandleRawMessage(payload);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void HandleLogMessageShouldJustPassOnTheEventToRunEventsHandler()
    {
        string log = "Hello";
        _parallelRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, log);

        _mockTestRunEventsHandler.Verify(mt =>
            mt.HandleLogMessage(TestMessageLevel.Error, log), Times.Once);
    }

    [TestMethod]
    public void HandleRunStatsChangeShouldJustPassOnTheEventToRunEventsHandler()
    {
        var eventArgs = new TestRunChangedEventArgs(null, null, null);
        _parallelRunEventsHandler.HandleTestRunStatsChange(eventArgs);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(eventArgs), Times.Once);
    }

    [TestMethod]
    public void LaunchProcessWithDebuggerAttachedShouldJustPassOnTheEventToRunEventsHandler()
    {
        var testProcessStartInfo = new TestProcessStartInfo();
        _parallelRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

        _mockTestRunEventsHandler.Verify(mt => mt.LaunchProcessWithDebuggerAttached(testProcessStartInfo), Times.Once);
    }

    [TestMethod]
    public void HandleRunCompleteShouldNotCallLastChunkResultsIfNotPresent()
    {
        var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero);

        _mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
            _mockProxyExecutionManager.Object, completeArgs, null, null, null)).Returns(false);

        _parallelRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);

        // Raw message must be sent
        _mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(null), Times.Never);

        _mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
            _mockProxyExecutionManager.Object, completeArgs, null, null, null), Times.Once);
    }

    [TestMethod]
    public void HandleRunCompleteShouldCallLastChunkResultsIfPresent()
    {
        string payload = "RunStats";
        var lastChunk = new TestRunChangedEventArgs(null, null, null);
        var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero);

        _mockDataSerializer.Setup(mds => mds.SerializePayload(MessageType.TestRunStatsChange, lastChunk))
            .Returns(payload);

        _mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
            _mockProxyExecutionManager.Object, completeArgs, null, null, null)).Returns(false);

        _parallelRunEventsHandler.HandleTestRunComplete(completeArgs, lastChunk, null, null);

        // Raw message must be sent
        _mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(lastChunk), Times.Once);

        _mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
            _mockProxyExecutionManager.Object, completeArgs, null, null, null), Times.Once);
    }

    [TestMethod]
    public void HandleRunCompleteShouldCallTestRunCompleteOnActualHandlerIfParallelMaangerReturnsCompleteAsTrue()
    {
        string payload = "ExecComplete";
        var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero);

        _mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
            _mockProxyExecutionManager.Object, completeArgs, null, null, null)).Returns(true);

        _mockDataSerializer.Setup(mds => mds.SerializeMessage(MessageType.ExecutionComplete)).Returns(payload);

        _parallelRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunStatsChange(null), Times.Never);

        _mockParallelProxyExecutionManager.Verify(mp => mp.HandlePartialRunComplete(
            _mockProxyExecutionManager.Object, completeArgs, null, null, null), Times.Once);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Once);

        _mockTestRunEventsHandler.Verify(mt => mt.HandleTestRunComplete(
            It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(),
            It.IsAny<ICollection<AttachmentSet>>(),
            It.IsAny<ICollection<string>>()), Times.Once);
    }

    [TestMethod]
    public void HandleRunCompleteShouldCollectMetrics()
    {
        var mockMetricsCollector = new Mock<IMetricsCollection>();
        _mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

        var completeArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero);

        _mockParallelProxyExecutionManager.Setup(mp => mp.HandlePartialRunComplete(
            _mockProxyExecutionManager.Object, completeArgs, null, null, null)).Returns(true);

        // Act
        _parallelRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);

        // Verify.
        mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.RunState, It.IsAny<string>()), Times.Once);
    }
}
