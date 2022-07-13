// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Client;
using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine;

/// <summary>
/// 
/// </summary>
public class InProcessVsTestConsoleWrapper : IVsTestConsoleWrapper
{
    // Must be in sync with the highest supported version in
    // src/Microsoft.TestPlatform.CrossPlatEngine/EventHandlers/TestRequestHandler.cs file.
    private readonly int _highestSupportedVersion = 6;

    private readonly ITranslationLayerRequestSender _requestSender;
    private readonly ITestPlatformEventSource _testPlatformEventSource;
    private bool _isInitialized;
    private bool _sessionStarted;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="consoleParameters"></param>
    public InProcessVsTestConsoleWrapper(ConsoleParameters consoleParameters)
        : this(
              consoleParameters,
              requestSender: new VsTestConsoleRequestSender(),
              testRequestManager: null,
              executorParam: null,
              testPlatformEventSource: null)
    { }

    /// <summary>
    /// 
    /// </summary>
    /// 
    /// <param name="consoleParameters"></param>
    /// <param name="requestSender"></param>
    /// <param name="testRequestManager"></param>
    /// 
    /// <exception cref="TransationLayerException"></exception>
    internal InProcessVsTestConsoleWrapper(
        ConsoleParameters consoleParameters,
        ITranslationLayerRequestSender requestSender,
        ITestRequestManager? testRequestManager,
        Executor? executorParam,
        ITestPlatformEventSource? testPlatformEventSource)
    {
        EqtTrace.Info("VsTestConsoleWrapper.StartSession: Starting VsTestConsoleWrapper session.");

        _testPlatformEventSource = testPlatformEventSource ?? TestPlatformEventSource.Instance;
        _testPlatformEventSource.TranslationLayerInitializeStart();

        // Start communication.
        _requestSender = requestSender;
        var port = _requestSender.InitializeCommunication();
        if (port <= 0)
        {
            // Close the sender as it failed to host server.
            _requestSender.Close();
            throw new TransationLayerException("Error hosting communication channel.");
        }

        // Fill the parameters.
        consoleParameters.ParentProcessId = Process.GetCurrentProcess().Id;
        consoleParameters.PortNumber = port;

        // Start vstest.console.
        // TODO: under VS we use consoleParameters.InheritEnvironmentVariables, we take that
        // into account when starting a testhost, or clean up in the service host, and use the
        // desired set, so all children can inherit it.
        consoleParameters.EnvironmentVariables.ToList().ForEach(pair =>
            Environment.SetEnvironmentVariable(pair.Key, pair.Value));

        string someExistingFile = typeof(InProcessVsTestConsoleWrapper).Assembly.Location;
        var args = new VsTestConsoleProcessManager(someExistingFile).BuildArguments(consoleParameters);
        // Skip vstest.console path, we are already running in process, so it would just end up
        // being understood as test dll to run. (it is present even though we don't provide
        // dotnet path, because it is a .dll file).
        args = args.Skip(1).ToArray();
        var executor = executorParam ?? new Executor(ConsoleOutput.Instance);

        // We standup the client, and it will allocate port as normal that we will never use.
        // This is just to avoid "duplicating" all the setup logic that is done in argument
        // processors before client processor. It created design mode client, and stores it as
        // single instance.
        // We connect back to the client but never use that connection, it only serves as
        // "await" to make sure the design mode client is already initialized.
        Task.Run<int>(() => executor.Execute(args));
        WaitForConnection();

        // Set the test request manager here.
        TestRequestManager =
            testRequestManager
            ?? ((DesignModeClient)DesignModeClient.Instance!).TestRequestManager;

        _testPlatformEventSource.TranslationLayerInitializeStop();
    }

    internal ITestRequestManager? TestRequestManager { get; set; }

    /// <inheritdoc/>
    public void AbortTestRun()
    {
        try
        {
            TestRequestManager?.AbortTestRun();
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in AbortTestRun: " + ex);
        }
    }

