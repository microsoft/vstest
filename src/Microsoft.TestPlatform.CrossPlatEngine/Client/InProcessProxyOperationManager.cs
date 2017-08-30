// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    
    public abstract class InProcessProxyOperationManager
    {
        private readonly ITestRuntimeProvider testHostManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class. 
        /// </summary>
        /// <param name="requestSender">Request Sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        /// <param name="clientConnectionTimeout">Client Connection Timeout.</param>
        protected InProcessProxyOperationManager(ITestRuntimeProvider testHostManager)
        {
            this.testHostManager = testHostManager;
        }

        /// <summary>
        /// Update the AdapterSourceMap
        /// </summary>
        /// <param name="sources">test sources</param>
        /// <param name="adapterSourceMap">Adapter Source Map</param>
        /// <param name="testRuntimeProvider">testhostmanager which updates the sources</param>
        public virtual void UpdateTestSources(IEnumerable<string> sources, Dictionary<string, IEnumerable<string>> adapterSourceMap)
        {
            var updatedTestSources = this.testHostManager.GetTestSources(sources);
            adapterSourceMap.Clear();
            adapterSourceMap.Add(Constants.UnspecifiedAdapterPath, updatedTestSources);
        }
    }
}
