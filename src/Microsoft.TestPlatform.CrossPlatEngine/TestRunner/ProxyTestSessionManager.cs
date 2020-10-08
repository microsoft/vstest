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
            throw new NotImplementedException();
        }

        public void Initialize(bool skipDefaultAdapters)
        {
            //throw new NotImplementedException();
        }

        public void StartTestSession(StartTestSessionCriteria criteria, IStartTestSessionEventsHandler eventsHandler)
        {
            var testSessionInfo = new TestSessionInfo();

            var taskList = new List<Task>();
            while (parallelLevel-- > 0)
            {
                taskList.Add(Task.Factory.StartNew(
                    () =>
                    {
                        var operationManagerProxy = this.proxyCreator();
                        operationManagerProxy.SetupChannel(criteria.Sources, criteria.RunSettings);

                        TestRunnerPool.Instance.AddProxy(testSessionInfo, operationManagerProxy);
                    }));
            }

            Task.WaitAll(taskList.ToArray());

            eventsHandler.HandleStartTestSessionComplete(testSessionInfo);
        }
    }
}
