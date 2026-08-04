// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// An <see cref="IProxyExecutionManager"/> that runs tests by driving a Microsoft.Testing.Platform
/// (MTP) application over the MTP <c>dotnettestcli</c> named-pipe protocol instead of the vstest
/// testhost protocol.
/// </summary>
internal sealed class MtpProxyExecutionManager : IProxyExecutionManager, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Optional data collection manager (e.g. code coverage). When present, it is started before the
    /// run to obtain profiler environment variables that are injected into the MTP application, is
    /// notified of the MTP application's process id, and is asked for its attachments (such as the
    /// .coverage file) once the run completes.
    /// </summary>
    private readonly IProxyDataCollectionManager? _dataCollectionManager;

    private readonly DataCollectionRunEventsHandler? _dataCollectionEventsHandler;

    /// <summary>
    /// Forwards per-test-case started/ended notifications (observed from the MTP application) to the
    /// out-of-process datacollector. Created only when a data collector asks for test-case-level
    /// events (e.g. Blame); left null for code coverage or when data collection is off.
    /// </summary>
    private MtpDataCollectionForwarder? _testCaseEventForwarder;

    private bool _isInitialized;

    public MtpProxyExecutionManager()
    {
    }

    public MtpProxyExecutionManager(IProxyDataCollectionManager dataCollectionManager)
    {
        _dataCollectionManager = dataCollectionManager;
        _dataCollectionEventsHandler = new DataCollectionRunEventsHandler();
    }

    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Environment variables to inject into the MTP application process. Used to pass code coverage
    /// profiler settings supplied by the data collector and the runsettings environment variables.
    /// </summary>
    public IDictionary<string, string?>? EnvironmentVariables { get; set; }

    public void Initialize(bool skipDefaultAdapters) => _isInitialized = true;

    public void InitializeTestRun(TestRunCriteria testRunCriteria, IInternalTestRunEventsHandler eventHandler)
        => Initialize(skipDefaultAdapters: true);

    public int StartTestRun(TestRunCriteria testRunCriteria, IInternalTestRunEventsHandler eventHandler)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var aggregate = new RunAggregate();
        var attachments = new List<AttachmentSet>();
        var executorUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invokedDataCollectors = new List<InvokedDataCollector>();
        int processId = 0;
        bool aborted = false;

        // Inject environment variables declared in the runsettings RunConfiguration/EnvironmentVariables
        // into the MTP application launch. On the classic path ProxyOperationManager reads these from the
        // runsettings and passes them to the testhost process; the MTP application is its own host, so we
        // apply them here. Done before BeforeTestRun so datacollector-provided profiler variables merge on
        // top and win on collision (matching the classic ordering).
        ApplyRunSettingsEnvironmentVariables(testRunCriteria.TestRunSettings);

        BeforeTestRun(eventHandler);

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

        AfterTestRun(attachments, invokedDataCollectors);

        // Surface the data collector messages produced during the run and at session end (e.g.
        // per-test-case notifications, warnings/errors, the Blame sequence-file path, disposal). They
        // are buffered on the run events handler while the run is in flight and delivered during the
        // AfterTestRunEnd exchange; the classic path relays them live, so on this path we flush them
        // once the run is done. Without this they are silently dropped.
        SurfaceDataCollectionMessages(eventHandler);

        TestRunStatistics finalStats = aggregate.Snapshot();
        var completeArgs = new TestRunCompleteEventArgs(
            finalStats,
            _cancellationTokenSource.IsCancellationRequested,
            aborted,
            null,
            new Collection<AttachmentSet>(attachments),
            stopwatch.Elapsed);

        foreach (InvokedDataCollector collector in invokedDataCollectors)
        {
            completeArgs.InvokedDataCollectors.Add(collector);
        }

        eventHandler.HandleTestRunComplete(completeArgs, null, attachments, executorUris.ToList());
        return processId;
    }

    /// <summary>
    /// Starts the data collector (if any) before the run and injects the profiler environment
    /// variables it produces into the MTP application launch. Any messages the data collector logged
    /// during startup are forwarded to the run events handler.
    /// </summary>
    private void BeforeTestRun(IInternalTestRunEventsHandler eventHandler)
    {
        if (_dataCollectionManager is null)
        {
            return;
        }

        _dataCollectionManager.Initialize();

        DataCollectionParameters parameters;
        try
        {
            parameters = _dataCollectionManager.BeforeTestRunStart(
                resetDataCollectors: true,
                isRunStartingNow: true,
                runEventsHandler: _dataCollectionEventsHandler!);
        }
        catch (Exception)
        {
            _dataCollectionManager.AfterTestRunEnd(isCanceled: true, runEventsHandler: _dataCollectionEventsHandler!);
            throw;
        }

        if (parameters?.EnvironmentVariables is { } dataCollectionEnvironmentVariables)
        {
            EnvironmentVariables ??= new Dictionary<string, string?>();
            foreach (KeyValuePair<string, string?> variable in dataCollectionEnvironmentVariables)
            {
                EnvironmentVariables[variable.Key] = variable.Value;
            }
        }

        // If a data collector needs per-test-case events (e.g. Blame tracks the currently running
        // test to attribute crashes), it opens a socket and returns its port. In the classic path
        // testhost connects to it; under MTP there is no testhost, so we connect from here and
        // forward the started/ended notifications we observe from the MTP application. A port of 0
        // means no collector needs these events (e.g. code coverage) and we do nothing.
        if (parameters?.DataCollectionEventsPort > 0)
        {
            _testCaseEventForwarder = new MtpDataCollectionForwarder();
            if (!_testCaseEventForwarder.Connect(parameters.DataCollectionEventsPort))
            {
                eventHandler.HandleLogMessage(
                    ObjectModel.Logging.TestMessageLevel.Warning,
                    "Could not connect to the data collector for per-test-case events; collectors that rely on them (such as Blame) may not function for this Microsoft.Testing.Platform run.");
                _testCaseEventForwarder.Dispose();
                _testCaseEventForwarder = null;
            }
        }

        // Surface any messages the data collector produced while starting up.
        SurfaceDataCollectionMessages(eventHandler);
    }

    /// <summary>
    /// Flushes any data collector log and raw messages buffered on the run events handler to the run's
    /// event handler. On the classic path the datacollector's messages are relayed to the console live; on
    /// this path there is no live pump, so we drain the buffers at the points where new messages have
    /// arrived (data collector startup and after the run completes). Both the human-readable log messages
    /// and the raw (e.g. telemetry) messages are surfaced and cleared, mirroring
    /// <see cref="ProxyExecutionManagerWithDataCollection"/> on the classic path.
    /// </summary>
    private void SurfaceDataCollectionMessages(IInternalTestRunEventsHandler eventHandler)
    {
        if (_dataCollectionEventsHandler is null)
        {
            return;
        }

        if (_dataCollectionEventsHandler.Messages.Count > 0)
        {
            foreach (Tuple<ObjectModel.Logging.TestMessageLevel, string?> message in _dataCollectionEventsHandler.Messages)
            {
                eventHandler.HandleLogMessage(message.Item1, message.Item2);
            }

            _dataCollectionEventsHandler.Messages.Clear();
        }

        if (_dataCollectionEventsHandler.RawMessages.Count > 0)
        {
            foreach (string rawMessage in _dataCollectionEventsHandler.RawMessages)
            {
                eventHandler.HandleRawMessage(rawMessage);
            }

            _dataCollectionEventsHandler.RawMessages.Clear();
        }
    }

    /// <summary>
    /// Ends the data collector (if any) after the run and collects its attachments (such as the
    /// .coverage file) and the list of invoked data collectors.
    /// </summary>
    private void AfterTestRun(List<AttachmentSet> attachments, List<InvokedDataCollector> invokedDataCollectors)
    {
        if (_dataCollectionManager is null)
        {
            return;
        }

        // Signal end-of-stream on the test-case event channel so the datacollector's wait on it
        // completes promptly instead of blocking until the connection timeout (~90s).
        _testCaseEventForwarder?.NotifySessionEnd();

        DataCollectionResult result = _dataCollectionManager.AfterTestRunEnd(
            _cancellationTokenSource.IsCancellationRequested,
            _dataCollectionEventsHandler!);

        if (result.Attachments is { Count: > 0 })
        {
            lock (attachments)
            {
                attachments.AddRange(result.Attachments);
            }
        }

        if (result.InvokedDataCollectors is { Count: > 0 })
        {
            invokedDataCollectors.AddRange(result.InvokedDataCollectors);
        }
    }

    public void Cancel(IInternalTestRunEventsHandler eventHandler) => _cancellationTokenSource.Cancel();

    public void Abort(IInternalTestRunEventsHandler eventHandler) => _cancellationTokenSource.Cancel();

    public void Close() => _cancellationTokenSource.Cancel();

    public void Dispose()
    {
        try
        {
            _testCaseEventForwarder?.Dispose();
        }
        catch
        {
            // ignore
        }

        try
        {
            _dataCollectionManager?.Dispose();
        }
        catch
        {
            // ignore
        }

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
        (string fileName, string baseArguments, string workingDirectory) = MtpLaunch.Resolve(source);

        string arguments = baseArguments;
        if (tests is { Count: > 0 })
        {
            arguments = $"{baseArguments} {BuildUidFilter(tests)}".TrimStart();
        }

        var mtpArtifacts = new AttachmentSet(new Uri(MtpConstants.DefaultExecutorUri), "Microsoft.Testing.Platform");
        int processId = 0;

        using var application = new TestApplication(fileName, arguments, workingDirectory, CopyEnvironmentVariables())
        {
            OnTestResult = message =>
            {
                var results = new List<TestResult>();

                foreach (SuccessfulTestResultMessage successful in message.SuccessfulTestMessages)
                {
                    // A test entering the in-progress state is our "test started" signal. Forwarding it
                    // lets per-test-case collectors (e.g. Blame) know which test is in flight, which is
                    // what makes crash attribution work when the test never reaches a terminal state.
                    if (MtpMessageConverter.IsInProgress(successful.State))
                    {
                        _testCaseEventForwarder?.NotifyTestCaseStart(
                            MtpMessageConverter.ToTestCase(successful.Uid ?? Guid.NewGuid().ToString(), successful.DisplayName, source));
                        continue;
                    }

                    TestResult result = MtpMessageConverter.ToTestResult(successful, source);
                    _testCaseEventForwarder?.NotifyTestCaseEnd(result);
                    results.Add(result);
                }

                foreach (FailedTestResultMessage failed in message.FailedTestMessages)
                {
                    TestResult result = MtpMessageConverter.ToTestResult(failed, source);
                    _testCaseEventForwarder?.NotifyTestCaseEnd(result);
                    results.Add(result);
                }

                if (results.Count == 0)
                {
                    return Task.CompletedTask;
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
                return Task.CompletedTask;
            },

            // Files produced by the MTP application itself (e.g. a .coverage file) arrive as file
            // artifacts. Surface them as a whole-run attachment set, mirroring the classic path where
            // run-level attachments flow through the run complete payload.
            OnFileArtifact = message =>
            {
                foreach (FileArtifactMessage artifact in message.FileArtifacts)
                {
                    if (string.IsNullOrEmpty(artifact.FullPath) || !TryCreateFileUri(artifact.FullPath!, out Uri? fileUri))
                    {
                        continue;
                    }

                    string display = string.IsNullOrEmpty(artifact.DisplayName) ? Path.GetFileName(artifact.FullPath!) : artifact.DisplayName!;
                    lock (mtpArtifacts)
                    {
                        mtpArtifacts.Attachments.Add(new UriDataAttachment(fileUri!, display));
                    }
                }

                return Task.CompletedTask;
            },
        };

        // Let the data collector (e.g. code coverage) know the process it should track once the MTP
        // application has started. The profiler env vars were already injected via EnvironmentVariables.
        Task AfterProcessStart(int pid)
        {
            processId = pid;
            _dataCollectionManager?.TestHostLaunched(pid);
            return Task.CompletedTask;
        }

        application.RunAsync(AfterProcessStart, _cancellationTokenSource.Token).GetAwaiter().GetResult();

        if (mtpArtifacts.Attachments.Count > 0)
        {
            lock (attachments)
            {
                attachments.Add(mtpArtifacts);
            }
        }

        return processId;
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

    /// <summary>
    /// Reads the environment variables declared in the runsettings
    /// <c>RunConfiguration/EnvironmentVariables</c> and merges them into <see cref="EnvironmentVariables"/>
    /// so they are applied to the MTP application launch.
    /// </summary>
    private void ApplyRunSettingsEnvironmentVariables(string? runSettings)
    {
        Dictionary<string, string?>? runSettingsEnvironmentVariables = InferRunSettingsHelper.GetEnvironmentVariables(runSettings);
        if (runSettingsEnvironmentVariables is null || runSettingsEnvironmentVariables.Count == 0)
        {
            return;
        }

        EnvironmentVariables ??= new Dictionary<string, string?>(
            Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (KeyValuePair<string, string?> variable in runSettingsEnvironmentVariables)
        {
            EnvironmentVariables[variable.Key] = variable.Value;
        }
    }

    /// <summary>
    /// Snapshots <see cref="EnvironmentVariables"/> into a read-only dictionary for the launcher, or
    /// returns <see langword="null"/> when there is nothing to inject.
    /// </summary>
    private IReadOnlyDictionary<string, string?>? CopyEnvironmentVariables()
        => EnvironmentVariables is { Count: > 0 }
            ? new Dictionary<string, string?>(EnvironmentVariables)
            : null;

    private static string BuildUidFilter(List<TestCase> tests)
    {
        IEnumerable<string> uids = tests
            .Select(test => (string)test.GetPropertyValue(MtpMessageConverter.MtpUidProperty, test.FullyQualifiedName));
        return $"--filter-uid {string.Join(" ", uids)}";
    }

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
