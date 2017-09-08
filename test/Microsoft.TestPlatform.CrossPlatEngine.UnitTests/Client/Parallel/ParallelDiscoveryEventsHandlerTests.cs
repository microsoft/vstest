// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
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
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    [TestClass]
    public class ParallelDiscoveryEventsHandlerTests
    {
        private ParallelDiscoveryEventsHandler parallelDiscoveryEventsHandler;

        private Mock<IProxyDiscoveryManager> mockProxyDiscoveryManager;

        private Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler;

        private Mock<IParallelProxyDiscoveryManager> mockParallelProxyDiscoveryManager;

        private Mock<IDataSerializer> mockDataSerializer;

        [TestInitialize]
        public void TestInit()
        {
            this.mockProxyDiscoveryManager = new Mock<IProxyDiscoveryManager>();
            this.mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            this.mockParallelProxyDiscoveryManager = new Mock<IParallelProxyDiscoveryManager>();
            this.mockDataSerializer = new Mock<IDataSerializer>();

            this.parallelDiscoveryEventsHandler = new ParallelDiscoveryEventsHandler(this.mockProxyDiscoveryManager.Object,
                this.mockTestDiscoveryEventsHandler.Object, this.mockParallelProxyDiscoveryManager.Object,
                new ParallelDiscoveryDataAggregator(), this.mockDataSerializer.Object);
        }
        
        [TestMethod]
        public void HandleDiscoveryCompleteShouldNotCallLastChunkResultsIfNotPresent()
        {
            int totalTests = 10;
            bool aborted = false;
            this.mockParallelProxyDiscoveryManager.Setup(mp => mp.HandlePartialDiscoveryComplete(
                   this.mockProxyDiscoveryManager.Object, totalTests, null, aborted)).Returns(false);

            var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalTests, aborted);
            this.parallelDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, null);

            // Raw message must be sent 
            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);

            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveredTests(null), Times.Never);

            this.mockParallelProxyDiscoveryManager.Verify(mp => mp.HandlePartialDiscoveryComplete(
                this.mockProxyDiscoveryManager.Object, totalTests, null, aborted), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCallLastChunkResultsIfPresent()
        {
            string payload = "Tests";
            int totalTests = 10;
            bool aborted = false;
            var lastChunk = new List<TestCase>();

            this.mockDataSerializer.Setup(mds => mds.SerializePayload(MessageType.TestCasesFound, lastChunk))
                .Returns(payload);

            this.mockParallelProxyDiscoveryManager.Setup(mp => mp.HandlePartialDiscoveryComplete(
                    this.mockProxyDiscoveryManager.Object, totalTests, lastChunk, aborted)).Returns(false);

            var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalTests, aborted);
            this.parallelDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, lastChunk);

            // Raw message must be sent 
            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);

            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveredTests(lastChunk), Times.Once);

            this.mockParallelProxyDiscoveryManager.Verify(mp => mp.HandlePartialDiscoveryComplete(
                this.mockProxyDiscoveryManager.Object, totalTests, null, aborted), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCallTestDiscoveryCompleteOnActualHandlerIfParallelManagerReturnsCompleteAsTrue()
        {
            string payload = "DiscoveryComplete";
            int totalTests = 10;
            bool aborted = false;

            this.mockParallelProxyDiscoveryManager.Setup(mp => mp.HandlePartialDiscoveryComplete(
                    this.mockProxyDiscoveryManager.Object, totalTests, null, aborted)).Returns(true);

            this.mockDataSerializer.Setup(mds => mds.SerializeMessage(MessageType.DiscoveryComplete)).Returns(payload);

            // Act
            var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalTests, aborted);
            this.parallelDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, null);

            // Verify
            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveredTests(null), Times.Never);

            this.mockParallelProxyDiscoveryManager.Verify(mp => mp.HandlePartialDiscoveryComplete(
                this.mockProxyDiscoveryManager.Object, totalTests, null, aborted), Times.Once);

            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Once);

            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryTestsShouldJustPassOnTheEventToDiscoveryEventsHandler()
        {
            var tests = new List<TestCase>();
            this.parallelDiscoveryEventsHandler.HandleDiscoveredTests(tests);

            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleDiscoveredTests(tests), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldSendTestCasesFoundRawMessageToDiscoveryEventsHandler()
        {
            string payload = "Tests";
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestCasesFound, Payload = payload });

            this.parallelDiscoveryEventsHandler.HandleRawMessage(payload);

            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldNotSendDiscoveryCompleteEventRawMessageToDiscoveryEventsHandler()
        {
            string payload = "DiscoveryComplete";
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.DiscoveryComplete, Payload = payload });

            this.parallelDiscoveryEventsHandler.HandleRawMessage(payload);

            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HandleRawMessageShouldSendLoggerRawMessageToDiscoveryEventsHandler()
        {
            string payload = "LogMessage";
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestMessage, Payload = payload });

            this.parallelDiscoveryEventsHandler.HandleRawMessage(payload);

            this.mockTestDiscoveryEventsHandler.Verify(mt => mt.HandleRawMessage(payload), Times.Once);
        }

        [TestMethod]
        public void HandleLogMessageShouldJustPassOnTheEventToDiscoveryEventsHandler()
        {
            string log = "Hello";
            this.parallelDiscoveryEventsHandler.HandleLogMessage(TestMessageLevel.Error, log);

            this.mockTestDiscoveryEventsHandler.Verify(mt =>
                mt.HandleLogMessage(TestMessageLevel.Error, log), Times.Once);
        }
    }
}
