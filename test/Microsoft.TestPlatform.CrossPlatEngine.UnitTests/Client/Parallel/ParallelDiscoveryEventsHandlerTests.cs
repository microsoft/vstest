// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
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
    public class ParallelDiscoveryEventsHandlerTests
    {
        private ParallelDiscoveryEventsHandler parallelDiscoveryEventsHandler;

        private Mock<IProxyDiscoveryManager> mockProxyDiscoveryManager;

        private Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler;

        private Mock<IParallelProxyDiscoveryManager> mockParallelProxyDiscoveryManager;

        private Mock<IDataSerializer> mockDataSerializer;

        private Mock<IRequestData> mockRequestData;

        [TestInitialize]
        public void TestInit()
        {
            mockProxyDiscoveryManager = new Mock<IProxyDiscoveryManager>();
            mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            mockParallelProxyDiscoveryManager = new Mock<IParallelProxyDiscoveryManager>();
            mockDataSerializer = new Mock<IDataSerializer>();
            mockRequestData = new Mock<IRequestData>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());

            parallelDiscoveryEventsHandler = new ParallelDiscoveryEventsHandler(mockRequestData.Object, mockProxyDiscoveryManager.Object,
                mockTestDiscoveryEventsHandler.Object, mockParallelProxyDiscoveryManager.Object,
                new ParallelDiscoveryDataAggregator(), mockDataSerializer.Object);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldNotCallLastChunkResultsIfNotPresent()
        {
            int totalTests = 10;
            bool aborted = false;
            mockParallelProxyDiscoveryManager.Setup(mp => mp.HandlePartialDiscoveryComplete(
                   mockProxyDiscoveryManager.Object, totalTests, null, aborted)).Returns(false);

            var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalTests, aborted);

            parallelDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, null);

            // Raw message must be sent 
            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);

            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveredTests(null), Times.Never);

            mockParallelProxyDiscoveryManager.Verify(mp => mp.HandlePartialDiscoveryComplete(
                mockProxyDiscoveryManager.Object, totalTests, null, aborted), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCallLastChunkResultsIfPresent()
        {
            string payload = "Tests";
            int totalTests = 10;
            bool aborted = false;
            var lastChunk = new List<TestCase>();

            mockDataSerializer.Setup(mds => mds.SerializePayload(MessageType.TestCasesFound, lastChunk))
                .Returns(payload);

            mockParallelProxyDiscoveryManager.Setup(mp => mp.HandlePartialDiscoveryComplete(
                    mockProxyDiscoveryManager.Object, totalTests, lastChunk, aborted)).Returns(false);

            var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalTests, aborted);

            parallelDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, lastChunk);

            // Raw message must be sent
            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);

            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveredTests(lastChunk), Times.Once);

            mockParallelProxyDiscoveryManager.Verify(mp => mp.HandlePartialDiscoveryComplete(
                mockProxyDiscoveryManager.Object, totalTests, null, aborted), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCollectMetrics()
        {
            string payload = "DiscoveryComplete";
            int totalTests = 10;
            bool aborted = false;

            mockParallelProxyDiscoveryManager.Setup(mp => mp.HandlePartialDiscoveryComplete(
                mockProxyDiscoveryManager.Object, totalTests, null, aborted)).Returns(true);

            mockDataSerializer.Setup(mds => mds.SerializeMessage(MessageType.DiscoveryComplete)).Returns(payload);

            var mockMetricsCollector = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalTests, aborted);

            // Act.
            parallelDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, null);

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.DiscoveryState, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCallTestDiscoveryCompleteOnActualHandlerIfParallelManagerReturnsCompleteAsTrue()
        {
            string payload = "DiscoveryComplete";
            int totalTests = 10;
            bool aborted = false;

            mockParallelProxyDiscoveryManager.Setup(mp => mp.HandlePartialDiscoveryComplete(
                    mockProxyDiscoveryManager.Object, totalTests, null, aborted)).Returns(true);

            mockDataSerializer.Setup(mds => mds.SerializeMessage(MessageType.DiscoveryComplete)).Returns(payload);

            // Act
            var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalTests, aborted);

            parallelDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, null);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveredTests(null), Times.Never);

            mockParallelProxyDiscoveryManager.Verify(mp => mp.HandlePartialDiscoveryComplete(
                mockProxyDiscoveryManager.Object, totalTests, null, aborted), Times.Once);

            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Once);

            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryTestsShouldJustPassOnTheEventToDiscoveryEventsHandler()
        {
            var tests = new List<TestCase>();
            parallelDiscoveryEventsHandler.HandleDiscoveredTests(tests);

            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveredTests(tests), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldSendTestCasesFoundRawMessageToDiscoveryEventsHandler()
        {
            string payload = "Tests";
            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestCasesFound, Payload = payload });

            parallelDiscoveryEventsHandler.HandleRawMessage(payload);

            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldNotSendDiscoveryCompleteEventRawMessageToDiscoveryEventsHandler()
        {
            string payload = "DiscoveryComplete";
            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.DiscoveryComplete, Payload = payload });

            parallelDiscoveryEventsHandler.HandleRawMessage(payload);

            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HandleRawMessageShouldSendLoggerRawMessageToDiscoveryEventsHandler()
        {
            string payload = "LogMessage";
            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestMessage, Payload = payload });

            parallelDiscoveryEventsHandler.HandleRawMessage(payload);

            mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
        }

        [TestMethod]
        public void HandleLogMessageShouldJustPassOnTheEventToDiscoveryEventsHandler()
        {
            string log = "Hello";
            parallelDiscoveryEventsHandler.HandleLogMessage(TestMessageLevel.Error, log);

            mockTestDiscoveryEventsHandler.Verify(mt =>
                mt.HandleLogMessage(TestMessageLevel.Error, log), Times.Once);
        }
    }
}
