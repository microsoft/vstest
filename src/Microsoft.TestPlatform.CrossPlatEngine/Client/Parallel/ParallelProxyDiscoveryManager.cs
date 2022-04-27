// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.TestPlatform;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

/// <summary>
/// ParallelProxyDiscoveryManager that manages parallel discovery
/// </summary>
internal class ParallelProxyDiscoveryManager : ParallelOperationManager<IProxyDiscoveryManager, ITestDiscoveryEventsHandler2>, IParallelProxyDiscoveryManager
{
    private readonly IDataSerializer _dataSerializer;
    private readonly DiscoveryDataAggregator _dataAggregator;
    private readonly IRequestData _requestData;

    private int _discoveryCompletedClients;
    private int _availableTestSources = -1;

    private DiscoveryCriteria? _actualDiscoveryCriteria;
    private IEnumerator<string>? _sourceEnumerator;
    private ITestDiscoveryEventsHandler2? _currentDiscoveryEventsHandler;

    public bool IsAbortRequested { get; private set; }

    /// <summary>
    /// LockObject to update discovery status in parallel
    /// </summary>
    private readonly object _discoveryStatusLockObject = new();

    public ParallelProxyDiscoveryManager(IRequestData requestData, Func<IProxyDiscoveryManager> actualProxyManagerCreator, DiscoveryDataAggregator dataAggregator, int parallelLevel, bool sharedHosts)
        : this(requestData, actualProxyManagerCreator, dataAggregator, JsonDataSerializer.Instance, parallelLevel, sharedHosts)
    {
    }

    internal ParallelProxyDiscoveryManager(IRequestData requestData, Func<IProxyDiscoveryManager> actualProxyManagerCreator, DiscoveryDataAggregator dataAggregator, IDataSerializer dataSerializer, int parallelLevel, bool sharedHosts)
        : base(actualProxyManagerCreator, parallelLevel, sharedHosts)
    {
        _requestData = requestData;
        _dataSerializer = dataSerializer;
        _dataAggregator = dataAggregator;
    }

    #region IProxyDiscoveryManager

    /// <inheritdoc/>
    public void Initialize(bool skipDefaultAdapters)
    {
        DoActionOnAllManagers((proxyManager) => proxyManager.Initialize(skipDefaultAdapters), doActionsInParallel: true);
    }

    /// <inheritdoc/>
    public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
    {
        _actualDiscoveryCriteria = discoveryCriteria;

        // Set the enumerator for parallel yielding of sources
        // Whenever a concurrent executor becomes free, it picks up the next source using this enumerator
        _sourceEnumerator = discoveryCriteria.Sources.GetEnumerator();
        _availableTestSources = discoveryCriteria.Sources.Count();

        EqtTrace.Verbose($"ParallelProxyDiscoveryManager.DiscoverTests: Start discovery. Total sources: {_availableTestSources}");

        // Mark all sources as NotDiscovered here because if we get an early cancellation it's
        // possible that we didn't yet start all the proxy managers and so we didn't mark all sources
        // as NotDiscovered.
        // For example, let's assume we have 10 sources, a batch size of 10 but only 8 cores, we
        // will then spawn 8 instances of this and if we now cancel, we will have 2 sources not
        // marked as NotDiscovered.
        _dataAggregator.MarkSourcesWithStatus(discoveryCriteria.Sources, DiscoveryStatus.NotDiscovered);

        DiscoverTestsPrivate(eventHandler);
    }

    /// <inheritdoc/>
    public void Abort()
    {
        IsAbortRequested = true;
        DoActionOnAllManagers((proxyManager) => proxyManager.Abort(), doActionsInParallel: true);
    }

    /// <inheritdoc/>
    public void Abort(ITestDiscoveryEventsHandler2 eventHandler)
    {
        IsAbortRequested = true;
        DoActionOnAllManagers((proxyManager) => proxyManager.Abort(eventHandler), doActionsInParallel: true);
    }

    /// <inheritdoc/>
    public void Close()
    {
        DoActionOnAllManagers(proxyManager => proxyManager.Close(), doActionsInParallel: true);
    }

    #endregion

    #region IParallelProxyDiscoveryManager methods

