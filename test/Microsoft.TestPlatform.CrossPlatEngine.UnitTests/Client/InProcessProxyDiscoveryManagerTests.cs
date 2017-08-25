// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class InProcessProxyDiscoveryManagerTests
    {
        private Mock<ITestHostManagerFactory> mockTestHostManagerFactory;
        private InProcessProxyDiscoveryManager inProcessProxyDiscoveryManager;
        private Mock<IDiscoveryManager> mockDiscoveryManager;

        [TestInitialize]
        public void TestInitialize()
        {
            this.mockTestHostManagerFactory = new Mock<ITestHostManagerFactory>();
            this.mockDiscoveryManager = new Mock<IDiscoveryManager>();
            this.mockTestHostManagerFactory.Setup(o => o.GetDiscoveryManager()).Returns(this.mockDiscoveryManager.Object);
            this.inProcessProxyDiscoveryManager = new InProcessProxyDiscoveryManager(this.mockTestHostManagerFactory.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.mockDiscoveryManager = null;
            this.mockTestHostManagerFactory = null;
            this.inProcessProxyDiscoveryManager = null;
        }

        [TestMethod]
        public void InitializeShouldCallDiscoveryManagerInitializeWithEmptyIEnumerable()
        {
            this.inProcessProxyDiscoveryManager.Initialize();
            this.mockDiscoveryManager.Verify(o => o.Initialize(Enumerable.Empty<string>()), Times.Once, "DiscoveryManager.Initialize() should get called with empty list");
        }

        [TestMethod]
        public void InitializeShouldSetIsInitializedTotrue()
        {
            this.inProcessProxyDiscoveryManager.Initialize();
            Assert.IsTrue(this.inProcessProxyDiscoveryManager.IsInitialized, "DiscoveryManager.Initialize() is not setting the value of varable IsInitialized to true");
        }

        [TestMethod]
        public void InitializeShouldCallDiscoveryManagerInitializeWithEmptyIEnumerableOnlyOnce()
        {
            this.inProcessProxyDiscoveryManager.Initialize();
            this.inProcessProxyDiscoveryManager.Initialize();
            this.mockDiscoveryManager.Verify(o => o.Initialize(Enumerable.Empty<string>()), Times.Once, "DiscoveryManager.Initialize() should get called once");
        }

        [TestMethod]
        public void DiscoverTestsShouldCallInitializeIfNotAlreadyInitialized()
        {
            var manualResetEvent = new ManualResetEvent(true);

            this.mockDiscoveryManager.Setup(o => o.Initialize(Enumerable.Empty<string>())).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyDiscoveryManager.DiscoverTests(null, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "DiscoverTests should call Initialize if not already initialized");
        }

        [TestMethod]
        public void DiscoverTestsShouldNotCallInitializeIfAlreadyInitialized()
        {
            var manualResetEvent = new ManualResetEvent(true);

            this.mockDiscoveryManager.Setup(o => o.Initialize(Enumerable.Empty<string>())).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyDiscoveryManager.Initialize();
            this.inProcessProxyDiscoveryManager.DiscoverTests(null, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000));
            this.mockDiscoveryManager.Verify(o => o.Initialize(Enumerable.Empty<string>()), Times.Once, "DiscoverTests should not call Initialize if already initialized");
        }

        [TestMethod]
        public void DiscoverTestsShouldCallDiscoveryManagerDiscoverTests()
        {
            var discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler>();
            var manualResetEvent = new ManualResetEvent(true);

            this.mockDiscoveryManager.Setup(o => o.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object)).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IDiscoveryManager.DiscoverTests should get called");
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleDiscoveryComplete()
        {
            var discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler>();
            var manualResetEvent = new ManualResetEvent(true);

            this.mockDiscoveryManager.Setup(o => o.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object)).Callback(
                () => throw new Exception());

            mockTestDiscoveryEventsHandler.Setup(o => o.HandleDiscoveryComplete(-1, It.IsAny<IEnumerable<TestCase>>(), true)).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "ITestDiscoveryEventsHandler.HandleDiscoveryComplete should get called");
        }

        [TestMethod]
        public void AbortShouldCallDiscoveryManagerAbort()
        {
            var manualResetEvent = new ManualResetEvent(true);

            this.mockDiscoveryManager.Setup(o => o.Abort()).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyDiscoveryManager.Abort();

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IDiscoveryManager.Abort should get called");
        }
    }
}
