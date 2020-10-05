// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunner
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    public class TestRunnerPool
    {
        private static object lockObject = new object();
        private static volatile TestRunnerPool instance;

        private Dictionary<Session, IList<ProxyOperationManager>> runnerPool;

        private TestRunnerPool()
        {
            this.runnerPool = new Dictionary<Session, IList<ProxyOperationManager>>();
        }

        public static TestRunnerPool Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new TestRunnerPool();
                        }
                    }
                }

                return instance;
            }
        }

        public void AddProxy(Session session, ProxyOperationManager proxyManager)
        {
            if (!this.runnerPool.ContainsKey(session))
            {
                this.runnerPool.Add(session, new List<ProxyOperationManager>());
            }

            this.runnerPool[session].Add(proxyManager);
        }

        public ProxyOperationManager GetFirstProxy(Session session)
        {
            return this.runnerPool[session][0];
        }

        public void RemoveFirstProxy(Session session)
        {
            // TODO: Don't remove, instead re-use the testhosts.
            this.runnerPool[session].RemoveAt(0);
        }

        public ProxyOperationManager GetAndRemoveFirstProxy(Session session)
        {
            var proxy = this.GetFirstProxy(session);
            this.RemoveFirstProxy(session);

            return proxy;
        }
    }
}
