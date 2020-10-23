// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunner
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    public class TestRunnerPool
    {
        private static object lockObject = new object();
        private static volatile TestRunnerPool instance;

        private Dictionary<TestSessionInfo, ProxyTestSessionManager> runnerPool;

        private TestRunnerPool()
        {
            this.runnerPool = new Dictionary<TestSessionInfo, ProxyTestSessionManager>();
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

        public void AddProxy(TestSessionInfo testSessionInfo, ProxyTestSessionManager proxyManager)
        {
            if (this.runnerPool.ContainsKey(testSessionInfo))
            {
                throw new ArgumentException("");
            }

            this.runnerPool.Add(testSessionInfo, proxyManager);
        }

        public ProxyOperationManager GetFirstProxy(TestSessionInfo testSessionInfo)
        {
            return this.runnerPool[testSessionInfo].OperationManagers[0];
        }

        public void RemoveFirstProxy(TestSessionInfo testSessionInfo)
        {
            // TODO: Don't remove, instead re-use the testhosts.
            this.runnerPool[testSessionInfo].OperationManagers.RemoveAt(0);
        }

        public ProxyOperationManager GetAndRemoveFirstProxy(TestSessionInfo testSessionInfo)
        {
            var proxy = this.GetFirstProxy(testSessionInfo);
            this.RemoveFirstProxy(testSessionInfo);

            return proxy;
        }
    }
}
