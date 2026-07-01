// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// An <see cref="IProxyExecutionManager"/> that runs tests by driving a Microsoft.Testing.Platform
/// (MTP) application over the MTP JSON-RPC protocol instead of the vstest testhost protocol.
/// </summary>
internal sealed class MtpProxyExecutionManager : IProxyExecutionManager, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Environment variables to inject into the MTP application process. Used to pass code coverage
    /// profiler settings supplied by the data collector.
    /// </summary>
    public IDictionary<string, string?>? EnvironmentVariables { get; set; }

    public void Initialize(bool skipDefaultAdapters) => _isInitialized = true;

    public void InitializeTestRun(TestRunCriteria testRunCriteria, IInternalTestRunEventsHandler eventHandler)
        => Initialize(skipDefaultAdapters: true);

    public int StartTestRun(TestRunCriteria testRunCriteria, IInternalTestRunEventsHandler eventHandler)
    {
        var stopwatch = Stopwatch.StartNew();
        var aggregate = new RunAggregate();
        var attachments = new List<AttachmentSet>();
        var executorUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int processId = 0;
        bool aborted = false;

        foreach (var (source, tests) in BuildWork(testRunCriteria))
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }

            try
            {
                processId = RunSource(source, tests, eventHandler, aggregate, attachments, executorUris);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                EqtTrace.Error("MtpProxyExecutionManager.StartTestRun: run failed for '{0}': {1}", source, ex);
                eventHandler.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, $"Microsoft.Testing.Platform run failed for '{source}': {ex.Message}");
                aborted = true;
            }
        }

        TestRunStatistics finalStats = aggregate.Snapshot();
        var completeArgs = new TestRunCompleteEventArgs(
            finalStats,
            _cancellationTokenSource.IsCancellationRequested,
            aborted,
            null,
            new Collection<AttachmentSet>(attachments),
            stopwatch.Elapsed);

        eventHandler.HandleTestRunComplete(completeArgs, null, attachments, executorUris.ToList());
        return processId;
    }

    public void Cancel(IInternalTestRunEventsHandler eventHandler) => _cancellationTokenSource.Cancel();

    public void Abort(IInternalTestRunEventsHandler eventHandler) => _cancellationTokenSource.Cancel();

    public void Close() => _cancellationTokenSource.Cancel();

    public void Dispose()
    {
        try
        {
            _cancellationTokenSource.Dispose();
        }
        catch
        {
            // ignore
        }
    }

    private int RunSource(
        string source,
        List<TestCase>? tests,
        IInternalTestRunEventsHandler eventHandler,
        RunAggregate aggregate,
        List<AttachmentSet> attachments,
        HashSet<string> executorUris)
    {
        var completed = new ManualResetEventSlim(false);

        using var connection = new MtpServerConnection();
        connection.LogReceived += (level, message) => eventHandler.HandleLogMessage(MtpClientHelpers.MapLevel(level), message);
        connection.TestNodesUpdated += parameters =>
        {
            if (MtpClientHelpers.IsCompletionSentinel(parameters))
            {
                completed.Set();
                return;
            }

            var results = new List<TestResult>();
            foreach (JsonElement node in MtpClientHelpers.EnumerateNodes(parameters))
            {
                if (!MtpTestNodeConverter.IsActionNode(node))
                {
                    continue;
                }

                if (!MtpTestNodeConverter.IsTerminalState(MtpTestNodeConverter.GetExecutionState(node)))
                {
                    continue;
                }

                results.Add(MtpTestNodeConverter.ToTestResult(node, source));
            }

            if (results.Count == 0)
            {
                return;
            }

            TestRunStatistics snapshot;
            lock (aggregate.Lock)
            {
                foreach (TestResult result in results)
                {
                    aggregate.Add(result);
                    if (result.TestCase.ExecutorUri is { } uri)
                    {
                        executorUris.Add(uri.ToString());
                    }
                }

                snapshot = aggregate.Snapshot();
            }

            eventHandler.HandleTestRunStatsChange(new TestRunChangedEventArgs(snapshot, results, null));
        };

        connection.Start(source, EnvironmentVariables, MtpClientHelpers.GetConnectionTimeout());
        connection.InvokeAsync(MtpConstants.InitializeMethod, MtpClientHelpers.InitializeParameters(), _cancellationTokenSource.Token).GetAwaiter().GetResult();

        var runId = Guid.NewGuid();
        var runParameters = new Dictionary<string, object?> { [MtpConstants.RunIdParameter] = runId.ToString() };
        if (tests is { Count: > 0 })
        {
            runParameters[MtpConstants.TestsParameter] = BuildTestsFilter(tests);
        }

        var runTask = connection.InvokeAsync(MtpConstants.RunTestsMethod, runParameters, _cancellationTokenSource.Token);
        JsonElement response = runTask.GetAwaiter().GetResult();
        completed.Wait(TimeSpan.FromSeconds(3));

        CollectAttachments(response, attachments);
        connection.SendNotification(MtpConstants.ExitMethod, null);
        return connection.ProcessId;
    }

    private static IEnumerable<(string Source, List<TestCase>? Tests)> BuildWork(TestRunCriteria criteria)
    {
        if (criteria.HasSpecificTests && criteria.Tests is not null)
        {
            return criteria.Tests
                .GroupBy(test => test.Source)
                .Select(group => (group.Key, (List<TestCase>?)group.ToList()));
        }

        return (criteria.Sources ?? Enumerable.Empty<string>())
            .Select(source => (source, (List<TestCase>?)null));
    }

    private static List<Dictionary<string, object?>> BuildTestsFilter(List<TestCase> tests)
        => tests
            .Select(test => new Dictionary<string, object?>
            {
                [MtpConstants.Uid] = test.GetPropertyValue(MtpTestNodeConverter.MtpUidProperty, test.FullyQualifiedName),
                [MtpConstants.DisplayName] = test.DisplayName,
            })
            .ToList();

    private static void CollectAttachments(JsonElement response, List<AttachmentSet> attachments)
    {
        if (response.ValueKind != JsonValueKind.Object
            || !response.TryGetProperty(MtpConstants.AttachmentsProperty, out JsonElement attachmentArray)
            || attachmentArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var set = new AttachmentSet(new Uri(MtpConstants.DefaultExecutorUri), "Microsoft.Testing.Platform");
        foreach (JsonElement attachment in attachmentArray.EnumerateArray())
        {
            if (attachment.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? path = GetStringProperty(attachment, MtpConstants.AttachmentUriProperty)
                ?? GetStringProperty(attachment, MtpConstants.AttachmentPathProperty);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (!TryCreateFileUri(path!, out Uri? fileUri))
            {
                continue;
            }

            string display = GetStringProperty(attachment, MtpConstants.DisplayName) ?? Path.GetFileName(path!);
            set.Attachments.Add(new UriDataAttachment(fileUri!, display));
        }

        if (set.Attachments.Count > 0)
        {
            lock (attachments)
            {
                attachments.Add(set);
            }
        }
    }

    private static string? GetStringProperty(JsonElement element, string name)
        => element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryCreateFileUri(string path, out Uri? uri)
    {
        try
        {
            uri = Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute) && absolute.IsFile
                ? absolute
                : new Uri(Path.GetFullPath(path));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException or NotSupportedException or PathTooLongException)
        {
            uri = null;
            return false;
        }
    }

    private sealed class RunAggregate
    {
        public object Lock { get; } = new();

        private readonly Dictionary<TestOutcome, long> _byOutcome = new();
        private long _executed;

        public void Add(TestResult result)
        {
            _byOutcome.TryGetValue(result.Outcome, out long count);
            _byOutcome[result.Outcome] = count + 1;
            _executed++;
        }

        public TestRunStatistics Snapshot()
            => new(_executed, new Dictionary<TestOutcome, long>(_byOutcome));
    }
}

#endif
