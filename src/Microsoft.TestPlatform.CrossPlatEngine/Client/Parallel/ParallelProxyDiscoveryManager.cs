// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
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

    /// <summary>
    /// ParallelProxyDiscoveryManager that manages parallel discovery
    /// </summary>
    internal class ParallelProxyDiscoveryManager : ParallelOperationManager<IProxyDiscoveryManager, ITestDiscoveryEventsHandler2>, IParallelProxyDiscoveryManager
    {
        private readonly IDataSerializer dataSerializer;

        #region DiscoverySpecificData

        private int discoveryCompletedClients = 0;
        private int availableTestSources = -1;

        private DiscoveryCriteria actualDiscoveryCriteria;

        private IEnumerator<string> sourceEnumerator;

        private ITestDiscoveryEventsHandler2 currentDiscoveryEventsHandler;

        private ParallelDiscoveryDataAggregator currentDiscoveryDataAggregator;

        private readonly IRequestData requestData;

        // This field indicates if abort was requested by testplatform (user)
        private bool discoveryAbortRequested = false;

        #endregion

        #region Concurrency Keeper Objects

        /// <summary>
        /// LockObject to update discovery status in parallel
        /// </summary>
        private readonly object discoveryStatusLockObject = new();

        #endregion

        public ParallelProxyDiscoveryManager(IRequestData requestData, Func<IProxyDiscoveryManager> actualProxyManagerCreator, int parallelLevel, bool sharedHosts)
            : this(requestData, actualProxyManagerCreator, JsonDataSerializer.Instance, parallelLevel, sharedHosts)
        {
        }

        internal ParallelProxyDiscoveryManager(IRequestData requestData, Func<IProxyDiscoveryManager> actualProxyManagerCreator, IDataSerializer dataSerializer, int parallelLevel, bool sharedHosts)
            : base(actualProxyManagerCreator, parallelLevel, sharedHosts)
        {
            this.requestData = requestData;
            this.dataSerializer = dataSerializer;
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
            actualDiscoveryCriteria = discoveryCriteria;

            // Set the enumerator for parallel yielding of sources
            // Whenever a concurrent executor becomes free, it picks up the next source using this enumerator
            sourceEnumerator = discoveryCriteria.Sources.GetEnumerator();
            availableTestSources = discoveryCriteria.Sources.Count();

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("ParallelProxyDiscoveryManager: Start discovery. Total sources: " + availableTestSources);
            }
            DiscoverTestsPrivate(eventHandler);
        }

        /// <inheritdoc/>
        public void Abort()
        {
            discoveryAbortRequested = true;
            DoActionOnAllManagers((proxyManager) => proxyManager.Abort(), doActionsInParallel: true);
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
            lock (discoveryStatusLockObject)
            {
                // Each concurrent Executor calls this method
                // So, we need to keep track of total discovery complete calls
                discoveryCompletedClients++;

                // If there are no more sources/testcases, a parallel executor is truly done with discovery
                allDiscoverersCompleted = discoveryCompletedClients == availableTestSources;

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("ParallelProxyDiscoveryManager: HandlePartialDiscoveryComplete: Total completed clients = {0}, Discovery complete = {1}.", discoveryCompletedClients, allDiscoverersCompleted);
                }
            }

            /*
             If discovery is complete or discovery aborting was requsted by testPlatfrom(user)
             we need to stop all ongoing discoveries, because we want to separate aborting request
             when testhost crashed by itself and when user requested it (f.e. through TW)
             Schedule the clean up for managers and handlers.
            */
            if (allDiscoverersCompleted || discoveryAbortRequested)
            {
                // Reset enumerators
                sourceEnumerator = null;

                currentDiscoveryDataAggregator = null;
                currentDiscoveryEventsHandler = null;

                // Dispose concurrent executors
                UpdateParallelLevel(0);

                return true;
            }

            /*  Discovery is not complete.
                Now when both.net framework and.net core projects can run in parallel
                we should clear manager and create new one for both cases.
                Otherwise `proxyDiscoveryManager` instance is alredy closed by now and it will give exception
                when trying to do some operation on it.
            */
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("ParallelProxyDiscoveryManager: HandlePartialDiscoveryComplete: Replace discovery manager. Shared: {0}, Aborted: {1}.", SharedHosts, isAborted);
            }

            RemoveManager(proxyDiscoveryManager);

            proxyDiscoveryManager = CreateNewConcurrentManager();
            var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
                                           requestData,
                                           proxyDiscoveryManager,
                                           currentDiscoveryEventsHandler,
                                           this,
                                           currentDiscoveryDataAggregator);
            AddManager(proxyDiscoveryManager, parallelEventsHandler);

            // Second, let's attempt to trigger discovery for the next source.
            DiscoverTestsOnConcurrentManager(proxyDiscoveryManager);

            return false;
        }

        #endregion

        private void DiscoverTestsPrivate(ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            currentDiscoveryEventsHandler = discoveryEventsHandler;

            // Reset the discovery complete data
            discoveryCompletedClients = 0;

            // One data aggregator per parallel discovery
            currentDiscoveryDataAggregator = new ParallelDiscoveryDataAggregator();

            foreach (var concurrentManager in GetConcurrentManagerInstances())
            {
                var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
                                                requestData,
                                                concurrentManager,
                                                discoveryEventsHandler,
                                                this,
                                                currentDiscoveryDataAggregator);

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
            // Peek to see if we have sources to trigger a discovery
            if (TryFetchNextSource(sourceEnumerator, out string nextSource))
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("ProxyParallelDiscoveryManager: Triggering test discovery for next source: {0}", nextSource);
                }

                // Kick off another discovery task for the next source
                var discoveryCriteria = new DiscoveryCriteria(new[] { nextSource }, actualDiscoveryCriteria.FrequencyOfDiscoveredTestsEvent, actualDiscoveryCriteria.DiscoveredTestEventTimeout, actualDiscoveryCriteria.RunSettings);
                discoveryCriteria.TestCaseFilter = actualDiscoveryCriteria.TestCaseFilter;
                Task.Run(() =>
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("ParallelProxyDiscoveryManager: Discovery started.");
                        }

                        proxyDiscoveryManager.DiscoverTests(discoveryCriteria, GetHandlerForGivenManager(proxyDiscoveryManager));
                    })
                    .ContinueWith(t =>
                    {
                        // Just in case, the actual discovery couldn't start for an instance. Ensure that
                        // we call discovery complete since we have already fetched a source. Otherwise
                        // discovery will not terminate
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error("ParallelProxyDiscoveryManager: Failed to trigger discovery. Exception: " + t.Exception);
                        }

                        var handler = GetHandlerForGivenManager(proxyDiscoveryManager);
                        var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = t.Exception.ToString() };
                        handler.HandleRawMessage(dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload));
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

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("ProxyParallelDiscoveryManager: No sources available for discovery.");
            }
        }
    }
}