    /// <inheritdoc/>
    public bool HandlePartialDiscoveryComplete(IProxyDiscoveryManager proxyDiscoveryManager, long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
    {
#if DEBUG
        // Ensures that the total count of sources remains the same between each discovery
        // completion of the same initial discovery request.
        var notDiscoveredCount = _dataAggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered).Count;
        var partiallyDiscoveredCount = _dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count;
        var fullyDiscoveredCount = _dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count;
        var expectedCount = _actualDiscoveryCriteria?.Sources.Count() ?? 0;
        Debug.Assert(notDiscoveredCount + partiallyDiscoveredCount + fullyDiscoveredCount == expectedCount,
            $"Total count of sources ({expectedCount}) should match the count of sources with status not discovered ({notDiscoveredCount}), partially discovered ({partiallyDiscoveredCount}) and fully discovered ({fullyDiscoveredCount}).");
#endif

        var allDiscoverersCompleted = false;
        lock (_discoveryStatusLockObject)
        {
            // Each concurrent Executor calls this method
            // So, we need to keep track of total discovery complete calls
            _discoveryCompletedClients++;

            // If there are no more sources/testcases, a parallel executor is truly done with discovery
            allDiscoverersCompleted = _discoveryCompletedClients == _availableTestSources;

            EqtTrace.Verbose("ParallelProxyDiscoveryManager.HandlePartialDiscoveryComplete: Total completed clients = {0}, Discovery complete = {1}, Aborted = {2}, Abort requested: {3}.", _discoveryCompletedClients, allDiscoverersCompleted, isAborted, IsAbortRequested);
        }

        // If discovery is complete or discovery aborting was requested by testPlatfrom(user)
        // we need to stop all ongoing discoveries, because we want to separate aborting request
        // when testhost crashed by itself and when user requested it (e.g. through TW).
        // Schedule the clean up for managers and handlers.
        if (allDiscoverersCompleted || IsAbortRequested)
        {
            // Reset enumerators
            _sourceEnumerator = null;

            _currentDiscoveryEventsHandler = null;

            // Dispose concurrent executors
            UpdateParallelLevel(0);

            if (allDiscoverersCompleted)
            {
                EqtTrace.Verbose("ParallelProxyDiscoveryManager.HandlePartialDiscoveryComplete: All sources were discovered.");
            }
            else
            {
                EqtTrace.Verbose($"ParallelProxyDiscoveryManager.HandlePartialDiscoveryComplete: Abort was requested.");
            }

            return true;
        }

        // Discovery is not complete.
        // Now when both.net framework and.net core projects can run in parallel we should clear
        // manager and create new one for both cases. Otherwise 'proxyDiscoveryManager' instance
        // is already closed by now and it will give exception when trying to do some operation
        // on it.
        EqtTrace.Verbose("ParallelProxyDiscoveryManager.HandlePartialDiscoveryComplete: Replace discovery manager. Shared: {0}, Aborted: {1}.", SharedHosts, isAborted);

        RemoveManager(proxyDiscoveryManager);

        if (_currentDiscoveryEventsHandler is null)
        {
            Debug.Assert(!TryFetchNextSource(_sourceEnumerator, out string nextSource), $"When discovery event handler is null, we should not have any more sources but we got '{nextSource}'.");
            EqtTrace.Verbose("ParallelProxyDiscoveryManager.HandlePartialDiscoveryComplete: Skip adding more manager because discovery event handler is null.");
            return false;
        }

        proxyDiscoveryManager = CreateNewConcurrentManager();
        var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
            _requestData,
            proxyDiscoveryManager,
            _currentDiscoveryEventsHandler,
            this,
            _dataAggregator);
        AddManager(proxyDiscoveryManager, parallelEventsHandler);

        // Second, let's attempt to trigger discovery for the next source.
        DiscoverTestsOnConcurrentManager(proxyDiscoveryManager);

        return false;
    }

    #endregion

    private void DiscoverTestsPrivate(ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        _currentDiscoveryEventsHandler = discoveryEventsHandler;

        // Reset the discovery complete data
        _discoveryCompletedClients = 0;

        foreach (var concurrentManager in GetConcurrentManagerInstances())
        {
            var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
                _requestData,
                concurrentManager,
                discoveryEventsHandler,
                this,
                _dataAggregator);

            UpdateHandlerForManager(concurrentManager, parallelEventsHandler);
            DiscoverTestsOnConcurrentManager(concurrentManager);
        }
    }

    /// <summary>
    /// Triggers the discovery for the next data object on the concurrent discoverer
    /// Each concurrent discoverer calls this method, once its completed working on previous data
    /// </summary>
    /// <param name="ProxyDiscoveryManager">Proxy discovery manager instance.</param>
    private void DiscoverTestsOnConcurrentManager(IProxyDiscoveryManager proxyDiscoveryManager)
    {
        Debug.Assert(_actualDiscoveryCriteria is not null, "Discovery criteria is null, DiscoverTests should have been called before reaching this point.");

        // Peek to see if we have sources to trigger a discovery
        if (TryFetchNextSource(_sourceEnumerator, out string nextSource))
        {
            EqtTrace.Verbose("ProxyParallelDiscoveryManager.DiscoverTestsOnConcurrentManager: Triggering test discovery for next source: {0}", nextSource);

            // Kick off another discovery task for the next source
            var discoveryCriteria = new DiscoveryCriteria(new[] { nextSource }, _actualDiscoveryCriteria.FrequencyOfDiscoveredTestsEvent, _actualDiscoveryCriteria.DiscoveredTestEventTimeout, _actualDiscoveryCriteria.RunSettings);
            discoveryCriteria.TestCaseFilter = _actualDiscoveryCriteria.TestCaseFilter;
            Task.Run(() =>
                {
                    EqtTrace.Verbose("ParallelProxyDiscoveryManager.DiscoverTestsOnConcurrentManager: Discovery started.");

                    proxyDiscoveryManager.DiscoverTests(discoveryCriteria, GetHandlerForGivenManager(proxyDiscoveryManager));
                })
                .ContinueWith(t =>
                    {
                        // Just in case, the actual discovery couldn't start for an instance. Ensure that
                        // we call discovery complete since we have already fetched a source. Otherwise
                        // discovery will not terminate
                        EqtTrace.Error("ParallelProxyDiscoveryManager.DiscoverTestsOnConcurrentManager: Failed to trigger discovery. Exception: " + t.Exception);

                        var handler = GetHandlerForGivenManager(proxyDiscoveryManager);
                        var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = t.Exception.ToString() };
                        handler.HandleRawMessage(_dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload));
                        handler.HandleLogMessage(TestMessageLevel.Error, t.Exception.ToString());

                        // Send discovery complete. Similar logic is also used in ProxyDiscoveryManager.DiscoverTests.
                        // Differences:
                        // Total tests must be zero here since parallel discovery events handler adds the count
                        // Keep `lastChunk` as null since we don't want a message back to the IDE (discovery didn't even begin)
                        // Set `isAborted` as true since we want this instance of discovery manager to be replaced
                        var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(-1, true);
                        handler.HandleDiscoveryComplete(discoveryCompleteEventsArgs, null);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        EqtTrace.Verbose("ProxyParallelDiscoveryManager.DiscoverTestsOnConcurrentManager: No sources available for discovery.");
    }
}
