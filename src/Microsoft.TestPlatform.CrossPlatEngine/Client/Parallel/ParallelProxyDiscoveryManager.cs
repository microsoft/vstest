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
        private IDataSerializer dataSerializer;

        #region DiscoverySpecificData

        private int discoveryCompletedClients = 0;
        private int availableTestSources = -1;

        private DiscoveryCriteria actualDiscoveryCriteria;

        private IEnumerator<string> sourceEnumerator;
        
        private ITestDiscoveryEventsHandler2 currentDiscoveryEventsHandler;

        private ParallelDiscoveryDataAggregator currentDiscoveryDataAggregator;

        private IRequestData requestData;

        #endregion

        #region Concurrency Keeper Objects

        /// <summary>
        /// LockObject to update discovery status in parallel
        /// </summary>
        private object discoveryStatusLockObject = new object();

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
        public void Initialize()
        {
            this.DoActionOnAllManagers((proxyManager) => proxyManager.Initialize(), doActionsInParallel: true);
        }

        /// <inheritdoc/>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
        {
            this.actualDiscoveryCriteria = discoveryCriteria;
                
            // Set the enumerator for parallel yielding of sources
            // Whenever a concurrent executor becomes free, it picks up the next source using this enumerator
            this.sourceEnumerator = discoveryCriteria.Sources.GetEnumerator();
            this.availableTestSources = discoveryCriteria.Sources.Count();
            
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("ParallelProxyDiscoveryManager: Start discovery. Total sources: " + this.availableTestSources);
            }
            this.DiscoverTestsPrivate(eventHandler);
        }

        /// <inheritdoc/>
        public void Abort()
        {
            this.DoActionOnAllManagers((proxyManager) => proxyManager.Abort(), doActionsInParallel: true);
        }

        /// <inheritdoc/>
        public void Close()
        {
            this.DoActionOnAllManagers(proxyManager => proxyManager.Close(), doActionsInParallel: true);
        }

        #endregion

        #region IParallelProxyDiscoveryManager methods

        /// <inheritdoc/>
        public bool HandlePartialDiscoveryComplete(IProxyDiscoveryManager proxyDiscoveryManager, long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            var allDiscoverersCompleted = false;
            lock (this.discoveryStatusLockObject)
            {
                // Each concurrent Executor calls this method 
                // So, we need to keep track of total discoverycomplete calls
                this.discoveryCompletedClients++;

                // If there are no more sources/testcases, a parallel executor is truly done with discovery
                allDiscoverersCompleted = this.discoveryCompletedClients == this.availableTestSources;

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("ParallelProxyDiscoveryManager: HandlePartialDiscoveryComplete: Total completed clients = {0}, Discovery complete = {1}.", this.discoveryCompletedClients, allDiscoverersCompleted);
                }
            }

            // Discovery is completed. Schedule the clean up for managers and handlers.
            if (allDiscoverersCompleted)
            {
                // Reset enumerators
                this.sourceEnumerator = null;

                this.currentDiscoveryDataAggregator = null;
                this.currentDiscoveryEventsHandler = null;

                // Dispose concurrent executors
                this.UpdateParallelLevel(0);

                return true;
            }

            // Discovery is not complete.
            // First, clean up the used proxy discovery manager if the last run was aborted
            // or this run doesn't support shared hosts (netcore tests)
            if (!this.SharedHosts || isAborted)
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("ParallelProxyDiscoveryManager: HandlePartialDiscoveryComplete: Replace discovery manager. Shared: {0}, Aborted: {1}.", this.SharedHosts, isAborted);
                }

                this.RemoveManager(proxyDiscoveryManager);

                proxyDiscoveryManager = this.CreateNewConcurrentManager();
                var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
                                               this.requestData,
                                               proxyDiscoveryManager,
                                               this.currentDiscoveryEventsHandler,
                                               this,
                                               this.currentDiscoveryDataAggregator);
                this.AddManager(proxyDiscoveryManager, parallelEventsHandler);
            }

            // Second, let's attempt to trigger discovery for the next source.
            this.DiscoverTestsOnConcurrentManager(proxyDiscoveryManager);

            return false;
        }

        #endregion

        private void DiscoverTestsPrivate(ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.currentDiscoveryEventsHandler = discoveryEventsHandler;

            // Reset the discoverycomplete data
            this.discoveryCompletedClients = 0;

            // One data aggregator per parallel discovery
            this.currentDiscoveryDataAggregator = new ParallelDiscoveryDataAggregator();

            foreach (var concurrentManager in this.GetConcurrentManagerInstances())
            {
                var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
                                                this.requestData,
                                                concurrentManager,
                                                discoveryEventsHandler,
                                                this,
                                                this.currentDiscoveryDataAggregator);

                this.UpdateHandlerForManager(concurrentManager, parallelEventsHandler);
                this.DiscoverTestsOnConcurrentManager(concurrentManager);
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
            if (this.TryFetchNextSource(this.sourceEnumerator, out string nextSource))
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("ProxyParallelDiscoveryManager: Triggering test discovery for next source: {0}", nextSource);
                }

                // Kick off another discovery task for the next source
                var discoveryCriteria = new DiscoveryCriteria(new[] { nextSource }, this.actualDiscoveryCriteria.FrequencyOfDiscoveredTestsEvent, this.actualDiscoveryCriteria.DiscoveredTestEventTimeout, this.actualDiscoveryCriteria.RunSettings);
                discoveryCriteria.TestCaseFilter = this.actualDiscoveryCriteria.TestCaseFilter;
                Task.Run(() =>
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("ParallelProxyDiscoveryManager: Discovery started.");
                        }

                        proxyDiscoveryManager.DiscoverTests(discoveryCriteria, this.GetHandlerForGivenManager(proxyDiscoveryManager));
                    })
                    .ContinueWith(t =>
                    {
                        // Just in case, the actual discovery couldn't start for an instance. Ensure that
                        // we call discovery complete since we have already fetched a source. Otherwise
                        // discovery will not terminate
                        EqtTrace.Error("ParallelProxyDiscoveryManager: Failed to trigger discovery. Exception: " + t.Exception);

                        var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = t.Exception.ToString() };
                        this.GetHandlerForGivenManager(proxyDiscoveryManager).HandleRawMessage(this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload));
                        this.GetHandlerForGivenManager(proxyDiscoveryManager).HandleLogMessage(TestMessageLevel.Error, t.Exception.ToString());

                        // Send discovery complete. Similar logic is also used in ProxyDiscoveryManager.DiscoverTests.
                        // Differences:
                        // Total tests must be zero here since parallel discovery events handler adds the count
                        // Keep `lastChunk` as null since we don't want a message back to the IDE (discovery didn't even begin)
                        // Set `isAborted` as true since we want this instance of discovery manager to be replaced
                        var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(-1, true);
                        this.GetHandlerForGivenManager(proxyDiscoveryManager).HandleDiscoveryComplete(discoveryCompleteEventsArgs, null);
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
