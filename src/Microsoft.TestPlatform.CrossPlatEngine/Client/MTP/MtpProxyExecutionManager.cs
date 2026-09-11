// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.Testing.Platform.ServerMode.Client;
using Microsoft.TestPlatform.Hashing;

using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.Utilities;

using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// An <see cref="IProxyExecutionManager"/> that runs tests by driving a Microsoft.Testing.Platform
/// (MTP) application over the MTP JSON-RPC protocol instead of the vstest testhost protocol.
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
    /// profiler settings supplied by the data collector.
    /// </summary>
    public IDictionary<string, string?>? EnvironmentVariables { get; set; }

    /// <summary>
    /// The test id algorithm declared for this run in runsettings, or <see langword="null"/> when the
    /// run does not declare one and the runner's own environment should decide.
    /// </summary>
    private TestCaseIdAlgorithm? _testCaseIdAlgorithm;

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
                List<TestCase>? testsToRun = tests;

                // A /TestCaseFilter run arrives as a source with no specific tests, so MTP has no notion
                // of the vstest filter expression. Discover the source, evaluate the expression against
                // the discovered tests (honoring traits and boolean operators exactly like the classic
                // path) and run only the matching test-node uids. Without this the filter is silently
                // ignored and the whole suite runs.
                if (tests is null && !testRunCriteria.TestCaseFilter.IsNullOrEmpty())
                {
                    testsToRun = DiscoverAndFilter(source, testRunCriteria.TestCaseFilter!, testRunCriteria.FilterOptions, eventHandler);

                    // The filter matched nothing for this source. Skip the source entirely: RunSource
                    // cannot express "run zero tests" - it only sends the MTP tests filter when the list
                    // has entries and otherwise omits it, which MTP treats as "run every test". So calling
                    // RunSource with an empty list would run the whole suite; the continue avoids that.
                    if (testsToRun.Count == 0)
                    {
                        continue;
                    }
                }

                processId = RunSource(source, testsToRun, eventHandler, aggregate, attachments, executorUris);
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
            EnvironmentVariables ??= CreateEnvironmentVariablesDictionary();
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
            foreach (var (level, message) in _dataCollectionEventsHandler.Messages)
            {
                eventHandler.HandleLogMessage(level, message);
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
        MtpServerClientOptions options = MtpClientOptionsFactory.CreateOptions(EnvironmentVariables);
        using IMtpServerClient client = MtpServerClientFactory.Launch(source, options);
        client.LogReceived += (_, e) => eventHandler.HandleLogMessage(MtpClientOptionsFactory.MapServerLogLevel(e.Level), e.Message);
        client.TestNodesUpdated += (_, e) =>
        {
            var results = new List<TestResult>();
            foreach (MtpTestNodeUpdate change in e.Changes)
            {
                if (!MtpTestNodeConverter.IsActionNode(change))
                {
                    continue;
                }

                string? state = change.ExecutionState;

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("MtpProxyExecutionManager: node update uid={0} state={1}", change.Uid, state ?? "(none)");
                }

                // A test entering the in-progress state is our "test started" signal. Forwarding it
                // lets per-test-case collectors (e.g. Blame) know which test is in flight, which is
                // what makes crash attribution work when the test never reaches a terminal state.
                if (_testCaseEventForwarder is { } forwarder && MtpTestNodeConverter.IsInProgressState(state))
                {
                    forwarder.NotifyTestCaseStart(MtpTestNodeConverter.ToTestCase(change, source, _testCaseIdAlgorithm));
                    continue;
                }

                if (!MtpTestNodeConverter.IsTerminalState(state))
                {
                    continue;
                }

                TestResult result = MtpTestNodeConverter.ToTestResult(change, source, _testCaseIdAlgorithm);
                _testCaseEventForwarder?.NotifyTestCaseEnd(result);
                results.Add(result);
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

        // Let the data collector (e.g. code coverage) know the process it should track. The profiler
        // env vars were already injected via the launch options above. Capture the id here rather than
        // reading it again after the exit handshake, when the process may already be gone.
        int processId = client.ProcessId;
        _dataCollectionManager?.TestHostLaunched(processId);

        try
        {
            client.InitializeAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();

            // Awaiting the run request is sufficient: server-to-client messages arrive on a single ordered
            // stream that the client reads sequentially and dispatches synchronously, so every node update
            // has already been delivered by the time the request completes.
            MtpRunResult runResult = (tests is { Count: > 0 }
                ? client.RunTestsAsync(BuildUids(tests), _cancellationTokenSource.Token)
                : client.RunTestsAsync(_cancellationTokenSource.Token)).GetAwaiter().GetResult();

            CollectAttachments(runResult, attachments);
        }
        finally
        {
            MtpServerClientFactory.TryExit(client);
        }

        return processId;
    }

    /// <summary>
    /// Discovers the tests in <paramref name="source"/> over MTP and returns only those matching the
    /// vstest <paramref name="filter"/> expression, so a filtered run executes exactly the selected
    /// tests instead of silently running the whole suite.
    /// </summary>
    /// <exception cref="ObjectModel.Adapter.TestPlatformFormatException">The filter expression could not be parsed.</exception>
    private List<TestCase> DiscoverAndFilter(string source, string filter, FilterOptions? filterOptions, IInternalTestRunEventsHandler eventHandler)
    {
        var filterWrapper = new FilterExpressionWrapper(filter, filterOptions);
        if (!filterWrapper.ParseError.IsNullOrEmpty())
        {
            throw new ObjectModel.Adapter.TestPlatformFormatException(filterWrapper.ParseError, filter);
        }

        var filterExpression = new TestCaseFilterExpression(filterWrapper);

        List<TestCase> discovered = DiscoverSourceTests(source, eventHandler);

        var matched = new List<TestCase>();
        foreach (TestCase testCase in discovered)
        {
            if (filterExpression.MatchTestCase(testCase, BuildPropertyProvider(testCase)))
            {
                matched.Add(testCase);
            }
        }

        return matched;
    }

    /// <summary>
    /// Runs a single MTP discovery pass against <paramref name="source"/> and returns the discovered
    /// tests. Used to resolve a <c>/TestCaseFilter</c> against the tests the MTP application reports,
    /// since MTP itself has no notion of the vstest filter expression. Discovery is started with no
    /// environment variables so execution-only data-collector profiler variables are never injected.
    /// </summary>
    private List<TestCase> DiscoverSourceTests(string source, IInternalTestRunEventsHandler eventHandler)
    {
        var discovered = new List<TestCase>();

        MtpServerClientOptions options = MtpClientOptionsFactory.CreateOptions();
        using IMtpServerClient client = MtpServerClientFactory.Launch(source, options);
        client.LogReceived += (_, e) => eventHandler.HandleLogMessage(MtpClientOptionsFactory.MapServerLogLevel(e.Level), e.Message);
        client.TestNodesUpdated += (_, e) =>
        {
            foreach (MtpTestNodeUpdate change in e.Changes)
            {
                if (MtpTestNodeConverter.IsActionNode(change))
                {
                    lock (discovered)
                    {
                        discovered.Add(MtpTestNodeConverter.ToTestCase(change, source, _testCaseIdAlgorithm));
                    }
                }
            }
        };

        try
        {
            client.InitializeAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();

            // Awaiting the discover request is sufficient: server-to-client messages arrive on a single
            // ordered stream that the client reads sequentially and dispatches synchronously, so every
            // node notification has already been delivered by the time the request completes.
            client.DiscoverTestsAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();
        }
        finally
        {
            MtpServerClientFactory.TryExit(client);
        }

        lock (discovered)
        {
            return discovered.ToList();
        }
    }

    /// <summary>
    /// Builds the property-value lookup a <see cref="TestCaseFilterExpression"/> uses to evaluate a
    /// filter against a single <see cref="TestCase"/>. Every property carried on the test case (e.g.
    /// FullyQualifiedName, DisplayName, Source, CodeFilePath, ...) is exposed by its label, plus the
    /// <c>Name</c> alias for DisplayName and every trait (so filters such as
    /// <c>TestCategory=Fast</c>, <c>Priority=1</c> or <c>Source=...</c> behave like they do on the
    /// classic path).
    /// </summary>
    private static Func<string, object?> BuildPropertyProvider(TestCase testCase)
    {
        var properties = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        void Add(string key, string? value)
        {
            if (string.IsNullOrEmpty(key) || value is null)
            {
                return;
            }

            if (!properties.TryGetValue(key, out List<string>? values))
            {
                values = new List<string>();
                properties[key] = values;
            }

            values.Add(value);
        }

        // Expose all registered properties on the test case by their filter label, so filters can match
        // against any property the converter populated (FullyQualifiedName, DisplayName, Source,
        // CodeFilePath, LineNumber, ...) rather than a hard-coded subset that silently evaluates to "no
        // value" for everything else.
        foreach (TestProperty property in testCase.Properties)
        {
            object? value = testCase.GetPropertyValue(property);
            switch (value)
            {
                case null:
                    break;
                case string[] multiValue:
                    foreach (string item in multiValue)
                    {
                        Add(property.Label, item);
                    }

                    break;
                default:
                    Add(property.Label, value.ToString());
                    break;
            }
        }

        // "Name" is the vstest filter alias for the display name; ensure both are always present even
        // if the property store labelled them differently.
        string displayName = testCase.DisplayName ?? testCase.FullyQualifiedName;
        if (!properties.ContainsKey("FullyQualifiedName"))
        {
            Add("FullyQualifiedName", testCase.FullyQualifiedName);
        }

        if (!properties.ContainsKey("DisplayName"))
        {
            Add("DisplayName", displayName);
        }

        Add("Name", displayName);

        // Traits (TestCategory, Priority, custom) are matched by trait name.
        foreach (Trait trait in testCase.Traits)
        {
            Add(trait.Name, trait.Value);
        }

        return name => properties.TryGetValue(name, out List<string>? values)
            ? (values.Count == 1 ? values[0] : values.ToArray())
            : null;
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

        // The test id algorithm selection is read by TestCase in whichever process builds the test
        // case. Here that is the runner, which does not receive these variables, so capture the
        // declared choice and pass it explicitly when converting nodes.
        //
        // Assigned before the early return below, and unconditionally, so that a run declaring
        // nothing clears what a previous run on this instance declared instead of inheriting it.
        // Resolving from a null or empty set yields null, which is exactly "nothing was declared".
        _testCaseIdAlgorithm = MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(runSettingsEnvironmentVariables);

        if (runSettingsEnvironmentVariables is null || runSettingsEnvironmentVariables.Count == 0)
        {
            return;
        }

        EnvironmentVariables ??= CreateEnvironmentVariablesDictionary();
        foreach (KeyValuePair<string, string?> variable in runSettingsEnvironmentVariables)
        {
            EnvironmentVariables[variable.Key] = variable.Value;
        }
    }

    /// <summary>
    /// Creates the dictionary used to collect environment variables for the MTP application launch,
    /// keyed case-insensitively on Windows (matching the classic testhost path) and case-sensitively
    /// elsewhere, so callers that pass case-variant duplicate keys collapse the same way the classic
    /// path did before the values reach the ordinal-keyed
    /// <see cref="MtpServerClientOptions.EnvironmentVariables"/>.
    /// </summary>
    private static Dictionary<string, string?> CreateEnvironmentVariablesDictionary()
        => new(Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    /// <summary>
    /// Projects the tests selected for a filtered run onto the MTP node uids the server matches on.
    /// </summary>
    /// <exception cref="TestPlatformException">
    /// A selected test carries no MTP node uid, so the run cannot be expressed.
    /// </exception>
    private static IReadOnlyCollection<string> BuildUids(List<TestCase> tests)
    {
        var uids = new List<string>(tests.Count);
        foreach (TestCase test in tests)
        {
            string? uid = test.GetPropertyValue<string>(MtpTestNodeConverter.MtpUidProperty, null);

            // The MTP server projects node.Uid alone when it builds a run filter and never reads
            // DisplayName or any other field, so a TestCase without MTP.TestNode.Uid simply cannot be
            // addressed. Substituting FullyQualifiedName here (as this method previously did) produces
            // a filter the server matches nothing against: the run completes "successfully" having
            // executed zero of the tests the user selected, with no error anywhere. Failing here turns
            // that invisible wrong answer into a visible, actionable one. Do not reintroduce a
            // fallback - there is no value that works other than the uid the server itself issued.
            //
            // This aborts the whole source rather than skipping the offending test: the caller reports
            // the failure and marks the run aborted, which is deliberate. Silently running the
            // addressable subset would recreate the same class of bug in a smaller form, reporting a
            // partial run as if it were the run the user asked for.
            if (uid.IsNullOrEmpty())
            {
                throw new TestPlatformException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CrossPlatEngineResources.MtpTestCaseMissingNodeUid,
                        test.DisplayName ?? test.FullyQualifiedName));
            }

            uids.Add(uid);
        }

        return uids;
    }

    private static void CollectAttachments(MtpRunResult runResult, List<AttachmentSet> attachments)
    {
        if (runResult.Artifacts.Count == 0)
        {
            return;
        }

        var set = new AttachmentSet(new Uri(MtpTestNodeConverter.DefaultExecutorUri), "Microsoft.Testing.Platform");
        foreach (MtpAttachment artifact in runResult.Artifacts)
        {
            string? path = artifact.Uri;
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (!TryCreateFileUri(path!, out Uri? fileUri))
            {
                continue;
            }

            string display = artifact.DisplayName ?? Path.GetFileName(path!);
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
