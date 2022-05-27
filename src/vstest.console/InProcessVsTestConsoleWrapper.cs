// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CommandLine;

internal class InProcessVsTestConsoleWrapper : IVsTestConsoleWrapper
{
    private readonly ITestPlatformEventSource _testPlatformEventSource = TestPlatformEventSource.Instance;
    private readonly VsTestConsoleRequestSender _requestSender;
    private bool _isInitialized;
    private bool _sessionStarted;

    // Must be in sync with the highest supported version in
    // src/Microsoft.TestPlatform.CrossPlatEngine/EventHandlers/TestRequestHandler.cs file.
    private readonly int _highestSupportedVersion = 6;

    public InProcessVsTestConsoleWrapper(ConsoleParameters _consoleParameters)
    {
        EqtTrace.Info("VsTestConsoleWrapper.StartSession: Starting VsTestConsoleWrapper session.");

        _testPlatformEventSource.TranslationLayerInitializeStart();

        // Start communication
        _requestSender = new VsTestConsoleRequestSender();
        var port = _requestSender.InitializeCommunication();

        if (port > 0)
        {
            // Fill the parameters
            _consoleParameters.ParentProcessId = Process.GetCurrentProcess().Id;
            _consoleParameters.PortNumber = port;

            // Start vstest.console
            // TODO: under VS we use consoleParameters.InheritEnvironmentVariables, we take that into account when starting a testhost, or clean up
            // in the service host, and use the desired set, so all children can inherit it.
            _consoleParameters.EnvironmentVariables.ToList().ForEach(pair => Environment.SetEnvironmentVariable(pair.Key, pair.Value));

            string someExistingFile = typeof(InProcessVsTestConsoleWrapper).Assembly.Location;
            var args = new VsTestConsoleProcessManager(someExistingFile).BuildArguments(_consoleParameters);
            // Skip vstest.console path, we are already running in process, so it would just end up being
            // understood as test dll to run. (it is present even though we don't provide dotnet path, because it is a .dll file.
            args = args.Skip(1).ToArray();
            var executor = new Executor(ConsoleOutput.Instance);

            // We standup the client, and it will allocate port as normall that we will never use.
            // This is just to avoid "duplicating" all the setup logic that is done in argument processors before
            // client processor. It created design mode client, and stores it as single instance.
            // We connect back to the client but never use that connection, it only serves as "await"
            // to make sure the design mode client is already initialized.
            Task.Run<int>(() => executor.Execute(args));
            WaitForConnection();
        }
        else
        {
            // Close the sender as it failed to host server
            _requestSender.Close();
            throw new TransationLayerException("Error hosting communication channel");
        }
    }

    public void AbortTestRun()
    {
        throw new NotImplementedException();
    }

    public void CancelDiscovery()
    {
        throw new NotImplementedException();
    }

    public void CancelTestRun()
    {
        throw new NotImplementedException();
    }

    public void EndSession()
    {
        // Session means vstest.console process in the original api
        // we don't have a process to manage, we are in-process.
    }

    public void StartSession()
    {
        // Session means vstest.console process in the original api
        // we don't have a process to manage, we are in-process.
    }

    public ITestSession StartTestSession(IList<string> sources, string runSettings, ITestSessionEventsHandler eventsHandler)
    {
        throw new NotImplementedException();
    }

    public ITestSession StartTestSession(IList<string> sources, string runSettings, TestPlatformOptions options, ITestSessionEventsHandler eventsHandler)
    {
        throw new NotImplementedException();
    }

    public ITestSession StartTestSession(IList<string> sources, string runSettings, TestPlatformOptions options, ITestSessionEventsHandler eventsHandler, ITestHostLauncher testHostLauncher)
    {
        throw new NotImplementedException();
    }

    public bool StopTestSession(TestSessionInfo testSessionInfo, ITestSessionEventsHandler eventsHandler)
    {
        throw new NotImplementedException();
    }

    public bool StopTestSession(TestSessionInfo testSessionInfo, TestPlatformOptions options, ITestSessionEventsHandler eventsHandler)
    {
        throw new NotImplementedException();
    }

    public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
    {
        throw new NotImplementedException();
    }

    public void DiscoverTests(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler)
    {
        throw new NotImplementedException();
    }

    public void DiscoverTests(IEnumerable<string> sources, string discoverySettings, TestPlatformOptions options, ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        throw new NotImplementedException();
    }

