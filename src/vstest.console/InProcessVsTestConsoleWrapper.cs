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
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
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

    public InProcessVsTestConsoleWrapper(
        string vstestConsolePath,
        string dotnetExePath,
        ConsoleParameters _consoleParameters)
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

            var args = new VsTestConsoleProcessManager(vstestConsolePath, dotnetExePath).BuildArguments(_consoleParameters);
            // Skip vstest.console path, we are already running in process, so it would just end up being
            // understood as test dll to run.
            args = args.Skip(1).ToArray();
            var executor = new Executor(ConsoleOutput.Instance);

            Task.Run<int>(() => executor.Execute(args));
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

        EnsureInitialized();
        _requestSender.DiscoverTests(
            sources,
            discoverySettings,
            options,
            testSessionInfo,
            discoveryEventsHandler);
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

        EnsureInitialized();
        _requestSender.StartTestRunWithCustomHost(
            testCaseList,
            runSettings,
            options,
            testSessionInfo,
            testRunEventsHandler,
            customTestHostLauncher);
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
