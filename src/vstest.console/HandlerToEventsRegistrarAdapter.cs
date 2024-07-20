﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine;

internal class DiscoveryHandlerToEventsRegistrarAdapter : ITestDiscoveryEventsRegistrar
{
    private readonly ITestDiscoveryEventsHandler2 _handler;
    private readonly EventHandler<DiscoveredTestsEventArgs> _handleDiscoveredTests;
    private readonly EventHandler<TestRunMessageEventArgs> _handleLogMessage;
    private readonly EventHandler<DiscoveryCompleteEventArgs> _handleDiscoveryComplete;
    private readonly EventHandler<string> _handleRawMessage;

    public DiscoveryHandlerToEventsRegistrarAdapter(ITestDiscoveryEventsHandler2 handler)
    {
        _handler = handler;
        _handleDiscoveredTests += (_, e) => _handler.HandleDiscoveredTests(e.DiscoveredTestCases);
        _handleLogMessage += (_, e) => _handler.HandleLogMessage(e.Level, e.Message);
        _handleDiscoveryComplete += (_, e) => _handler.HandleDiscoveryComplete(e, null);
        _handleRawMessage += (_, e) =>
        {
            // No-op by design.
            //
            // For out-of-process vstest.console, raw messages are passed to the translation layer but
            // they are never read and don't get passed to the actual events handler in TW. If they
            // were (as it happens for in-process vstest.console since there is no more translation
            // layer) a NotImplemented exception would be raised as per the time this of writing this
            // note.
            //
            // Consider changing this logic in the future if TW changes the handling logic for raw
            // messages.
        };
    }

    public void LogWarning(string message)
    {
        _handler.HandleLogMessage(TestMessageLevel.Warning, message);
    }

    public void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
    {
        discoveryRequest.OnDiscoveredTests += _handleDiscoveredTests;
        discoveryRequest.OnDiscoveryMessage += _handleLogMessage;
        discoveryRequest.OnDiscoveryComplete += _handleDiscoveryComplete;
        discoveryRequest.OnRawMessageReceived += _handleRawMessage;
    }

    public void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
    {
        discoveryRequest.OnDiscoveredTests -= _handleDiscoveredTests;
        discoveryRequest.OnDiscoveryMessage -= _handleLogMessage;
        discoveryRequest.OnDiscoveryComplete -= _handleDiscoveryComplete;
        discoveryRequest.OnRawMessageReceived -= _handleRawMessage;
    }
}

internal class RunHandlerToEventsRegistrarAdapter : ITestRunEventsRegistrar
{
    private readonly ITestRunEventsHandler _handler;
    private readonly EventHandler<TestRunMessageEventArgs> _handleLogMessage;
    private readonly EventHandler<string> _handleRawMessage;
    private readonly EventHandler<TestRunChangedEventArgs> _handleTestRunStatsChange;
    private readonly EventHandler<TestRunCompleteEventArgs> _handleTestRunComplete;

    public RunHandlerToEventsRegistrarAdapter(ITestRunEventsHandler handler)
    {
        _handler = handler;
        _handleLogMessage = (_, e) => _handler.HandleLogMessage(e.Level, e.Message);
        _handleRawMessage = (_, e) =>
        {
            // No-op by design.
            //
            // For out-of-process vstest.console, raw messages are passed to the translation layer but
            // they are never read and don't get passed to the actual events handler in TW. If they
            // were (as it happens for in-process vstest.console since there is no more translation
            // layer) a NotImplemented exception would be raised as per the time this of writing this
            // note.
            //
            // Consider changing this logic in the future if TW changes the handling logic for raw
            // messages.
        };
        _handleTestRunComplete = (_, e) => _handler.HandleTestRunComplete(e, null, null, null);
        _handleTestRunStatsChange = (_, e) => _handler.HandleTestRunStatsChange(e);
    }

    public void LogWarning(string message)
    {
        _handler.HandleLogMessage(TestMessageLevel.Warning, message);
    }

    public void RegisterTestRunEvents(ITestRunRequest testRunRequest)
    {
        testRunRequest.TestRunMessage += _handleLogMessage;
        testRunRequest.OnRawMessageReceived += _handleRawMessage;
        testRunRequest.OnRunStatsChange += _handleTestRunStatsChange;
        testRunRequest.OnRunCompletion += _handleTestRunComplete;
    }

    public void UnregisterTestRunEvents(ITestRunRequest testRunRequest)
    {
        testRunRequest.TestRunMessage -= _handleLogMessage;
        testRunRequest.OnRawMessageReceived -= _handleRawMessage;
        testRunRequest.OnRunStatsChange -= _handleTestRunStatsChange;
        testRunRequest.OnRunCompletion -= _handleTestRunComplete;
    }
}
