// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Discovery
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    [TestClass]
    public class DiscoveryManagerTests
    {
        private DiscoveryManager discoveryManager;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;
        private TestSessionMessageLogger sessionLogger;

        [TestInitialize]
        public void TestInit()
        {
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.sessionLogger = TestSessionMessageLogger.Instance;
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.discoveryManager = new DiscoveryManager(this.mockRequestData.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestDiscoveryExtensionManager.Destroy();
            TestPluginCache.Instance = null;
        }

        #region Initialize tests

        [TestMethod]
        public void InitializeShouldUpdateAdditionalExtenions()
        {
            var mockFileHelper = new Mock<IFileHelper>();
            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(false);
            TestPluginCache.Instance = new TestableTestPluginCache();

            this.discoveryManager.Initialize(
                new string[] { typeof(DiscoveryManagerTests).GetTypeInfo().Assembly.Location }, mockLogger.Object);

            var allDiscoverers = TestDiscoveryExtensionManager.Create().Discoverers;

            Assert.IsNotNull(allDiscoverers);
            Assert.IsTrue(allDiscoverers.Any());
        }

        #endregion

        #region DiscoverTests tests

        [TestMethod]
        public void DiscoverTestsShouldLogIfTheSourceDoesNotExist()
        {
            var criteria = new DiscoveryCriteria(new List<string> { "imaginary.dll" }, 100, null);
            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();

            this.discoveryManager.DiscoverTests(criteria, mockLogger.Object);

            var errorMessage = string.Format(CultureInfo.CurrentCulture, "Could not find file {0}.", Path.Combine(Directory.GetCurrentDirectory(), "imaginary.dll"));
            mockLogger.Verify(
                l =>
                l.HandleLogMessage(
                    Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestMessageLevel.Warning,
                    errorMessage),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldLogIfTheSourceDoesNotExistIfItHasAPackage()
        {
            var criteria = new DiscoveryCriteria(new List<string> { "imaginary.exe" }, 100, null);

            var packageName = "recipe.AppxRecipe";

            var fakeDirectory = Directory.GetDirectoryRoot(typeof(DiscoveryManagerTests).GetTypeInfo().Assembly.Location);

            criteria.Package = Path.Combine(fakeDirectory, Path.Combine(packageName));
            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();

            this.discoveryManager.DiscoverTests(criteria, mockLogger.Object);

            var errorMessage = string.Format(CultureInfo.CurrentCulture, "Could not find file {0}.", Path.Combine(fakeDirectory, "imaginary.exe"));
            mockLogger.Verify(
                l =>
                l.HandleLogMessage(
                    Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestMessageLevel.Warning,
                    errorMessage),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldLogIfThereAreNoValidSources()
        {
            var sources = new List<string> { "imaginary.dll" };
            var criteria = new DiscoveryCriteria(sources, 100, null);
            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();

            this.discoveryManager.DiscoverTests(criteria, mockLogger.Object);

            var sourcesString = string.Join(",", sources.ToArray());
            var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.NoValidSourceFoundForDiscovery, sourcesString);
            mockLogger.Verify(
                l =>
                l.HandleLogMessage(
                    Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestMessageLevel.Warning,
                    errorMessage),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldLogIfTheSameSourceIsSpecifiedTwice()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            var sources = new List<string>
                              {
                                  typeof(DiscoveryManagerTests).GetTypeInfo().Assembly.Location,
                                  typeof(DiscoveryManagerTests).GetTypeInfo().Assembly.Location
                              };

            var criteria = new DiscoveryCriteria(sources, 100, null);
            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();

            this.discoveryManager.DiscoverTests(criteria, mockLogger.Object);

            var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.DuplicateSource, sources[0]);
            mockLogger.Verify(
                l =>
                l.HandleLogMessage(
                    Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestMessageLevel.Warning,
                    errorMessage),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldDiscoverTestsInTheSpecifiedSource()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            var sources = new List<string>
                              {
                                  typeof(DiscoveryManagerTests).GetTypeInfo().Assembly.Location
                              };

            var criteria = new DiscoveryCriteria(sources, 1, null);
            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();

            this.discoveryManager.DiscoverTests(criteria, mockLogger.Object);

            // Assert that the tests are passed on via the handletestruncomplete event.
            mockLogger.Verify(l => l.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldSendMetricsOnDiscoveryComplete()
        {
            var metricsCollector = new MetricsCollection();
            metricsCollector.Add("DummyMessage", "DummyValue");

            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(metricsCollector);

            DiscoveryCompleteEventArgs receivedDiscoveryCompleteEventArgs = null;

            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            var sources = new List<string>
            {
                typeof(DiscoveryManagerTests).GetTypeInfo().Assembly.Location
            };

            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();
            var criteria = new DiscoveryCriteria(sources, 1, null);

            mockLogger.Setup(ml => ml.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>()))
                .Callback(
                    (DiscoveryCompleteEventArgs complete,
                        IEnumerable<TestCase> tests) =>
                    {
                        receivedDiscoveryCompleteEventArgs = complete;
                    });

            // Act.
            this.discoveryManager.DiscoverTests(criteria, mockLogger.Object);

            // Assert
            Assert.IsNotNull(receivedDiscoveryCompleteEventArgs.Metrics);
            Assert.IsTrue(receivedDiscoveryCompleteEventArgs.Metrics.Any());
            Assert.IsTrue(receivedDiscoveryCompleteEventArgs.Metrics.ContainsKey("DummyMessage"));
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var dict = new Dictionary<string, object>();
            dict.Add("DummyMessage", "DummyValue");

            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            var sources = new List<string>
                              {
                                  typeof(DiscoveryManagerTests).GetTypeInfo().Assembly.Location
                              };

            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();
            var criteria = new DiscoveryCriteria(sources, 1, null);

            // Act.
            this.discoveryManager.DiscoverTests(criteria, mockLogger.Object);

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.DiscoveryState, It.IsAny<string>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TotalTestsDiscovered, It.IsAny<object>()), Times.Once);
        }

        [TestMethod]
        public void DiscoveryInitializeShouldVerifyWarningMessageIfAdapterFailedToLoad()
        {
            var assemblyLocation = typeof(DiscoveryManagerTests).GetTypeInfo().Assembly.Location;
            var mockLogger = new Mock<ITestDiscoveryEventsHandler2>();
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { assemblyLocation },
                () => { });

            //Act
            this.discoveryManager.Initialize(new List<string> { assemblyLocation }, mockLogger.Object);

            //when handler instance returns warning
            sessionLogger.SendMessage(TestMessageLevel.Warning, "verify that the HandleLogMessage method getting invoked at least once");

            // Verify.
            mockLogger.Verify(rd => rd.HandleLogMessage(TestMessageLevel.Warning, "verify that the HandleLogMessage method getting invoked at least once"), Times.Once);
        }

        #endregion
    }
}
