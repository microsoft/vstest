// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeTestRunEventsRegistrar : ITestRunEventsRegistrar
{
    public List<object> AllEvents { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<EventRecord<TestRunCompleteEventArgs>> RunCompletionEvents { get; } = new();
    public List<EventRecord<TestRunStartEventArgs>> RunStartEvents { get; } = new();
    public List<EventRecord<TestRunChangedEventArgs>> RunStatsChange { get; } = new();
    public List<EventRecord<string>> RawMessageEvents { get; } = new();
    public List<EventRecord<TestRunMessageEventArgs>> TestRunMessageEvents { get; } = new();

    public void LogWarning(string message)
    {
        AllEvents.Add(message);
        Warnings.Add(message);
    }

    public void RegisterTestRunEvents(ITestRunRequest testRunRequest)
    {
        testRunRequest.TestRunMessage += OnTestRunMessage;
        testRunRequest.OnRawMessageReceived += OnRawMessage;
        testRunRequest.OnRunStart += OnRunStart;
        testRunRequest.OnRunStatsChange += OnRunStatsChange;
        testRunRequest.OnRunCompletion += OnRunCompletion;
    }

    public void UnregisterTestRunEvents(ITestRunRequest testRunRequest)
    {
        testRunRequest.TestRunMessage -= OnTestRunMessage;
        testRunRequest.OnRawMessageReceived -= OnRawMessage;
        testRunRequest.OnRunStart -= OnRunStart;
        testRunRequest.OnRunStatsChange -= OnRunStatsChange;
        testRunRequest.OnRunCompletion -= OnRunCompletion;
    }

    private void OnRunCompletion(object? sender, TestRunCompleteEventArgs e)
    {
        var eventRecord = new EventRecord<TestRunCompleteEventArgs>(sender, e);
        AllEvents.Add(eventRecord);
        RunCompletionEvents.Add(eventRecord);
    }

    private void OnRunStart(object? sender, TestRunStartEventArgs e)
    {
        var eventRecord = new EventRecord<TestRunStartEventArgs>(sender, e);
        AllEvents.Add(eventRecord);
        RunStartEvents.Add(eventRecord);
    }

    private void OnRunStatsChange(object? sender, TestRunChangedEventArgs e)
    {
        var eventRecord = new EventRecord<TestRunChangedEventArgs>(sender, e);
        AllEvents.Add(eventRecord);
        RunStatsChange.Add(eventRecord);
    }

    private void OnRawMessage(object? sender, string e)
    {
        var eventRecord = new EventRecord<string>(sender, e);
        AllEvents.Add(eventRecord);
        RawMessageEvents.Add(eventRecord);
    }

    private void OnTestRunMessage(object? sender, TestRunMessageEventArgs e)
    {
        var eventRecord = new EventRecord<TestRunMessageEventArgs>(sender, e);
        AllEvents.Add(eventRecord);
        TestRunMessageEvents.Add(eventRecord);
    }
}
