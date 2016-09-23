// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class ProxyDiscoveryManagerTests
    {
        private ProxyDiscoveryManager testDiscoveryManager;

        private Mock<ITestHostManager> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int testableClientConnectionTimeout = 400;

        private DiscoveryCriteria discoveryCriteria;

        public ProxyDiscoveryManagerTests()
        {
            this.mockTestHostManager = new Mock<ITestHostManager>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.testDiscoveryManager = new ProxyDiscoveryManager(
                                            this.mockRequestSender.Object,
                                            this.mockTestHostManager.Object,
                                            this.testableClientConnectionTimeout);
            this.discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);

            // Default setup test host manager as shared (desktop)
            this.mockTestHostManager.SetupGet(th => th.Shared).Returns(true);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeExtensionsOnNoExtensions()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.testDiscoveryManager.Initialize();

            this.mockRequestSender.Verify(s => s.InitializeDiscovery(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public void InitializeShouldInitializeExtensionsIfPresent()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            try
            {
                var extensions = new string[] { "e1.dll", "e2.dll" };

                // Setup Mocks.
                TestPluginCacheTests.SetupMockAdditionalPathExtensions(extensions);
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

                this.testDiscoveryManager.Initialize();

                // Also verify that we have waited for client connection.
                this.mockRequestSender.Verify(s => s.WaitForRequestHandlerConnection(It.IsAny<int>()), Times.Once);
                this.mockRequestSender.Verify(
                    s => s.InitializeDiscovery(extensions, true),
                    Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldNotIntializeIfDoneSoAlready()
        {
            this.testDiscoveryManager.Initialize();

            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.AtMostOnce);
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.AtMostOnce);
        }

        [TestMethod]
        public void DiscoverTestsShouldIntializeIfNotInitializedAlready()
        {
            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldThrowExceptionIfClientConnectionTimeout()
        {
            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            // Act.
            Assert.ThrowsException<TestPlatformException>(
                () => this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null));
        }

        [TestMethod]
        public void DiscoverTestsShouldInitiateServerDiscoveryLoop()
        {
            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            // Assert.
            this.mockRequestSender.Verify(s => s.DiscoverTests(It.IsAny<DiscoveryCriteria>(), null), Times.Once);
        }
    }
}
