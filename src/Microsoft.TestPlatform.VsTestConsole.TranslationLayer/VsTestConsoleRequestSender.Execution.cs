// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using TranslationLayerResources = Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Resources.Resources;

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer;

/// <summary>
/// Test execution request/response handling for <see cref="VsTestConsoleRequestSender"/>.
/// </summary>
internal partial class VsTestConsoleRequestSender
{
    /// <inheritdoc/>
    public void StartTestRun(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler runEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.StartTestRun: Starting test run.");

        SendMessageAndListenAndReportTestResults(
            MessageType.TestRunAllSourcesWithDefaultHost,
            new TestRunRequestPayload()
            {
                Sources = sources.ToList(),
                RunSettings = runSettings,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            },
            runEventsHandler,
            telemetryEventsHandler,
            null);
    }

    /// <inheritdoc/>
    public async Task StartTestRunAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler runEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunAsync: Starting test run.");

        await SendMessageAndListenAndReportTestResultsAsync(
            MessageType.TestRunAllSourcesWithDefaultHost,
            new TestRunRequestPayload()
            {
                Sources = sources.ToList(),
                RunSettings = runSettings,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            },
            runEventsHandler,
            telemetryEventsHandler,
            null).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void StartTestRun(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler runEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.StartTestRun: Starting test run.");

        SendMessageAndListenAndReportTestResults(
            MessageType.TestRunAllSourcesWithDefaultHost,
            new TestRunRequestPayload()
            {
                TestCases = testCases.ToList(),
                RunSettings = runSettings,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            },
            runEventsHandler,
            telemetryEventsHandler,
            null);
    }

    /// <inheritdoc/>
    public async Task StartTestRunAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler runEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunAsync: Starting test run.");

        await SendMessageAndListenAndReportTestResultsAsync(
            MessageType.TestRunAllSourcesWithDefaultHost,
            new TestRunRequestPayload()
            {
                TestCases = testCases.ToList(),
                RunSettings = runSettings,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            },
            runEventsHandler,
            telemetryEventsHandler,
            null).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void StartTestRunWithCustomHost(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler runEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler,
        ITestHostLauncher customHostLauncher)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunWithCustomHost: Starting test run.");

