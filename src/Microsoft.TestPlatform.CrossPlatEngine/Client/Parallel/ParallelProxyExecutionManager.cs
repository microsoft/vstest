// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

/// <summary>
/// ParallelProxyExecutionManager that manages parallel execution
/// </summary>
internal class ParallelProxyExecutionManager : ParallelOperationManager<IProxyExecutionManager, ITestRunEventsHandler>, IParallelProxyExecutionManager
{
    private readonly IDataSerializer _dataSerializer;

    #region TestRunSpecificData

    // This variable id to differentiate between implicit (abort requested by testPlatform) and explicit (test host aborted) abort.
    private bool _abortRequested;

    private int _runCompletedClients;
    private int _runStartedClients;
    private int _availableTestSources = -1;

    private TestRunCriteria _actualTestRunCriteria;

    private IEnumerator<string> _sourceEnumerator;

    private IEnumerator _testCaseListEnumerator;

    private bool _hasSpecificTestsRun;

    private ITestRunEventsHandler _currentRunEventsHandler;

    private ParallelRunDataAggregator _currentRunDataAggregator;

    private readonly IRequestData _requestData;
    private bool _skipDefaultAdapters;

    /// <inheritdoc/>
    public bool IsInitialized { get; private set; }

    #endregion

    #region Concurrency Keeper Objects

    /// <summary>
    /// LockObject to update execution status in parallel
    /// </summary>
    private readonly object _executionStatusLockObject = new();

    #endregion

    public ParallelProxyExecutionManager(IRequestData requestData, Func<IProxyExecutionManager> actualProxyManagerCreator, int parallelLevel)
        : this(requestData, actualProxyManagerCreator, JsonDataSerializer.Instance, parallelLevel, true)
    {
    }

    public ParallelProxyExecutionManager(IRequestData requestData, Func<IProxyExecutionManager> actualProxyManagerCreator, int parallelLevel, bool sharedHosts)
        : this(requestData, actualProxyManagerCreator, JsonDataSerializer.Instance, parallelLevel, sharedHosts)
    {
    }

    internal ParallelProxyExecutionManager(IRequestData requestData, Func<IProxyExecutionManager> actualProxyManagerCreator, IDataSerializer dataSerializer, int parallelLevel, bool sharedHosts)
        : base(actualProxyManagerCreator, parallelLevel, sharedHosts)
    {
        _requestData = requestData;
        _dataSerializer = dataSerializer;
    }

    #region IProxyExecutionManager

    public void Initialize(bool skipDefaultAdapters)
    {
        _skipDefaultAdapters = skipDefaultAdapters;
        DoActionOnAllManagers((proxyManager) => proxyManager.Initialize(skipDefaultAdapters), doActionsInParallel: true);
        IsInitialized = true;
    }

    public int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
    {
        _hasSpecificTestsRun = testRunCriteria.HasSpecificTests;
        _actualTestRunCriteria = testRunCriteria;

        if (_hasSpecificTestsRun)
        {
            var testCasesBySource = new Dictionary<string, List<TestCase>>();
            foreach (var test in testRunCriteria.Tests)
            {
                if (!testCasesBySource.ContainsKey(test.Source))
                {
                    testCasesBySource.Add(test.Source, new List<TestCase>());
                }

                testCasesBySource[test.Source].Add(test);
            }

            // Do not use "Dictionary.ValueCollection.Enumerator" - it becomes nondeterministic once we go out of scope of this method
            // Use "ToArray" to copy ValueColleciton to a simple array and use it's enumerator
            // Set the enumerator for parallel yielding of testCases
            // Whenever a concurrent executor becomes free, it picks up the next set of testCases using this enumerator
            var testCaseLists = testCasesBySource.Values.ToArray();
            _testCaseListEnumerator = testCaseLists.GetEnumerator();
            _availableTestSources = testCaseLists.Length;
        }
        else
        {
            // Set the enumerator for parallel yielding of sources
            // Whenever a concurrent executor becomes free, it picks up the next source using this enumerator
            _sourceEnumerator = testRunCriteria.Sources.GetEnumerator();
            _availableTestSources = testRunCriteria.Sources.Count();
        }

        EqtTrace.Verbose("ParallelProxyExecutionManager: Start execution. Total sources: " + _availableTestSources);

        return StartTestRunPrivate(eventHandler);
    }