    /// <inheritdoc/>
    public void CancelDiscovery()
    {
        try
        {
            TestRequestManager?.CancelDiscovery();
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in CancelDiscovery: " + ex);
        }
    }

    /// <inheritdoc/>
    public void CancelTestRun()
    {
        try
        {
            TestRequestManager?.CancelTestRun();
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in CancelTestRun: " + ex);
        }
    }

    /// <inheritdoc/>
    public void EndSession()
    {
        // Session means vstest.console process in the original api
        // we don't have a process to manage, we are in-process.
    }

    /// <inheritdoc/>
    public void StartSession()
    {
        // Session means vstest.console process in the original api
        // we don't have a process to manage, we are in-process.
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public ITestSession? StartTestSession(
        IList<string> sources,
        string? runSettings,
        ITestSessionEventsHandler eventsHandler)
    {
        return StartTestSession(sources, runSettings, options: null, eventsHandler);
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public ITestSession? StartTestSession(
        IList<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestSessionEventsHandler eventsHandler)
    {
        return StartTestSession(
            sources,
            runSettings,
            options,
            eventsHandler,
            testHostLauncher: null);
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public ITestSession? StartTestSession(
        IList<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestSessionEventsHandler eventsHandler,
        ITestHostLauncher? testHostLauncher)
    {
        _testPlatformEventSource.TranslationLayerStartTestSessionStart();

        var resetEvent = new ManualResetEvent(false);
        TestSessionInfo? testSessionInfo = null;

        try
        {
            TestRequestManager?.ResetOptions();
            var startTestSessionPayload = new StartTestSessionPayload()
            {
                Sources = sources,
                RunSettings = runSettings,
                HasCustomHostLauncher = testHostLauncher != null,
                IsDebuggingEnabled = (testHostLauncher != null)
                                     && testHostLauncher.IsDebug,
                TestPlatformOptions = options
            };

            var inProcessEventsHandler = new InProcessTestSessionEventsHandler(eventsHandler);
            inProcessEventsHandler.StartTestSessionCompleteEventHandler += (_, eventArgs) =>
            {
                testSessionInfo = eventArgs?.TestSessionInfo;
                resetEvent.Set();
            };

            TestRequestManager?.StartTestSession(
                startTestSessionPayload,
                (ITestHostLauncher3?)testHostLauncher,
                inProcessEventsHandler,
                new ProtocolConfig { Version = _highestSupportedVersion });

            resetEvent.WaitOne();
            inProcessEventsHandler.StartTestSessionCompleteEventHandler = null;
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in StartTestSession: " + ex);

            eventsHandler.HandleLogMessage(TestMessageLevel.Error, ex.ToString());
            eventsHandler.HandleStartTestSessionComplete(new());
        }

        _testPlatformEventSource.TranslationLayerStartTestSessionStop();

        return new TestSession(testSessionInfo, eventsHandler, this);
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public bool StopTestSession(
        TestSessionInfo? testSessionInfo,
        ITestSessionEventsHandler eventsHandler)
    {
        return StopTestSession(testSessionInfo, options: null, eventsHandler);
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public bool StopTestSession(
        TestSessionInfo? testSessionInfo,
        TestPlatformOptions? options,
        ITestSessionEventsHandler eventsHandler)
    {
        _testPlatformEventSource.TranslationLayerStopTestSessionStart();

        var isStopped = false;
        var resetEvent = new ManualResetEvent(false);

        try
        {
            TestRequestManager?.ResetOptions();
            var stopTestSessionPayload = new StopTestSessionPayload()
            {
                TestSessionInfo = testSessionInfo,
                CollectMetrics = options?.CollectMetrics ?? false
            };

            var inProcessEventsHandler = new InProcessTestSessionEventsHandler(eventsHandler);
            inProcessEventsHandler.StopTestSessionCompleteEventHandler += (_, eventArgs) =>
            {
                isStopped = (eventArgs?.IsStopped == true);
                resetEvent.Set();
            };

            TestRequestManager?.StopTestSession(
                stopTestSessionPayload,
                inProcessEventsHandler,
                new ProtocolConfig { Version = _highestSupportedVersion });

            resetEvent.WaitOne();
            inProcessEventsHandler.StopTestSessionCompleteEventHandler = null;
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in StopTestSession: " + ex);

            eventsHandler.HandleLogMessage(TestMessageLevel.Error, ex.ToString());
            eventsHandler.HandleStopTestSessionComplete(new());
        }

        _testPlatformEventSource.TranslationLayerStopTestSessionStop();

        return isStopped;
    }

    /// <inheritdoc/>
    public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
    {
        try
        {
            TestRequestManager?.InitializeExtensions(
                pathToAdditionalExtensions,
                skipExtensionFilters: true);
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in InitializeExtensions: " + ex);
        }
    }

    /// <inheritdoc/>
    public void DiscoverTests(
        IEnumerable<string> sources,
        string? discoverySettings,
        ITestDiscoveryEventsHandler discoveryEventsHandler)
    {
        DiscoverTests(
            sources,
            discoverySettings,
            options: null,
            new DiscoveryEventsHandleConverter(discoveryEventsHandler));
    }

    /// <inheritdoc/>
    public void DiscoverTests(
        IEnumerable<string> sources,
        string? discoverySettings,
        TestPlatformOptions? options,
        ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        DiscoverTests(
            sources,
            discoverySettings,
            options,
            testSessionInfo: null,
            discoveryEventsHandler);
    }

    /// <inheritdoc/>
    public void DiscoverTests(
        IEnumerable<string> sources,
        string? discoverySettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        _testPlatformEventSource.TranslationLayerDiscoveryStart();

        try
        {
            TestRequestManager?.ResetOptions();
            var discoveryRequestPayload = new DiscoveryRequestPayload()
            {
                Sources = sources,
                RunSettings = discoverySettings,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            };

            TestRequestManager?.DiscoverTests(
                discoveryRequestPayload,
                new DiscoveryHandlerToEventsRegistrarAdapter(discoveryEventsHandler),
                new ProtocolConfig { Version = _highestSupportedVersion });
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in StartDiscovery: " + ex);

            discoveryEventsHandler.HandleLogMessage(TestMessageLevel.Error, ex.ToString());
            var errorDiscoveryComplete = new DiscoveryCompleteEventArgs
            {
                IsAborted = true,
                TotalCount = -1,
            };
            discoveryEventsHandler.HandleDiscoveryComplete(
                errorDiscoveryComplete,
                lastChunk: null);
        }

        _testPlatformEventSource.TranslationLayerDiscoveryStop();
    }

    /// <inheritdoc/>
    public void RunTests(
        IEnumerable<string> sources,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler)
    {
        RunTests(
            sources,
            runSettings,
            options: null,
            testRunEventsHandler);
    }

    /// <inheritdoc/>
    public void RunTests(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler)
    {
        RunTests(
            sources,
            runSettings,
            options,
            testSessionInfo: null,
            testRunEventsHandler);
    }

    /// <inheritdoc/>
    public void RunTests(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler)
    {
        var sourceList = sources.ToList();
        _testPlatformEventSource.TranslationLayerExecutionStart(
            0,
            0,
            sourceList.Count,
            runSettings ?? string.Empty);

        try
        {
            TestRequestManager?.ResetOptions();

            var testRunPayload = new TestRunRequestPayload
            {
                Sources = sourceList.ToList(),
                RunSettings = runSettings,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            };

            TestRequestManager?.RunTests(
                testRunPayload,
                customTestHostLauncher: null,
                new RunHandlerToEventsRegistrarAdapter(testRunEventsHandler),
                new ProtocolConfig { Version = _highestSupportedVersion });
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in StartTestRun: " + ex);
            var testRunCompleteArgs = new TestRunCompleteEventArgs(null, false, true, ex, null, null, TimeSpan.MinValue);

            testRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, ex.ToString());
            testRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, null, null, null);
        }

        _testPlatformEventSource.TranslationLayerExecutionStop();
    }

    /// <inheritdoc/>
    public void RunTests(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler)
    {
        RunTests(
            testCases,
            runSettings,
            options: null,
            testRunEventsHandler);
    }

    /// <inheritdoc/>
    public void RunTests(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler)
    {
        RunTests(
            testCases,
            runSettings,
            options,
            testSessionInfo: null,
            testRunEventsHandler);
    }

    /// <inheritdoc/>
    public void RunTests(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler)
    {
        var testCaseList = testCases.ToList();
        _testPlatformEventSource.TranslationLayerExecutionStart(
            0,
            0,
            testCaseList.Count,
            runSettings ?? string.Empty);

        try
        {
            TestRequestManager?.ResetOptions();

            var testRunPayload = new TestRunRequestPayload
            {
                TestCases = testCases.ToList(),
                RunSettings = runSettings,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            };

            TestRequestManager?.RunTests(
                testRunPayload,
                customTestHostLauncher: null,
                new RunHandlerToEventsRegistrarAdapter(testRunEventsHandler),
                new ProtocolConfig { Version = _highestSupportedVersion });
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in StartTestRun: " + ex);
            var testRunCompleteArgs = new TestRunCompleteEventArgs(null, false, true, ex, null, null, TimeSpan.MinValue);

            testRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, ex.ToString());
            testRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, null, null, null);
        }

        _testPlatformEventSource.TranslationLayerExecutionStop();
    }

    /// <inheritdoc/>
    public void RunTestsWithCustomTestHost(
        IEnumerable<string> sources,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher? customTestHostLauncher)
    {
        RunTestsWithCustomTestHost(
            sources,
            runSettings,
            options: null,
            testRunEventsHandler,
            customTestHostLauncher);
    }

    /// <inheritdoc/>
    public void RunTestsWithCustomTestHost(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher? customTestHostLauncher)
    {
        RunTestsWithCustomTestHost(
            sources,
            runSettings,
            options,
            testSessionInfo: null,
            testRunEventsHandler,
            customTestHostLauncher);
    }

    /// <inheritdoc/>
    public void RunTestsWithCustomTestHost(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher? customTestHostLauncher)
    {
        var sourceList = sources.ToList();
        _testPlatformEventSource.TranslationLayerExecutionStart(
            1,
            sourceList.Count,
            0,
            runSettings ?? string.Empty);

        try
        {
            TestRequestManager?.ResetOptions();

            var shouldLaunchTesthost = true;

            // We must avoid re-launching the test host if the test run payload already
            // contains test session info. Test session info being present is an indicative
            // of an already running test host spawned by a start test session call.
            var customLauncher =
                shouldLaunchTesthost && testSessionInfo == null
                    ? customTestHostLauncher
                    : null;

            var testRunPayload = new TestRunRequestPayload
            {
                Sources = sourceList,
                RunSettings = runSettings,
                DebuggingEnabled = (customLauncher?.IsDebug == true),
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            };

            // TODO: this will need to be via an adapter instead. Because we will get a "live"
            // implementation rather than our own implementation that we normally have in design
            // mode client. It probably will even throw on this cast.
            var upcastCustomLauncher = (ITestHostLauncher3?)customLauncher;

            TestRequestManager?.RunTests(
                testRunPayload,
                upcastCustomLauncher,
                new RunHandlerToEventsRegistrarAdapter(testRunEventsHandler),
                new ProtocolConfig { Version = _highestSupportedVersion });
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in StartTestRun: " + ex);
            var testRunCompleteArgs = new TestRunCompleteEventArgs(null, false, true, ex, null, null, TimeSpan.MinValue);

            testRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, ex.ToString());
            testRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, null, null, null);
        }

