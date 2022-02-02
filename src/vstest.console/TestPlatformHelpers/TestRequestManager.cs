// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;

using System;
using System.Xml;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.Client;
using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;
using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using System.Collections.Concurrent;

/// <summary>
/// Defines the test request manger which can fire off discovery and test run requests.
/// </summary>
internal class TestRequestManager : ITestRequestManager
{
    private static ITestRequestManager testRequestManagerInstance;

    private const int runRequestTimeout = 5000;

    private readonly ITestPlatform _testPlatform;
    private readonly ITestPlatformEventSource _testPlatformEventSource;
    private readonly Task<IMetricsPublisher> _metricsPublisher;
    private readonly object _syncObject = new object();

    private bool _isDisposed;
    private bool _telemetryOptedIn;
    private CommandLineOptions _commandLineOptions;
    private TestRunResultAggregator _testRunResultAggregator;
    private InferHelper _inferHelper;
    private IProcessHelper _processHelper;
    private ITestRunAttachmentsProcessingManager _attachmentsProcessingManager;

    private int _runId = 0;
    private int _discoveryId = 0;

    /// <summary>
    /// Maintains the current active execution request.
    /// </summary>
    private ConcurrentDictionary<int, ITestRunRequest> testRunRequests = new ConcurrentDictionary<int, ITestRunRequest>();

    /// <summary>
    /// Maintains the current active discovery request.
    /// </summary>
    private ConcurrentDictionary<int, IDiscoveryRequest> discoveryRequests = new ConcurrentDictionary<int, IDiscoveryRequest>();

    /// <summary>
    /// Maintains the current active test run attachments processing cancellation token source.
    /// Assumption: There can only be one active attachments processing request.
    /// </summary>
    //TODO: !
    private CancellationTokenSource _currentAttachmentsProcessingCancellationTokenSource;

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="TestRequestManager"/> class.
    /// </summary>
    public TestRequestManager()
        : this(
              CommandLineOptions.Instance,
              TestPlatformFactory.GetTestPlatform(),
              TestRunResultAggregator.Instance,
              TestPlatformEventSource.Instance,
              new InferHelper(AssemblyMetadataProvider.Instance),
              MetricsPublisherFactory.GetMetricsPublisher(
                  IsTelemetryOptedIn(),
                  CommandLineOptions.Instance.IsDesignMode),
              new ProcessHelper(),
              new TestRunAttachmentsProcessingManager(
                  TestPlatformEventSource.Instance,
                  new CodeCoverageDataAttachmentsHandler()))
    {
    }

    internal TestRequestManager(
        CommandLineOptions _commandLineOptions,
        ITestPlatform _testPlatform,
        TestRunResultAggregator _testRunResultAggregator,
        ITestPlatformEventSource _testPlatformEventSource,
        InferHelper _inferHelper,
        Task<IMetricsPublisher> _metricsPublisher,
        IProcessHelper _processHelper,
        ITestRunAttachmentsProcessingManager _attachmentsProcessingManager)
    {
        _testPlatform = _testPlatform;
        _commandLineOptions = _commandLineOptions;
        _testRunResultAggregator = _testRunResultAggregator;
        _testPlatformEventSource = _testPlatformEventSource;
        _inferHelper = _inferHelper;
        _metricsPublisher = _metricsPublisher;
        _processHelper = _processHelper;
        _attachmentsProcessingManager = _attachmentsProcessingManager;
    }

    #endregion

    /// <summary>
    /// Gets the test request manager instance.
    /// </summary>
    public static ITestRequestManager Instance
    {
        get
        {
            if (testRequestManagerInstance == null)
            {
                testRequestManagerInstance = new TestRequestManager();
            }

            return testRequestManagerInstance;
        }
    }

    #region ITestRequestManager

    /// <inheritdoc />
    public void InitializeExtensions(
        IEnumerable<string> pathToAdditionalExtensions,
        bool skipExtensionFilters)
    {
        // It is possible for an Editor/IDE to keep running the runner in design mode for long
        // duration. We clear the extensions cache to ensure the extensions don't get reused
        // across discovery/run requests.
        EqtTrace.Info("TestRequestManager.InitializeExtensions: Initialize extensions started.");
        _testPlatform.ClearExtensions();
        _testPlatform.UpdateExtensions(pathToAdditionalExtensions, skipExtensionFilters);
        EqtTrace.Info("TestRequestManager.InitializeExtensions: Initialize extensions completed.");
    }

    /// <inheritdoc />
    public void ResetOptions()
    {
        _commandLineOptions.Reset();
    }

