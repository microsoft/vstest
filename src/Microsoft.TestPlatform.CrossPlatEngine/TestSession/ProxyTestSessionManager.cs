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

    using CrossPlatResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// Orchestrates test session operations for the engine communicating with the client.
    /// </summary>
    public class ProxyTestSessionManager : IProxyTestSessionManager
    {
        private readonly object lockObject = new object();
        private StartTestSessionCriteria testSessionCriteria;
        private int parallelLevel;
        private bool skipDefaultAdapters;
        private TestSessionInfo testSessionInfo;
        private Func<ProxyOperationManager> proxyCreator;
        private Queue<Guid> availableProxyQueue;
        private IDictionary<Guid, ProxyOperationManagerContainer> proxyMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyTestSessionManager"/> class.
        /// </summary>
        /// 
        /// <param name="criteria">The test session criteria.</param>
        /// <param name="parallelLevel">The parallel level.</param>
        /// <param name="proxyCreator">The proxy creator.</param>
        public ProxyTestSessionManager(
            StartTestSessionCriteria criteria,
            int parallelLevel,
            Func<ProxyOperationManager> proxyCreator)
        {
            this.testSessionCriteria = criteria;
            this.parallelLevel = parallelLevel;
            this.proxyCreator = proxyCreator;

            this.availableProxyQueue = new Queue<Guid>();
            this.proxyMap = new Dictionary<Guid, ProxyOperationManagerContainer>();
        }

        /// <inheritdoc/>
        public void Initialize(bool skipDefaultAdapters)
        {
            this.skipDefaultAdapters = skipDefaultAdapters;
        }

        /// <inheritdoc/>
        public void StartSession(ITestSessionEventsHandler eventsHandler)
        {
            if (this.testSessionInfo != null)
            {
                return;
            }

            this.testSessionInfo = new TestSessionInfo();

            var taskList = new Task[this.parallelLevel];

            // Create all the proxies in parallel, one task per proxy.
            for (int i = 0; i < this.parallelLevel; ++i)
            {
                taskList[i] = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        // Create and cache the proxy.
                        var operationManagerProxy = this.proxyCreator();

                        // Initialize the proxy.
                        operationManagerProxy.Initialize(this.skipDefaultAdapters);

                        // Start the test host associated to the proxy.
                        operationManagerProxy.SetupChannel(
                            this.testSessionCriteria.Sources,
                            this.testSessionCriteria.RunSettings,
                            eventsHandler);

                        this.EnqueueNewProxy(operationManagerProxy);
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
                });
            }

            // Wait for proxy creation to be over.
            Task.WaitAll(taskList);

            // Make the session available.
            TestSessionPool.Instance.AddSession(testSessionInfo, this);

            // Let the caller know the session has been created.
            eventsHandler.HandleStartTestSessionComplete(testSessionInfo);
        }

        /// <inheritdoc/>
        public void StopSession()
        {
            if (this.testSessionInfo == null)
            {
                return;
            }

            int index = 0;
            var taskList = new Task[this.proxyMap.Count];

            // Dispose of all the proxies in parallel, one task per proxy.
            foreach (var kvp in this.proxyMap)
            {
                taskList[index++] = Task.Factory.StartNew(() =>
                {
                    // Initiate the end session handshake with the underlying testhost.
                    kvp.Value.Proxy.Close();
                });
            }

            // Wait for proxy disposal to be over.
            Task.WaitAll(taskList);

            this.testSessionInfo = null;
        }

        /// <summary>
        /// Dequeues a proxy to be used either by discovery or execution.
        /// </summary>
        /// 
        /// <param name="runSettings">The run settings.</param>
        /// 
        /// <returns>The dequeued proxy.</returns>
        public ProxyOperationManager DequeueProxy(string runSettings)
        {
            ProxyOperationManagerContainer proxyContainer = null;

            lock (this.lockObject)
            {
                // No proxy available means the caller will have to create its own proxy.
                if (this.availableProxyQueue.Count == 0)
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

                // Get the proxy id from the available queue.
                var proxyId = this.availableProxyQueue.Dequeue();

                // Get the actual proxy.
                proxyContainer = this.proxyMap[proxyId];

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
        public void EnqueueProxy(Guid proxyId)
        {
            lock (this.lockObject)
            {
                // Check if the proxy exists.
                if (!this.proxyMap.ContainsKey(proxyId))
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatResources.NoSuchProxyId,
                            proxyId));
                }

                // Get the actual proxy.
                var proxyContainer = this.proxyMap[proxyId];
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

                // Re-enqueue the proxy in the available queue.
                this.availableProxyQueue.Enqueue(proxyId);
            }
        }

        private void EnqueueNewProxy(ProxyOperationManager operationManagerProxy)
        {
            lock (this.lockObject)
            {
                // Add the proxy to the map.
                this.proxyMap.Add(
                    operationManagerProxy.Id,
                    new ProxyOperationManagerContainer(
                        operationManagerProxy,
                        available: true));

                // Enqueue the proxy id in the available queue.
                this.availableProxyQueue.Enqueue(operationManagerProxy.Id);
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
