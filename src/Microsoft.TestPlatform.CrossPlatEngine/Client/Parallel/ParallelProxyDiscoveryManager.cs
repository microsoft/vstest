// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommunicationUtilities;
using CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using ObjectModel.Engine;
using ObjectModel.Logging;

/// <summary>
/// ParallelProxyDiscoveryManager that manages parallel discovery
/// </summary>
internal class ParallelProxyDiscoveryManager : ParallelOperationManager<IProxyDiscoveryManager, ITestDiscoveryEventsHandler2, SourceDetail>, IParallelProxyDiscoveryManager
{
    private readonly IDataSerializer _dataSerializer;
    private readonly Dictionary<string, SourceDetail> _sourceToSourceDetailMap;
    private int _discoveryCompletedClients;
    private int _availableTestSources = -1;

    private DiscoveryCriteria _actualDiscoveryCriteria;

    private IEnumerator<string> _sourceEnumerator;

    private ITestDiscoveryEventsHandler2 _currentDiscoveryEventsHandler;

    private ParallelDiscoveryDataAggregator _currentDiscoveryDataAggregator;
    private bool _skipDefaultAdapters;
    private readonly IRequestData _requestData;

    public bool IsAbortRequested { get; private set; }

    /// <summary>
    /// LockObject to update discovery status in parallel
    /// </summary>
    private readonly object _discoveryStatusLockObject = new();

    public ParallelProxyDiscoveryManager(IRequestData requestData, Func<SourceDetail, IProxyDiscoveryManager> actualProxyManagerCreator, int parallelLevel, Dictionary<string, SourceDetail> sourceToSourceDetailMap)
        : this(requestData, actualProxyManagerCreator, JsonDataSerializer.Instance, parallelLevel, sourceToSourceDetailMap)
    {
    }

    internal ParallelProxyDiscoveryManager(IRequestData requestData, Func<SourceDetail, IProxyDiscoveryManager> actualProxyManagerCreator, IDataSerializer dataSerializer, int parallelLevel, Dictionary<string, SourceDetail> sourceToSourceDetailMap)
        : base(actualProxyManagerCreator, parallelLevel)
    {
        _requestData = requestData;
        _dataSerializer = dataSerializer;
        _sourceToSourceDetailMap = sourceToSourceDetailMap;
    }

    #region IProxyDiscoveryManager

    /// <inheritdoc/>
    public void Initialize(bool skipDefaultAdapters)
    {
        // The parent ctor does not pre-create any managers, save the info for later.
        // DoActionOnAllManagers((proxyManager) => proxyManager.Initialize(skipDefaultAdapters), doActionsInParallel: true);
        _skipDefaultAdapters = skipDefaultAdapters;
    }

    /// <inheritdoc/>
    public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
    {
        _actualDiscoveryCriteria = discoveryCriteria;

        // Set the enumerator for parallel yielding of sources
        // Whenever a concurrent executor becomes free, it picks up the next source using this enumerator
        _sourceEnumerator = discoveryCriteria.Sources.GetEnumerator();
        _availableTestSources = discoveryCriteria.Sources.Count();

        EqtTrace.Verbose("ParallelProxyDiscoveryManager: Start discovery. Total sources: " + _availableTestSources);

        // One data aggregator per parallel discovery
        _currentDiscoveryDataAggregator = new ParallelDiscoveryDataAggregator();

        // Marking all sources as not discovered before starting actual discovery
        _currentDiscoveryDataAggregator.MarkSourcesWithStatus(discoveryCriteria.Sources.ToList(), DiscoveryStatus.NotDiscovered);

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
        var allDiscoverersCompleted = false;
        // REVIEW: Interlocked.Increment the count, and the condition below probably does not need to be in a lock?
        lock (_discoveryStatusLockObject)
        {
            // Each concurrent Executor calls this method
            // So, we need to keep track of total discovery complete calls
            _discoveryCompletedClients++;

            // If there are no more sources/testcases, a parallel executor is truly done with discovery
            allDiscoverersCompleted = _discoveryCompletedClients == _availableTestSources;

            EqtTrace.Verbose("ParallelProxyDiscoveryManager: HandlePartialDiscoveryComplete: Total completed clients = {0}, Discovery complete = {1}.", _discoveryCompletedClients, allDiscoverersCompleted);
        }

        /*
         If discovery is complete or discovery aborting was requsted by testPlatfrom(user)
         we need to stop all ongoing discoveries, because we want to separate aborting request
         when testhost crashed by itself and when user requested it (f.e. through TW)
         Schedule the clean up for managers and handlers.
        */
        if (allDiscoverersCompleted || IsAbortRequested)
        {
            // Reset enumerators
            _sourceEnumerator = null;

            _currentDiscoveryDataAggregator = null;
            _currentDiscoveryEventsHandler = null;

            // Dispose concurrent executors
            UpdateParallelLevel(0);

            return true;
        }

        // REVIEW: this was here before I did multi tfm work, this should be reviewed, because
        // the comment builds on false premise, so maybe too much work is done here, because we should take shared hosts into account, and schedule
        // the next source on the same manager if we have the possibility.
        // and not kill testhost for every source.
        /*  Discovery is not complete.
            Now when both.net framework and.net core projects can run in parallel
            we should clear manager and create new one for both cases.
            Otherwise `proxyDiscoveryManager` instance is alredy closed by now and it will give exception
            when trying to do some operation on it.
        */
        var SharedHosts = false;
        EqtTrace.Verbose("ParallelProxyDiscoveryManager: HandlePartialDiscoveryComplete: Replace discovery manager. Shared: {0}, Aborted: {1}.", SharedHosts, isAborted);

        RemoveManager(proxyDiscoveryManager);

        // If we have more sources, create manager for that source.
        // The source determines which type of host to create, because it can have a framework
        // and architecture associated with it.
        if (TryFetchNextSource(_sourceEnumerator, out string source))
        {
            var sourceDetail = _sourceToSourceDetailMap[source];
            proxyDiscoveryManager = CreateNewConcurrentManager(sourceDetail);
            var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
                                           _requestData,
                                           proxyDiscoveryManager,
                                           _currentDiscoveryEventsHandler,
                                           this,
                                           _currentDiscoveryDataAggregator);
            AddManager(proxyDiscoveryManager, parallelEventsHandler);
        }

