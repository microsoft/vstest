// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Client.Discovery;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DiscoveryRequestTests
    {
        IDiscoveryRequest discoveryRequest;
        Mock<IProxyDiscoveryManager> discoveryManager;
        DiscoveryCriteria discoveryCriteria;
        private Mock<IRequestData> mockRequestData;

        public DiscoveryRequestTests()
        {
            this.discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            this.discoveryManager = new Mock<IProxyDiscoveryManager>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());
            this.discoveryRequest = new DiscoveryRequest(mockRequestData.Object, this.discoveryCriteria, this.discoveryManager.Object);
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

            eventsHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(1, false, null), Enumerable.Empty<TestCase>());
            this.discoveryManager.Verify(dm => dm.Close(), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCloseDiscoveryManagerBeforeRaiseDiscoveryComplete()
        {
            var events = new List<string>();
            this.discoveryManager.Setup(dm => dm.Close()).Callback(() => events.Add("close"));
            this.discoveryRequest.OnDiscoveryComplete += (s, e) => events.Add("complete");
            var eventsHandler = this.discoveryRequest as ITestDiscoveryEventsHandler2;

            eventsHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(1, false, null), Enumerable.Empty<TestCase>());

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

        /// <summary>
        /// OnDiscoveryStart should be invoked with null filter expression if filter is null.
        /// </summary>
        [TestMethod]
        public void DiscoverAsyncShouldInvokeOnDiscoveryStartWithNullFilterExpressionIfFilterIsNull()
        {
            this.discoveryCriteria.TestCaseFilter = null;
            DiscoveryStartEventArgs receivedArgs = null;
            this.discoveryRequest.OnDiscoveryStart += (s, e) => receivedArgs = e;

            // Action
            this.discoveryRequest.DiscoverAsync();

            // Assert
            Assert.IsNull(receivedArgs.FilterExpression);
        }

        /// <summary>
        /// OnDiscoveryStart should be invoked with null filter expression if filter is invalid.
        /// </summary>
        [TestMethod]
        public void DiscoverAsyncShouldInvokeOnDiscoveryStartWithNullFilterExpressionIfFilterIsInvalid()
        {
            this.discoveryCriteria.TestCaseFilter = "Name=Test1=Test2";
            DiscoveryStartEventArgs receivedArgs = null;
            this.discoveryRequest.OnDiscoveryStart += (s, e) => receivedArgs = e;

            // Action
            this.discoveryRequest.DiscoverAsync();

            // Assert
            Assert.IsNull(receivedArgs.FilterExpression);
        }

        /// <summary>
        /// OnDiscoveryStart should be invoked with proper filter expression if filter is valid.
        /// </summary>
        [TestMethod]
        public void DiscoverAsyncShouldInvokeOnDiscoveryStartWithProperFilterExpressionIfFilterIsValid()
        {
            this.discoveryCriteria.TestCaseFilter = "Name=Test1";
            DiscoveryStartEventArgs receivedArgs = null;
            this.discoveryRequest.OnDiscoveryStart += (s, e) => receivedArgs = e;

            // Action
            this.discoveryRequest.DiscoverAsync();

            // Assert
            Assert.IsNotNull(receivedArgs.FilterExpression);
            Assert.AreEqual(this.discoveryCriteria.TestCaseFilter, receivedArgs.FilterExpression.TestCaseFilterValue);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var dict = new Dictionary<string, string>();
            dict.Add("DummyMessage", "DummyValue");

            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            var eventsHandler = this.discoveryRequest as ITestDiscoveryEventsHandler2;

            // Act
            eventsHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(1, false, dict), Enumerable.Empty<TestCase>());

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenInSecForDiscovery, It.IsAny<string>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add("DummyMessage", "DummyValue"), Times.Once);
        }
    }
}