    /// <inheritdoc />
    public void DiscoverTests(
        DiscoveryRequestPayload discoveryPayload,
        ITestDiscoveryEventsRegistrar discoveryEventsRegistrar,
        ProtocolConfig protocolConfig)
    {
        EqtTrace.Info("TestRequestManager.DiscoverTests: Discovery tests started.");

        var runsettings = discoveryPayload.RunSettings;

        if (discoveryPayload.TestPlatformOptions != null)
        {
            _telemetryOptedIn = discoveryPayload.TestPlatformOptions.CollectMetrics;
        }

        var requestData = GetRequestData(protocolConfig);
        if (UpdateRunSettingsIfRequired(
            runsettings,
            discoveryPayload.Sources?.ToList(),
            discoveryEventsRegistrar,
            out string updatedRunsettings))
        {
            runsettings = updatedRunsettings;
        }

        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettings);
        var batchSize = runConfiguration.BatchSize;
        var testCaseFilterFromRunsettings = runConfiguration.TestCaseFilter;

        if (requestData.IsTelemetryOptedIn)
        {
            // Collect metrics.
            CollectMetrics(requestData, runConfiguration);

            // Collect commands.
            LogCommandsTelemetryPoints(requestData);
        }

        // Create discovery request.
        var criteria = new DiscoveryCriteria(
            discoveryPayload.Sources,
            batchSize,
            _commandLineOptions.TestStatsEventTimeout,
            runsettings,
            discoveryPayload.TestSessionInfo)
        {
            TestCaseFilter = _commandLineOptions.TestCaseFilterValue
                ?? testCaseFilterFromRunsettings,
        };