    public void Abort(ITestRunEventsHandler runEventsHandler)
    {
        // Test platform initiated abort.
        _abortRequested = true;
        DoActionOnAllManagers((proxyManager) => proxyManager.Abort(runEventsHandler), doActionsInParallel: true);
    }

    public void Cancel(ITestRunEventsHandler runEventsHandler)
    {
        DoActionOnAllManagers((proxyManager) => proxyManager.Cancel(runEventsHandler), doActionsInParallel: true);
    }

    public void Close()
    {
        DoActionOnAllManagers(proxyManager => proxyManager.Close(), doActionsInParallel: true);
    }

    #endregion

    #region IParallelProxyExecutionManager methods

    /// <summary>
    /// Handles Partial Run Complete event coming from a specific concurrent proxy execution manager
    /// Each concurrent proxy execution manager will signal the parallel execution manager when its complete
    /// </summary>
    /// <param name="proxyExecutionManager">Concurrent Execution manager that completed the run</param>
    /// <param name="testRunCompleteArgs">RunCompleteArgs for the concurrent run</param>
    /// <param name="lastChunkArgs">LastChunk testresults for the concurrent run</param>
    /// <param name="runContextAttachments">RunAttachments for the concurrent run</param>
    /// <param name="executorUris">ExecutorURIs of the adapters involved in executing the tests</param>
    /// <returns>True if parallel run is complete</returns>
    public bool HandlePartialRunComplete(
        IProxyExecutionManager proxyExecutionManager,
        TestRunCompleteEventArgs testRunCompleteArgs,
        TestRunChangedEventArgs lastChunkArgs,
        ICollection<AttachmentSet> runContextAttachments,
        ICollection<string> executorUris)
    {
        var allRunsCompleted = false;
        lock (_executionStatusLockObject)
        {
            // Each concurrent Executor calls this method 
            // So, we need to keep track of total run complete calls
            _runCompletedClients++;

            allRunsCompleted = testRunCompleteArgs.IsCanceled || _abortRequested
                ? _runCompletedClients == _runStartedClients
                : _runCompletedClients == _availableTestSources;

            EqtTrace.Verbose("ParallelProxyExecutionManager: HandlePartialRunComplete: Total completed clients = {0}, Run complete = {1}, Run canceled: {2}.", _runCompletedClients, allRunsCompleted, testRunCompleteArgs.IsCanceled);
        }

        // verify that all executors are done with the execution and there are no more sources/testcases to execute
        if (allRunsCompleted)
        {
            // Reset enumerators
            _sourceEnumerator = null;
            _testCaseListEnumerator = null;

            _currentRunDataAggregator = null;
            _currentRunEventsHandler = null;

            // Dispose concurrent executors
            // Do not do the cleanup task in the current thread as we will unnecessarily add to execution time
            UpdateParallelLevel(0);

            return true;
        }


        EqtTrace.Verbose("ParallelProxyExecutionManager: HandlePartialRunComplete: Replace execution manager. Shared: {0}, Aborted: {1}.", SharedHosts, testRunCompleteArgs.IsAborted);

        RemoveManager(proxyExecutionManager);
        proxyExecutionManager = CreateNewConcurrentManager();
        var parallelEventsHandler = GetEventsHandler(proxyExecutionManager);
        AddManager(proxyExecutionManager, parallelEventsHandler);

        // If cancel is triggered for any one run or abort is requested by test platform, there is no reason to fetch next source
        // and queue another test run
        if (!testRunCompleteArgs.IsCanceled && !_abortRequested)
        {
            StartTestRunOnConcurrentManager(proxyExecutionManager);
        }

        return false;
    }

    #endregion

    private int StartTestRunPrivate(ITestRunEventsHandler runEventsHandler)
    {
        _currentRunEventsHandler = runEventsHandler;

        // Reset the run complete data
        _runCompletedClients = 0;

        // One data aggregator per parallel run
        _currentRunDataAggregator = new ParallelRunDataAggregator(_actualTestRunCriteria.TestRunSettings);

        foreach (var concurrentManager in GetConcurrentManagerInstances())
        {
            var parallelEventsHandler = GetEventsHandler(concurrentManager);
            UpdateHandlerForManager(concurrentManager, parallelEventsHandler);
            StartTestRunOnConcurrentManager(concurrentManager);
        }

        return 1;
    }

