﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class InProcessProxyDiscoveryManagerTests
    {
        private Mock<ITestHostManagerFactory> mockTestHostManagerFactory;
        private InProcessProxyDiscoveryManager inProcessProxyDiscoveryManager;
        private Mock<IDiscoveryManager> mockDiscoveryManager;
        private Mock<ITestRuntimeProvider> mockTestHostManager;

        public InProcessProxyDiscoveryManagerTests()
        {
            mockTestHostManagerFactory = new Mock<ITestHostManagerFactory>();
            mockDiscoveryManager = new Mock<IDiscoveryManager>();
            mockTestHostManager = new Mock<ITestRuntimeProvider>();
            mockTestHostManagerFactory.Setup(o => o.GetDiscoveryManager()).Returns(mockDiscoveryManager.Object);
            inProcessProxyDiscoveryManager = new InProcessProxyDiscoveryManager(mockTestHostManager.Object, mockTestHostManagerFactory.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            mockDiscoveryManager = null;
            mockTestHostManagerFactory = null;
            inProcessProxyDiscoveryManager = null;
            mockTestHostManager = null;
        }

        [TestMethod]
        public void DiscoverTestsShouldCallInitialize()
        {
            var manualResetEvent = new ManualResetEvent(false);
            mockDiscoveryManager.Setup(o => o.Initialize(Enumerable.Empty<string>(), null)).Callback(
                () => manualResetEvent.Set());

            var discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);
            inProcessProxyDiscoveryManager.DiscoverTests(discoveryCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "DiscoverTests should call Initialize");
        }

        [TestMethod]
        public void DiscoverTestsShouldUpdateTestPluginCacheWithExtensionsReturnByTestHost()
        {
            var manualResetEvent = new ManualResetEvent(false);
            mockDiscoveryManager.Setup(o => o.Initialize(Enumerable.Empty<string>(), null)).Callback(
                () => manualResetEvent.Set());

            var path = Path.Combine(Path.GetTempPath(), "DiscoveryDummy.dll");
            mockTestHostManager.Setup(o => o.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string> { path });
            var expectedResult = TestPluginCache.Instance.GetExtensionPaths(string.Empty);
            expectedResult.Add(path);
            var discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);

            inProcessProxyDiscoveryManager.DiscoverTests(discoveryCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "DiscoverTests should call Initialize");
            CollectionAssert.AreEquivalent(expectedResult, TestPluginCache.Instance.GetExtensionPaths(string.Empty));
        }

        [TestMethod]
        public void DiscoverTestsShouldCallDiscoveryManagerDiscoverTests()
        {
            var discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            var manualResetEvent = new ManualResetEvent(false);

            mockDiscoveryManager.Setup(o => o.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object)).Callback(
                () => manualResetEvent.Set());

            inProcessProxyDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IDiscoveryManager.DiscoverTests should get called");
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleDiscoveryComplete()
        {
            var discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            var manualResetEvent = new ManualResetEvent(false);

            mockDiscoveryManager.Setup(o => o.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object)).Callback(
                () => throw new Exception());

            mockTestDiscoveryEventsHandler.Setup(o => o.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>())).Callback(
                () => manualResetEvent.Set());

            inProcessProxyDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "ITestDiscoveryEventsHandler.HandleDiscoveryComplete should get called");
        }

        [TestMethod]
        public void AbortShouldCallDiscoveryManagerAbort()
        {
            var manualResetEvent = new ManualResetEvent(false);

            mockDiscoveryManager.Setup(o => o.Abort()).Callback(
                () => manualResetEvent.Set());

            inProcessProxyDiscoveryManager.Abort();

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IDiscoveryManager.Abort should get called");
        }

        [TestMethod]
        public void DiscoverTestRunShouldAllowRuntimeProviderToUpdateAdapterSource()
        {
            var inputSources = new List<string> { "test.dll" };
            var discoveryCriteria = new DiscoveryCriteria(inputSources, 1, string.Empty);
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            var manualResetEvent = new ManualResetEvent(false);

            mockTestHostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources)).Returns(discoveryCriteria.Sources);
            mockDiscoveryManager.Setup(o => o.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object)).Callback(
                () => manualResetEvent.Set());

            inProcessProxyDiscoveryManager = new InProcessProxyDiscoveryManager(mockTestHostManager.Object, mockTestHostManagerFactory.Object);
            inProcessProxyDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IDiscoveryManager.DiscoverTests should get called");
            mockTestHostManager.Verify(hm => hm.GetTestSources(inputSources), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestRunShouldUpdateTestSourcesIfSourceDiffersFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var inputSource =  new List<string> { "inputPackage.appxrecipe" };

            var discoveryCriteria = new DiscoveryCriteria(inputSource, 1, string.Empty);
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            var manualResetEvent = new ManualResetEvent(false);

            mockTestHostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources)).Returns(actualSources);

            mockDiscoveryManager.Setup(o => o.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object)).Callback(
                () => manualResetEvent.Set());

            inProcessProxyDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IDiscoveryManager.DiscoverTests should get called");

            // AdapterSourceMap should contain updated testSources.
            Assert.AreEqual(actualSources.FirstOrDefault(), discoveryCriteria.AdapterSourceMap.FirstOrDefault().Value.FirstOrDefault());
            Assert.AreEqual(inputSource.FirstOrDefault(), discoveryCriteria.Package);
        }
    }
}
