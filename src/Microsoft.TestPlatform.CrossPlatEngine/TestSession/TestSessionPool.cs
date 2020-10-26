// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    public class TestSessionPool
    {
        private static object instanceLockObject = new object();
        private static volatile TestSessionPool instance;

        private object lockObject = new object();
        private Dictionary<TestSessionInfo, ProxyTestSessionManager> sessionPool;

        private TestSessionPool()
        {
            this.sessionPool = new Dictionary<TestSessionInfo, ProxyTestSessionManager>();
        }

        public static TestSessionPool Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLockObject)
                    {
                        if (instance == null)
                        {
                            instance = new TestSessionPool();
                        }
                    }
                }

                return instance;
            }
        }

        public void AddSession(TestSessionInfo testSessionInfo, ProxyTestSessionManager proxyManager)
        {
            lock (this.lockObject)
            {
                if (this.sessionPool.ContainsKey(testSessionInfo))
                {
                    throw new ArgumentException("");
                }

                this.sessionPool.Add(testSessionInfo, proxyManager);
            }
        }

        public void RemoveSession(TestSessionInfo testSessionInfo)
        {
            ProxyTestSessionManager proxyManager = null;

            lock (this.lockObject)
            {
                if (!this.sessionPool.ContainsKey(testSessionInfo))
                {
                    throw new ArgumentException("");
                }

                proxyManager = this.sessionPool[testSessionInfo];
                this.sessionPool.Remove(testSessionInfo);
            }

            proxyManager.StopSession();
        }

        public ProxyOperationManager TakeProxy(TestSessionInfo testSessionInfo)
        {
            ProxyTestSessionManager sessionManager = null;
            lock (this.lockObject)
            {
                sessionManager = this.sessionPool[testSessionInfo];
            }

            return sessionManager.DequeueProxy();
        }

        public void ReturnProxy(TestSessionInfo testSessionInfo, Guid proxyId)
        {
            ProxyTestSessionManager sessionManager = null;
            lock (this.lockObject)
            {
                sessionManager = this.sessionPool[testSessionInfo];
            }

            sessionManager.EnqueueProxy(proxyId);
        }
    }
}
