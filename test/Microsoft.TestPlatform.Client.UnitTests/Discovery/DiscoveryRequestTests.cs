// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.Discovery
{
    using Client.Discovery;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using ObjectModel.Client;
    using ObjectModel.Engine;
    using System;
    using System.Collections.Generic;
    
    [TestClass]
    public class DiscoveryRequestTests
    {
        IDiscoveryRequest discoveryRequest;
        Mock<IProxyDiscoveryManager> discoveryManager;
        DiscoveryCriteria discoveryCriteria;

        [TestInitialize]
        public void TestInit()
        {
            discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            discoveryManager = new Mock<IProxyDiscoveryManager>();
            discoveryRequest = new DiscoveryRequest(discoveryCriteria, discoveryManager.Object);
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
            //DiscoveryAsyn has not been called, discoveryInProgress should be false
            discoveryRequest.Abort();
            discoveryManager.Verify(dm => dm.Abort(), Times.Never);
        }

        [TestMethod]
        public void WaitForCompletionIfDiscoveryRequestDisposedShouldThrowObjectDisposedException()
        {
            discoveryRequest.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => discoveryRequest.WaitForCompletion());
        }
    }
}
