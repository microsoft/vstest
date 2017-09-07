// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Client.Discovery;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using ObjectModel;
    using ObjectModel.Client;
    using ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    [TestClass]
    public class DiscoveryRequestTests
    {
        IDiscoveryRequest discoveryRequest;
        Mock<IProxyDiscoveryManager> discoveryManager;
        DiscoveryCriteria discoveryCriteria;

        public DiscoveryRequestTests()
        {
            this.discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            this.discoveryManager = new Mock<IProxyDiscoveryManager>();
            this.discoveryRequest = new DiscoveryRequest(new RequestData(new DummyMetricCollector()), this.discoveryCriteria, this.discoveryManager.Object);
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
            var eventsHandler = this.discoveryRequest as ITestDiscoveryEventsHandler;

            eventsHandler.HandleDiscoveryComplete(1, Enumerable.Empty<TestCase>(), false);

            this.discoveryManager.Verify(dm => dm.Close(), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCloseDiscoveryManagerBeforeRaiseDiscoveryComplete()
        {
            var events = new List<string>();
            this.discoveryManager.Setup(dm => dm.Close()).Callback(() => events.Add("close"));
            this.discoveryRequest.OnDiscoveryComplete += (s, e) => events.Add("complete");
            var eventsHandler = this.discoveryRequest as ITestDiscoveryEventsHandler;

            eventsHandler.HandleDiscoveryComplete(1, Enumerable.Empty<TestCase>(), false);

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("close", events[0]);
            Assert.AreEqual("complete", events[1]);
        }
    }
}
