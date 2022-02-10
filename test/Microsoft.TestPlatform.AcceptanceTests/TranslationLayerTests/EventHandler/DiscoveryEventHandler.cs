﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

/// <inheritdoc />
public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
{
    /// <summary>
    /// Gets the discovered test cases.
    /// </summary>
    public List<TestCase> DiscoveredTestCases { get; }

    public DiscoveryEventHandler()
    {
        DiscoveredTestCases = new List<TestCase>();
    }

    public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
    {
        if (discoveredTestCases != null)
        {
            DiscoveredTestCases.AddRange(discoveredTestCases);
        }
    }

    public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
    {
        if (lastChunk != null)
        {
            DiscoveredTestCases.AddRange(lastChunk);
        }
    }

    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        // No Op
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No op
    }
}

public struct TestMessage
{
    public TestMessageLevel TestMessageLevel;
    public string Message;

    public TestMessage(TestMessageLevel testMessageLevel, string message)
    {
        TestMessageLevel = testMessageLevel;
        Message = message;
    }
}

/// <inheritdoc />
public class DiscoveryEventHandler2 : ITestDiscoveryEventsHandler2
{
    /// <summary>
    /// Gets the discovered test cases.
    /// </summary>
    public List<TestCase> DiscoveredTestCases { get; }

    public IReadOnlyCollection<string> FullyDiscoveredSources { get; private set; }
    public IReadOnlyCollection<string> PartiallyDiscoveredSources { get; private set; }
    public IReadOnlyCollection<string> NotDiscoveredSources { get; private set; }

    public List<TestMessage> TestMessages;

    /// <summary>
    /// Gets the metrics.
    /// </summary>
    public IDictionary<string, object> Metrics { get; private set; }

    public DiscoveryEventHandler2()
    {
        DiscoveredTestCases = new List<TestCase>();
        TestMessages = new List<TestMessage>();
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No Op
    }

    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        TestMessages.Add(new TestMessage(level, message));
    }

    public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
    {
        if (lastChunk != null)
        {
            DiscoveredTestCases.AddRange(lastChunk);
        }

        Metrics = discoveryCompleteEventArgs.Metrics;
        FullyDiscoveredSources = discoveryCompleteEventArgs.FullyDiscoveredSources;
        PartiallyDiscoveredSources = discoveryCompleteEventArgs.PartiallyDiscoveredSources;
        NotDiscoveredSources = discoveryCompleteEventArgs.NotDiscoveredSources;
    }

    public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
    {
        if (discoveredTestCases != null)
        {
            DiscoveredTestCases.AddRange(discoveredTestCases);
        }
    }
}

/// Discovery Event Handler for batch size
public class DiscoveryEventHandlerForBatchSize : ITestDiscoveryEventsHandler2, ITestDiscoveryEventsHandler
{
    /// <summary>
    /// Gets the batch size.
    /// </summary>
    public long BatchSize { get; private set; }

    /// <summary>
    /// Gets the discovered test cases.
    /// </summary>
    public List<TestCase> DiscoveredTestCases { get; }

    public DiscoveryEventHandlerForBatchSize()
    {
        DiscoveredTestCases = new List<TestCase>();
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No Op
    }

    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        // No Op
    }

    public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
    {
        if (lastChunk != null)
        {
            DiscoveredTestCases.AddRange(lastChunk);
        }
    }

    public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
    {
        if (lastChunk != null)
        {
            DiscoveredTestCases.AddRange(lastChunk);
        }
    }

    public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
    {
        if (discoveredTestCases != null && discoveredTestCases.Any())
        {
            DiscoveredTestCases.AddRange(discoveredTestCases);
            BatchSize = discoveredTestCases.Count();
        }
    }
}