        SendMessageAndListenAndReportTestResults(
            MessageType.GetTestRunnerProcessStartInfoForRunAll,
            new TestRunRequestPayload()
            {
                Sources = sources.ToList(),
                RunSettings = runSettings,
                DebuggingEnabled = customHostLauncher.IsDebug,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            },
            runEventsHandler,
            telemetryEventsHandler,
            customHostLauncher);
    }

    /// <inheritdoc/>
    public async Task StartTestRunWithCustomHostAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler runEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler,
        ITestHostLauncher customHostLauncher)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunWithCustomHostAsync: Starting test run.");

        await SendMessageAndListenAndReportTestResultsAsync(
            MessageType.GetTestRunnerProcessStartInfoForRunAll,
            new TestRunRequestPayload()
            {
                Sources = sources.ToList(),
                RunSettings = runSettings,
                DebuggingEnabled = customHostLauncher.IsDebug,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            },
            runEventsHandler,
            telemetryEventsHandler,
            customHostLauncher).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void StartTestRunWithCustomHost(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler runEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler,
        ITestHostLauncher customHostLauncher)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunWithCustomHost: Starting test run.");

        SendMessageAndListenAndReportTestResults(
            MessageType.GetTestRunnerProcessStartInfoForRunSelected,
            new TestRunRequestPayload
            {
                TestCases = testCases.ToList(),
                RunSettings = runSettings,
                DebuggingEnabled = customHostLauncher.IsDebug,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            },
            runEventsHandler,
            telemetryEventsHandler,
            customHostLauncher);
    }

    /// <inheritdoc/>
    public async Task StartTestRunWithCustomHostAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler runEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler,
        ITestHostLauncher customHostLauncher)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunWithCustomHostAsync: Starting test run.");

        await SendMessageAndListenAndReportTestResultsAsync(
            MessageType.GetTestRunnerProcessStartInfoForRunSelected,
            new TestRunRequestPayload()
            {
                TestCases = testCases.ToList(),
                RunSettings = runSettings,
                DebuggingEnabled = customHostLauncher.IsDebug,
                TestPlatformOptions = options,
                TestSessionInfo = testSessionInfo
            },
            runEventsHandler,
            telemetryEventsHandler,
            customHostLauncher).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void CancelTestRun()
    {
        EqtTrace.Info("VsTestConsoleRequestSender.CancelTestRun: Canceling test run.");

        _communicationManager.SendMessage(MessageType.CancelTestRun);
    }

    /// <inheritdoc/>
    public void AbortTestRun()
    {
        EqtTrace.Info("VsTestConsoleRequestSender.AbortTestRun: Aborting test run.");

        _communicationManager.SendMessage(MessageType.AbortTestRun);
    }

    private void SendMessageAndListenAndReportTestResults(
        string messageType,
        object payload,
        ITestRunEventsHandler eventHandler,
        ITelemetryEventsHandler telemetryEventsHandler,
        ITestHostLauncher? customHostLauncher)
    {
        try
        {
            _communicationManager.SendMessage(messageType, payload, _protocolVersion);
            var isTestRunComplete = false;

            // Cycle through the messages that vstest.console sends.
            // Currently each operation is not a separate task since it should not take that
            // much time to complete.
            //
            // This is just a notification.
            while (!isTestRunComplete)
            {
                var message = TryReceiveMessage();

                if (string.Equals(MessageType.TestRunStatsChange, message.MessageType))
                {
                    var testRunChangedArgs = _dataSerializer
                        .DeserializePayload<TestRunChangedEventArgs>(
                            message);
                    eventHandler.HandleTestRunStatsChange(testRunChangedArgs);
                }
                else if (string.Equals(MessageType.ExecutionComplete, message.MessageType))
                {
                    EqtTrace.Info(
                        "VsTestConsoleRequestSender.SendMessageAndListenAndReportTestResults: Execution complete.");

                    var testRunCompletePayload = _dataSerializer
                        .DeserializePayload<TestRunCompletePayload>(message);
                    TPDebug.Assert(testRunCompletePayload is not null, "testRunCompletePayload is null");
                    eventHandler.HandleTestRunComplete(
                        testRunCompletePayload.TestRunCompleteArgs!,
                        testRunCompletePayload.LastRunTests,
                        testRunCompletePayload.RunAttachments,
                        testRunCompletePayload.ExecutorUris);
                    isTestRunComplete = true;
                }
                else if (string.Equals(MessageType.TestMessage, message.MessageType))
                {
                    var testMessagePayload = _dataSerializer
                        .DeserializePayload<TestMessagePayload>(message);
                    TPDebug.Assert(testMessagePayload is not null, "testMessagePayload is null");
                    eventHandler.HandleLogMessage(
                        testMessagePayload.MessageLevel,
                        testMessagePayload.Message);
                }
                else if (string.Equals(MessageType.CustomTestHostLaunch, message.MessageType))
                {
                    HandleCustomHostLaunch(customHostLauncher, message);
                }
                else if (string.Equals(MessageType.EditorAttachDebugger, message.MessageType) || string.Equals(MessageType.EditorAttachDebugger2, message.MessageType))
                {
                    AttachDebuggerToProcess(customHostLauncher, message);
                }
                else if (string.Equals(MessageType.TelemetryEventMessage, message.MessageType))
                {
                    HandleTelemetryEvent(telemetryEventsHandler, message);
                }
            }
        }
        catch (Exception exception)
        {
            EqtTrace.Error("Aborting Test Run Operation: {0}", exception);
            eventHandler.HandleLogMessage(
                TestMessageLevel.Error,
                TranslationLayerResources.AbortedTestsRun + " " + exception.ToString());
            var completeArgs = new TestRunCompleteEventArgs(
                null, false, true, exception, null, null, TimeSpan.Zero);
            eventHandler.HandleTestRunComplete(completeArgs, null, null, null);

            // Earlier we were closing the connection with vstest.console in case of exceptions.
            // Removing that code because vstest.console might be in a healthy state and letting
            // the client know of the error, so that the TL can wait for the next instruction
            // from the client itself.
            // Also, connection termination might not kill the process which could result in
            // files being locked by testhost.
        }

        _testPlatformEventSource.TranslationLayerExecutionStop();
    }

    private async Task SendMessageAndListenAndReportTestResultsAsync(
        string messageType,
        object payload,
        ITestRunEventsHandler eventHandler,
        ITelemetryEventsHandler telemetryEventsHandler,
        ITestHostLauncher? customHostLauncher)
    {
        try
        {
            _communicationManager.SendMessage(messageType, payload, _protocolVersion);
            var isTestRunComplete = false;

            // Cycle through the messages that vstest.console sends.
            // Currently each operation is not a separate task since it should not take that
            // much time to complete.
            //
            // This is just a notification.
            while (!isTestRunComplete)
            {
                var message = await TryReceiveMessageAsync().ConfigureAwait(false);

                if (string.Equals(MessageType.TestRunStatsChange, message.MessageType))
                {
                    var testRunChangedArgs = _dataSerializer
                        .DeserializePayload<TestRunChangedEventArgs>(message);
                    eventHandler.HandleTestRunStatsChange(testRunChangedArgs);
                }
                else if (string.Equals(MessageType.ExecutionComplete, message.MessageType))
                {
                    EqtTrace.Info(
                        "VsTestConsoleRequestSender.SendMessageAndListenAndReportTestResultsAsync: Execution complete.");

                    var testRunCompletePayload = _dataSerializer
                        .DeserializePayload<TestRunCompletePayload>(message);
                    TPDebug.Assert(testRunCompletePayload is not null, "testRunCompletePayload is null");
                    eventHandler.HandleTestRunComplete(
                        testRunCompletePayload.TestRunCompleteArgs!,
                        testRunCompletePayload.LastRunTests,
                        testRunCompletePayload.RunAttachments,
                        testRunCompletePayload.ExecutorUris);
                    isTestRunComplete = true;
                }
                else if (string.Equals(MessageType.TestMessage, message.MessageType))
                {
                    var testMessagePayload = _dataSerializer.DeserializePayload<TestMessagePayload>(message);
                    TPDebug.Assert(testMessagePayload is not null, "testMessagePayload is null");
                    eventHandler.HandleLogMessage(
                        testMessagePayload.MessageLevel,
                        testMessagePayload.Message);
                }
                else if (string.Equals(MessageType.CustomTestHostLaunch, message.MessageType))
                {
                    HandleCustomHostLaunch(customHostLauncher, message);
                }
                else if (string.Equals(MessageType.EditorAttachDebugger, message.MessageType))
                {
                    AttachDebuggerToProcess(customHostLauncher, message);
                }
                else if (string.Equals(MessageType.TelemetryEventMessage, message.MessageType))
                {
                    HandleTelemetryEvent(telemetryEventsHandler, message);
                }
            }
        }
        catch (Exception exception)
        {
            EqtTrace.Error("Aborting Test Run Operation: {0}", exception);
            eventHandler.HandleLogMessage(
                TestMessageLevel.Error,
                TranslationLayerResources.AbortedTestsRun + " " + exception.ToString());
            var completeArgs = new TestRunCompleteEventArgs(
                null, false, true, exception, null, null, TimeSpan.Zero);
            eventHandler.HandleTestRunComplete(completeArgs, null, null, null);

            // Earlier we were closing the connection with vstest.console in case of exceptions.
            // Removing that code because vstest.console might be in a healthy state and letting
            // the client know of the error, so that the TL can wait for the next instruction
            // from the client itself.
            // Also, connection termination might not kill the process which could result in
            // files being locked by testhost.
        }

        _testPlatformEventSource.TranslationLayerExecutionStop();
    }
}
