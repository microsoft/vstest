// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// ParallelProxyDiscoveryManager that manages parallel discovery
    /// </summary>
    internal class ParallelProxyDiscoveryManager : ParallelOperationManager<IProxyDiscoveryManager, ITestDiscoveryEventsHandler2>, IParallelProxyDiscoveryManager
    {
        #region DiscoverySpecificData

        private int discoveryCompletedClients = 0;
        private int availableTestSources = -1;

        private DiscoveryCriteria actualDiscoveryCriteria;

        private IEnumerator<string> sourceEnumerator;
        
        private Task lastParallelDiscoveryCleanUpTask = null;

        private ITestDiscoveryEventsHandler2 currentDiscoveryEventsHandler;

        private ParallelDiscoveryDataAggregator currentDiscoveryDataAggregator;

        #endregion

        #region Concurrency Keeper Objects

        /// <summary>
        /// LockObject to update discovery status in parallel
        /// </summary>
        private object discoveryStatusLockObject = new object();

        #endregion
        
        public ParallelProxyDiscoveryManager(Func<IProxyDiscoveryManager> actualProxyManagerCreator, int parallelLevel, bool sharedHosts)
            : base(actualProxyManagerCreator, parallelLevel, sharedHosts)
        {
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
                // Do not do the cleanuptask in the current thread as we will unncessarily add to discovery time
                this.lastParallelDiscoveryCleanUpTask = Task.Run(() => this.UpdateParallelLevel(0));

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

        #region ParallelOperationManager Methods

        /// <summary>
        /// Closes the instance of the IProxyDiscoveryManager Instance
        /// </summary>
        /// <param name="managerInstance"></param>
        protected override void DisposeInstance(IProxyDiscoveryManager managerInstance)
        {
            if (managerInstance != null)
            {
                try
                {
                    managerInstance.Close();
                }
                catch (Exception ex)
                {
                    // ignore any exceptions
                    EqtTrace.Error("ParallelProxyDiscoveryManager: Failed to dispose discovery manager. Exception: " + ex);
                }
            }
        }

        #endregion

        private void DiscoverTestsPrivate(ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.currentDiscoveryEventsHandler = discoveryEventsHandler;

            // Cleanup Task for cleaning up the parallel executors except for the default one
            // We do not do this in Sync so that this task does not add up to discovery time
            if (this.lastParallelDiscoveryCleanUpTask != null)
            {
                try
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("ProxyParallelDiscoveryManager: Wait for last cleanup to complete.");
                    }

                    this.lastParallelDiscoveryCleanUpTask.Wait();
                }
                catch (Exception ex)
                {
                    // if there is an exception disposing off concurrent hosts ignore it
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("ParallelProxyDiscoveryManager: Exception while invoking an action on DiscoveryManager: {0}", ex);
                    }
                }

                this.lastParallelDiscoveryCleanUpTask = null;
            }

            // Reset the discoverycomplete data
            this.discoveryCompletedClients = 0;

            // One data aggregator per parallel discovery
            this.currentDiscoveryDataAggregator = new ParallelDiscoveryDataAggregator();

            foreach (var concurrentManager in this.GetConcurrentManagerInstances())
            {
                var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
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
                        if (EqtTrace.IsWarningEnabled)
                        {
                            EqtTrace.Warning("ParallelProxyDiscoveryManager: Failed to trigger discovery. Exception: " + t.Exception);
                        }

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