        // REVIEW: is this really how it should be done? Proxy manager can be if we don't pass any and if we don't have more sources?
        // Let's attempt to trigger discovery for the source.
        DiscoverTestsOnConcurrentManager(source, proxyDiscoveryManager);

        return false;
    }

    #endregion

    private void DiscoverTestsPrivate(ITestDiscoveryEventsHandler2 discoveryEventsHandler)
    {
        _currentDiscoveryEventsHandler = discoveryEventsHandler;

        // Reset the discovery complete data
        _discoveryCompletedClients = 0;

        // REVIEW: what did I meant in the ERR comment below?? :D
        // ERR: I need to schedule them until I reach maxParallelLevel or until I run out of sources.
        // This won't schedule any source for discovery, because there are not concurrent managers.
        foreach (var concurrentManager in GetConcurrentManagerInstances())
        {
            if (!TryFetchNextSource(_sourceEnumerator, out string source))
            {
                throw new InvalidOperationException("There are no more sources");
            }

            var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
                _requestData,
                concurrentManager,
                discoveryEventsHandler,
                this,
                _currentDiscoveryDataAggregator);

            UpdateHandlerForManager(concurrentManager, parallelEventsHandler);
            DiscoverTestsOnConcurrentManager(source, concurrentManager);
        }
    }

    /// <summary>
    /// Triggers the discovery for the next data object on the concurrent discoverer
    /// Each concurrent discoverer calls this method, once its completed working on previous data
    /// </summary>
    /// <param name="ProxyDiscoveryManager">Proxy discovery manager instance.</param>
    private void DiscoverTestsOnConcurrentManager(string source, IProxyDiscoveryManager proxyDiscoveryManager)
    {
        if (source == null)
        {
            EqtTrace.Verbose("ProxyParallelDiscoveryManager: No sources available for discovery.");
            return;
        }

        EqtTrace.Verbose("ProxyParallelDiscoveryManager: Triggering test discovery for next source: {0}", source);

        // Kick off another discovery task for the next source
        var discoveryCriteria = new DiscoveryCriteria(
            new[] { source },
            _actualDiscoveryCriteria.FrequencyOfDiscoveredTestsEvent,
            _actualDiscoveryCriteria.DiscoveredTestEventTimeout,
            _actualDiscoveryCriteria.RunSettings
        );
        discoveryCriteria.TestCaseFilter = _actualDiscoveryCriteria.TestCaseFilter;
        Task.Run(() =>
            {
                EqtTrace.Verbose("ParallelProxyDiscoveryManager: Discovery started.");

                proxyDiscoveryManager.DiscoverTests(discoveryCriteria, GetHandlerForGivenManager(proxyDiscoveryManager));
            })
            .ContinueWith(t =>
                {
                    // Just in case, the actual discovery couldn't start for an instance. Ensure that
                    // we call discovery complete since we have already fetched a source. Otherwise
                    // discovery will not terminate
                    EqtTrace.Error("ParallelProxyDiscoveryManager: Failed to trigger discovery. Exception: " + t.Exception);

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
}
