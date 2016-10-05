// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface defining the parallel discovery manager
    /// </summary>
    public interface IParallelProxyDiscoveryManager : IParallelOperationManager, IProxyDiscoveryManager
    {
        /// <summary>
        /// Handles Partial Discovery Complete event coming from a specific concurrent proxy discovery manager
        /// Each concurrent proxy execution manager will signal the parallel execution manager when its complete
        /// </summary>
        /// <param name="totalTests">Total Tests discovered for the concurrent discovery</param>
        /// <param name="lastChunk">LastChunk testcases for the concurrent discovery</param>
        /// <param name="isAborted">True is discovery is aborted</param>
        /// <returns>True if parallel discovery is complete</returns>
        bool HandlePartialDiscoveryComplete(
            IProxyDiscoveryManager proxyDiscoveryManager,
            long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted);
    }
}
