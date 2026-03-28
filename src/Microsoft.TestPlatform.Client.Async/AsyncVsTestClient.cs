// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TestPlatform.Client.Async.Internal;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Microsoft.TestPlatform.Client.Async;

/// <summary>
/// Async client for communicating with vstest.console processes.
/// Each instance is stateless — all shared state lives in <see cref="AsyncTestSession"/>.
/// Multiple sessions can be active concurrently.
/// </summary>
public sealed class AsyncVsTestClient : IAsyncVsTestClient
{
    /// <summary>
    /// Creates a new instance of the async vstest client.
    /// </summary>
    public AsyncVsTestClient()
    {
    }

    /// <inheritdoc />
    public async Task<IAsyncTestSession> StartSessionAsync(
        string vstestConsolePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vstestConsolePath))
        {
            throw new ArgumentException("Path to vstest.console must not be empty.", nameof(vstestConsolePath));
        }

        var sender = new AsyncRequestSender();
        sender.StartListening();

        ProcessManager? process = null;
        try
        {
            process = ProcessManager.Launch(vstestConsolePath, sender.Port);
            await sender.WaitForConnectionAsync(process, cancellationToken).ConfigureAwait(false);

            // Send empty extension initialization (no custom extensions).
            await sender.SendMessageAsync(
                ProtocolConstants.ExecutionInitialize,
                Array.Empty<string>(),
                cancellationToken).ConfigureAwait(false);

            return new AsyncTestSession(sender, process);
        }
        catch
        {
            process?.Dispose();
            sender.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DiscoveryResult> DiscoverTestsAsync(
        IAsyncTestSession session,
        IEnumerable<string> sources,
        string? runSettings = null,
        CancellationToken cancellationToken = default)
    {
        var s = GetSession(session);
        ThrowIfDisconnected(s);

        // Initialize discovery extensions.
        await s.Sender.SendMessageAsync(
            ProtocolConstants.DiscoveryInitialize,
            Array.Empty<string>(),
            cancellationToken).ConfigureAwait(false);

        // Send discovery request.
        var payload = new DiscoveryRequestPayloadDto
        {
            Sources = sources.ToList(),
            RunSettings = runSettings,
        };

        await s.Sender.SendMessageAsync(
            ProtocolConstants.StartDiscovery,
            payload,
            cancellationToken).ConfigureAwait(false);

        // Collect results.
        var testCases = new List<TestCase>();
        long totalCount = 0;
        bool isAborted = false;

        while (true)
        {
            var message = await s.Sender.ReceiveMessageAsync(s.Process, cancellationToken).ConfigureAwait(false);
            if (message == null) break;

            switch (message.MessageType)
            {
                case ProtocolConstants.TestCasesFound:
                    if (message.Payload.HasValue)
                    {
                        foreach (var element in message.Payload.Value.EnumerateArray())
                        {
                            testCases.Add(TestObjectDeserializer.DeserializeTestCase(element));
                        }
                    }
                    break;

                case ProtocolConstants.DiscoveryComplete:
                    if (message.Payload.HasValue)
                    {
                        var payload2 = message.Payload.Value;
                        if (payload2.TryGetProperty("TotalTests", out var totalElem))
                        {
                            totalCount = totalElem.GetInt64();
                        }
                        if (payload2.TryGetProperty("IsAborted", out var abortedElem))
                        {
                            isAborted = abortedElem.GetBoolean();
                        }
                        // Collect any last chunk of discovered tests.
                        if (payload2.TryGetProperty("LastDiscoveredTests", out var lastTests) &&
                            lastTests.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in lastTests.EnumerateArray())
                            {
                                testCases.Add(TestObjectDeserializer.DeserializeTestCase(element));
                            }
                        }
                    }
                    return new DiscoveryResult(testCases, totalCount, isAborted);

                case ProtocolConstants.TestMessage:
                    // Diagnostic messages from the server — skip for now.
                    break;

                default:
                    // Unknown message type — skip.
                    break;
            }
        }

        return new DiscoveryResult(testCases, totalCount, isAborted: true);
    }

    /// <inheritdoc />
    public Task<TestRunResult> RunTestsAsync(
        IAsyncTestSession session,
        IEnumerable<string> sources,
        string? runSettings = null,
        IProgress<TestRunChangedEventArgs>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new TestRunRequestPayloadDto
        {
            Sources = sources.ToList(),
            RunSettings = runSettings,
            KeepAlive = false,
        };

        return RunTestsCoreAsync(session, payload, progress, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TestRunResult> RunTestsAsync(
        IAsyncTestSession session,
        IEnumerable<TestCase> testCases,
        string? runSettings = null,
        IProgress<TestRunChangedEventArgs>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new TestRunRequestPayloadDto
        {
            TestCases = testCases.Select(TestCaseDto.FromTestCase).ToList(),
            RunSettings = runSettings,
            KeepAlive = false,
        };

        return RunTestsCoreAsync(session, payload, progress, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // The client itself holds no state — sessions are disposed individually.
        return default;
    }

    private static async Task<TestRunResult> RunTestsCoreAsync(
        IAsyncTestSession session,
        TestRunRequestPayloadDto payload,
        IProgress<TestRunChangedEventArgs>? progress,
        CancellationToken cancellationToken)
    {
        var s = GetSession(session);
        ThrowIfDisconnected(s);

        // Initialize execution extensions.
        await s.Sender.SendMessageAsync(
            ProtocolConstants.ExecutionInitialize,
            Array.Empty<string>(),
            cancellationToken).ConfigureAwait(false);

        // Send run request.
        await s.Sender.SendMessageAsync(
            ProtocolConstants.TestRunAllSourcesWithDefaultHost,
            payload,
            cancellationToken).ConfigureAwait(false);

        // Collect results.
        var testResults = new List<TestResult>();
        ITestRunStatistics? statistics = null;
        bool isCanceled = false;
        bool isAborted = false;
        TimeSpan elapsedTime = TimeSpan.Zero;

        while (true)
        {
            var message = await s.Sender.ReceiveMessageAsync(s.Process, cancellationToken).ConfigureAwait(false);
            if (message == null) break;

            switch (message.MessageType)
            {
                case ProtocolConstants.TestRunStatsChange:
                    if (message.Payload.HasValue)
                    {
                        var changedResults = DeserializeTestRunChangedResults(message.Payload.Value);
                        testResults.AddRange(changedResults);

                        if (progress != null)
                        {
                            var eventArgs = new TestRunChangedEventArgs(
                                null, changedResults, Enumerable.Empty<TestCase>());
                            progress.Report(eventArgs);
                        }
                    }
                    break;

                case ProtocolConstants.ExecutionComplete:
                    if (message.Payload.HasValue)
                    {
                        ParseExecutionComplete(
                            message.Payload.Value,
                            testResults,
                            out statistics,
                            out isCanceled,
                            out isAborted,
                            out elapsedTime);
                    }
                    return new TestRunResult(testResults, statistics, isCanceled, isAborted, elapsedTime);

                case ProtocolConstants.TestMessage:
                    break;

                default:
                    break;
            }
        }

        return new TestRunResult(testResults, statistics, isCanceled, isAborted: true, elapsedTime);
    }

    private static List<TestResult> DeserializeTestRunChangedResults(JsonElement payload)
    {
        var results = new List<TestResult>();

        if (payload.TryGetProperty("NewTestResults", out var newResults) &&
            newResults.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in newResults.EnumerateArray())
            {
                results.Add(TestObjectDeserializer.DeserializeTestResult(element));
            }
        }

        return results;
    }

    private static void ParseExecutionComplete(
        JsonElement payload,
        List<TestResult> testResults,
        out ITestRunStatistics? statistics,
        out bool isCanceled,
        out bool isAborted,
        out TimeSpan elapsedTime)
    {
        statistics = null;
        isCanceled = false;
        isAborted = false;
        elapsedTime = TimeSpan.Zero;

        // Collect any last test results.
        if (payload.TryGetProperty("LastRunTests", out var lastRunTests) &&
            lastRunTests.ValueKind == JsonValueKind.Object)
        {
            if (lastRunTests.TryGetProperty("NewTestResults", out var lastNewResults) &&
                lastNewResults.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in lastNewResults.EnumerateArray())
                {
                    testResults.Add(TestObjectDeserializer.DeserializeTestResult(element));
                }
            }
        }

        if (payload.TryGetProperty("TestRunCompleteArgs", out var completeArgs))
        {
            if (completeArgs.TryGetProperty("IsCanceled", out var canceledElem))
            {
                isCanceled = canceledElem.GetBoolean();
            }
            if (completeArgs.TryGetProperty("IsAborted", out var abortedElem))
            {
                isAborted = abortedElem.GetBoolean();
            }
            if (completeArgs.TryGetProperty("ElapsedTimeInRunningTests", out var elapsedElem))
            {
                string? elapsedStr = elapsedElem.GetString();
                if (elapsedStr != null && TimeSpan.TryParse(elapsedStr, out var parsed))
                {
                    elapsedTime = parsed;
                }
            }
            if (completeArgs.TryGetProperty("TestRunStatistics", out var statsElem) &&
                statsElem.ValueKind == JsonValueKind.Object)
            {
                statistics = DeserializeTestRunStatistics(statsElem);
            }
        }
    }

    private static ITestRunStatistics? DeserializeTestRunStatistics(JsonElement statsElem)
    {
        long executedTests = 0;
        var stats = new Dictionary<TestOutcome, long>();

        if (statsElem.TryGetProperty("ExecutedTests", out var execElem))
        {
            executedTests = execElem.GetInt64();
        }

        if (statsElem.TryGetProperty("Stats", out var statsDict) &&
            statsDict.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in statsDict.EnumerateObject())
            {
                if (int.TryParse(prop.Name, out int outcomeInt) &&
                    Enum.IsDefined(typeof(TestOutcome), outcomeInt))
                {
                    stats[(TestOutcome)outcomeInt] = prop.Value.GetInt64();
                }
            }
        }

        return new TestRunStatistics(executedTests, stats);
    }

    private static AsyncTestSession GetSession(IAsyncTestSession session)
    {
        if (session is not AsyncTestSession asyncSession)
        {
            throw new ArgumentException("Session was not created by this client.", nameof(session));
        }
        return asyncSession;
    }

    private static void ThrowIfDisconnected(AsyncTestSession session)
    {
        if (!session.IsConnected)
        {
            throw new InvalidOperationException(
                "The vstest.console session is no longer connected. " +
                (session.Process.HasExited
                    ? $"Process exited with code {session.Process.ExitCode}. {session.Process.ErrorOutput}"
                    : "Connection was closed."));
        }
    }
}