    private ParallelRunEventsHandler GetEventsHandler(IProxyExecutionManager concurrentManager)
    {
        if (concurrentManager is ProxyExecutionManagerWithDataCollection)
        {
            var concurrentManagerWithDataCollection = concurrentManager as ProxyExecutionManagerWithDataCollection;
            var attachmentsProcessingManager = new TestRunAttachmentsProcessingManager(TestPlatformEventSource.Instance, new DataCollectorAttachmentsProcessorsFactory());

            return new ParallelDataCollectionEventsHandler(
                _requestData,
                concurrentManagerWithDataCollection,
                _currentRunEventsHandler,
                this,
                _currentRunDataAggregator,
                attachmentsProcessingManager,
                concurrentManagerWithDataCollection.CancellationToken);
        }

        return new ParallelRunEventsHandler(
            _requestData,
            concurrentManager,
            _currentRunEventsHandler,
            this,
            _currentRunDataAggregator);
    }

    /// <summary>
    /// Triggers the execution for the next data object on the concurrent executor
    /// Each concurrent executor calls this method, once its completed working on previous data
    /// </summary>
    /// <param name="proxyExecutionManager">Proxy execution manager instance.</param>
    /// <returns>True, if execution triggered</returns>
    private void StartTestRunOnConcurrentManager(IProxyExecutionManager proxyExecutionManager)
    {
        TestRunCriteria testRunCriteria = null;
        if (!_hasSpecificTestsRun)
        {
            if (TryFetchNextSource(_sourceEnumerator, out string nextSource))
            {
                EqtTrace.Info("ProxyParallelExecutionManager: Triggering test run for next source: {0}", nextSource);
                testRunCriteria = new TestRunCriteria(new[] { nextSource }, _actualTestRunCriteria);
            }
        }
        else
        {
            if (TryFetchNextSource(_testCaseListEnumerator, out List<TestCase> nextSetOfTests))
            {
                EqtTrace.Info("ProxyParallelExecutionManager: Triggering test run for next source: {0}", nextSetOfTests?.FirstOrDefault()?.Source);
                testRunCriteria = new TestRunCriteria(nextSetOfTests, _actualTestRunCriteria);
            }
        }

        if (testRunCriteria != null)
        {
            if (!proxyExecutionManager.IsInitialized)
            {
                proxyExecutionManager.Initialize(_skipDefaultAdapters);
            }

            Task.Run(() =>
                {
                    Interlocked.Increment(ref _runStartedClients);
                    EqtTrace.Verbose("ParallelProxyExecutionManager: Execution started. Started clients: " + _runStartedClients);

                    proxyExecutionManager.StartTestRun(testRunCriteria, GetHandlerForGivenManager(proxyExecutionManager));
                })
                .ContinueWith(t =>
                    {
                        // Just in case, the actual execution couldn't start for an instance. Ensure that
                        // we call execution complete since we have already fetched a source. Otherwise
                        // execution will not terminate
                        EqtTrace.Error("ParallelProxyExecutionManager: Failed to trigger execution. Exception: " + t.Exception);

                        var handler = GetHandlerForGivenManager(proxyExecutionManager);
                        var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = t.Exception.ToString() };
                        handler.HandleRawMessage(_dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload));
                        handler.HandleLogMessage(TestMessageLevel.Error, t.Exception.ToString());

                        // Send a run complete to caller. Similar logic is also used in ProxyExecutionManager.StartTestRun
                        // Differences:
                        // Aborted is sent to allow the current execution manager replaced with another instance
                        // Ensure that the test run aggregator in parallel run events handler doesn't add these statistics
                        // (since the test run didn't even start)
                        var completeArgs = new TestRunCompleteEventArgs(null, false, true, null, new Collection<AttachmentSet>(), new Collection<InvokedDataCollector>(), TimeSpan.Zero);
                        handler.HandleTestRunComplete(completeArgs, null, null, null);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        EqtTrace.Verbose("ProxyParallelExecutionManager: No sources available for execution.");
    }
}
