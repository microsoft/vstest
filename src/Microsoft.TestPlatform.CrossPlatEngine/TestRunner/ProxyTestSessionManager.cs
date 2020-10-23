// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunner
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
        private Func<ProxyOperationManager> proxyCreator;
        private int parallelLevel;
        private bool skipDefaultAdapters;

        public IList<ProxyOperationManager> OperationManagers { get; set; }

        public ProxyTestSessionManager(Func<ProxyOperationManager> proxyCreator, int parallelLevel)
        {
            this.proxyCreator = proxyCreator;
            this.parallelLevel = parallelLevel;
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            // TODO: This should be called by StopTestSession to dispose of all the testhosts.
            throw new NotImplementedException();
        }

        public void Initialize(bool skipDefaultAdapters)
        {
            this.skipDefaultAdapters = skipDefaultAdapters;
        }

        public void StartTestSession(StartTestSessionCriteria criteria, ITestSessionEventsHandler eventsHandler)
        {
            var testSessionInfo = new TestSessionInfo();

            var taskList = new List<Task>();
            while (parallelLevel-- > 0)
            {
                taskList.Add(Task.Factory.StartNew(
                    () =>
                    {
                        var operationManagerProxy = this.proxyCreator();
                        operationManagerProxy.Initialize(this.skipDefaultAdapters);
                        operationManagerProxy.SetupChannel(criteria.Sources, criteria.RunSettings, eventsHandler);

                        // TODO: Instead of adding the ProxyOperationManager to a list in TestRunnerPool, keep the list
                        // in the TestSessionManager (i.e. here) and add THIS object to the mapping.
                        TestRunnerPool.Instance.AddProxy(testSessionInfo, this);
                    }));
            }

            Task.WaitAll(taskList.ToArray());

            eventsHandler.HandleStartTestSessionComplete(testSessionInfo);
        }
    }
}
