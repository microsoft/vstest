// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DiscovererEnumeratorTests
    {
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly DiscovererEnumerator discovererEnumerator;
        private readonly Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private readonly DiscoveryResultCache discoveryResultCache;
        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IMetricsCollection> mockMetricsCollection;
        private readonly Mock<IAssemblyProperties> mockAssemblyProperties;
        private readonly Mock<IRunSettings> runSettingsMock;
        private readonly Mock<IMessageLogger> messageLoggerMock;

        public DiscovererEnumeratorTests()
        {
            mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
            discoveryResultCache = new DiscoveryResultCache(1000, TimeSpan.FromHours(1), (tests) => { });
            mockRequestData = new Mock<IRequestData>();
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockAssemblyProperties = new Mock<IAssemblyProperties>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);
            discovererEnumerator = new DiscovererEnumerator(mockRequestData.Object, discoveryResultCache, mockTestPlatformEventSource.Object, mockAssemblyProperties.Object, cancellationTokenSource.Token);
            runSettingsMock = new Mock<IRunSettings>();
            messageLoggerMock = new Mock<IMessageLogger>();
            TestPluginCacheHelper.SetupMockExtensions( new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });
            TestDiscoveryExtensionManager.Destroy();
        }

        [TestCleanup]
        public void Cleanup()
        {
            ManagedDllTestDiscoverer.Reset();
            NativeDllTestDiscoverer.Reset();
            JsonTestDiscoverer.Reset();
            NotImplementedTestDiscoverer.Reset();
        }

        [TestMethod]
        public void LoadTestsShouldReportWarningOnNoDiscoverers()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(TestPluginCache).GetTypeInfo().Assembly.Location },
                () => { });
            var sources = new List<string> { typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, null, messageLoggerMock.Object);

            var messageFormat =
                "No test is available in {0}. Make sure that test discoverer & executors are registered and platform & framework version settings are appropriate and try again.";
            var message = string.Format(messageFormat, string.Join(" ", sources));

            messageLoggerMock.Verify(
                l =>
                l.SendMessage(TestMessageLevel.Warning, message), Times.Once);
        }

        [TestMethod]
        public void LoadTestsShouldNotCallIntoDiscoverersIfNoneMatchesSources()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });
            var sources = new List<string> { "temp.jpeg" };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, null, messageLoggerMock.Object);

            Assert.IsFalse(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.IsFalse(NativeDllTestDiscoverer.IsNativeDiscoverTestCalled);
        }

        [TestMethod]
        public void LoadTestsShouldCallOnlyNativeDiscovererIfNativeAssembliesPassed()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            mockAssemblyProperties.Setup(pe => pe.GetAssemblyType("native.dll")).Returns(AssemblyType.Native);

            var sources = new List<string>
                              {
                                  "native.dll"
                              };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            string testCaseFilter = "TestFilter";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, testCaseFilter, messageLoggerMock.Object);

            Assert.IsTrue(NativeDllTestDiscoverer.IsNativeDiscoverTestCalled);
            CollectionAssert.AreEqual(sources, NativeDllTestDiscoverer.Sources.ToList());

            Assert.IsFalse(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.IsFalse(JsonTestDiscoverer.IsDiscoverTestCalled);
        }

        [TestMethod]
        public void LoadTestsShouldCallOnlyManagedDiscovererIfManagedAssembliesPassed()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            mockAssemblyProperties.Setup(pe => pe.GetAssemblyType("managed.dll")).Returns(AssemblyType.Managed);

            var sources = new List<string>
                              {
                                  "managed.dll"
                              };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            string testCaseFilter = "TestFilter";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, testCaseFilter, messageLoggerMock.Object);

            Assert.IsTrue(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            CollectionAssert.AreEqual(sources, ManagedDllTestDiscoverer.Sources.ToList());

            Assert.IsFalse(NativeDllTestDiscoverer.IsNativeDiscoverTestCalled);
            Assert.IsFalse(JsonTestDiscoverer.IsDiscoverTestCalled);
        }

        [TestMethod]
        public void LoadTestsShouldCallBothNativeAndManagedDiscoverersWithCorrectSources()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            mockAssemblyProperties.Setup(pe => pe.GetAssemblyType("native.dll")).Returns(AssemblyType.Native);
            mockAssemblyProperties.Setup(pe => pe.GetAssemblyType("managed.dll")).Returns(AssemblyType.Managed);

            var nativeSources = new List<string>
                              {
                                  "native.dll"
                              };
            var managedSources = new List<string>
                              {
                                  "managed.dll"
                              };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", nativeSources.Concat(managedSources) }
            };

            string testCaseFilter = "TestFilter";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, testCaseFilter, messageLoggerMock.Object);

            Assert.IsTrue(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            CollectionAssert.AreEqual(managedSources, ManagedDllTestDiscoverer.Sources.ToList());

            Assert.IsTrue(NativeDllTestDiscoverer.IsNativeDiscoverTestCalled);
            CollectionAssert.AreEqual(nativeSources, NativeDllTestDiscoverer.Sources.ToList());

            Assert.IsFalse(JsonTestDiscoverer.IsDiscoverTestCalled);
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoADiscovererThatMatchesTheSources()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            var sources = new List<string>
                              {
                                  typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location,
                                  typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                              };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };
            string testCaseFilter = "TestFilter";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, testCaseFilter, messageLoggerMock.Object);

            Assert.IsTrue(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.IsFalse(JsonTestDiscoverer.IsDiscoverTestCalled);

            // Also validate that the right set of arguments were passed on to the discoverer.
            CollectionAssert.AreEqual(sources, ManagedDllTestDiscoverer.Sources.ToList());
            Assert.AreEqual(runSettingsMock.Object, DllTestDiscoverer.DiscoveryContext.RunSettings);
            Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
            Assert.AreEqual(messageLoggerMock.Object, DllTestDiscoverer.MessageLogger);
            Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoMultipleDiscoverersThatMatchesTheSources()
        {
            TestPluginCacheHelper.SetupMockExtensions(
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

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            var runSettings = runSettingsMock.Object;

            string testCaseFilter = "TestFilter";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettings, testCaseFilter, messageLoggerMock.Object);

            Assert.IsTrue(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.IsTrue(JsonTestDiscoverer.IsDiscoverTestCalled);

            // Also validate that the right set of arguments were passed on to the discoverer.
            CollectionAssert.AreEqual(dllsources, ManagedDllTestDiscoverer.Sources.ToList());
            Assert.AreEqual(runSettings, DllTestDiscoverer.DiscoveryContext.RunSettings);
            Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
            Assert.AreEqual(messageLoggerMock.Object, DllTestDiscoverer.MessageLogger);
            Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);

            CollectionAssert.AreEqual(jsonsources, JsonTestDiscoverer.Sources.ToList());
            Assert.AreEqual(runSettings, JsonTestDiscoverer.DiscoveryContext.RunSettings);
            Assert.AreEqual(testCaseFilter, (JsonTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
            Assert.AreEqual(messageLoggerMock.Object, JsonTestDiscoverer.MessageLogger);
            Assert.IsNotNull(JsonTestDiscoverer.DiscoverySink);
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoOtherDiscoverersWhenCreatingOneFails()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            var sources = new List<string>
                              {
                                  "test1.csv",
                                  typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                              };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            var runSettings = runSettingsMock.Object;

            string testCaseFilter = "TestFilter";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettings, testCaseFilter, messageLoggerMock.Object);

            Assert.IsTrue(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.IsFalse(SingletonTestDiscoverer.IsDiscoverTestCalled);

            // Also validate that the right set of arguments were passed on to the discoverer.
            CollectionAssert.AreEqual(new List<string> { sources[1] }, ManagedDllTestDiscoverer.Sources.ToList());
            Assert.AreEqual(runSettings, DllTestDiscoverer.DiscoveryContext.RunSettings);
            Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
            Assert.AreEqual(messageLoggerMock.Object, DllTestDiscoverer.MessageLogger);
            Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoOtherDiscoverersEvenIfDiscoveryInOneFails()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            var sources = new List<string>
                              {
                                  "test1.cs",
                                  typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                              };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            var runSettings = runSettingsMock.Object;

            string testCaseFilter = "TestFilter";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettings, testCaseFilter, messageLoggerMock.Object);

            Assert.IsTrue(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.IsTrue(NotImplementedTestDiscoverer.IsDiscoverTestCalled);

            // Also validate that the right set of arguments were passed on to the discoverer.
            CollectionAssert.AreEqual(new List<string> { sources[1] }, ManagedDllTestDiscoverer.Sources.ToList());
            Assert.AreEqual(runSettings, DllTestDiscoverer.DiscoveryContext.RunSettings);
            Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
            Assert.AreEqual(messageLoggerMock.Object, DllTestDiscoverer.MessageLogger);
            Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);

            // Check if we log the failure.
            var message = string.Format(
                    CultureInfo.CurrentUICulture,
                    "An exception occurred while test discoverer '{0}' was loading tests. Exception: {1}",
                    typeof(NotImplementedTestDiscoverer).Name,
                    "The method or operation is not implemented.");

            messageLoggerMock.Verify(l => l.SendMessage(TestMessageLevel.Error, message), Times.Once);
        }

        [TestMethod]
        public void LoadTestsShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var dict = new Dictionary<string, object>
            {
                { "DummyMessage", "DummyValue" }
            };

            TestPluginCacheHelper.SetupMockExtensions(
                new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                () => { });

            var sources = new List<string>
                              {
                                  typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location,
                                  typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                              };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            string testCaseFilter = "TestFilter";
            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, testCaseFilter, messageLoggerMock.Object);

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenInSecByAllAdapters, It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery, It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TotalTestsByAdapter + ".discoverer://manageddlldiscoverer/", It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + ".discoverer://manageddlldiscoverer/", It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TotalTestsByAdapter + ".discoverer://nativedlldiscoverer/", It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + ".discoverer://nativedlldiscoverer/", It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec, It.IsAny<object>()), Times.Once);
        }

        [TestMethod]
        public void LoadTestsShouldNotCallIntoDiscoverersWhenCancelled()
        {
            // Setup
            string[] extensions = new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location };
            TestPluginCacheHelper.SetupMockExtensions(extensions, () => { });

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

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            // Act
            cancellationTokenSource.Cancel();
            var runSettings = runSettingsMock.Object;
            string testCaseFilter = "TestFilter";
            discovererEnumerator.LoadTests(extensionSourceMap, runSettings, testCaseFilter, messageLoggerMock.Object);

            // Validate
            Assert.IsFalse(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.IsFalse(JsonTestDiscoverer.IsDiscoverTestCalled);
            messageLoggerMock.Verify(logger => logger.SendMessage(TestMessageLevel.Warning, "Discovery of tests cancelled."), Times.Once);
        }

        [TestMethod]
        public void LoadTestsShouldCallIntoTheAdapterWithTheRightTestCaseSink()
        {
            InvokeLoadTestWithMockSetup();

            Assert.IsTrue(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.AreEqual(2, discoveryResultCache.Tests.Count);
        }

        [TestMethod]
        public void LoadTestsShouldNotShowAnyWarningOnTestsDiscovered()
        {
            InvokeLoadTestWithMockSetup();

            Assert.AreEqual(2, discoveryResultCache.Tests.Count);

            messageLoggerMock.Verify(m => m.SendMessage(TestMessageLevel.Warning, It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void LoadTestShouldInstrumentDiscoveryStart()
        {
            InvokeLoadTestWithMockSetup();
            mockTestPlatformEventSource.Verify(x => x.DiscoveryStart(), Times.Once);
        }

        [TestMethod]
        public void LoadTestShouldInstrumentDiscoveryStop()
        {
            InvokeLoadTestWithMockSetup();
            mockTestPlatformEventSource.Verify(x => x.DiscoveryStop(It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void LoadTestShouldInstrumentAdapterDiscoveryStart()
        {
            InvokeLoadTestWithMockSetup();
            mockTestPlatformEventSource.Verify(x => x.AdapterDiscoveryStart(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void LoadTestShouldInstrumentAdapterDiscoveryStop()
        {
            InvokeLoadTestWithMockSetup();
            mockTestPlatformEventSource.Verify(x => x.AdapterDiscoveryStop(It.IsAny<long>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void LoadTestsShouldIterateOverAllExtensionsInTheMapAndDiscoverTests()
        {
            TestPluginCacheHelper.SetupMockExtensions(
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

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location, jsonsources },
                { "_none_", dllsources }
            };

            var runSettings = runSettingsMock.Object;

            string testCaseFilter = "TestFilter";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettings, testCaseFilter, messageLoggerMock.Object);

            Assert.IsTrue(ManagedDllTestDiscoverer.IsManagedDiscoverTestCalled);
            Assert.IsTrue(JsonTestDiscoverer.IsDiscoverTestCalled);

            // Also validate that the right set of arguments were passed on to the discoverer.
            CollectionAssert.AreEqual(dllsources, ManagedDllTestDiscoverer.Sources.ToList());
            Assert.AreEqual(runSettings, DllTestDiscoverer.DiscoveryContext.RunSettings);
            Assert.AreEqual(testCaseFilter, (DllTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
            Assert.AreEqual(messageLoggerMock.Object, DllTestDiscoverer.MessageLogger);
            Assert.IsNotNull(DllTestDiscoverer.DiscoverySink);

            CollectionAssert.AreEqual(jsonsources, JsonTestDiscoverer.Sources.ToList());
            Assert.AreEqual(runSettings, JsonTestDiscoverer.DiscoveryContext.RunSettings);
            Assert.AreEqual(testCaseFilter, (JsonTestDiscoverer.DiscoveryContext as DiscoveryContext).FilterExpressionWrapper.FilterString);
            Assert.AreEqual(messageLoggerMock.Object, JsonTestDiscoverer.MessageLogger);
            Assert.IsNotNull(JsonTestDiscoverer.DiscoverySink);
        }

        [TestMethod]
        public void LoadTestsShouldLogWarningMessageOnNoTestsInAssemblies()
        {
            SetupForNoTestsAvailableInGivenAssemblies(out var extensionSourceMap, out var sourcesString);

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, null, messageLoggerMock.Object);

            var expectedMessage =
                $"No test is available in {sourcesString}. Make sure that test discoverer & executors are registered and platform & framework version settings are appropriate and try again.";

            messageLoggerMock.Verify(l => l.SendMessage(TestMessageLevel.Warning, expectedMessage));
        }

        [TestMethod]
        public void LoadTestsShouldLogWarningMessageOnNoTestsInAssembliesWithTestCaseFilter()
        {
            SetupForNoTestsAvailableInGivenAssemblies(out var extensionSourceMap, out var sourcesString);

            var testCaseFilter = "Name~TestMethod1";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, testCaseFilter, messageLoggerMock.Object);

            var expectedMessage =
                $"No test matches the given testcase filter `{testCaseFilter}` in {sourcesString}";

            messageLoggerMock.Verify(l => l.SendMessage(TestMessageLevel.Warning, expectedMessage));
        }

        [TestMethod]
        public void LoadTestsShouldShortenTheLongTestCaseFilterWhenNoTestsDiscovered()
        {
            SetupForNoTestsAvailableInGivenAssemblies(out var extensionSourceMap, out var sourcesString);

            var veryLengthyTestCaseFilter = "FullyQualifiedName=TestPlatform.CrossPlatEngine" +
                                            ".UnitTests.Discovery.DiscovererEnumeratorTests." +
                                            "LoadTestsShouldShortenTheLongTestCaseFilterWhenNoTestsDiscovered" +
                                            "TestCaseFilterWithVeryLengthTestCaseNameeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, veryLengthyTestCaseFilter, messageLoggerMock.Object);

            var expectedTestCaseFilter = veryLengthyTestCaseFilter.Substring(0, 256) + "...";
            var expectedMessage =
                $"No test matches the given testcase filter `{expectedTestCaseFilter}` in {sourcesString}";

            messageLoggerMock.Verify(l => l.SendMessage(TestMessageLevel.Warning, expectedMessage));
        }

        private static void SetupForNoTestsAvailableInGivenAssemblies(
            out Dictionary<string, IEnumerable<string>> extensionSourceMap,
            out string sourcesString)
        {
            var crossPlatEngineAssemblyLocation = typeof(DiscovererEnumerator).GetTypeInfo().Assembly.Location;
            var objectModelAseeAssemblyLocation = typeof(TestCase).GetTypeInfo().Assembly.Location;
            var sources = new string[] { crossPlatEngineAssemblyLocation, objectModelAseeAssemblyLocation };

            extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };
            sourcesString = string.Join(" ", crossPlatEngineAssemblyLocation, objectModelAseeAssemblyLocation);
        }

        private void InvokeLoadTestWithMockSetup()
        {
            TestPluginCacheHelper.SetupMockExtensions(
                    new string[] { typeof(DiscovererEnumeratorTests).GetTypeInfo().Assembly.Location },
                    () => { });

            var sources = new List<string>
                                  {
                                      typeof(DiscoveryResultCacheTests).GetTypeInfo().Assembly.Location
                                  };

            var extensionSourceMap = new Dictionary<string, IEnumerable<string>>
            {
                { "_none_", sources }
            };

            discovererEnumerator.LoadTests(extensionSourceMap, runSettingsMock.Object, null, messageLoggerMock.Object);
        }

        #region Implementation

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
        [DefaultExecutorUri("discoverer://manageddlldiscoverer")]
        [Category("managed")]
        private class ManagedDllTestDiscoverer : DllTestDiscoverer
        {
            public static bool IsManagedDiscoverTestCalled { get; private set; }

            public static IEnumerable<string> Sources { get; set; }

            public override void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                Sources = sources;
                IsManagedDiscoverTestCalled = true;
                base.DiscoverTests(sources, discoveryContext, logger, discoverySink);
            }

            public static void Reset()
            {
                IsManagedDiscoverTestCalled = false;
                Sources = null;
            }
        }

        [FileExtension(".dll")]
        [DefaultExecutorUri("discoverer://nativedlldiscoverer")]
        [Category("native")]
        private class NativeDllTestDiscoverer : DllTestDiscoverer
        {
            public static bool IsNativeDiscoverTestCalled { get; private set; }

            public static IEnumerable<string> Sources { get; set; }

            public override void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                Sources = sources;
                IsNativeDiscoverTestCalled = true;
                base.DiscoverTests(sources, discoveryContext, logger, discoverySink);
            }

            public static void Reset()
            {
                IsNativeDiscoverTestCalled = false;
                Sources = null;
            }
        }

        private class DllTestDiscoverer : ITestDiscoverer
        {
            public static IDiscoveryContext DiscoveryContext { get; private set; }

            public static IMessageLogger MessageLogger { get; private set; }

            public static ITestCaseDiscoverySink DiscoverySink { get; private set; }

            public virtual void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                if (ShouldTestDiscovered(sources) == false)
                {
                    return;
                }

                DiscoveryContext = discoveryContext;
                MessageLogger = logger;
                DiscoverySink = discoverySink;

                var testCase = new TestCase("A.C.M", new Uri("executor://dllexecutor"), "A");
                discoverySink.SendTestCase(testCase);
            }

            private static bool ShouldTestDiscovered(IEnumerable<string> sources)
            {
                var shouldTestDiscovered = false;
                foreach (var source in sources)
                {
                    if (source.Equals("native.dll") || source.Equals("managed.dll") || source.EndsWith("CrossPlatEngine.UnitTests.dll"))
                    {
                        shouldTestDiscovered = true;
                        break;
                    }
                }

                return shouldTestDiscovered;
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
