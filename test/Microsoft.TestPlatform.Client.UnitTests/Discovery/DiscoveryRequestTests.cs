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
        readonly DiscoveryRequest discoveryRequest;
        readonly Mock<IProxyDiscoveryManager> discoveryManager;
        private readonly Mock<ITestLoggerManager> loggerManager;
        readonly DiscoveryCriteria discoveryCriteria;
        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IDataSerializer> mockDataSerializer;

        public DiscoveryRequestTests()
        {
            discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            discoveryManager = new Mock<IProxyDiscoveryManager>();
            loggerManager = new Mock<ITestLoggerManager>();
            mockRequestData = new Mock<IRequestData>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());
            mockDataSerializer = new Mock<IDataSerializer>();
            discoveryRequest = new DiscoveryRequest(mockRequestData.Object, discoveryCriteria, discoveryManager.Object, loggerManager.Object, mockDataSerializer.Object);
        }

        [TestMethod]
        public void ConstructorSetsDiscoveryCriteriaAndDiscoveryManager()
        {
            Assert.AreEqual(discoveryCriteria, discoveryRequest.DiscoveryCriteria);
            Assert.AreEqual(discoveryManager.Object, (discoveryRequest as DiscoveryRequest).DiscoveryManager);
        }

        [TestMethod]
        public void DiscoveryAsycIfDiscoveryRequestIsDisposedThrowsObjectDisposedException()
        {
            discoveryRequest.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() => discoveryRequest.DiscoverAsync());
        }

        [TestMethod]
        public void DiscoverAsyncSetsDiscoveryInProgressAndCallManagerToDiscoverTests()
        {
            discoveryRequest.DiscoverAsync();

            Assert.IsTrue((discoveryRequest as DiscoveryRequest).DiscoveryInProgress);
            discoveryManager.Verify(dm => dm.DiscoverTests(discoveryCriteria, discoveryRequest as DiscoveryRequest), Times.Once);
        }

        [TestMethod]
        public void DiscoveryAsyncIfDiscoverTestsThrowsExceptionSetsDiscoveryInProgressToFalseAndThrowsThatException()
        {
            discoveryManager.Setup(dm => dm.DiscoverTests(discoveryCriteria, discoveryRequest as DiscoveryRequest)).Throws(new Exception("DummyException"));
            try
            {
                discoveryRequest.DiscoverAsync();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is Exception);
                Assert.AreEqual("DummyException", ex.Message);
                Assert.IsFalse((discoveryRequest as DiscoveryRequest).DiscoveryInProgress);
            }
        }

        [TestMethod]
        public void AbortIfDiscoveryRequestDisposedShouldThrowObjectDisposedException()
        {
            discoveryRequest.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => discoveryRequest.Abort());
        }

        [TestMethod]
        public void AbortIfDiscoveryIsinProgressShouldCallDiscoveryManagerAbort()
        {
            // Just to set the IsDiscoveryInProgress flag
            discoveryRequest.DiscoverAsync();

            discoveryRequest.Abort();
            discoveryManager.Verify(dm => dm.Abort(), Times.Once);
        }

        [TestMethod]
        public void AbortIfDiscoveryIsNotInProgressShouldNotCallDiscoveryManagerAbort()
        {
            // DiscoveryAsync has not been called, discoveryInProgress should be false
            discoveryRequest.Abort();
            discoveryManager.Verify(dm => dm.Abort(), Times.Never);
        }

        [TestMethod]
        public void WaitForCompletionIfDiscoveryRequestDisposedShouldThrowObjectDisposedException()
        {
            discoveryRequest.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => discoveryRequest.WaitForCompletion());
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCloseDiscoveryManager()
        {
            var eventsHandler = discoveryRequest as ITestDiscoveryEventsHandler2;

            eventsHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(1, false), Enumerable.Empty<TestCase>());
            discoveryManager.Verify(dm => dm.Close(), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCloseDiscoveryManagerBeforeRaiseDiscoveryComplete()
        {
            var events = new List<string>();
            discoveryManager.Setup(dm => dm.Close()).Callback(() => events.Add("close"));
            discoveryRequest.OnDiscoveryComplete += (s, e) => events.Add("complete");
            var eventsHandler = discoveryRequest as ITestDiscoveryEventsHandler2;

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
            discoveryRequest.OnDiscoveryStart += (s, e) => onDiscoveryStartHandlerCalled = true;

            // Action
            discoveryRequest.DiscoverAsync();

            // Assert
            Assert.IsTrue(onDiscoveryStartHandlerCalled, "DiscoverAsync should invoke OnDiscoveryStart event");
        }

        [TestMethod]
        public void DiscoverAsyncShouldInvokeHandleDiscoveryStartofLoggerManager()
        {
            // Action
            discoveryRequest.DiscoverAsync();

            // Assert
            loggerManager.Verify(lm => lm.HandleDiscoveryStart(It.IsAny<DiscoveryStartEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var dict = new Dictionary<string, object>
            {
                { "DummyMessage", "DummyValue" }
            };

            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            var eventsHandler = discoveryRequest as ITestDiscoveryEventsHandler2;
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
            discoveryRequest.OnRawMessageReceived += (object sender, string e) => onDiscoveryCompleteInvoked = true;

            discoveryRequest.HandleRawMessage(string.Empty);

            Assert.IsTrue(onDiscoveryCompleteInvoked);
        }

        [TestMethod]
        public void HandleRawMessageShouldAddVSTestDataPointsIfTelemetryOptedIn()
        {
            bool onDiscoveryCompleteInvoked = true;
            mockRequestData.Setup(x => x.IsTelemetryOptedIn).Returns(true);
            discoveryRequest.OnRawMessageReceived += (object sender, string e) => onDiscoveryCompleteInvoked = true;

            mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.DiscoveryComplete });

            mockDataSerializer.Setup(x => x.DeserializePayload<DiscoveryCompletePayload>(It.IsAny<Message>()))
                .Returns(new DiscoveryCompletePayload());

            discoveryRequest.HandleRawMessage(string.Empty);

            mockDataSerializer.Verify(x => x.SerializePayload(It.IsAny<string>(), It.IsAny<DiscoveryCompletePayload>()), Times.Once);
            mockRequestData.Verify(x => x.MetricsCollection, Times.AtLeastOnce);
            Assert.IsTrue(onDiscoveryCompleteInvoked);
        }

        [TestMethod]
        public void HandleRawMessageShouldInvokeHandleDiscoveryCompleteOfLoggerManager()
        {
            loggerManager.Setup(x => x.LoggersInitialized).Returns(true);
            mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.DiscoveryComplete });
            mockDataSerializer.Setup(x => x.DeserializePayload<DiscoveryCompletePayload>(It.IsAny<Message>()))
                .Returns(new DiscoveryCompletePayload()
                {
                    TotalTests = 1,
                    IsAborted = false,
                    LastDiscoveredTests = Enumerable.Empty<TestCase>()
                });

            discoveryRequest.HandleRawMessage(string.Empty);

            loggerManager.Verify(lm => lm.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldInvokeHandleDiscoveryCompleteOfLoggerManager()
        {
            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(1, false);
            var eventsHandler = discoveryRequest as ITestDiscoveryEventsHandler2;
            eventsHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, Enumerable.Empty<TestCase>());

            loggerManager.Verify(lm => lm.HandleDiscoveryComplete(discoveryCompleteEventArgs), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldNotInvokeHandleDiscoveredTestsIfLastChunkNotPresent()
        {
            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(1, false);
            var eventsHandler = discoveryRequest as ITestDiscoveryEventsHandler2;
            eventsHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, Enumerable.Empty<TestCase>());

            loggerManager.Verify(lm => lm.HandleDiscoveredTests(It.IsAny<DiscoveredTestsEventArgs>()), Times.Never);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldInvokeHandleDiscoveredTestsIfLastChunkPresent()
        {
            var activeTestCases = new List<TestCase>
            {
                new TestCase(
                    "A.C.M2",
                    new Uri("executor://dummy"),
                    "A")
            };

            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(1, false);
            var eventsHandler = discoveryRequest as ITestDiscoveryEventsHandler2;
            eventsHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, activeTestCases);

            loggerManager.Verify(lm => lm.HandleDiscoveredTests(It.IsAny<DiscoveredTestsEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveredTestsShouldInvokeHandleDiscoveredTestsOfLoggerManager()
        {
            discoveryRequest.HandleDiscoveredTests(null);

            loggerManager.Verify(lm => lm.HandleDiscoveredTests(It.IsAny<DiscoveredTestsEventArgs>()), Times.Once);
        }
    }
}
