// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CrossPlatEngineResources = Resources.Resources;

    /// <summary>
    /// Orchestrates discovery operations for the engine communicating with the test host process.
    /// </summary>
    public class DiscoveryManager : IDiscoveryManager
    {
        private readonly TestSessionMessageLogger sessionMessageLogger;
        private readonly ITestPlatformEventSource testPlatformEventSource;
        private readonly IRequestData requestData;
        private ITestDiscoveryEventsHandler2 testDiscoveryEventsHandler;
        private DiscoveryCriteria discoveryCriteria;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private string previousSource = null;
        private ConcurrentDictionary<string, DiscoveryStatus> DiscoveredSourcesWithStatus { get; set; } = new ConcurrentDictionary<string, DiscoveryStatus>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryManager"/> class.
        /// </summary>
        public DiscoveryManager(IRequestData requestData) : this(requestData, TestPlatformEventSource.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryManager"/> class.
        /// </summary>
        /// <param name="requestData">
        /// The Request Data for providing discovery services and data.
        /// </param>
        /// <param name="testPlatformEventSource">
        ///     The test platform event source.
        /// </param>
        protected DiscoveryManager(IRequestData requestData, ITestPlatformEventSource testPlatformEventSource)
        {
            this.sessionMessageLogger = TestSessionMessageLogger.Instance;
            this.sessionMessageLogger.TestRunMessage += this.TestSessionMessageHandler;
            this.testPlatformEventSource = testPlatformEventSource;
            this.requestData = requestData;
        }

        /// <summary>
        /// Initializes the discovery manager.
        /// </summary>
        /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
        public void Initialize(IEnumerable<string> pathToAdditionalExtensions, ITestDiscoveryEventsHandler2 eventHandler)
        {
            this.testPlatformEventSource.AdapterSearchStart();
            this.testDiscoveryEventsHandler = eventHandler;
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
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
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
                    var verifiedSources = GetValidSources(kvp.Value, this.sessionMessageLogger, discoveryCriteria.Package);
                    if (verifiedSources.Any())
                    {
                        verifiedExtensionSourceMap.Add(kvp.Key, kvp.Value);
                        // Mark all sources as NotDiscovered before actual discovery starts
                        MarkSourcesWithStatus(verifiedSources, DiscoveryStatus.NotDiscovered);
                    }
                }

                // If there are sources to discover
                if (verifiedExtensionSourceMap.Any())
                {
                    new DiscovererEnumerator(this.requestData, discoveryResultCache,cancellationTokenSource.Token).LoadTests(
                                                verifiedExtensionSourceMap,
                                                RunSettingsUtilities.CreateAndInitializeRunSettings(discoveryCriteria.RunSettings),
                                                discoveryCriteria.TestCaseFilter,
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
                    if (lastChunk != null)
                    {
                        UpdateTestCases(lastChunk, this.discoveryCriteria.Package);
                        /* When discovery is complete we will have case that the last source which was discovered still marked as partiallyDiscovered.
                         * So we need to mark it as fullyDiscovered.*/
                        MarkTheLastSourceAsFullyDiscovered(lastChunk);
                    }

                    // Collecting Discovery State
                    this.requestData.MetricsCollection.Add(TelemetryDataConstants.DiscoveryState, "Completed");

                    // Collecting Total Tests Discovered
                    this.requestData.MetricsCollection.Add(TelemetryDataConstants.TotalTestsDiscovered, totalDiscoveredTestCount);

                    /* This is added because new aborting flow is introduced where we send CancelDiscovery message to testhost and
                     * collect list of sources to send back to IDE. Previously vstest.console.exe just closed connection with testhost.
                     * So now we can have scenario when discovery from adapter is almost is finished and user requested abort.
                     * Then we can get race condition, when 2 separate flows will run inside testhost, so we dont know which one will finish discovery earlier.
                     * To fix this here we should check if aborting was already requested, so we need to send totalTest = -1 and isAborted = true, if this flow finishes earlier
                     * then aborting flow.
                     */
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        totalDiscoveredTestCount = -1;
                    }

                    var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalDiscoveredTestCount, cancellationTokenSource.IsCancellationRequested,
                                                                                     GetFilteredSources(DiscoveryStatus.FullyDiscovered),
                                                                                     GetFilteredSources(DiscoveryStatus.PartiallyDiscovered),
                                                                                     GetFilteredSources(DiscoveryStatus.NotDiscovered));

                    discoveryCompleteEventsArgs.Metrics = this.requestData.MetricsCollection.Metrics;

                    eventHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, lastChunk);
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
            this.cancellationTokenSource.Cancel();
        }

        /// <inheritdoc/>
        public void Abort(ITestDiscoveryEventsHandler2 eventHandler)
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                this.Abort();
            }

            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(-1, true,
                                                                            GetFilteredSources(DiscoveryStatus.FullyDiscovered),
                                                                            GetFilteredSources(DiscoveryStatus.PartiallyDiscovered),
                                                                            GetFilteredSources(DiscoveryStatus.NotDiscovered));

            eventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);
        }

        private void OnReportTestCases(IEnumerable<TestCase> testCases)
        {
            UpdateTestCases(testCases, this.discoveryCriteria.Package);

            if (this.testDiscoveryEventsHandler != null)
            {
                this.testDiscoveryEventsHandler.HandleDiscoveredTests(testCases);
                // We need to mark sources based on already discovered testcases
                MarkSourcesBasedOnDiscoveredTestCases(testCases);
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
        /// <param name="package">package</param>
        /// <returns> The list of verified sources. </returns>
        internal static IEnumerable<string> GetValidSources(IEnumerable<string> sources, IMessageLogger logger, string package)
        {
            Debug.Assert(sources != null, "sources");
            var verifiedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string source in sources)
            {
                // It is possible that runtime provider sent relative source path for remote scenario.
                string src = !Path.IsPathRooted(source) ? Path.Combine(Directory.GetCurrentDirectory(), source) : source;

                if (!File.Exists(src))
                {
                    void SendWarning()
                    {
                        var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.FileNotFound, src);
                        logger.SendMessage(TestMessageLevel.Warning, errorMessage);
                    }

                    if (string.IsNullOrEmpty(package))
                    {
                        SendWarning();

                        continue;
                    }

                    // It is also possible that this is a packaged app, so the tests might be inside the package
                    src = !Path.IsPathRooted(source) ? Path.Combine(Path.GetDirectoryName(package), source) : source;

                    if (!File.Exists(src))
                    {
                        SendWarning();

                        continue;
                    }
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
                foreach (var tc in testCases)
                {
                    tc.Source = package;
                }
            }
        }

        /// <summary>
        /// Mark sources based on already discovered testCases
        /// </summary>
        /// <param name="testCases">List of testCases which were already discovered</param>
        private void MarkSourcesBasedOnDiscoveredTestCases(IEnumerable<TestCase> testCases)
        {
            if (testCases == null || testCases.Count() == 0) return;

            foreach (var testCase in testCases)
            {
                string currentSource = testCase.Source;

                // If it is first list of testCases which was discovered, we mark sources as partiallyDiscovered
                // Or if current source is the same as previous we mark them as partiallyDiscovered again for consistency
                if (previousSource is null || previousSource == currentSource)
                {
                    MarkSourceWithStatus(currentSource, DiscoveryStatus.PartiallyDiscovered);
                }
                // If source is changed, we need to mark previous source as already fullyDiscovered
                // and currentSource should be partiallyDiscovered
                else if (previousSource != currentSource)
                {
                    MarkSourceWithStatus(previousSource, DiscoveryStatus.FullyDiscovered);
                    MarkSourceWithStatus(currentSource, DiscoveryStatus.PartiallyDiscovered);
                }

                this.previousSource = currentSource;
            }
        }

        /// <summary>
        /// Mark the last source as fullyDiscovered
        /// </summary>
        /// <param name="lastChunk">Last chunk of testCases which were discovered</param>
        private void MarkTheLastSourceAsFullyDiscovered(IList<TestCase> lastChunk)
        {
            int size = lastChunk.Count;
            var lastTestCase = lastChunk[size - 1];
            string lastSource = lastTestCase.Source;
            DiscoveredSourcesWithStatus[lastSource] = DiscoveryStatus.FullyDiscovered;
        }

        /// <summary>
        /// Mark the source with particular DiscoveryStatus
        /// </summary>
        /// <param name="source">Sources to mark</param>
        /// <param name="status">DiscoveryStatus to mark for source</param>
        private void MarkSourceWithStatus(string source, DiscoveryStatus status)
        {
            if (source == null) return;
            DiscoveredSourcesWithStatus[source] = status;
        }

        /// <summary>
        /// Mark sources with particular DiscoveryStatus
        /// </summary>
        /// <param name="sources">List of sources to mark</param>
        /// <param name="status">DiscoveryStatus to mark for list of sources</param>
        private void MarkSourcesWithStatus(IEnumerable<string> sources, DiscoveryStatus status)
        {
            if (sources == null || sources.Count() == 0) return;

            foreach (var source in sources)
            {
                DiscoveredSourcesWithStatus[source] = status;
            }
        }

        /// <summary>
        /// Filters discovery sources based on discovery status condition
        /// </summary>
        /// <param name="discoveryStatus">discoveryStatus indicates if source was fully/partially/not discovered</param>
        /// <returns></returns>
        private IReadOnlyCollection<string> GetFilteredSources(DiscoveryStatus discoveryStatus)
        {
            var discoveredSources = DiscoveredSourcesWithStatus;

            // If by some accident discoveredSources map is empty we will return empty list
            if (discoveredSources == null || discoveredSources.Count == 0)
            {
                return new List<string>();
            }

            return discoveredSources.Where(source => source.Value == discoveryStatus)
                                    .Select(source => source.Key).ToList();
        }
    }
}