        // Grab an id so we can have a simple way of referring back to the request
        // if it fails and we end up in finally.
        var id = Interlocked.Increment(ref _discoveryId);
        try
        {
            EqtTrace.Info("TestRequestManager.DiscoverTests: Synchronization context taken");
            var discoveryRequest = _testPlatform.CreateDiscoveryRequest(
                requestData,
                criteria,
                discoveryPayload.TestPlatformOptions);
            if (!discoveryRequests.TryAdd(id, discoveryRequest))
            {
                // This should never happen because we atomically increment the id, but let's have it in case I am wrong.
                throw new InvalidOperationException($"Discovery request with id '{id}' already exists.");
            }

            discoveryEventsRegistrar?.RegisterDiscoveryEvents(discoveryRequest);

            // Notify start of discovery start.
            _testPlatformEventSource.DiscoveryRequestStart();

            // Start the discovery of tests and wait for completion.
            discoveryRequest.DiscoverAsync();
            discoveryRequest.WaitForCompletion();
        }
        finally
        {
            if (discoveryRequests.TryRemove(id, out var discoveryRequest))
            {
                // Dispose the discovery request and unregister for events.
                discoveryEventsRegistrar?.UnregisterDiscoveryEvents(discoveryRequest);
                discoveryRequest.Dispose();
            }

            EqtTrace.Info("TestRequestManager.DiscoverTests: Discovery tests completed.");
            _testPlatformEventSource.DiscoveryRequestStop();

            // Posts the discovery complete event.
            _metricsPublisher.Result.PublishMetrics(
                TelemetryDataConstants.TestDiscoveryCompleteEvent,
                requestData.MetricsCollection.Metrics);
        }
    }

    /// <inheritdoc />
    public void RunTests(
        TestRunRequestPayload testRunRequestPayload,
        ITestHostLauncher testHostLauncher,
        ITestRunEventsRegistrar testRunEventsRegistrar,
        ProtocolConfig protocolConfig)
    {
        EqtTrace.Info("TestRequestManager.RunTests: run tests started.");

        TestRunCriteria runCriteria = null;
        var runsettings = testRunRequestPayload.RunSettings;

        if (testRunRequestPayload.TestPlatformOptions != null)
        {
            _telemetryOptedIn = testRunRequestPayload.TestPlatformOptions.CollectMetrics;
        }

        var requestData = GetRequestData(protocolConfig);

        // Get sources to auto detect fx and arch for both run selected or run all scenario.
        var sources = GetSources(testRunRequestPayload);

        if (UpdateRunSettingsIfRequired(
            runsettings,
            sources,
            testRunEventsRegistrar,
            out string updatedRunsettings))
        {
            runsettings = updatedRunsettings;
        }

        if (InferRunSettingsHelper.AreRunSettingsCollectorsIncompatibleWithTestSettings(runsettings))
        {
            throw new SettingsException(
                string.Format(
                    Resources.RunsettingsWithDCErrorMessage,
                    runsettings));
        }

        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettings);
        var batchSize = runConfiguration.BatchSize;

        if (requestData.IsTelemetryOptedIn)
        {
            // Collect metrics.
            CollectMetrics(requestData, runConfiguration);

            // Collect commands.
            LogCommandsTelemetryPoints(requestData);

            // Collect data for legacy settings.
            LogTelemetryForLegacySettings(requestData, runsettings);
        }

        // Get Fakes data collector settings.
        if (!string.Equals(Environment.GetEnvironmentVariable("VSTEST_SKIP_FAKES_CONFIGURATION"), "1"))
        {
            // The commandline options do not have sources in design time mode,
            // and so we fall back to using sources instead.
            if (_commandLineOptions.Sources.Any())
            {
                GenerateFakesUtilities.GenerateFakesSettings(
                    _commandLineOptions,
                    _commandLineOptions.Sources.ToList(),
                    ref runsettings);
            }
            else if (sources.Any())
            {
                GenerateFakesUtilities.GenerateFakesSettings(
                    _commandLineOptions,
                    sources,
                    ref runsettings);
            }
        }

        if (testRunRequestPayload.Sources != null && testRunRequestPayload.Sources.Any())
        {
            runCriteria = new TestRunCriteria(
                              testRunRequestPayload.Sources,
                              batchSize,
                              testRunRequestPayload.KeepAlive,
                              runsettings,
                              _commandLineOptions.TestStatsEventTimeout,
                              testHostLauncher,
                              testRunRequestPayload.TestPlatformOptions?.TestCaseFilter,
                              testRunRequestPayload.TestPlatformOptions?.FilterOptions,
                              testRunRequestPayload.TestSessionInfo,
                              debugEnabledForTestSession: testRunRequestPayload.TestSessionInfo != null
                                  && testRunRequestPayload.DebuggingEnabled);
        }
        else
        {
            runCriteria = new TestRunCriteria(
                              testRunRequestPayload.TestCases,
                              batchSize,
                              testRunRequestPayload.KeepAlive,
                              runsettings,
                              _commandLineOptions.TestStatsEventTimeout,
                              testHostLauncher,
                              testRunRequestPayload.TestSessionInfo,
                              debugEnabledForTestSession: testRunRequestPayload.TestSessionInfo != null
                                  && testRunRequestPayload.DebuggingEnabled);
        }

        // Run tests.
        try
        {
            RunTests(
                requestData,
                runCriteria,
                testRunEventsRegistrar,
                testRunRequestPayload.TestPlatformOptions);
             EqtTrace.Info("TestRequestManager.RunTests: run tests completed.");
        }
        finally
        {
            _testPlatformEventSource.ExecutionRequestStop();

            // Post the run complete event
            _metricsPublisher.Result.PublishMetrics(
                TelemetryDataConstants.TestExecutionCompleteEvent,
                requestData.MetricsCollection.Metrics);
        }
    }

    /// <inheritdoc/>
    public void ProcessTestRunAttachments(
        TestRunAttachmentsProcessingPayload attachmentsProcessingPayload,
        ITestRunAttachmentsProcessingEventsHandler attachmentsProcessingEventsHandler,
        ProtocolConfig protocolConfig)
    {
        EqtTrace.Info("TestRequestManager.ProcessTestRunAttachments: Test run attachments processing started.");

        _telemetryOptedIn = attachmentsProcessingPayload.CollectMetrics;
        var requestData = GetRequestData(protocolConfig);

        // Make sure to run the run request inside a lock as the below section is not thread-safe.
        // There can be only one discovery, execution or attachments processing request at a given
        // point in time.
        lock (_syncObject)
        {
            try
            {
                EqtTrace.Info("TestRequestManager.ProcessTestRunAttachments: Synchronization context taken.");
                _testPlatformEventSource.TestRunAttachmentsProcessingRequestStart();

                _currentAttachmentsProcessingCancellationTokenSource = new CancellationTokenSource();

                Task task = _attachmentsProcessingManager.ProcessTestRunAttachmentsAsync(
                    requestData,
                    attachmentsProcessingPayload.Attachments,
                    attachmentsProcessingEventsHandler,
                    _currentAttachmentsProcessingCancellationTokenSource.Token);
                task.Wait();
            }
            finally
            {
                if (_currentAttachmentsProcessingCancellationTokenSource != null)
                {
                    _currentAttachmentsProcessingCancellationTokenSource.Dispose();
                    _currentAttachmentsProcessingCancellationTokenSource = null;
                }

                EqtTrace.Info("TestRequestManager.ProcessTestRunAttachments: Test run attachments processing completed.");
                _testPlatformEventSource.TestRunAttachmentsProcessingRequestStop();

                // Post the attachments processing complete event.
                _metricsPublisher.Result.PublishMetrics(
                    TelemetryDataConstants.TestAttachmentsProcessingCompleteEvent,
                    requestData.MetricsCollection.Metrics);
            }
        }
    }

    /// <inheritdoc/>
    public void StartTestSession(
        StartTestSessionPayload payload,
        ITestHostLauncher testHostLauncher,
        ITestSessionEventsHandler eventsHandler,
        ProtocolConfig protocolConfig)
    {
        EqtTrace.Info("TestRequestManager.StartTestSession: Starting test session.");

        if (payload.TestPlatformOptions != null)
        {
            _telemetryOptedIn = payload.TestPlatformOptions.CollectMetrics;
        }

        var requestData = GetRequestData(protocolConfig);

        if (UpdateRunSettingsIfRequired(
            payload.RunSettings,
            payload.Sources,
            null,
            out string updatedRunsettings))
        {
            payload.RunSettings = updatedRunsettings;
        }

        if (InferRunSettingsHelper.AreRunSettingsCollectorsIncompatibleWithTestSettings(payload.RunSettings))
        {
            throw new SettingsException(
                string.Format(
                    Resources.RunsettingsWithDCErrorMessage,
                    payload.RunSettings));
        }

        // TODO (copoiena): Collect metrics ?

        lock (_syncObject)
        {
            try
            {
                EqtTrace.Info("TestRequestManager.StartTestRunner: Synchronization context taken.");
                _testPlatformEventSource.StartTestSessionStart();

                var criteria = new StartTestSessionCriteria()
                {
                    Sources = payload.Sources,
                    RunSettings = payload.RunSettings,
                    TestHostLauncher = testHostLauncher
                };

                if (!_testPlatform.StartTestSession(requestData, criteria, eventsHandler))
                {
                    EqtTrace.Warning("TestRequestManager.StartTestSession: Unable to start test session.");
                }
            }
            finally
            {
                EqtTrace.Info("TestRequestManager.StartTestSession: Starting test session completed.");
                _testPlatformEventSource.StartTestSessionStop();

                // Post the attachments processing complete event.
                _metricsPublisher.Result.PublishMetrics(
                    TelemetryDataConstants.StartTestSessionCompleteEvent,
                    requestData.MetricsCollection.Metrics);
            }
        }
    }

    private void LogTelemetryForLegacySettings(IRequestData requestData, string runsettings)
    {
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.TestSettingsUsed,
            InferRunSettingsHelper.IsTestSettingsEnabled(runsettings));

        if (InferRunSettingsHelper.TryGetLegacySettingElements(
            runsettings,
            out Dictionary<string, string> legacySettingsTelemetry))
        {
            foreach (var ciData in legacySettingsTelemetry)
            {
                // We are collecting telemetry for the legacy nodes and attributes used in the runsettings.
                requestData.MetricsCollection.Add(
                    string.Format(
                        "{0}.{1}",
                        TelemetryDataConstants.LegacySettingPrefix,
                        ciData.Key),
                    ciData.Value);
            }
        }
    }

    /// <inheritdoc />
    public void CancelTestRun()
    {
        EqtTrace.Info("TestRequestManager.CancelTestRun: Sending cancel request to all run requests.");
        var requests = testRunRequests?.ToList();
        foreach (var request in requests)
        {
            EqtTrace.Info("TestRequestManager.CancelTestRun: Sending cancel request to run {0}.", request.Key);
            request.Value?.CancelAsync();
        }
    }

    /// <inheritdoc />
    public void CancelDiscovery()
    {
        EqtTrace.Info("TestRequestManager.CancelDiscovery: Sending cancel request all discovery requests.");
        var requests = discoveryRequests?.ToList();
        foreach (var request in requests)
        {
            EqtTrace.Info("TestRequestManager.CancelDiscovery: Sending cancel request to discovery {0}.", request.Key);
            request.Value?.Abort();
        }
    }

    /// <inheritdoc />
    public void AbortTestRun()
    {
        EqtTrace.Info("TestRequestManager.AbortTestRun: Sending abort request to all run requests.");
        var requests = testRunRequests?.ToList();
        foreach (var request in requests)
        {
            EqtTrace.Info("TestRequestManager.AbortTestRun: Sending abort request to run {0}.", request.Key);
            request.Value?.Abort();
        }
    }

    /// <inheritdoc/>
    public void CancelTestRunAttachmentsProcessing()
    {
        EqtTrace.Info("TestRequestManager.CancelTestRunAttachmentsProcessing: Sending cancel request.");
        _currentAttachmentsProcessingCancellationTokenSource?.Cancel();
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);

        // Use SupressFinalize in case a subclass
        // of this type implements a finalizer.
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _metricsPublisher.Result.Dispose();
            }

            _isDisposed = true;
        }
    }

    private bool UpdateRunSettingsIfRequired(
        string runsettingsXml,
        IList<string> sources,
        IBaseTestEventsRegistrar registrar,
        out string updatedRunSettingsXml)
    {
        bool settingsUpdated = false;
        updatedRunSettingsXml = runsettingsXml;
        var sourcePlatforms = new Dictionary<string, Architecture>();
        var sourceFrameworks = new Dictionary<string, Framework>();

        if (!string.IsNullOrEmpty(runsettingsXml))
        {
            // TargetFramework is full CLR. Set DesignMode based on current context.
            using (var stream = new StringReader(runsettingsXml))
            using (var reader = XmlReader.Create(
                stream,
                XmlRunSettingsUtilities.ReaderSettings))
            {
                var document = new XmlDocument();
                document.Load(reader);
                var navigator = document.CreateNavigator();
                var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
                var loggerRunSettings = XmlRunSettingsUtilities.GetLoggerRunSettings(runsettingsXml)
                    ?? new LoggerRunSettings();

                settingsUpdated |= UpdateFramework(
                    document,
                    navigator,
                    sources,
                    sourceFrameworks,
                    registrar,
                    out Framework chosenFramework);

                // Choose default architecture based on the framework.
                // For .NET core, the default platform architecture should be based on the process.	
                Architecture defaultArchitecture = Architecture.X86;
                if (chosenFramework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                    || chosenFramework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0
                // This is a special case for 1 version of Nuget.Frameworks that was shipped with using identifier NET5 instead of NETCoreApp5 for .NET 5.
                    || chosenFramework.Name.IndexOf("net5", StringComparison.OrdinalIgnoreCase) >= 0)
                {
#if NETCOREAPP
                    // We are running in vstest.console that is either started via dotnet.exe
                    // or via vstest.console.exe .NET Core executable. For AnyCPU dlls this
                    // should resolve 32-bit SDK when running from 32-bit dotnet process and 
                    // 64-bit SDK when running from 64-bit dotnet process.
                    // As default architecture we specify the expected test host architecture,
                    // it can be specified by user on the command line with --arch or through runsettings.
                    // If it's not specified by user will be filled by current processor architecture;
                    // should be the same as SDK.
                    defaultArchitecture = runConfiguration.TargetPlatform;
                    EqtTrace.Verbose($"Default architecture: {defaultArchitecture}");
#else
                    // We are running in vstest.console.exe that was built against .NET
                    // Framework. This console prefers 32-bit because it needs to run as 32-bit
                    // to be compatible with QTAgent. It runs as 32-bit both under VS and in
                    // Developer console. Set the default architecture based on the OS
                    // architecture, to find 64-bit dotnet SDK when running AnyCPU dll on 64-bit
                    // system, and 32-bit SDK when running AnyCPU dll on 32-bit OS.
                    // We want to find 64-bit SDK because it is more likely to be installed.
                    defaultArchitecture = Environment.Is64BitOperatingSystem ? Architecture.X64 : Architecture.X86;
#endif
                }

                settingsUpdated |= UpdatePlatform(
                    document,
                    navigator,
                    sources,
                    sourcePlatforms,
                    defaultArchitecture,
                    out Architecture chosenPlatform);
                CheckSourcesForCompatibility(
                    chosenFramework,
                    chosenPlatform,
                    defaultArchitecture,
                    sourcePlatforms,
                    sourceFrameworks,
                    registrar);
                settingsUpdated |= UpdateDesignMode(document, runConfiguration);
                settingsUpdated |= UpdateCollectSourceInformation(document, runConfiguration);
                settingsUpdated |= UpdateTargetDevice(navigator, document, runConfiguration);
                settingsUpdated |= AddOrUpdateConsoleLogger(document, runConfiguration, loggerRunSettings);

                updatedRunSettingsXml = navigator.OuterXml;
            }
        }

        return settingsUpdated;
    }

    private bool AddOrUpdateConsoleLogger(
        XmlDocument document,
        RunConfiguration runConfiguration,
        LoggerRunSettings loggerRunSettings)
    {
        // Update console logger settings.
        bool consoleLoggerUpdated = UpdateConsoleLoggerIfExists(document, loggerRunSettings);

        // In case of CLI, add console logger if not already present.
        bool designMode = runConfiguration.DesignModeSet
            ? runConfiguration.DesignMode
            : _commandLineOptions.IsDesignMode;
        if (!designMode && !consoleLoggerUpdated)
        {
            AddConsoleLogger(document, loggerRunSettings);
        }

        // Update is required:
        //     1) in case of CLI;
        //     2) in case of design mode if console logger is present in runsettings.
        return !designMode || consoleLoggerUpdated;
    }

    private bool UpdateTargetDevice(
        XPathNavigator navigator,
        XmlDocument document,
        RunConfiguration runConfiguration)
    {
        bool updateRequired = InferRunSettingsHelper.TryGetDeviceXml(navigator, out string deviceXml);
        if (updateRequired)
        {
            InferRunSettingsHelper.UpdateTargetDevice(document, deviceXml);
        }
        return updateRequired;
    }

    private bool UpdateCollectSourceInformation(
        XmlDocument document,
        RunConfiguration runConfiguration)
    {
        bool updateRequired = !runConfiguration.CollectSourceInformationSet;
        if (updateRequired)
        {
            InferRunSettingsHelper.UpdateCollectSourceInformation(
                document,
                _commandLineOptions.ShouldCollectSourceInformation);
        }
        return updateRequired;
    }

    private bool UpdateDesignMode(XmlDocument document, RunConfiguration runConfiguration)
    {
        // If user is already setting DesignMode via runsettings or CLI args; we skip.
        bool updateRequired = !runConfiguration.DesignModeSet;
        if (updateRequired)
        {
            InferRunSettingsHelper.UpdateDesignMode(
                document,
                _commandLineOptions.IsDesignMode);
        }
        return updateRequired;
    }

    private void CheckSourcesForCompatibility(
        Framework chosenFramework,
        Architecture chosenPlatform,
        Architecture defaultArchitecture,
        IDictionary<string, Architecture> sourcePlatforms,
        IDictionary<string, Framework> sourceFrameworks,
        IBaseTestEventsRegistrar registrar)
    {
        // Find compatible sources.
        var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(
            chosenPlatform,
            defaultArchitecture,
            chosenFramework,
            sourcePlatforms,
            sourceFrameworks,
            out var incompatibleSettingWarning);

        // Raise warnings for incompatible sources
        if (!string.IsNullOrEmpty(incompatibleSettingWarning))
        {
            EqtTrace.Warning(incompatibleSettingWarning);
            registrar?.LogWarning(incompatibleSettingWarning);
        }

        // Log compatible sources
        if (EqtTrace.IsInfoEnabled)
        {
            EqtTrace.Info("Compatible sources list : ");
            EqtTrace.Info(string.Join("\n", compatibleSources.ToArray()));
        }
    }

    private bool UpdatePlatform(
        XmlDocument document,
        XPathNavigator navigator,
        IList<string> sources,
        IDictionary<string, Architecture> sourcePlatforms,
        Architecture defaultArchitecture,
        out Architecture chosenPlatform)
    {
        // Get platform from sources.
        var inferedPlatform = _inferHelper.AutoDetectArchitecture(
            sources,
            sourcePlatforms,
            defaultArchitecture);

        EqtTrace.Info($"Infered platform '{inferedPlatform}'.");

        // Get platform from runsettings.
        bool updatePlatform = IsAutoPlatformDetectRequired(navigator, out chosenPlatform);

        // Update platform if required. For command line scenario update happens in
        // ArgumentProcessor.
        if (updatePlatform)
        {
            EqtTrace.Info($"Platform update to '{inferedPlatform}' required.");
            InferRunSettingsHelper.UpdateTargetPlatform(
                document,
                inferedPlatform.ToString(),
                overwrite: true);
            chosenPlatform = inferedPlatform;
        }

        return updatePlatform;
    }

    private bool UpdateFramework(
        XmlDocument document,
        XPathNavigator navigator,
        IList<string> sources,
        IDictionary<string, Framework> sourceFrameworks,
        IBaseTestEventsRegistrar registrar,
        out Framework chosenFramework)
    {
        // Get framework from sources. 
        // This looks like you can optimize it by moving it down to if (updateFramework), but it has a side-effect of 
        // populating the sourceFrameworks, which is later checked when source compatibility check is done against the value
        // that we either inferred as the common framework, or that is forced in runsettings.
        var inferedFramework = _inferHelper.AutoDetectFramework(sources, sourceFrameworks);

        // See if framework is forced by runsettings. If not autodetect it.
        bool updateFramework = IsAutoFrameworkDetectRequired(navigator, out chosenFramework);

        // Update framework if required. For command line scenario update happens in
        // ArgumentProcessor.
        if (updateFramework)
        {
            InferRunSettingsHelper.UpdateTargetFramework(
                document,
                inferedFramework.ToString(),
                overwrite: true);
            chosenFramework = inferedFramework;
        }

        // Raise warnings for unsupported frameworks.
        if (Constants.DotNetFramework35.Equals(chosenFramework.Name))
        {
            EqtTrace.Warning("TestRequestManager.UpdateRunSettingsIfRequired: throw warning on /Framework:Framework35 option.");
            registrar.LogWarning(Resources.Framework35NotSupported);
        }

        return updateFramework;
    }

    /// <summary>
    /// Add console logger in runsettings.
    /// </summary>
    /// <param name="document">Runsettings document.</param>
    /// <param name="loggerRunSettings">Logger run settings.</param>
    private void AddConsoleLogger(XmlDocument document, LoggerRunSettings loggerRunSettings)
    {
        var consoleLogger = new LoggerSettings
        {
            FriendlyName = ConsoleLogger.FriendlyName,
            Uri = new Uri(ConsoleLogger.ExtensionUri),
            AssemblyQualifiedName = typeof(ConsoleLogger).AssemblyQualifiedName,
            CodeBase = typeof(ConsoleLogger).GetTypeInfo().Assembly.Location,
            IsEnabled = true
        };

        loggerRunSettings.LoggerSettingsList.Add(consoleLogger);
        RunSettingsProviderExtensions.UpdateRunSettingsXmlDocumentInnerXml(
            document,
            Constants.LoggerRunSettingsName,
            loggerRunSettings.ToXml().InnerXml);
    }

    /// <summary>
    /// Add console logger in runsettings if exists.
    /// </summary>
    /// <param name="document">Runsettings document.</param>
    /// <param name="loggerRunSettings">Logger run settings.</param>
    /// <returns>True if updated console logger in runsettings successfully.</returns>
    private bool UpdateConsoleLoggerIfExists(
        XmlDocument document,
        LoggerRunSettings loggerRunSettings)
    {
        var defaultConsoleLogger = new LoggerSettings
        {
            FriendlyName = ConsoleLogger.FriendlyName,
            Uri = new Uri(ConsoleLogger.ExtensionUri)
        };

        var existingLoggerIndex = loggerRunSettings.GetExistingLoggerIndex(defaultConsoleLogger);

        // Update assemblyQualifiedName and codeBase of existing logger.
        if (existingLoggerIndex >= 0)
        {
            var consoleLogger = loggerRunSettings.LoggerSettingsList[existingLoggerIndex];
            consoleLogger.AssemblyQualifiedName = typeof(ConsoleLogger).AssemblyQualifiedName;
            consoleLogger.CodeBase = typeof(ConsoleLogger).GetTypeInfo().Assembly.Location;
            RunSettingsProviderExtensions.UpdateRunSettingsXmlDocumentInnerXml(
                document,
                Constants.LoggerRunSettingsName,
                loggerRunSettings.ToXml().InnerXml);

            return true;
        }

        return false;
    }

    private void RunTests(
        IRequestData requestData,
        TestRunCriteria testRunCriteria,
        ITestRunEventsRegistrar testRunEventsRegistrar,
        TestPlatformOptions options)
    {
        var id = Interlocked.Increment(ref _runId);
        try
        {
            var runRequest = _testPlatform.CreateTestRunRequest(
                requestData,
                testRunCriteria,
                options);

            if (!testRunRequests.TryAdd(id, runRequest))
            {
                // This should never happen because we atomically increment the id, but let's have it in case I am wrong.
                throw new InvalidOperationException($"Discovery request with id '{id}' already exists.");
            }

            _testRunResultAggregator.RegisterTestRunEvents(runRequest);
            testRunEventsRegistrar?.RegisterTestRunEvents(runRequest);

            _testPlatformEventSource.ExecutionRequestStart();

            runRequest.ExecuteAsync();

            // Wait for the run completion event
            runRequest.WaitForCompletion();
        }
        catch (Exception ex)
        {
            EqtTrace.Error("TestRequestManager.RunTests: failed to run tests: {0}", ex);
            _testRunResultAggregator.MarkTestRunFailed();
            throw;
        }
        finally
        {
            if (testRunRequests.TryRemove(id, out var runRequest))
            {
                _testRunResultAggregator.UnregisterTestRunEvents(runRequest);
                testRunEventsRegistrar?.UnregisterTestRunEvents(runRequest);

                runRequest.Dispose();
            }
        }
    }

    private bool IsAutoFrameworkDetectRequired(
        XPathNavigator navigator,
        out Framework chosenFramework)
    {
        bool required = true;
        chosenFramework = null;
        if (_commandLineOptions.IsDesignMode)
        {
            bool isValidFx =
                InferRunSettingsHelper.TryGetFrameworkXml(
                    navigator,
                    out var frameworkFromrunsettingsXml);
            required = !isValidFx || string.IsNullOrWhiteSpace(frameworkFromrunsettingsXml);
            if (!required)
            {
                chosenFramework = Framework.FromString(frameworkFromrunsettingsXml);
            }
        }
        else if (!_commandLineOptions.IsDesignMode
            && _commandLineOptions.FrameworkVersionSpecified)
        {
            required = false;
            chosenFramework = _commandLineOptions.TargetFrameworkVersion;
        }

        return required;
    }

    private bool IsAutoPlatformDetectRequired(
        XPathNavigator navigator,
        out Architecture chosenPlatform)
    {
        bool required = true;
        chosenPlatform = Architecture.Default;
        if (_commandLineOptions.IsDesignMode)
        {
            bool isValidPlatform = InferRunSettingsHelper.TryGetPlatformXml(
                navigator,
                out var platformXml);

            required = !isValidPlatform || string.IsNullOrWhiteSpace(platformXml);
            if (!required)
            {
                chosenPlatform = (Architecture)Enum.Parse(
                    typeof(Architecture),
                    platformXml, true);
            }
        }
        else if (!_commandLineOptions.IsDesignMode && _commandLineOptions.ArchitectureSpecified)
        {
            required = false;
            chosenPlatform = _commandLineOptions.TargetArchitecture;
        }

        return required;
    }

    /// <summary>
    /// Collect metrics.
    /// </summary>
    /// <param name="requestData">Request data for common Discovery/Execution services.</param>
    /// <param name="runConfiguration">Run configuration.</param>
    private void CollectMetrics(IRequestData requestData, RunConfiguration runConfiguration)
    {
        // Collecting Target Framework.
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.TargetFramework,
            runConfiguration.TargetFramework.Name);

        // Collecting Target Platform.
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.TargetPlatform,
            runConfiguration.TargetPlatform.ToString());

        // Collecting Max CPU count.
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.MaxCPUcount,
            runConfiguration.MaxCpuCount);

        // Collecting Target Device. Here, it will be updated run settings so, target device
        // will be under run configuration only.
        var targetDevice = runConfiguration.TargetDevice;
        if (string.IsNullOrEmpty(targetDevice))
        {
            requestData.MetricsCollection.Add(
                TelemetryDataConstants.TargetDevice,
                "Local Machine");
        }
        else if (targetDevice.Equals("Device", StringComparison.Ordinal)
            || targetDevice.Contains("Emulator"))
        {
            requestData.MetricsCollection.Add(
                TelemetryDataConstants.TargetDevice,
                targetDevice);
        }
        else
        {
            // For IOT scenarios.
            requestData.MetricsCollection.Add(
                TelemetryDataConstants.TargetDevice,
                "Other");
        }

        // Collecting TestPlatform version.
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.TestPlatformVersion,
            Product.Version);

        // Collecting TargetOS.
        requestData.MetricsCollection.Add(
           TelemetryDataConstants.TargetOS,
           new PlatformEnvironment().OperatingSystemVersion);

        //Collecting DisableAppDomain.
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.DisableAppDomain,
            runConfiguration.DisableAppDomain);

    }

    /// <summary>
    /// Checks whether Telemetry opted in or not.
    /// By Default opting out
    /// </summary>
    /// <returns>Returns Telemetry Opted out or not</returns>
    private static bool IsTelemetryOptedIn()
    {
        var telemetryStatus = Environment.GetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN");
        return !string.IsNullOrEmpty(telemetryStatus)
            && telemetryStatus.Equals("1", StringComparison.Ordinal);
    }

    /// <summary>
    /// Log Command Line switches for Telemetry purposes
    /// </summary>
    /// <param name="requestData">Request Data providing common discovery/execution services.</param>
    private void LogCommandsTelemetryPoints(IRequestData requestData)
    {
        var commandsUsed = new List<string>();

        var parallel = _commandLineOptions.Parallel;
        if (parallel)
        {
            commandsUsed.Add("/Parallel");
        }

        var platform = _commandLineOptions.ArchitectureSpecified;
        if (platform)
        {
            commandsUsed.Add("/Platform");
        }

        var enableCodeCoverage = _commandLineOptions.EnableCodeCoverage;
        if (enableCodeCoverage)
        {
            commandsUsed.Add("/EnableCodeCoverage");
        }

        var inIsolation = _commandLineOptions.InIsolation;
        if (inIsolation)
        {
            commandsUsed.Add("/InIsolation");
        }

        var useVsixExtensions = _commandLineOptions.UseVsixExtensions;
        if (useVsixExtensions)
        {
            commandsUsed.Add("/UseVsixExtensions");
        }

        var frameworkVersionSpecified = _commandLineOptions.FrameworkVersionSpecified;
        if (frameworkVersionSpecified)
        {
            commandsUsed.Add("/Framework");
        }

        var settings = _commandLineOptions.SettingsFile;
        if (!string.IsNullOrEmpty(settings))
        {
            var extension = Path.GetExtension(settings);
            if (string.Equals(extension, ".runsettings", StringComparison.OrdinalIgnoreCase))
            {
                commandsUsed.Add("/settings//.RunSettings");
            }
            else if (string.Equals(extension, ".testsettings", StringComparison.OrdinalIgnoreCase))
            {
                commandsUsed.Add("/settings//.TestSettings");
            }
            else if (string.Equals(extension, ".vsmdi", StringComparison.OrdinalIgnoreCase))
            {
                commandsUsed.Add("/settings//.vsmdi");
            }
            else if (string.Equals(extension, ".testrunConfig", StringComparison.OrdinalIgnoreCase))
            {
                commandsUsed.Add("/settings//.testrunConfig");
            }
        }

        requestData.MetricsCollection.Add(
            TelemetryDataConstants.CommandLineSwitches,
            string.Join(",", commandsUsed.ToArray()));
    }

    /// <summary>
    /// Gets request data.
    /// </summary>
    /// <param name="protocolConfig">Protocol config.</param>
    /// <returns>Request data.</returns>
    private IRequestData GetRequestData(ProtocolConfig protocolConfig)
    {
        return new RequestData
        {
            ProtocolConfig = protocolConfig,
            MetricsCollection =
                           _telemetryOptedIn || IsTelemetryOptedIn()
                               ? (IMetricsCollection)new MetricsCollection()
                               : new NoOpMetricsCollection(),
            IsTelemetryOptedIn = _telemetryOptedIn || IsTelemetryOptedIn()
        };
    }

    private List<String> GetSources(TestRunRequestPayload testRunRequestPayload)
    {
        List<string> sources = new List<string>();
        if (testRunRequestPayload.Sources != null
            && testRunRequestPayload.Sources.Count > 0)
        {
            sources = testRunRequestPayload.Sources;
        }
        else if (testRunRequestPayload.TestCases != null
            && testRunRequestPayload.TestCases.Count > 0)
        {
            ISet<string> sourcesSet = new HashSet<string>();
            foreach (var testCase in testRunRequestPayload.TestCases)
            {
                sourcesSet.Add(testCase.Source);
            }
            sources = sourcesSet.ToList();
        }
        return sources;
    }
}
