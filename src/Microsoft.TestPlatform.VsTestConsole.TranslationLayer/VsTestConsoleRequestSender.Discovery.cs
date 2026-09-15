// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
/// Test discovery request/response handling for <see cref="VsTestConsoleRequestSender"/>.
/// </summary>
internal partial class VsTestConsoleRequestSender
{
    /// <inheritdoc/>
    public void DiscoverTests(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestDiscoveryEventsHandler2 eventHandler)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.DiscoverTests: Starting test discovery.");

        SendMessageAndListenAndReportTestCases(
            sources,
            runSettings,
            options,
            testSessionInfo,
            eventHandler);
    }

    /// <inheritdoc/>
    public async Task DiscoverTestsAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestDiscoveryEventsHandler2 eventHandler)
    {
        EqtTrace.Info("VsTestConsoleRequestSender.DiscoverTestsAsync: Starting test discovery.");

        await SendMessageAndListenAndReportTestCasesAsync(
            sources,
            runSettings,
            options,
            testSessionInfo,
            eventHandler).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void CancelDiscovery()
    {
        EqtTrace.Info("VsTestConsoleRequestSender.CancelDiscovery: Canceling test discovery.");

        _communicationManager.SendMessage(MessageType.CancelDiscovery);
    }

    private void SendMessageAndListenAndReportTestCases(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestDiscoveryEventsHandler2 eventHandler)
    {
        try
        {
            _communicationManager.SendMessage(
                MessageType.StartDiscovery,
                new DiscoveryRequestPayload()
                {
                    Sources = sources,
                    RunSettings = runSettings,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                _protocolVersion);
            var isDiscoveryComplete = false;

            // Cycle through the messages that vstest.console sends.
            // Currently each operation is not a separate task since it should not take that
            // much time to complete.
            //
            // This is just a notification.
            while (!isDiscoveryComplete)
            {
                var message = TryReceiveMessage();

                if (string.Equals(MessageType.TestCasesFound, message.MessageType))
                {
                    var testCases = _dataSerializer
                        .DeserializePayload<IEnumerable<TestCase>>(message);

                    eventHandler.HandleDiscoveredTests(testCases);
                }
                else if (string.Equals(MessageType.DiscoveryComplete, message.MessageType))
                {
                    EqtTrace.Info(
                        "VsTestConsoleRequestSender.SendMessageAndListenAndReportTestCases: Discovery complete.");

                    var discoveryCompletePayload = _dataSerializer.DeserializePayload<DiscoveryCompletePayload>(message);
                    TPDebug.Assert(discoveryCompletePayload is not null, "discoveryCompletePayload is null");

                    var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs
                    {
                        TotalCount = discoveryCompletePayload.TotalTests,
                        IsAborted = discoveryCompletePayload.IsAborted,
                        FullyDiscoveredSources = discoveryCompletePayload.FullyDiscoveredSources,
                        PartiallyDiscoveredSources = discoveryCompletePayload.PartiallyDiscoveredSources,
                        NotDiscoveredSources = discoveryCompletePayload.NotDiscoveredSources,
                        SkippedDiscoveredSources = discoveryCompletePayload.SkippedDiscoverySources,
                        DiscoveredExtensions = discoveryCompletePayload.DiscoveredExtensions,
                        Metrics = discoveryCompletePayload.Metrics,
                    };

                    eventHandler.HandleDiscoveryComplete(
                        discoveryCompleteEventArgs,
                        discoveryCompletePayload.LastDiscoveredTests);
                    isDiscoveryComplete = true;
                }
                else if (string.Equals(MessageType.TestMessage, message.MessageType))
                {
                    var testMessagePayload = _dataSerializer.DeserializePayload<TestMessagePayload>(message);
                    TPDebug.Assert(testMessagePayload is not null, "testMessagePayload is null");
                    eventHandler.HandleLogMessage(
                        testMessagePayload.MessageLevel,
                        testMessagePayload.Message);
                }
            }
        }
        catch (Exception exception)
        {
            EqtTrace.Error("Aborting Test Discovery Operation: {0}", exception);
            eventHandler.HandleLogMessage(
                TestMessageLevel.Error,
                TranslationLayerResources.AbortedTestsDiscovery);
            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(-1, true);
            eventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);

            // Earlier we were closing the connection with vstest.console in case of exceptions.
            // Removing that code because vstest.console might be in a healthy state and letting
            // the client know of the error, so that the TL can wait for the next instruction
            // from the client itself.
            // Also, connection termination might not kill the process which could result in
            // files being locked by testhost.
        }

        _testPlatformEventSource.TranslationLayerDiscoveryStop();
    }

    private async Task SendMessageAndListenAndReportTestCasesAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestDiscoveryEventsHandler2 eventHandler)
    {
        try
        {
            _communicationManager.SendMessage(
                MessageType.StartDiscovery,
                new DiscoveryRequestPayload()
                {
                    Sources = sources,
                    RunSettings = runSettings,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                _protocolVersion);
            var isDiscoveryComplete = false;

            // Cycle through the messages that vstest.console sends.
            // Currently each operation is not a separate task since it should not take that
            // much time to complete.
            //
            // This is just a notification.
            while (!isDiscoveryComplete)
            {
                var message = await TryReceiveMessageAsync().ConfigureAwait(false);

                if (string.Equals(MessageType.TestCasesFound, message.MessageType))
                {
                    var testCases = _dataSerializer
                        .DeserializePayload<IEnumerable<TestCase>>(message);

                    eventHandler.HandleDiscoveredTests(testCases);
                }
                else if (string.Equals(MessageType.DiscoveryComplete, message.MessageType))
                {
                    EqtTrace.Info(
                        "VsTestConsoleRequestSender.SendMessageAndListenAndReportTestCasesAsync: Discovery complete.");

                    var discoveryCompletePayload =
                        _dataSerializer.DeserializePayload<DiscoveryCompletePayload>(message);
                    TPDebug.Assert(discoveryCompletePayload is not null, "discoveryCompletePayload is null");
                    var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs
                    {
                        TotalCount = discoveryCompletePayload.TotalTests,
                        IsAborted = discoveryCompletePayload.IsAborted,
                        FullyDiscoveredSources = discoveryCompletePayload.FullyDiscoveredSources,
                        PartiallyDiscoveredSources = discoveryCompletePayload.PartiallyDiscoveredSources,
                        NotDiscoveredSources = discoveryCompletePayload.NotDiscoveredSources,
                        SkippedDiscoveredSources = discoveryCompletePayload.SkippedDiscoverySources,
                        DiscoveredExtensions = discoveryCompletePayload.DiscoveredExtensions,
                    };

                    // Adding Metrics from VsTestConsole
                    discoveryCompleteEventArgs.Metrics = discoveryCompletePayload.Metrics;

                    eventHandler.HandleDiscoveryComplete(
                        discoveryCompleteEventArgs,
                        discoveryCompletePayload.LastDiscoveredTests);
                    isDiscoveryComplete = true;
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
            }
        }
        catch (Exception exception)
        {
            EqtTrace.Error("Aborting Test Discovery Operation: {0}", exception);

            eventHandler.HandleLogMessage(
                TestMessageLevel.Error,
                TranslationLayerResources.AbortedTestsDiscovery);

            eventHandler.HandleDiscoveryComplete(new(-1, true), null);

            // Earlier we were closing the connection with vstest.console in case of exceptions.
            // Removing that code because vstest.console might be in a healthy state and letting
            // the client know of the error, so that the TL can wait for the next instruction
            // from the client itself.
            // Also, connection termination might not kill the process which could result in
            // files being locked by testhost.
        }

        _testPlatformEventSource.TranslationLayerDiscoveryStop();
    }
}
