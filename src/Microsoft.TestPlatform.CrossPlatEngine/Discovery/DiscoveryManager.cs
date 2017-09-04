// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    /// <summary>
    /// Orchestrates discovery operations for the engine communicating with the test host process.
    /// </summary>
    public class DiscoveryManager : IDiscoveryManager
    {
        private TestSessionMessageLogger sessionMessageLogger;
        private ITestPlatformEventSource testPlatformEventSource;
        private IMetricsCollector metricsCollector;
        private ITestDiscoveryEventsHandler testDiscoveryEventsHandler;
        private DiscoveryCriteria discoveryCriteria;

        private int totalTestsDiscovered;
        private Dictionary<string, int> adapterUrisAndTestCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryManager"/> class.
        /// </summary>
        public DiscoveryManager(IMetricsCollector metricsCollector) : this(TestPlatformEventSource.Instance, metricsCollector)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryManager"/> class.
        /// </summary>
        /// <param name="testPlatformEventSource">
        /// The test platform event source.
        /// </param>
        protected DiscoveryManager(ITestPlatformEventSource testPlatformEventSource, IMetricsCollector metricsCollector)
        {
            this.sessionMessageLogger = TestSessionMessageLogger.Instance;
            this.sessionMessageLogger.TestRunMessage += this.TestSessionMessageHandler;
            this.testPlatformEventSource = testPlatformEventSource;

            this.metricsCollector = metricsCollector;
            this.totalTestsDiscovered = 0;
            this.adapterUrisAndTestCount = new Dictionary<string, int>();
        }

        /// <summary>
        /// Initializes the discovery manager.
        /// </summary>
        /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
        public void Initialize(IEnumerable<string> pathToAdditionalExtensions)
        {
            this.testPlatformEventSource.AdapterSearchStart();

            if (pathToAdditionalExtensions != null && pathToAdditionalExtensions.Any())
            {
                // Start using these additional extensions
                TestPluginCache.Instance.DefaultExtensionPaths = pathToAdditionalExtensions;
            }

            // Load and Initialize extensions.
            TestDiscoveryExtensionManager.LoadAndInitializeAllExtensions(false);
            this.testPlatformEventSource.AdapterSearchStop();
        }

        /// <summary>
        /// Discovers tests
        /// </summary>
        /// <param name="discoveryCriteria">Settings, parameters for the discovery request</param>
        /// <param name="eventHandler">EventHandler for handling discovery events from Engine</param>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler)
        {
            var discoveryResultCache = new DiscoveryResultCache(
                discoveryCriteria.FrequencyOfDiscoveredTestsEvent,
                discoveryCriteria.DiscoveredTestEventTimeout,
                this.OnReportTestCases);

            try
            {
                this.discoveryCriteria = discoveryCriteria;
                EqtTrace.Info("TestDiscoveryManager.DoDiscovery: Background test discovery started.");
                this.testDiscoveryEventsHandler = eventHandler;
                var verifiedExtensionSourceMap = new Dictionary<string, IEnumerable<string>>();

                // Validate the sources 
                foreach (var kvp in discoveryCriteria.AdapterSourceMap)
                {
                    var verifiedSources = GetValidSources(kvp.Value, this.sessionMessageLogger);
                    if (verifiedSources.Any())
                    {
                        verifiedExtensionSourceMap.Add(kvp.Key, kvp.Value);
                    }
                }

                // If there are sources to discover
                if (verifiedExtensionSourceMap.Any())
                {
                    new DiscovererEnumerator(discoveryResultCache,this.metricsCollector).LoadTests(
                        verifiedExtensionSourceMap,
                        RunSettingsUtilities.CreateAndInitializeRunSettings(discoveryCriteria.RunSettings),
                        this.sessionMessageLogger);
                }
            }
            finally
            {
                // Discovery complete. Raise the DiscoveryCompleteEvent.
                EqtTrace.Verbose("TestDiscoveryManager.DoDiscovery: Background Test Discovery complete.");

                var totalDiscoveredTestCount = discoveryResultCache.TotalDiscoveredTests;
                var lastChunk = discoveryResultCache.Tests;

                EqtTrace.Verbose("TestDiscoveryManager.DiscoveryComplete: Calling DiscoveryComplete callback.");
 
                if (eventHandler != null)
                {
                    if(lastChunk != null)
                    {
                        UpdateTestCases(lastChunk, this.discoveryCriteria.Package);
                    }

                    this.totalTestsDiscovered += lastChunk.Count();

                    //For Collecting Metric
                    AddTestDiscoveredToEachAdapter(lastChunk);

                    // Collecting Discovery State
                    this.metricsCollector.AddMetric(UnitTestTelemetryDataConstants.DiscoveryState, "Completed");

                    // Collecting Total Tests Discovered
                    this.metricsCollector.AddMetric(UnitTestTelemetryDataConstants.TotalTestsDiscovered, totalTestsDiscovered.ToString());

                    // Collecting Number of tests discover by each adapter
                    LogNumberOfTestDiscoveredByEachAdapter();

                    eventHandler.HandleDiscoveryComplete(totalDiscoveredTestCount, lastChunk, false);
                }
                else
                {
                    EqtTrace.Warning(
                        "DiscoveryManager: Could not pass the discovery complete message as the callback is null.");
                }

                EqtTrace.Verbose("TestDiscoveryManager.DiscoveryComplete: Called DiscoveryComplete callback.");

                this.testDiscoveryEventsHandler = null;
            }
        }

        /// <summary>
        /// Aborts the test discovery.
        /// </summary>
        public void Abort()
        {
            // do nothing for now.
        }

        private void OnReportTestCases(IEnumerable<TestCase> testCases)
        {
            UpdateTestCases(testCases, this.discoveryCriteria.Package);

            if (this.testDiscoveryEventsHandler != null)
            {
                this.testDiscoveryEventsHandler.HandleDiscoveredTests(testCases);

                this.totalTestsDiscovered += testCases.Count();

                //For Collecting Metrics
                AddTestDiscoveredToEachAdapter(testCases);
            }
            else
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("DiscoveryManager: Could not pass the test results as the callback is null.");
                }
            }
        }

        /// <summary>
        /// Verify/Normalize the test source files.
        /// </summary>
        /// <param name="sources"> Paths to source file to look for tests in.  </param>
        /// <param name="logger">logger</param>
        /// <returns> The list of verified sources. </returns>
        internal static IEnumerable<string> GetValidSources(IEnumerable<string> sources, IMessageLogger logger)
        {
            Debug.Assert(sources != null, "sources");
            var verifiedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string source in sources)
            {
                // It is possible that runtime provider sent relative source path for remote scenario.
                string src = !Path.IsPathRooted(source) ? Path.Combine(Directory.GetCurrentDirectory(), source) : source;

                if (!File.Exists(src))
                {
                    var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.FileNotFound, src);
                    logger.SendMessage(TestMessageLevel.Warning, errorMessage);

                    continue;
                }

                if (!verifiedSources.Add(src))
                {
                    var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.DuplicateSource, src);
                    logger.SendMessage(TestMessageLevel.Warning, errorMessage);
                }
            }

            // No valid source is found => we cannot discover. 
            if (!verifiedSources.Any())
            {
                var sourcesString = string.Join(",", sources.ToArray());
                var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.NoValidSourceFoundForDiscovery, sourcesString);
                logger.SendMessage(TestMessageLevel.Warning, errorMessage);

                EqtTrace.Warning("TestDiscoveryManager: None of the source {0} is valid. ", sourcesString);

                return verifiedSources;
            }

            // Log the sources from where tests are being discovered
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("TestDiscoveryManager: Discovering tests from sources {0}", string.Join(",", verifiedSources.ToArray()));
            }

            return verifiedSources;
        }

        private void TestSessionMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info(
                    "TestDiscoveryManager.RunMessage: calling TestRunMessage({0}, {1}) callback.",
                    e.Level,
                    e.Message);
            }

            if (this.testDiscoveryEventsHandler != null)
            {
                this.testDiscoveryEventsHandler.HandleLogMessage(e.Level, e.Message);
            }
            else
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning(
                        "DiscoveryManager: Could not pass the log message  '{0}' as the callback is null.",
                        e.Message);
                }
            }
        }


        private static void UpdateTestCases(IEnumerable<TestCase> testCases, string package)
        {
            // Update TestCase objects Source data to contain the actual source(package) provided by IDE(users),
            // else these test cases are not displayed in TestWindow.
            if (!string.IsNullOrEmpty(package))
            {
                foreach(var tc in testCases)
                {
                    tc.Source = package;
                }
            }
        }

        private void AddTestDiscoveredToEachAdapter(IEnumerable<TestCase> TotalTests)
        {
            var groupByUris = TotalTests.GroupBy(x => x.ExecutorUri.ToString());

            int currentCountOfTests = 0;
            int alreadyExistTestCount = 0;

            foreach (var uri in groupByUris)
            {
                currentCountOfTests = uri.Count();

                if (adapterUrisAndTestCount.TryGetValue(uri.Key, out alreadyExistTestCount))
                {
                    adapterUrisAndTestCount[uri.Key] = alreadyExistTestCount + currentCountOfTests;
                }
                else
                {
                    adapterUrisAndTestCount.Add(uri.Key, currentCountOfTests);
                }
            }
        }

        private void LogNumberOfTestDiscoveredByEachAdapter()
        {
            // Collecting Total Tests Discovered by each Adapter
            foreach (var key in adapterUrisAndTestCount.Keys)
            {
                this.metricsCollector.AddMetric(String.Format("{0}.{1}", UnitTestTelemetryDataConstants.TotalTestsByAdapter, key), adapterUrisAndTestCount[key].ToString());
            }

            // Collectoing Number Of Adapters used to discover tests.
            this.metricsCollector.AddMetric(UnitTestTelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, adapterUrisAndTestCount.Count.ToString());
        }
    }
}
