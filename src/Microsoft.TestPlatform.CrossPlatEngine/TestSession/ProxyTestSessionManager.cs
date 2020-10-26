// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// 
    /// </summary>
    public class ProxyTestSessionManager : IProxyTestSessionManager
    {
        private readonly object lockObject = new object();
        private int parallelLevel;
        private bool skipDefaultAdapters;
        private Func<ProxyOperationManager> proxyCreator;
        private Queue<Guid> availableProxyQueue;
        private IDictionary<Guid, ProxyOperationManagerContainer> proxyMap;

        public ProxyTestSessionManager(int parallelLevel, Func<ProxyOperationManager> proxyCreator)
        {
            this.parallelLevel = parallelLevel;
            this.proxyCreator = proxyCreator;
            this.availableProxyQueue = new Queue<Guid>();
            this.proxyMap = new Dictionary<Guid, ProxyOperationManagerContainer>();
        }

        public void Initialize(bool skipDefaultAdapters)
        {
            this.skipDefaultAdapters = skipDefaultAdapters;
        }

        public void StartSession(StartTestSessionCriteria criteria, ITestSessionEventsHandler eventsHandler)
        {
            var testSessionInfo = new TestSessionInfo();
            Task[] taskList = new Task[this.parallelLevel];

            for (int i = 0; i < this.parallelLevel; ++i)
            {
                taskList[i] = Task.Factory.StartNew(() =>
                {
                    var operationManagerProxy = this.CreateProxy();
                    operationManagerProxy.Initialize(this.skipDefaultAdapters);
                    operationManagerProxy.SetupChannel(criteria.Sources, criteria.RunSettings, eventsHandler);

                    TestSessionPool.Instance.AddSession(testSessionInfo, this);
                });
            }

            Task.WaitAll(taskList);
            eventsHandler.HandleStartTestSessionComplete(testSessionInfo);
        }

        public void StopSession()
        {
            foreach (var kvp in this.proxyMap)
            {
                // TODO: Do nothing for now because in the current implementation the testhosts are
                // disposed of right after the test run is done. However, when we'll decide to
                // re-use the testhosts for discovery & execution we'll perform some changes for
                // keeping them alive indefinetely, so the responsability for killing testhosts
                // will be with the users of the vstest.console wrapper. Then we'll need to be able
                // to dispose of the testhosts here.
            }
        }

        public ProxyOperationManager DequeueProxy()
        {
            ProxyOperationManagerContainer proxyContainer = null;

            lock (this.lockObject)
            {
                if (this.availableProxyQueue.Count == 0)
                {
                    throw new ArgumentException("");
                }

                var proxyId = this.availableProxyQueue.Dequeue();
                proxyContainer = this.proxyMap[proxyId];

                proxyContainer.Available = false;
            }

            return proxyContainer.Proxy;
        }

        public void EnqueueProxy(Guid proxyId)
        {
            lock (this.lockObject)
            {
                if (!this.proxyMap.ContainsKey(proxyId))
                {
                    throw new ArgumentException("");
                }

                var proxyContainer = this.proxyMap[proxyId];
                if (proxyContainer.Available)
                {
                    throw new ArgumentException("");
                }

                proxyContainer.Available = true;
                this.availableProxyQueue.Enqueue(proxyId);
            }
        }

        private ProxyOperationManager CreateProxy()
        {
            var proxy = this.proxyCreator();

            lock (this.lockObject)
            {
                this.proxyMap.Add(proxy.Id, new ProxyOperationManagerContainer(proxy, available: true));
                this.availableProxyQueue.Enqueue(proxy.Id);
            }

            return proxy;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class ProxyOperationManagerContainer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="available"></param>
        public ProxyOperationManagerContainer(ProxyOperationManager proxy, bool available)
        {
            this.Proxy = proxy;
            this.Available = available;
        }

        /// <summary>
        /// 
        /// </summary>
        public ProxyOperationManager Proxy { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Available { get; set; }
    }
}
