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
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

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
                    }
                }


                // If there are sources to discover
                if (verifiedExtensionSourceMap.Any())
                {
                    new DiscovererEnumerator(this.requestData, discoveryResultCache, cancellationTokenSource.Token).LoadTests(
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
                    }

                    // Collecting Discovery State
                    this.requestData.MetricsCollection.Add(TelemetryDataConstants.DiscoveryState, "Completed");

                    // Collecting Total Tests Discovered
                    this.requestData.MetricsCollection.Add(TelemetryDataConstants.TotalTestsDiscovered, totalDiscoveredTestCount);
                    var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(totalDiscoveredTestCount, false)
                    {
                        Metrics = this.requestData.MetricsCollection.Metrics
                    };

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

        private void OnReportTestCases(IEnumerable<TestCase> testCases)
        {
            UpdateTestCases(testCases, this.discoveryCriteria.Package);

            if (this.testDiscoveryEventsHandler != null)
            {
                this.testDiscoveryEventsHandler.HandleDiscoveredTests(testCases);
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
    }
}
