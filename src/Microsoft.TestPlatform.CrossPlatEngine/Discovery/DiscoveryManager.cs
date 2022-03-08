// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;

/// <summary>
/// Orchestrates discovery operations for the engine communicating with the test host process.
/// </summary>
public class DiscoveryManager : IDiscoveryManager
{
    private readonly TestSessionMessageLogger _sessionMessageLogger;
    private readonly ITestPlatformEventSource _testPlatformEventSource;
    private readonly IRequestData _requestData;
    private ITestDiscoveryEventsHandler2 _testDiscoveryEventsHandler;
    private DiscoveryCriteria _discoveryCriteria;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly DiscoverySourceStatusCache _discoverySourceStatusCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryManager"/> class.
    /// </summary>
    public DiscoveryManager(IRequestData requestData)
        : this(requestData, TestPlatformEventSource.Instance)
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
        _sessionMessageLogger = TestSessionMessageLogger.Instance;
        _sessionMessageLogger.TestRunMessage += TestSessionMessageHandler;
        _testPlatformEventSource = testPlatformEventSource;
        _requestData = requestData;
    }

    /// <summary>
    /// Initializes the discovery manager.
    /// </summary>
    /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
    public void Initialize(IEnumerable<string> pathToAdditionalExtensions, ITestDiscoveryEventsHandler2 eventHandler)
    {
        _testPlatformEventSource.AdapterSearchStart();
        _testDiscoveryEventsHandler = eventHandler;
        if (pathToAdditionalExtensions != null && pathToAdditionalExtensions.Any())
        {
            // Start using these additional extensions
            TestPluginCache.Instance.DefaultExtensionPaths = pathToAdditionalExtensions;
        }

        // Load and Initialize extensions.
        TestDiscoveryExtensionManager.LoadAndInitializeAllExtensions(false);
        _testPlatformEventSource.AdapterSearchStop();
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
            OnReportTestCases);

        try
        {
            _discoveryCriteria = discoveryCriteria;
            EqtTrace.Info("TestDiscoveryManager.DoDiscovery: Background test discovery started.");
            _testDiscoveryEventsHandler = eventHandler;
            var verifiedExtensionSourceMap = new Dictionary<string, IEnumerable<string>>();

            // Validate the sources
            foreach (var kvp in discoveryCriteria.AdapterSourceMap)
            {
                var verifiedSources = GetValidSources(kvp.Value, _sessionMessageLogger, discoveryCriteria.Package);
                if (verifiedSources.Count > 0)
                {
                    verifiedExtensionSourceMap.Add(kvp.Key, kvp.Value);
                    // Mark all sources as NotDiscovered before actual discovery starts
                    _discoverySourceStatusCache.MarkSourcesWithStatus(verifiedSources, DiscoveryStatus.NotDiscovered);
                }
            }

            // If there are sources to discover
            if (verifiedExtensionSourceMap.Any())
            {
                new DiscovererEnumerator(_requestData, discoveryResultCache, _cancellationTokenSource.Token).LoadTests(
                    verifiedExtensionSourceMap,
                    RunSettingsUtilities.CreateAndInitializeRunSettings(discoveryCriteria.RunSettings),
                    discoveryCriteria.TestCaseFilter,
                    _sessionMessageLogger);
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
                    UpdateTestCases(lastChunk, _discoveryCriteria.Package);
                }

                // Collecting Discovery State
                _requestData.MetricsCollection.Add(TelemetryDataConstants.DiscoveryState, "Completed");

                // Collecting Total Tests Discovered
                _requestData.MetricsCollection.Add(TelemetryDataConstants.TotalTestsDiscovered, totalDiscoveredTestCount);

                _discoverySourceStatusCache.MarkSourcesBasedOnDiscoveredTestCases(
                    lastChunk,
                    isComplete: !_cancellationTokenSource.IsCancellationRequested);

                var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(
                    _cancellationTokenSource.IsCancellationRequested ? -1 : totalDiscoveredTestCount,
                    _cancellationTokenSource.IsCancellationRequested,
                    _discoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered),
                    _discoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered),
                    _discoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered));

                discoveryCompleteEventsArgs.DiscoveredExtensions = TestPluginCache.Instance.TestExtensions?.GetCachedExtensions();
                discoveryCompleteEventsArgs.Metrics = _requestData.MetricsCollection.Metrics;

                eventHandler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, lastChunk);
            }
            else
            {
                EqtTrace.Warning(
                    "DiscoveryManager: Could not pass the discovery complete message as the callback is null.");
            }

            EqtTrace.Verbose("TestDiscoveryManager.DiscoveryComplete: Called DiscoveryComplete callback.");

            _testDiscoveryEventsHandler = null;
        }
    }

    /// <summary>
    /// Aborts the test discovery.
    /// </summary>
    public void Abort()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <inheritdoc/>
    public void Abort(ITestDiscoveryEventsHandler2 eventHandler)
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            Abort();
        }

        var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(
            -1, true,
            _discoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered),
            _discoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered),
            _discoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered));

        eventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);
    }

    private void OnReportTestCases(ICollection<TestCase> testCases)
    {
        UpdateTestCases(testCases, _discoveryCriteria.Package);

        if (_testDiscoveryEventsHandler != null)
        {
            _testDiscoveryEventsHandler.HandleDiscoveredTests(testCases);
            _discoverySourceStatusCache.MarkSourcesBasedOnDiscoveredTestCases(testCases, isComplete: false);
        }
        else
        {
            EqtTrace.Warning("DiscoveryManager: Could not pass the test results as the callback is null.");
        }
    }

    /// <summary>
    /// Verify/Normalize the test source files.
    /// </summary>
    /// <param name="sources"> Paths to source file to look for tests in.  </param>
    /// <param name="logger">logger</param>
    /// <param name="package">package</param>
    /// <returns> The list of verified sources. </returns>
    internal static HashSet<string> GetValidSources(IEnumerable<string> sources, IMessageLogger logger, string package)
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
        if (verifiedSources.Count == 0)
        {
            var sourcesString = string.Join(",", sources.ToArray());
            var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.NoValidSourceFoundForDiscovery, sourcesString);
            logger.SendMessage(TestMessageLevel.Warning, errorMessage);

            EqtTrace.Warning("TestDiscoveryManager: None of the source {0} is valid. ", sourcesString);

            return verifiedSources;
        }

        // Log the sources from where tests are being discovered
        EqtTrace.Info("TestDiscoveryManager: Discovering tests from sources {0}", string.Join(",", verifiedSources.ToArray()));

        return verifiedSources;
    }

    private void TestSessionMessageHandler(object sender, TestRunMessageEventArgs e)
    {
        EqtTrace.Info(
            "TestDiscoveryManager.RunMessage: calling TestRunMessage({0}, {1}) callback.",
            e.Level,
            e.Message);

        if (_testDiscoveryEventsHandler != null)
        {
            _testDiscoveryEventsHandler.HandleLogMessage(e.Level, e.Message);
        }
        else
        {
            EqtTrace.Warning(
                "DiscoveryManager: Could not pass the log message  '{0}' as the callback is null.",
                e.Message);
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
