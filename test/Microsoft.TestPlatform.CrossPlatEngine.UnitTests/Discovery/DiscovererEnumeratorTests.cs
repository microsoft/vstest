// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class DiscovererEnumeratorTests
    {
        private DiscovererEnumerator discovererEnumerator;
        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private DiscoveryResultCache discoveryResultCache;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
            this.discoveryResultCache = new DiscoveryResultCache(1000, TimeSpan.FromHours(1), (tests) => { });
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.discovererEnumerator = new DiscovererEnumerator(this.mockRequestData.Object, this.discoveryResultCache, this.mockTestPlatformEventSource.Object);

            TestDiscoveryExtensionManager.Destroy();
        }

        [TestMethod]
        public void LoadTestsShouldReportWarningOnNoDiscoverers()
        {
            TestPluginCacheTests.SetupMockExtensions(
                new string[] { typeof(TestPluginCache).GetTypeInfo().Assembly.Location },
                () => { });
            var sources = new List<string> { typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location };
            var mockLogger = new Mock<IMessageLogger>();

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
            extensionSourceMap.Add("_none_", sources);

            this.discovererEnumerator.LoadTests(extensionSourceMap, new Mock<IRunSettings>().Object, null, mockLogger.Object);

            var messageFormat =
                "No test is available in {0}. Make sure that test discoverer & executors are registered and platform & framework version settings are appropriate and try again.";
            var message = string.Format(messageFormat, string.Join(" ", sources));

            mockLogger.Verify(
                l =>
                l.SendMessage(TestMessageLevel.Warning, message), Times.Once);
        }

        [TestMethod]
        public void LoadTestsShouldNotCallIntoDiscoverersIfNoneMatchesSources()
        {
            TestPluginCacheTests.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });
            var sources = new List<string> { "temp.jpeg" };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
            extensionSourceMap.Add("_none_", sources);

            this.discovererEnumerator.LoadTests(extensionSourceMap, new Mock<IRunSettings>().Object, null, new Mock<IMessageLogger>().Object);

            Assert.IsFalse(DllTestDiscoverer.IsDiscoverTestCalled);
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoADiscovererThatMatchesTheSources()
        {
            try
            {
                TestPluginCacheTests.SetupMockExtensions(
                    new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                    () => { });

                var sources = new List<string>
                                  {
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location,
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                                  };

                var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
                extensionSourceMap.Add("_none_", sources);

                var settings = new Mock<IRunSettings>().Object;
                var logger = new Mock<IMessageLogger>().Object;
                string testCaseFilter = "TestFilter";

                this.discovererEnumerator.LoadTests(extensionSourceMap, settings, testCaseFilter, logger);

                Assert.IsTrue(DllTestDiscoverer.IsDiscoverTestCalled);
                Assert.IsFalse(JsonTestDiscoverer.IsDiscoverTestCalled);

                // Also validate that the right set of arguments were passed on to the discoverer.
                CollectionAssert.AreEqual(sources, DllTestDiscoverer.Sources.ToList());
                Assert.AreEqual(settings, DllTestDiscoverer.DiscoveryContext.RunSettings);
                Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
                Assert.AreEqual(logger, DllTestDiscoverer.MessageLogger);
                Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoMultipleDiscoverersThatMatchesTheSources()
        {
            try
            {
                TestPluginCacheTests.SetupMockExtensions(
                    new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                    () => { });

                var dllsources = new List<string>
                                  {
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location,
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                                  };
                var jsonsources = new List<string>
                                  {
                                      "test1.json",
                                      "test2.json"
                                  };
                var sources = new List<string>(dllsources);
                sources.AddRange(jsonsources);

                var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
                extensionSourceMap.Add("_none_", sources);

                var settings = new Mock<IRunSettings>().Object;
                var logger = new Mock<IMessageLogger>().Object;
                string testCaseFilter = "TestFilter";

                this.discovererEnumerator.LoadTests(extensionSourceMap, settings, testCaseFilter, logger);

                Assert.IsTrue(DllTestDiscoverer.IsDiscoverTestCalled);
                Assert.IsTrue(JsonTestDiscoverer.IsDiscoverTestCalled);

                // Also validate that the right set of arguments were passed on to the discoverer.
                CollectionAssert.AreEqual(dllsources, DllTestDiscoverer.Sources.ToList());
                Assert.AreEqual(settings, DllTestDiscoverer.DiscoveryContext.RunSettings);
                Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
                Assert.AreEqual(logger, DllTestDiscoverer.MessageLogger);
                Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);

                CollectionAssert.AreEqual(jsonsources, JsonTestDiscoverer.Sources.ToList());
                Assert.AreEqual(settings, JsonTestDiscoverer.DiscoveryContext.RunSettings);
                Assert.AreEqual(testCaseFilter, (JsonTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
                Assert.AreEqual(logger, JsonTestDiscoverer.MessageLogger);
                Assert.IsNotNull(JsonTestDiscoverer.DiscoverySink);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoOtherDiscoverersWhenCreatingOneFails()
        {
            try
            {
                TestPluginCacheTests.SetupMockExtensions(
                    new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                    () => { });

                var sources = new List<string>
                                  {
                                      "test1.csv",
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                                  };

                var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
                extensionSourceMap.Add("_none_", sources);

                var settings = new Mock<IRunSettings>().Object;
                var logger = new Mock<IMessageLogger>().Object;
                string testCaseFilter = "TestFilter";

                this.discovererEnumerator.LoadTests(extensionSourceMap, settings, testCaseFilter, logger);

                Assert.IsTrue(DllTestDiscoverer.IsDiscoverTestCalled);
                Assert.IsFalse(SingletonTestDiscoverer.IsDiscoverTestCalled);

                // Also validate that the right set of arguments were passed on to the discoverer.
                CollectionAssert.AreEqual(new List<string> { sources[1] }, DllTestDiscoverer.Sources.ToList());
                Assert.AreEqual(settings, DllTestDiscoverer.DiscoveryContext.RunSettings);
                Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
                Assert.AreEqual(logger, DllTestDiscoverer.MessageLogger);
                Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoOtherDiscoverersEvenIfDiscoveryInOneFails()
        {
            try
            {
                TestPluginCacheTests.SetupMockExtensions(
                    new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                    () => { });

                var sources = new List<string>
                                  {
                                      "test1.cs",
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                                  };

                var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
                extensionSourceMap.Add("_none_", sources);

                var settings = new Mock<IRunSettings>().Object;
                var mocklogger = new Mock<IMessageLogger>();
                string testCaseFilter = "TestFilter";

                this.discovererEnumerator.LoadTests(extensionSourceMap, settings, testCaseFilter, mocklogger.Object);

                Assert.IsTrue(DllTestDiscoverer.IsDiscoverTestCalled);
                Assert.IsTrue(NotImplementedTestDiscoverer.IsDiscoverTestCalled);

                // Also validate that the right set of arguments were passed on to the discoverer.
                CollectionAssert.AreEqual(new List<string> { sources[1] }, DllTestDiscoverer.Sources.ToList());
                Assert.AreEqual(settings, DllTestDiscoverer.DiscoveryContext.RunSettings);
                Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
                Assert.AreEqual(mocklogger.Object, DllTestDiscoverer.MessageLogger);
                Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);

                // Check if we log the failure.
                var message = string.Format(
                        CultureInfo.CurrentUICulture,
                        "An exception occurred while test discoverer '{0}' was loading tests. Exception: {1}",
                        typeof(NotImplementedTestDiscoverer).Name,
                        "The method or operation is not implemented.");

                mocklogger.Verify(l => l.SendMessage(TestMessageLevel.Error, message), Times.Once);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestsShouldCollectMetrics()
        {
            try
            {
                var mockMetricsCollector = new Mock<IMetricsCollection>();
                var dict = new Dictionary<string, object>();
                dict.Add("DummyMessage", "DummyValue");

                TestPluginCacheTests.SetupMockExtensions(
                    new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                    () => { });

                var sources = new List<string>
                                  {
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location,
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                                  };

                var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
                extensionSourceMap.Add("_none_", sources);

                var settings = new Mock<IRunSettings>().Object;
                var logger = new Mock<IMessageLogger>().Object;

                mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
                this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

                string testCaseFilter = "TestFilter";
                this.discovererEnumerator.LoadTests(extensionSourceMap, settings, testCaseFilter, logger);

                // Verify.
                mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenInSecByAllAdapters, It.IsAny<object>()), Times.Once);
                mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, It.IsAny<object>()), Times.Once);
                mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery, It.IsAny<object>()), Times.Once);
                mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TotalTestsByAdapter + ".discoverer://dlldiscoverer/", It.IsAny<object>()), Times.Once);
                mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + ".discoverer://dlldiscoverer/", It.IsAny<object>()), Times.Once);
                mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec, It.IsAny<object>()), Times.Once);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoTheAdapterWithTheRightTestCaseSink()
        {
            try
            {
                this.InvokeLoadTestWithMockSetup();

                Assert.IsTrue(DllTestDiscoverer.IsDiscoverTestCalled);
                Assert.AreEqual(1, this.discoveryResultCache.Tests.Count);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestShouldInstrumentDiscoveryStart()
        {
            try
            {
                this.InvokeLoadTestWithMockSetup();
                this.mockTestPlatformEventSource.Verify(x => x.DiscoveryStart(), Times.Once);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestShouldInstrumentDiscoveryStop()
        {
            try
            {
                this.InvokeLoadTestWithMockSetup();
                this.mockTestPlatformEventSource.Verify(x => x.DiscoveryStop(It.IsAny<long>()), Times.Once);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestShouldInstrumentAdapterDiscoveryStart()
        {
            try
            {
                this.InvokeLoadTestWithMockSetup();
                this.mockTestPlatformEventSource.Verify(x => x.AdapterDiscoveryStart(It.IsAny<string>()), Times.AtLeastOnce);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestShouldInstrumentAdapterDiscoveryStop()
        {
            try
            {
                this.InvokeLoadTestWithMockSetup();
                this.mockTestPlatformEventSource.Verify(x => x.AdapterDiscoveryStop(It.IsAny<long>()), Times.AtLeastOnce);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        [TestMethod]
        public void LoadTestsShouldIterateOverAllExtensionsInTheMapAndDiscoverTests()
        {
            try
            {
                TestPluginCacheTests.SetupMockExtensions(
                    new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                    () => { });

                var dllsources = new List<string>
                                  {
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location,
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                                  };
                var jsonsources = new List<string>
                                  {
                                      "test1.json",
                                      "test2.json"
                                  };

                var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
                extensionSourceMap.Add(typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location, jsonsources);
                extensionSourceMap.Add("_none_", dllsources);

                var settings = new Mock<IRunSettings>().Object;
                var logger = new Mock<IMessageLogger>().Object;
                string testCaseFilter = "TestFilter";

                this.discovererEnumerator.LoadTests(extensionSourceMap, settings, testCaseFilter, logger);

                Assert.IsTrue(DllTestDiscoverer.IsDiscoverTestCalled);
                Assert.IsTrue(JsonTestDiscoverer.IsDiscoverTestCalled);

                // Also validate that the right set of arguments were passed on to the discoverer.
                CollectionAssert.AreEqual(dllsources, DllTestDiscoverer.Sources.ToList());
                Assert.AreEqual(settings, DllTestDiscoverer.DiscoveryContext.RunSettings);
                Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
                Assert.AreEqual(logger, DllTestDiscoverer.MessageLogger);
                Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);

                CollectionAssert.AreEqual(jsonsources, JsonTestDiscoverer.Sources.ToList());
                Assert.AreEqual(settings, JsonTestDiscoverer.DiscoveryContext.RunSettings);
                Assert.AreEqual(testCaseFilter, (JsonTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
                Assert.AreEqual(logger, JsonTestDiscoverer.MessageLogger);
                Assert.IsNotNull(JsonTestDiscoverer.DiscoverySink);
            }
            finally
            {
                this.ResetDiscoverers();
            }
        }

        private void ResetDiscoverers()
        {
            DllTestDiscoverer.Reset();
            JsonTestDiscoverer.Reset();
            NotImplementedTestDiscoverer.Reset();
        }

        private void InvokeLoadTestWithMockSetup()
        {
            TestPluginCacheTests.SetupMockExtensions(
                    new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                    () => { });

            var sources = new List<string>
                                  {
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                                  };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>();
            extensionSourceMap.Add("_none_", sources);

            var settings = new Mock<IRunSettings>().Object;
            var logger = new Mock<IMessageLogger>().Object;

            this.discovererEnumerator.LoadTests(extensionSourceMap, settings, null, logger);
        }


        #region implementation

        /// <summary>
        /// Placing this before others so that at runtime this would be the first to be discovered as a discoverer.
        /// </summary>
        [FileExtension(".csv")]
        [DefaultExecutorUri("discoverer://csvdiscoverer")]
        private class SingletonTestDiscoverer : ITestDiscoverer
        {
            private SingletonTestDiscoverer()
            {
            }

            public static bool IsDiscoverTestCalled { get; private set; }

            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                IsDiscoverTestCalled = true;
            }

            public static void Reset()
            {
                IsDiscoverTestCalled = false;
            }
        }

        [FileExtension(".cs")]
        [DefaultExecutorUri("discoverer://csvdiscoverer")]
        private class NotImplementedTestDiscoverer : ITestDiscoverer
        {
            public static bool IsDiscoverTestCalled { get; private set; }

            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                IsDiscoverTestCalled = true;
                throw new NotImplementedException();
            }

            public static void Reset()
            {
                IsDiscoverTestCalled = false;
            }
        }

        [FileExtension(".dll")]
        [DefaultExecutorUri("discoverer://dlldiscoverer")]
        private class DllTestDiscoverer : ITestDiscoverer
        {
            public static bool IsDiscoverTestCalled { get; private set; }

            public static IEnumerable<string> Sources { get; private set; }

            public static IDiscoveryContext DiscoveryContext { get; private set; }

            public static IMessageLogger MessageLogger { get; private set; }

            public static ITestCaseDiscoverySink DiscoverySink { get; private set; }

            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                IsDiscoverTestCalled = true;
                Sources = sources;
                DiscoveryContext = discoveryContext;
                MessageLogger = logger;
                DiscoverySink = discoverySink;

                var testCase = new TestCase("A.C.M", new Uri("executor://dllexecutor"), "A");
                discoverySink.SendTestCase(testCase);
            }

            public static void Reset()
            {
                IsDiscoverTestCalled = false;
            }
        }

        [FileExtension(".json")]
        [DefaultExecutorUri("discoverer://jsondiscoverer")]
        private class JsonTestDiscoverer : ITestDiscoverer
        {
            public static bool IsDiscoverTestCalled { get; private set; }

            public static IEnumerable<string> Sources { get; private set; }

            public static IDiscoveryContext DiscoveryContext { get; private set; }

            public static IMessageLogger MessageLogger { get; private set; }

            public static ITestCaseDiscoverySink DiscoverySink { get; private set; }

            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                IsDiscoverTestCalled = true;
                Sources = sources;
                DiscoveryContext = discoveryContext;
                MessageLogger = logger;
                DiscoverySink = discoverySink;
            }

            public static void Reset()
            {
                IsDiscoverTestCalled = false;
            }
        }

        #endregion
    }
}
