// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// ParallelProxyDiscoveryManager that manages parallel discovery
    /// </summary>
    internal class ParallelProxyDiscoveryManager : ParallelOperationManager<IProxyDiscoveryManager, ITestDiscoveryEventsHandler>, IParallelProxyDiscoveryManager
    {
        #region DiscoverySpecificData

        private int discoveryCompletedClients = 0;

        private DiscoveryCriteria actualDiscoveryCriteria;

        private IEnumerator<string> sourceEnumerator;
        
        private Task lastParallelDiscoveryCleanUpTask = null;

        private ITestDiscoveryEventsHandler currentDiscoveryEventsHandler;

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
        void IProxyDiscoveryManager.DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler)
        {
            this.actualDiscoveryCriteria = discoveryCriteria;
                
            // Set the enumerator for parallel yielding of sources
            // Whenever a concurrent executor becomes free, it picks up the next source using this enumerator
            this.sourceEnumerator = discoveryCriteria.Sources.GetEnumerator();
            
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

            // In Case of Abort, clean old one and create a new ProxyDiscoveryManager in place of old one.
            if (!this.SharedHosts || isAborted)
            {
                this.RemoveManager(proxyDiscoveryManager);

                proxyDiscoveryManager = this.CreateNewConcurrentManager();

                var parallelEventsHandler = new ParallelDiscoveryEventsHandler(
                                               proxyDiscoveryManager,
                                               this.currentDiscoveryEventsHandler,
                                               this,
                                               this.currentDiscoveryDataAggregator);
                this.AddManager(proxyDiscoveryManager, parallelEventsHandler);
            }

            // If there are no more sources/testcases, a parallel executor is truly done with discovery
            if (!this.DiscoverTestsOnConcurrentManager(proxyDiscoveryManager))
            {
                lock (this.discoveryStatusLockObject)
                {
                    // Each concurrent Executor calls this method 
                    // So, we need to keep track of total discoverycomplete calls
                    this.discoveryCompletedClients++;
                    allDiscoverersCompleted = this.discoveryCompletedClients == this.GetConcurrentManagersCount();
                }

                // verify that all executors are done with the discovery and there are no more sources/testcases to execute
                if (allDiscoverersCompleted)
                {
                    // Reset enumerators
                    this.sourceEnumerator = null;

                    this.currentDiscoveryDataAggregator = null;
                    this.currentDiscoveryEventsHandler = null;

                    // Dispose concurrent executors
                    // Do not do the cleanuptask in the current thread as we will unncessarily add to discovery time
                    this.lastParallelDiscoveryCleanUpTask = Task.Run(() =>
                    {
                        this.UpdateParallelLevel(0);
                    });
                }
            }

            return allDiscoverersCompleted;
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
                catch (Exception)
                {
                    // ignore any exceptions
                }
            }
        }

        #endregion

        private void DiscoverTestsPrivate(ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            this.currentDiscoveryEventsHandler = discoveryEventsHandler;

            // Cleanup Task for cleaning up the parallel executors except for the default one
            // We do not do this in Sync so that this task does not add up to discovery time
            if (this.lastParallelDiscoveryCleanUpTask != null)
            {
                try
                {
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

                Task.Run(() => this.DiscoverTestsOnConcurrentManager(concurrentManager));
            }
        }

        /// <summary>
        /// Triggers the discovery for the next data object on the concurrent discoverer
        /// Each concurrent discoverer calls this method, once its completed working on previous data
        /// </summary>
        /// <param name="ProxyDiscoveryManager">Proxy discovery manager instance.</param>
        /// <returns>True, if discovery triggered</returns>
        private bool DiscoverTestsOnConcurrentManager(IProxyDiscoveryManager proxyDiscoveryManager)
        {
            DiscoveryCriteria discoveryCriteria = null;

            string nextSource = null;
            if (this.TryFetchNextSource(this.sourceEnumerator, out nextSource))
            {
                EqtTrace.Info("ProxyParallelDiscoveryManager: Triggering test discovery for next source: {0}", nextSource);
                discoveryCriteria = new DiscoveryCriteria(new List<string>() { nextSource }, this.actualDiscoveryCriteria.FrequencyOfDiscoveredTestsEvent, this.actualDiscoveryCriteria.DiscoveredTestEventTimeout, this.actualDiscoveryCriteria.RunSettings);
            }

            if (discoveryCriteria != null)
            {
                proxyDiscoveryManager.DiscoverTests(discoveryCriteria, this.GetHandlerForGivenManager(proxyDiscoveryManager));
                return true;
            }

            return false;
        }
    }
}