        _testPlatformEventSource.TranslationLayerExecutionStop();
    }

    /// <inheritdoc/>
    public void RunTestsWithCustomTestHost(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher? customTestHostLauncher)
    {
        RunTestsWithCustomTestHost(
            testCases,
            runSettings,
            options: null,
            testRunEventsHandler,
            customTestHostLauncher);
    }

    /// <inheritdoc/>
    public void RunTestsWithCustomTestHost(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher? customTestHostLauncher)
    {
        RunTestsWithCustomTestHost(
            testCases,
            runSettings,
            options,
            testSessionInfo: null,
            testRunEventsHandler,
            customTestHostLauncher);
    }

    /// <inheritdoc/>
    public void RunTestsWithCustomTestHost(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher? customTestHostLauncher)
    {
        var testCaseList = testCases.ToList();
        _testPlatformEventSource.TranslationLayerExecutionStart(
            1,
            0,
            testCaseList.Count,
            runSettings ?? string.Empty);

        try
        {
            TestRequestManager?.ResetOptions();

            var shouldLaunchTesthost = true;

            // We must avoid re-launching the test host if the test run payload already
            // contains test session info. Test session info being present is an indicative
            // of an already running test host spawned by a start test session call.
            var customLauncher =
                shouldLaunchTesthost && testSessionInfo == null
                    ? customTestHostLauncher
                    : null;

            var testRunPayload = new TestRunRequestPayload
            {
                TestCases = testCases.ToList(),
                RunSettings = runSettings,
                DebuggingEnabled = (customLauncher?.IsDebug == true),
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            };

            // TODO: this will need to be via an adapter instead. Because we will get a "live"
            // implementation rather than our own implementation that we normally have in design
            // mode client. It probably will even throw on this cast.
            var upcastCustomLauncher = (ITestHostLauncher3?)customLauncher;

            TestRequestManager?.RunTests(
                testRunPayload,
                upcastCustomLauncher,
                new RunHandlerToEventsRegistrarAdapter(testRunEventsHandler),
                new ProtocolConfig { Version = _highestSupportedVersion });
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in StartTestRun: " + ex);
            var testRunCompleteArgs = new TestRunCompleteEventArgs(null, false, true, ex, null, null, TimeSpan.MinValue);

            testRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, ex.ToString());
            testRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, null, null, null);
        }

        _testPlatformEventSource.TranslationLayerExecutionStop();
    }

    #region Async, not implemented
    /// <inheritdoc/>
    public async Task DiscoverTestsAsync(
        IEnumerable<string> sources,
        string? discoverySettings,
        ITestDiscoveryEventsHandler discoveryEventsHandler)
    {
        await DiscoverTestsAsync(
                sources,
                discoverySettings,
                options: null,
                new DiscoveryEventsHandleConverter(discoveryEventsHandler))
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DiscoverTestsAsync(
        IEnumerable<string> sources,
        string? discoverySettings,
        TestPlatformOptions? options,
        ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        await DiscoverTestsAsync(
                sources,
                discoverySettings,
                options,
                testSessionInfo: null,
                discoveryEventsHandler)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DiscoverTestsAsync(
        IEnumerable<string> sources,
        string? discoverySettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task InitializeExtensionsAsync(IEnumerable<string> pathToAdditionalExtensions)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task ProcessTestRunAttachmentsAsync(
        IEnumerable<AttachmentSet> attachments,
        string? processingSettings,
        bool isLastBatch,
        bool collectMetrics,
        ITestRunAttachmentsProcessingEventsHandler eventsHandler,
        CancellationToken cancellationToken)
    {
        await ProcessTestRunAttachmentsAsync(
                attachments,
                invokedDataCollectors: null,
                processingSettings,
                isLastBatch,
                collectMetrics,
                eventsHandler,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ProcessTestRunAttachmentsAsync(
        IEnumerable<AttachmentSet> attachments,
        IEnumerable<InvokedDataCollector>? invokedDataCollectors,
        string? processingSettings,
        bool isLastBatch,
        bool collectMetrics,
        ITestRunAttachmentsProcessingEventsHandler eventsHandler,
        CancellationToken cancellationToken)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task RunTestsAsync(
        IEnumerable<string> sources,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler)
    {
        await RunTestsAsync(
                sources,
                runSettings,
                options: null,
                testRunEventsHandler)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunTestsAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler)
    {
        await RunTestsAsync(
                sources,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunTestsAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task RunTestsAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler)
    {
        await RunTestsAsync(
                testCases,
                runSettings,
                options: null,
                testRunEventsHandler)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunTestsAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler)
    {
        await RunTestsAsync(
                testCases,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunTestsAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task RunTestsWithCustomTestHostAsync(
        IEnumerable<string> sources,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher)
    {
        await RunTestsWithCustomTestHostAsync(
                sources,
                runSettings,
                options: null,
                testRunEventsHandler,
                customTestHostLauncher)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunTestsWithCustomTestHostAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher)
    {
        await RunTestsWithCustomTestHostAsync(
                sources,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler,
                customTestHostLauncher)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunTestsWithCustomTestHostAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task RunTestsWithCustomTestHostAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher)
    {
        await RunTestsWithCustomTestHostAsync(
                testCases,
                runSettings,
                options: null,
                testRunEventsHandler,
                customTestHostLauncher)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunTestsWithCustomTestHostAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher)
    {
        await RunTestsWithCustomTestHostAsync(
                testCases,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler,
                customTestHostLauncher)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunTestsWithCustomTestHostAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task StartSessionAsync()
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public async Task<ITestSession?> StartTestSessionAsync(
        IList<string> sources,
        string? runSettings,
        ITestSessionEventsHandler eventsHandler)
    {
        return await StartTestSessionAsync(
                sources,
                runSettings,
                options: null,
                eventsHandler)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public async Task<ITestSession?> StartTestSessionAsync(
        IList<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestSessionEventsHandler eventsHandler)
    {
        return await StartTestSessionAsync(
                sources,
                runSettings,
                options,
                eventsHandler,
                testHostLauncher: null)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public async Task<ITestSession?> StartTestSessionAsync(
        IList<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestSessionEventsHandler eventsHandler,
        ITestHostLauncher? testHostLauncher)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public async Task<bool> StopTestSessionAsync(
        TestSessionInfo? testSessionInfo,
        ITestSessionEventsHandler eventsHandler)
    {
        return await StopTestSessionAsync(
                testSessionInfo,
                options: null,
                eventsHandler)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public async Task<bool> StopTestSessionAsync(
        TestSessionInfo? testSessionInfo,
        TestPlatformOptions? options,
        ITestSessionEventsHandler eventsHandler)
    {
        // The Task.Delay(100) is a dumb way of silencing the analyzers, otherwise an error saying
        // the method lacks an await statement will pop up. This is only temporary though, until a
        // working implementation of this method will completely replace the current method body.
        await Task.Delay(100, CancellationToken.None);
        throw new NotImplementedException();
    }
    #endregion

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Process is not started.");
            StartSession();
            _sessionStarted = WaitForConnection();
        }

        if (!_sessionStarted && _requestSender != null)
        {
            EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Process Started.");
            _sessionStarted = WaitForConnection();
        }
    }

    private bool WaitForConnection()
    {
        EqtTrace.Info("VsTestConsoleWrapper.WaitForConnection: Waiting for connection to command line runner.");

        var timeout = EnvironmentHelper.GetConnectionTimeout();
        if (!_requestSender.WaitForRequestHandlerConnection(timeout * 1000))
        {
            throw new Exception("Waiting for request handler connection timed out.");
        }

        _testPlatformEventSource.TranslationLayerInitializeStop();
        return true;
    }
}
