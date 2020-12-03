// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    using CrossPlatResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// Orchestrates test session operations for the engine communicating with the client.
    /// </summary>
    public class ProxyTestSessionManager : IProxyTestSessionManager
    {
        private readonly object lockObject = new object();
        private int parallelLevel;
        private bool skipDefaultAdapters;
        private Func<ProxyOperationManager> proxyCreator;
        private Queue<Guid> availableProxyQueue;
        private IDictionary<Guid, ProxyOperationManagerContainer> proxyMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyTestSessionManager"/> class.
        /// </summary>
        /// 
        /// <param name="parallelLevel">The parallel level.</param>
        /// <param name="proxyCreator">The proxy creator.</param>
        public ProxyTestSessionManager(int parallelLevel, Func<ProxyOperationManager> proxyCreator)
        {
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
        public void StartSession(
            StartTestSessionCriteria criteria,
            ITestSessionEventsHandler eventsHandler)
        {
            var testSessionInfo = new TestSessionInfo();
            Task[] taskList = new Task[this.parallelLevel];

            // Create all the proxies in parallel, one task per proxy.
            for (int i = 0; i < this.parallelLevel; ++i)
            {
                taskList[i] = Task.Factory.StartNew(() =>
                {
                    // Create the proxy.
                    var operationManagerProxy = this.CreateProxy();

                    // Initialize the proxy.
                    operationManagerProxy.Initialize(this.skipDefaultAdapters);

                    // Start the test host associated to the proxy.
                    operationManagerProxy.SetupChannel(
                        criteria.Sources,
                        criteria.RunSettings,
                        eventsHandler);
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
            // TODO (copoiena): Do nothing for now because in the current implementation the
            // testhosts are disposed of right after the test run is done. However, when we'll
            // decide to re-use the testhosts for discovery & execution we'll perform some
            // changes for keeping them alive indefinetely, so the responsability for killing
            // testhosts will be with the users of the vstest.console wrapper. Then we'll need
            // to be able to dispose of the testhosts here.

            // foreach (var kvp in this.proxyMap)
            // {
            // }
        }

        /// <summary>
        /// Dequeues a proxy to be used either by discovery or execution.
        /// </summary>
        /// 
        /// <returns>The dequeued proxy.</returns>
        public ProxyOperationManager DequeueProxy()
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

        private ProxyOperationManager CreateProxy()
        {
            // Invoke the proxy creator.
            var proxy = this.proxyCreator();

            lock (this.lockObject)
            {
                // Add the proxy to the map.
                this.proxyMap.Add(proxy.Id, new ProxyOperationManagerContainer(proxy, available: true));

                // Enqueue the proxy id in the available queue.
                this.availableProxyQueue.Enqueue(proxy.Id);
            }

            return proxy;
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
