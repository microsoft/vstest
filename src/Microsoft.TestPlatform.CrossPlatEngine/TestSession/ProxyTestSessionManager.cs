// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    using CrossPlatResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// Orchestrates test session operations for the engine communicating with the client.
    /// </summary>
    public class ProxyTestSessionManager : IProxyTestSessionManager
    {
        private readonly object lockObject = new object();
        private readonly object proxyOperationLockObject = new object();
        private StartTestSessionCriteria testSessionCriteria;
        private int testhostCount;
        private TestSessionInfo testSessionInfo;
        private Func<ProxyOperationManager> proxyCreator;
        private IList<ProxyOperationManagerContainer> proxyContainerList;
        private IDictionary<string, int> proxyMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyTestSessionManager"/> class.
        /// </summary>
        /// 
        /// <param name="criteria">The test session criteria.</param>
        /// <param name="testhostCount">The testhost count.</param>
        /// <param name="proxyCreator">The proxy creator.</param>
        public ProxyTestSessionManager(
            StartTestSessionCriteria criteria,
            int testhostCount,
            Func<ProxyOperationManager> proxyCreator)
        {
            this.testSessionCriteria = criteria;
            this.testhostCount = testhostCount;
            this.proxyCreator = proxyCreator;

            this.proxyContainerList = new List<ProxyOperationManagerContainer>();
            this.proxyMap = new Dictionary<string, int>();
        }

        /// <inheritdoc/>
        public void StartSession(ITestSessionEventsHandler eventsHandler)
        {
            lock (this.lockObject)
            {
                if (this.testSessionInfo != null)
                {
                    return;
                }
                this.testSessionInfo = new TestSessionInfo();
            }

            // Create all the proxies in parallel, one task per proxy.
            var taskList = new Task[this.testhostCount];
            for (int i = 0; i < this.testhostCount; ++i)
            {
                // The testhost count is equal to 1 because one of the following conditions
                // holds true:
                //     1. we're dealing with a shared testhost (e.g.: .NET Framework testhost)
                //        that must process multiple sources within the same testhost process;
                //     2. we're dealing with a single testhost (shared or not, it doesn't matter)
                //        that must process a single source;
                // Either way, no further processing of the original test source list is needed
                // in either of those cases.
                //
                // Consequentely, if the testhost count is greater than one it means that the
                // testhost is not shared (e.g.: .NET Core testhost), in which case each test
                // source must be processed by a dedicated testhost, which is the reason we
                // create a list with a single element, i.e. the current source to be processed.
                var sources = (this.testhostCount == 1)
                    ? this.testSessionCriteria.Sources
                    : new List<string>() { this.testSessionCriteria.Sources[i] };

                taskList[i] = Task.Factory.StartNew(() => this.SetupRawProxy(
                    sources,
                    this.testSessionCriteria.RunSettings,
                    eventsHandler));
            }

            // Wait for proxy creation to be over.
            Task.WaitAll(taskList);

            // Make the session available.
            TestSessionPool.Instance.AddSession(this.testSessionInfo, this);

            // Let the caller know the session has been created.
            eventsHandler.HandleStartTestSessionComplete(this.testSessionInfo);
        }

        /// <inheritdoc/>
        public void StopSession()
        {
            lock (this.lockObject)
            {
                if (this.testSessionInfo == null)
                {
                    return;
                }
                this.testSessionInfo = null;
            }

            lock (this.proxyOperationLockObject)
            {
                // Dispose of all the proxies in parallel, one task per proxy.
                int index = 0;
                var taskList = new Task[this.proxyContainerList.Count];
                foreach (var proxyContainer in this.proxyContainerList)
                {
                    taskList[index++] = Task.Factory.StartNew(() =>
                    {
                        // Initiate the end session handshake with the underlying testhost.
                        proxyContainer.Proxy.Close();
                    });
                }

                // Wait for proxy disposal to be over.
                Task.WaitAll(taskList);

                this.proxyContainerList.Clear();
                this.proxyMap.Clear();
            }
        }

        /// <summary>
        /// Dequeues a proxy to be used either by discovery or execution.
        /// </summary>
        /// 
        /// <param name="source">The source to be associated to this proxy.</param>
        /// <param name="runSettings">The run settings.</param>
        /// 
        /// <returns>The dequeued proxy.</returns>
        public ProxyOperationManager DequeueProxy(string source, string runSettings)
        {
            ProxyOperationManagerContainer proxyContainer = null;

            lock (this.proxyOperationLockObject)
            {
                // No proxy available means the caller will have to create its own proxy.
                if (!this.proxyMap.ContainsKey(source)
                    || !this.proxyContainerList[this.proxyMap[source]].IsAvailable)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatResources.NoAvailableProxyForDeque));
                }

                // We must ensure the current run settings match the run settings from when the
                // testhost was started. If not, throw an exception to force the caller to create
                // its own proxy instead.
                //
                // TODO (copoiena): This run settings match is rudimentary. We should refine the
                // match criteria in the future.
                if (!this.testSessionCriteria.RunSettings.Equals(runSettings))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatResources.NoProxyMatchesDescription));
                }

                // Get the actual proxy.
                proxyContainer = this.proxyContainerList[this.proxyMap[source]];

                // Mark the proxy as unavailable.
                proxyContainer.IsAvailable = false;
            }

            return proxyContainer.Proxy;
        }

        /// <summary>
        /// Enqueues a proxy back once discovery or executions is done with it.
        /// </summary>
        /// 
        /// <param name="proxyId">The id of the proxy to be re-enqueued.</param>
        public void EnqueueProxy(int proxyId)
        {
            lock (this.proxyOperationLockObject)
            {
                // Check if the proxy exists.
                if (proxyId < 0 || proxyId >= this.proxyContainerList.Count)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatResources.NoSuchProxyId,
                            proxyId));
                }

                // Get the actual proxy.
                var proxyContainer = this.proxyContainerList[proxyId];
                if (proxyContainer.IsAvailable)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatResources.ProxyIsAlreadyAvailable,
                            proxyId));
                }

                // Mark the proxy as available.
                proxyContainer.IsAvailable = true;
            }
        }

        private int EnqueueNewProxy(
            IList<string> sources,
            ProxyOperationManagerContainer operationManagerContainer)
        {
            lock (this.proxyOperationLockObject)
            {
                var index = this.proxyContainerList.Count;

                // Add the proxy container to the proxy container list.
                this.proxyContainerList.Add(operationManagerContainer);

                foreach (var source in sources)
                {
                    // Add the proxy index to the map.
                    this.proxyMap.Add(
                        source,
                        index);
                }

                return index;
            }
        }

        private void SetupRawProxy(
            IList<string> sources,
            string runSettings,
            ITestSessionEventsHandler eventsHandler)
        {
            try
            {
                // Create and cache the proxy.
                var operationManagerProxy = this.proxyCreator();
                if (operationManagerProxy == null)
                {
                    return;
                }

                // Initialize the proxy.
                operationManagerProxy.Initialize(skipDefaultAdapters: false);

                // Start the test host associated to the proxy.
                operationManagerProxy.SetupChannel(
                    sources,
                    runSettings,
                    eventsHandler);

                // Associate each source in the source list with this new proxy operation
                // container.
                var operationManagerContainer = new ProxyOperationManagerContainer(
                    operationManagerProxy,
                    available: true);

                operationManagerContainer.Proxy.Id = this.EnqueueNewProxy(sources, operationManagerContainer);
            }
            catch (Exception ex)
            {
                // Log & silently eat up the exception. It's a valid course of action to
                // just forfeit proxy creation. This means that anyone wishing to get a
                // proxy operation manager would have to create their own, on the spot,
                // instead of getting one already created, and this case is handled
                // gracefully already.
                EqtTrace.Error(
                    "ProxyTestSessionManager.StartSession: Cannot create proxy. Error: {0}",
                    ex.ToString());
                return;
            }
        }
    }

    /// <summary>
    /// Defines a container encapsulating the proxy and its corresponding state info.
    /// </summary>
    internal class ProxyOperationManagerContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManagerContainer"/> class.
        /// </summary>
        /// 
        /// <param name="proxy">The proxy.</param>
        /// <param name="available">A flag indicating if the proxy is available to do work.</param>
        public ProxyOperationManagerContainer(ProxyOperationManager proxy, bool available)
        {
            this.Proxy = proxy;
            this.IsAvailable = available;
        }

        /// <summary>
        /// Gets or sets the proxy.
        /// </summary>
        public ProxyOperationManager Proxy { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if the proxy is available to do work.
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}