    public void DiscoverTests(IEnumerable<string> sources, string discoverySettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        _testPlatformEventSource.TranslationLayerDiscoveryStart();

        var designModeClient = (DesignModeClient)DesignModeClient.Instance;
        var testRequestManager = designModeClient._testRequestManager;


        // Comes from DesignModeClient private methods, but without all the sending stuff.
        try
        {
            testRequestManager.ResetOptions();
            var discoveryRequestPayload = new DiscoveryRequestPayload()
            {
                Sources = sources,
                RunSettings = discoverySettings,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            };

            testRequestManager.DiscoverTests(discoveryRequestPayload, new DiscoveryHandlerToEventsRegistrarAdapter(discoveryEventsHandler), new ProtocolConfig { Version = _highestSupportedVersion });
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
            discoveryEventsHandler.HandleDiscoveryComplete(errorDiscoveryComplete, lastChunk: null);
        }
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Process is not started.");
            StartSession();
            _sessionStarted = WaitForConnection();

            //if (_sessionStarted)
            //{
            //    EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Send a request to initialize extensions.");
            //    _requestSender.InitializeExtensions(_pathToAdditionalExtensions);
            //}
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
            throw new Exception("errrrrr");
        }

        _testPlatformEventSource.TranslationLayerInitializeStop();
        return true;
    }

    public void RunTests(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public void RunTests(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public void RunTests(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public void RunTests(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public void RunTests(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public void RunTests(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        var testCaseList = testCases.ToList();
        _testPlatformEventSource.TranslationLayerExecutionStart(
            1,
            0,
            testCaseList.Count,
            runSettings ?? string.Empty);

        var designModeClient = (DesignModeClient)DesignModeClient.Instance;
        var testRequestManager = designModeClient._testRequestManager;
        try
        {
            testRequestManager.ResetOptions();

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
                DebuggingEnabled = customLauncher.IsDebug,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            };

            testRequestManager.RunTests(testRunPayload, customLauncher, new RunHandlerToEventsRegistrarAdapter(testRunEventsHandler), new ProtocolConfig { Version = _highestSupportedVersion });
        }
        catch (Exception ex)
        {
            EqtTrace.Error("DesignModeClient: Exception in StartTestRun: " + ex);
            var testRunCompleteArgs = new TestRunCompleteEventArgs(null, false, true, ex, null, null, TimeSpan.MinValue);

            testRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, ex.ToString());
            testRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, null, null, null);
        }
    }


    #region Async, not implemented
    public Task DiscoverTestsAsync(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task DiscoverTestsAsync(IEnumerable<string> sources, string discoverySettings, TestPlatformOptions options, ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task DiscoverTestsAsync(IEnumerable<string> sources, string discoverySettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        throw new NotImplementedException();
    }
    public Task InitializeExtensionsAsync(IEnumerable<string> pathToAdditionalExtensions)
    {
        throw new NotImplementedException();
    }

    public Task ProcessTestRunAttachmentsAsync(IEnumerable<AttachmentSet> attachments, string processingSettings, bool isLastBatch, bool collectMetrics, ITestRunAttachmentsProcessingEventsHandler eventsHandler, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task ProcessTestRunAttachmentsAsync(IEnumerable<AttachmentSet> attachments, IEnumerable<InvokedDataCollector> invokedDataCollectors, string processingSettings, bool isLastBatch, bool collectMetrics, ITestRunAttachmentsProcessingEventsHandler eventsHandler, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestRunEventsHandler testRunEventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsWithCustomTestHostAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsWithCustomTestHostAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsWithCustomTestHostAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsWithCustomTestHostAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsWithCustomTestHostAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public Task RunTestsWithCustomTestHostAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, TestSessionInfo testSessionInfo, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
    {
        throw new NotImplementedException();
    }

    public Task StartSessionAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ITestSession> StartTestSessionAsync(IList<string> sources, string runSettings, ITestSessionEventsHandler eventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task<ITestSession> StartTestSessionAsync(IList<string> sources, string runSettings, TestPlatformOptions options, ITestSessionEventsHandler eventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task<ITestSession> StartTestSessionAsync(IList<string> sources, string runSettings, TestPlatformOptions options, ITestSessionEventsHandler eventsHandler, ITestHostLauncher testHostLauncher)
    {
        throw new NotImplementedException();
    }

    public Task<bool> StopTestSessionAsync(TestSessionInfo testSessionInfo, ITestSessionEventsHandler eventsHandler)
    {
        throw new NotImplementedException();
    }

    public Task<bool> StopTestSessionAsync(TestSessionInfo testSessionInfo, TestPlatformOptions options, ITestSessionEventsHandler eventsHandler)
    {
        throw new NotImplementedException();
    }

    #endregion
}
