// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunner
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// 
    /// </summary>
    public class ProxyStartTestRunnerManager : IProxyStartTestRunnerManager
    {
        private Func<ProxyOperationManager> proxyCreator;
        private int parallelLevel;

        public ProxyStartTestRunnerManager(Func<ProxyOperationManager> proxyCreator, int parallelLevel)
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

        public void StartTestRunner(StartTestRunnerCriteria criteria, IStartTestRunnerEventsHandler eventsHandler)
        {
            var session = new Session();

            // Should be done by spawning a task.
            while (parallelLevel-- > 0)
            {
                var operationManagerProxy = this.proxyCreator();
                operationManagerProxy.SetupChannel(criteria.Sources, criteria.RunSettings);

                TestRunnerPool.Instance.AddProxy(session, operationManagerProxy);
            }

            eventsHandler.HandleStartTestRunnerComplete(session);
        }
    }
}
