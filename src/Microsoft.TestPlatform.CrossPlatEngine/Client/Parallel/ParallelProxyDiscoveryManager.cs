// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

/// <summary>
/// ParallelProxyDiscoveryManager that manages parallel discovery
/// </summary>
internal class ParallelProxyDiscoveryManager : IParallelProxyDiscoveryManager
{
    private readonly IDataSerializer _dataSerializer;
    private readonly ParallelOperationManager<IProxyDiscoveryManager, ITestDiscoveryEventsHandler2, DiscoveryCriteria> _parallelOperationManager;
    private readonly Dictionary<string, TestRuntimeProviderInfo> _sourceToTestHostProviderMap;
    private int _discoveryCompletedClients;
    private int _availableTestSources = -1;

    private ParallelDiscoveryDataAggregator _currentDiscoveryDataAggregator;
    private bool _skipDefaultAdapters;
    private readonly IRequestData _requestData;

    public bool IsAbortRequested { get; private set; }

    /// <summary>
    /// LockObject to update discovery status in parallel
    /// </summary>
    private readonly object _discoveryStatusLockObject = new();

    public ParallelProxyDiscoveryManager(
        IRequestData requestData,
        Func<TestRuntimeProviderInfo, IProxyDiscoveryManager> actualProxyManagerCreator,
        int parallelLevel,
        List<TestRuntimeProviderInfo> testHostProviders)
        : this(requestData, actualProxyManagerCreator, JsonDataSerializer.Instance, parallelLevel, testHostProviders)
    {
    }

    internal ParallelProxyDiscoveryManager(
        IRequestData requestData,
        Func<TestRuntimeProviderInfo, IProxyDiscoveryManager> actualProxyManagerCreator,
        IDataSerializer dataSerializer,
        int parallelLevel,
        List<TestRuntimeProviderInfo> testHostProviders)
    {
        _requestData = requestData;
        _dataSerializer = dataSerializer;
        _parallelOperationManager = new(actualProxyManagerCreator, parallelLevel);
        _sourceToTestHostProviderMap = testHostProviders
            .SelectMany(provider => provider.SourceDetails.Select(s => new KeyValuePair<string, TestRuntimeProviderInfo>(s.Source, provider)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    #region IProxyDiscoveryManager

    /// <inheritdoc/>
    public void Initialize(bool skipDefaultAdapters)
    {
        _skipDefaultAdapters = skipDefaultAdapters;
    }

    /// <inheritdoc/>
    public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
    {
        var workloads = SplitToWorkloads(discoveryCriteria, _sourceToTestHostProviderMap);
        _availableTestSources = workloads.Count;

        EqtTrace.Verbose("ParallelProxyDiscoveryManager: Start discovery. Total sources: " + _availableTestSources);

        // One data aggregator per parallel discovery
        _currentDiscoveryDataAggregator = new ParallelDiscoveryDataAggregator();

        // Marking all sources as not discovered before starting actual discovery
        _currentDiscoveryDataAggregator.MarkSourcesWithStatus(discoveryCriteria.Sources.ToList(), DiscoveryStatus.NotDiscovered);

        _parallelOperationManager.StartWork(workloads, eventHandler, GetParallelEventHandler, DiscoverTestsOnConcurrentManager);
    }

    private ITestDiscoveryEventsHandler2 GetParallelEventHandler(ITestDiscoveryEventsHandler2 eventHandler, IProxyDiscoveryManager concurrentManager)
    {
        return new ParallelDiscoveryEventsHandler(
                                           _requestData,
                                           concurrentManager,
                                           eventHandler,
                                           this,
                                           _currentDiscoveryDataAggregator);
    }

    /// <inheritdoc/>
    public void Abort()
    {
        IsAbortRequested = true;
        _parallelOperationManager.DoActionOnAllManagers((proxyManager) => proxyManager.Abort(), doActionsInParallel: true);
    }

    /// <inheritdoc/>
    public void Abort(ITestDiscoveryEventsHandler2 eventHandler)
    {
        IsAbortRequested = true;
        _parallelOperationManager.DoActionOnAllManagers((proxyManager) => proxyManager.Abort(eventHandler), doActionsInParallel: true);
    }

    /// <inheritdoc/>
    public void Close()
    {
        _parallelOperationManager.DoActionOnAllManagers(proxyManager => proxyManager.Close(), doActionsInParallel: true);
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
            _parallelOperationManager.StopAllManagers();

            return true;
        }

        _parallelOperationManager.RunNextWork(proxyDiscoveryManager);

        return false;
    }

    #endregion

    private List<ProviderSpecificWorkload<DiscoveryCriteria>> SplitToWorkloads(DiscoveryCriteria discoveryCriteria, Dictionary<string, TestRuntimeProviderInfo> sourceToTestHostProviderMap)
    {
        List<ProviderSpecificWorkload<DiscoveryCriteria>> workloads = new();
        foreach (var source in discoveryCriteria.Sources)
        {
            var testHostProviderInfo = sourceToTestHostProviderMap[source];
            var runsettingsXml = testHostProviderInfo.RunSettings;
            var updatedDiscoveryCriteria = new ProviderSpecificWorkload<DiscoveryCriteria>(NewDiscoveryCriteriaFromSourceAndSettings(source, discoveryCriteria, runsettingsXml), testHostProviderInfo);
            workloads.Add(updatedDiscoveryCriteria);
        }

        return workloads;

        static DiscoveryCriteria NewDiscoveryCriteriaFromSourceAndSettings(string source, DiscoveryCriteria discoveryCriteria, string runsettingsXml)
        {
            var criteria = new DiscoveryCriteria(
                new[] { source },
                discoveryCriteria.FrequencyOfDiscoveredTestsEvent,
                discoveryCriteria.DiscoveredTestEventTimeout,
                runsettingsXml,
                discoveryCriteria.TestSessionInfo
            );

            criteria.TestCaseFilter = discoveryCriteria.TestCaseFilter;

            return criteria;
        }
    }

    /// <summary>
    /// Triggers the discovery for the next data object on the concurrent discoverer
    /// Each concurrent discoverer calls this method, once its completed working on previous data
    /// </summary>
    /// <param name="ProxyDiscoveryManager">Proxy discovery manager instance.</param>
    private void DiscoverTestsOnConcurrentManager(IProxyDiscoveryManager proxyDiscoveryManager, ITestDiscoveryEventsHandler2 eventHandler, DiscoveryCriteria discoveryCriteria)
    {

        // Kick off another discovery task for the next source
        Task.Run(() =>
            {
                EqtTrace.Verbose("ParallelProxyDiscoveryManager: Discovery started.");

                proxyDiscoveryManager.DiscoverTests(discoveryCriteria, eventHandler);
            })
            .ContinueWith(t =>
                {
                    // Just in case, the actual discovery couldn't start for an instance. Ensure that
                    // we call discovery complete since we have already fetched a source. Otherwise
                    // discovery will not terminate
                    EqtTrace.Error("ParallelProxyDiscoveryManager: Failed to trigger discovery. Exception: " + t.Exception);

                    var handler = eventHandler;
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
