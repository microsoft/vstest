// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;

    /// <summary>
    /// The factory that provides discovery and execution managers to the test host.
    /// </summary>
    public class TestHostManagerFactory : ITestHostManagerFactory
    {
        private IDiscoveryManager discoveryManager;
        private IExecutionManager executionManager;

        private IMetricsCollector metricsCollector;

        public TestHostManagerFactory()
        {
        }

        public TestHostManagerFactory(IMetricsCollector metricsCollector)
        {
            this.metricsCollector = metricsCollector ?? throw new System.ArgumentNullException(nameof(metricsCollector));
        }

        /// <summary>
        /// The discovery manager instance for any discovery related operations inside of the test host.
        /// </summary>
        /// <returns>The discovery manager.</returns>
        public IDiscoveryManager GetDiscoveryManager()
        {
            if(this.discoveryManager == null)
            {
                this.discoveryManager = new DiscoveryManager(this.metricsCollector);
            }
            return this.discoveryManager;
        }

        /// <summary>
        /// The execution manager instance for any discovery related operations inside of the test host.
        /// </summary>
        /// <returns>The execution manager.</returns>
        public IExecutionManager GetExecutionManager()
        {
            if (this.executionManager == null)
            {
                this.executionManager = new ExecutionManager(this.metricsCollector);
            }

            return this.executionManager;
        }
    }
}
