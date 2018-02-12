// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Client.Discovery;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DiscoveryRequestTests
    {
        DiscoveryRequest discoveryRequest;
        Mock<IProxyDiscoveryManager> discoveryManager;
        private readonly Mock<ITestLoggerManager> loggerManager;
        DiscoveryCriteria discoveryCriteria;
        private Mock<IRequestData> mockRequestData;
        private Mock<IDataSerializer> mockDataSerializer;

        public DiscoveryRequestTests()
        {
            this.discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            this.discoveryManager = new Mock<IProxyDiscoveryManager>();
            this.loggerManager = new Mock<ITestLoggerManager>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.discoveryRequest = new DiscoveryRequest(this.mockRequestData.Object, this.discoveryCriteria, this.discoveryManager.Object, loggerManager.Object, this.mockDataSerializer.Object);
        }

        [TestMethod]
        public void ConstructorSetsDiscoveryCriteriaAndDiscoveryManager()
        {
            Assert.AreEqual(this.discoveryCriteria, this.discoveryRequest.DiscoveryCriteria);
            Assert.AreEqual(this.discoveryManager.Object, (this.discoveryRequest as DiscoveryRequest).DiscoveryManager);
        }

        [TestMethod]
        public void DiscoveryAsycIfDiscoveryRequestIsDisposedThrowsObjectDisposedException()
        {
            this.discoveryRequest.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() => this.discoveryRequest.DiscoverAsync());
        }

        [TestMethod]
        public void DiscoverAsyncSetsDiscoveryInProgressAndCallManagerToDiscoverTests()
        {
            this.discoveryRequest.DiscoverAsync();

            Assert.IsTrue((this.discoveryRequest as DiscoveryRequest).DiscoveryInProgress);
            this.discoveryManager.Verify(dm => dm.DiscoverTests(this.discoveryCriteria, this.discoveryRequest as DiscoveryRequest), Times.Once);
        }

        [TestMethod]
        public void DiscoveryAsyncIfDiscoverTestsThrowsExceptionSetsDiscoveryInProgressToFalseAndThrowsThatException()
        {
            this.discoveryManager.Setup(dm => dm.DiscoverTests(this.discoveryCriteria, this.discoveryRequest as DiscoveryRequest)).Throws(new Exception("DummyException"));
            try
            {
                this.discoveryRequest.DiscoverAsync();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is Exception);
                Assert.AreEqual("DummyException", ex.Message);
                Assert.IsFalse((this.discoveryRequest as DiscoveryRequest).DiscoveryInProgress);
            }
        }

        [TestMethod]
        public void AbortIfDiscoveryRequestDisposedShouldThrowObjectDisposedException()
        {
            this.discoveryRequest.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => this.discoveryRequest.Abort());
        }

        [TestMethod]
        public void AbortIfDiscoveryIsinProgressShouldCallDiscoveryManagerAbort()
        {
            // Just to set the IsDiscoveryInProgress flag
            this.discoveryRequest.DiscoverAsync();

            this.discoveryRequest.Abort();
            this.discoveryManager.Verify(dm => dm.Abort(), Times.Once);
        }

        [TestMethod]
        public void AbortIfDiscoveryIsNotInProgressShouldNotCallDiscoveryManagerAbort()
        {
            // DiscoveryAsync has not been called, discoveryInProgress should be false
            this.discoveryRequest.Abort();
            this.discoveryManager.Verify(dm => dm.Abort(), Times.Never);
        }

        [TestMethod]
        public void WaitForCompletionIfDiscoveryRequestDisposedShouldThrowObjectDisposedException()
        {
            this.discoveryRequest.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => this.discoveryRequest.WaitForCompletion());
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCloseDiscoveryManager()
        {
            var eventsHandler = this.discoveryRequest as ITestDiscoveryEventsHandler2;

            eventsHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(1, false), Enumerable.Empty<TestCase>());
            this.discoveryManager.Verify(dm => dm.Close(), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldNotInvokeHandleDiscoveredTestsIfLastChunkNotPresent()
        {
            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(1, false);
            var eventsHandler = this.discoveryRequest as ITestDiscoveryEventsHandler2;
            eventsHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, Enumerable.Empty<TestCase>());

            loggerManager.Verify(lm => lm.HandleDiscoveredTests(It.IsAny<DiscoveredTestsEventArgs>()), Times.Never);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCloseDiscoveryManagerBeforeRaiseDiscoveryComplete()
        {
            var events = new List<string>();
            this.discoveryManager.Setup(dm => dm.Close()).Callback(() => events.Add("close"));
            this.discoveryRequest.OnDiscoveryComplete += (s, e) => events.Add("complete");
            var eventsHandler = this.discoveryRequest as ITestDiscoveryEventsHandler2;

            eventsHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(1, false), Enumerable.Empty<TestCase>());

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("close", events[0]);
            Assert.AreEqual("complete", events[1]);
        }

        /// <summary>
        /// DiscoverAsync should invoke OnDiscoveryStart event.
        /// </summary>
        [TestMethod]
        public void DiscoverAsyncShouldInvokeOnDiscoveryStart()
        {
            bool onDiscoveryStartHandlerCalled = false;
            this.discoveryRequest.OnDiscoveryStart += (s, e) => onDiscoveryStartHandlerCalled = true;

            // Action
            this.discoveryRequest.DiscoverAsync();

            // Assert
            Assert.IsTrue(onDiscoveryStartHandlerCalled, "DiscoverAsync should invoke OnDiscoveryStart event");
        }

        [TestMethod]
        public void DiscoverAsyncShouldInvokeHandleDiscoveryStartofLoggerManager()
        {
            // Action
            this.discoveryRequest.DiscoverAsync();

            // Assert
            loggerManager.Verify(lm => lm.HandleDiscoveryStart(It.IsAny<DiscoveryStartEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var dict = new Dictionary<string, object>();
            dict.Add("DummyMessage", "DummyValue");

            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            var eventsHandler = this.discoveryRequest as ITestDiscoveryEventsHandler2;
            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(1, false);
            discoveryCompleteEventArgs.Metrics = dict;

            // Act
            eventsHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, Enumerable.Empty<TestCase>());

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenInSecForDiscovery, It.IsAny<double>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add("DummyMessage", "DummyValue"), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldHandleRawMessage()
        {
            bool onDiscoveryCompleteInvoked = false;
            this.discoveryRequest.OnRawMessageReceived += (object sender, string e) =>
                {
                    onDiscoveryCompleteInvoked = true;
                };

            this.discoveryRequest.HandleRawMessage(string.Empty);

            Assert.IsTrue(onDiscoveryCompleteInvoked);
        }

        [TestMethod]
        public void HandleRawMessageShouldAddVSTestDataPointsIfTelemetryOptedIn()
        {
            bool onDiscoveryCompleteInvoked = true;
            this.mockRequestData.Setup(x => x.IsTelemetryOptedIn).Returns(true);
            this.discoveryRequest.OnRawMessageReceived += (object sender, string e) =>
                {
                    onDiscoveryCompleteInvoked = true;
                };

            this.mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.DiscoveryComplete });

            this.mockDataSerializer.Setup(x => x.DeserializePayload<DiscoveryCompletePayload>(It.IsAny<Message>()))
                .Returns(new DiscoveryCompletePayload());

            this.discoveryRequest.HandleRawMessage(string.Empty);

            this.mockDataSerializer.Verify(x => x.SerializePayload(It.IsAny<string>(), It.IsAny<DiscoveryCompletePayload>()), Times.Once);
            this.mockRequestData.Verify(x => x.MetricsCollection, Times.AtLeastOnce);
            Assert.IsTrue(onDiscoveryCompleteInvoked);
        }

        [TestMethod]
        public void HandleRawMessageShouldInvokeHandleDiscoveryCompleteOfLoggerManager()
        {
            this.loggerManager.Setup(x => x.AreLoggersInitialized()).Returns(true);
            this.mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.DiscoveryComplete });
            this.mockDataSerializer.Setup(x => x.DeserializePayload<DiscoveryCompletePayload>(It.IsAny<Message>()))
                .Returns(new DiscoveryCompletePayload()
                {
                    TotalTests = 1,
                    IsAborted = false,
                    LastDiscoveredTests = Enumerable.Empty<TestCase>()
                });

            this.discoveryRequest.HandleRawMessage(string.Empty);

            this.loggerManager.Verify(lm => lm.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldInvokeHandleDiscoveredTestsIfLastChunkPresent()
        {
            var activeTestCases = new List<ObjectModel.TestCase>
            {
                new ObjectModel.TestCase(
                    "A.C.M2",
                    new Uri("executor://dummy"),
                    "A")
            };

            this.loggerManager.Setup(x => x.AreLoggersInitialized()).Returns(true);
            this.mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.DiscoveryComplete });
            this.mockDataSerializer.Setup(x => x.DeserializePayload<DiscoveryCompletePayload>(It.IsAny<Message>()))
                .Returns(new DiscoveryCompletePayload()
                {
                    TotalTests = 1,
                    IsAborted = false,
                    LastDiscoveredTests = activeTestCases
                });

            this.discoveryRequest.HandleRawMessage(string.Empty);

            this.loggerManager.Verify(lm => lm.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldInvokeHandleDiscoveredTestsOfLoggerManager()
        {
            this.loggerManager.Setup(x => x.AreLoggersInitialized()).Returns(true);
            this.mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.TestCasesFound });
            this.mockDataSerializer.Setup(x => x.DeserializePayload<IEnumerable<TestCase>>(It.IsAny<Message>()))
                .Returns(Enumerable.Empty<TestCase>());

            this.discoveryRequest.HandleRawMessage(string.Empty);

            loggerManager.Verify(lm => lm.HandleDiscoveredTests(It.IsAny<DiscoveredTestsEventArgs>()), Times.Once);
        }
    }
}
