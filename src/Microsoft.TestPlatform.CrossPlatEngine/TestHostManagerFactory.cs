// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
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

        /// <summary>
        /// The discovery manager instance for any discovery related operations inside of the test host.
        /// </summary>
        /// <returns>The discovery manager.</returns>
        public IDiscoveryManager GetDiscoveryManager()
        {
            if(this.discoveryManager == null)
            {
                this.discoveryManager = new DiscoveryManager();
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
                this.executionManager = new ExecutionManager();
            }

            return this.executionManager;
        }
    }
}
