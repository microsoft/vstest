// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    using CrossPlatResources = Resources.Resources;

    /// <summary>
    /// Orchestrates test session operations for the engine communicating with the client.
    /// </summary>
    public class ProxyTestSessionManager : IProxyTestSessionManager
    {
        private readonly object lockObject = new();
        private readonly object proxyOperationLockObject = new();
        private volatile bool proxySetupFailed = false;
        private readonly StartTestSessionCriteria testSessionCriteria;
        private readonly int testhostCount;
        private TestSessionInfo testSessionInfo;
        private readonly Func<ProxyOperationManager> proxyCreator;
        private readonly IList<ProxyOperationManagerContainer> proxyContainerList;
        private readonly IDictionary<string, int> proxyMap;

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
            testSessionCriteria = criteria;
            this.testhostCount = testhostCount;
            this.proxyCreator = proxyCreator;

            proxyContainerList = new List<ProxyOperationManagerContainer>();
            proxyMap = new Dictionary<string, int>();
        }

        /// <inheritdoc/>
        public virtual bool StartSession(ITestSessionEventsHandler eventsHandler)
        {
            lock (lockObject)
            {
                if (testSessionInfo != null)
                {
                    return false;
                }
                testSessionInfo = new TestSessionInfo();
            }

            // Create all the proxies in parallel, one task per proxy.
            var taskList = new Task[testhostCount];
            for (int i = 0; i < taskList.Length; ++i)
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
                var sources = (testhostCount == 1)
                    ? testSessionCriteria.Sources
                    : new List<string>() { testSessionCriteria.Sources[i] };

                taskList[i] = Task.Factory.StartNew(() =>
                {
                    if (!SetupRawProxy(
                        sources,
                        testSessionCriteria.RunSettings))
                    {
                        proxySetupFailed = true;
                    }
                });
            }

            // Wait for proxy creation to be over.
            Task.WaitAll(taskList);

            // Dispose of all proxies if even one of them failed during setup.
            if (proxySetupFailed)
            {
                DisposeProxies();
                return false;
            }

            // Make the session available.
            if (!TestSessionPool.Instance.AddSession(testSessionInfo, this))
            {
                DisposeProxies();
                return false;
            }

            // Let the caller know the session has been created.
            eventsHandler.HandleStartTestSessionComplete(testSessionInfo);
            return true;
        }

        /// <inheritdoc/>
        public virtual bool StopSession()
        {
            lock (lockObject)
            {
                if (testSessionInfo == null)
                {
                    return false;
                }
                testSessionInfo = null;
            }

            DisposeProxies();
            return true;
        }

        /// <summary>
        /// Dequeues a proxy to be used either by discovery or execution.
        /// </summary>
        /// 
        /// <param name="source">The source to be associated to this proxy.</param>
        /// <param name="runSettings">The run settings.</param>
        /// 
        /// <returns>The dequeued proxy.</returns>
        public virtual ProxyOperationManager DequeueProxy(string source, string runSettings)
        {
            ProxyOperationManagerContainer proxyContainer = null;

            lock (proxyOperationLockObject)
            {
                // No proxy available means the caller will have to create its own proxy.
                if (!proxyMap.ContainsKey(source)
                    || !proxyContainerList[proxyMap[source]].IsAvailable)
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
                if (!testSessionCriteria.RunSettings.Equals(runSettings))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatResources.NoProxyMatchesDescription));
                }

                // Get the actual proxy.
                proxyContainer = proxyContainerList[proxyMap[source]];

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
        /// 
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        public virtual bool EnqueueProxy(int proxyId)
        {
            lock (proxyOperationLockObject)
            {
                // Check if the proxy exists.
                if (proxyId < 0 || proxyId >= proxyContainerList.Count)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatResources.NoSuchProxyId,
                            proxyId));
                }

                // Get the actual proxy.
                var proxyContainer = proxyContainerList[proxyId];
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

            return true;
        }

        private int EnqueueNewProxy(
            IList<string> sources,
            ProxyOperationManagerContainer operationManagerContainer)
        {
            lock (proxyOperationLockObject)
            {
                var index = proxyContainerList.Count;

                // Add the proxy container to the proxy container list.
                proxyContainerList.Add(operationManagerContainer);

                foreach (var source in sources)
                {
                    // Add the proxy index to the map.
                    proxyMap.Add(
                        source,
                        index);
                }

                return index;
            }
        }

        private bool SetupRawProxy(
            IList<string> sources,
            string runSettings)
        {
            try
            {
                // Create and cache the proxy.
                var operationManagerProxy = proxyCreator();
                if (operationManagerProxy == null)
                {
                    return false;
                }

                // Initialize the proxy.
                operationManagerProxy.Initialize(skipDefaultAdapters: false);

                // Start the test host associated to the proxy.
                if (!operationManagerProxy.SetupChannel(sources, runSettings))
                {
                    return false;
                }

                // Associate each source in the source list with this new proxy operation
                // container.
                var operationManagerContainer = new ProxyOperationManagerContainer(
                    operationManagerProxy,
                    available: true);

                operationManagerContainer.Proxy.Id = EnqueueNewProxy(sources, operationManagerContainer);
                return true;
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
            }

            return false;
        }

        private void DisposeProxies()
        {
            lock (proxyOperationLockObject)
            {
                if (proxyContainerList.Count == 0)
                {
                    return;
                }

                // Dispose of all the proxies in parallel, one task per proxy.
                int i = 0;
                var taskList = new Task[proxyContainerList.Count];
                foreach (var proxyContainer in proxyContainerList)
                {
                    taskList[i++] = Task.Factory.StartNew(() =>
                        // Initiate the end session handshake with the underlying testhost.
                        proxyContainer.Proxy.Close());
                }

                // Wait for proxy disposal to be over.
                Task.WaitAll(taskList);

                proxyContainerList.Clear();
                proxyMap.Clear();
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
            Proxy = proxy;
            IsAvailable = available;
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
