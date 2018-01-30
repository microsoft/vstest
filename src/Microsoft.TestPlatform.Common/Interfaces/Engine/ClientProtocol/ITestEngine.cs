// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

    /// <summary>
    /// Defines the functionality of a test engine.
    /// </summary>
    public interface ITestEngine
    {
        /// <summary>
        /// Fetches the DiscoveryManager for this engine. This manager would provide all functionality required for discovery.
        /// </summary>
        /// <param name="requestData">The Request Data for providing discovery services and data.</param>
        /// <param name="testHostManager">Test host manager for the current test discovery.</param>
        /// <param name="discoveryCriteria">The discovery Criteria.</param>
        /// <returns>
        /// ITestDiscoveryManager object that can do discovery
        /// </returns>
        IProxyDiscoveryManager GetDiscoveryManager(IRequestData requestData, ITestRuntimeProvider testHostManager, DiscoveryCriteria discoveryCriteria);

        /// <summary>
        /// Fetches the ExecutionManager for this engine. This manager would provide all functionality required for execution.
        /// </summary>
        /// <param name="requestData">The request data for providing common execution services and data</param>
        /// <param name="testHostManager">Test host manager for current test run.</param>
        /// <param name="testRunCriteria">TestRunCriteria of the current test run</param>
        /// <returns>ITestExecutionManager object that can do execution</returns>
        IProxyExecutionManager GetExecutionManager(IRequestData requestData, ITestRuntimeProvider testHostManager, TestRunCriteria testRunCriteria);

        /// <summary>
        /// Fetches the extension manager for this engine. This manager would provide extensibility
        /// features that this engine supports.
        /// </summary>
        /// <returns>ITestExtensionManager object that helps with extensibility</returns>
        ITestExtensionManager GetExtensionManager();

        /// <summary>
        /// Fetches the logger manager for this engine. This manager will provide logger extensibility features that this engine supports.
        /// </summary>
        /// <param name="requestData">The request data for providing common execution services and data</param>
        /// <returns>ITestLoggerManager object that helps with logger extensibility.</returns>
        ITestLoggerManager GetLoggerManager(IRequestData requestData);
    }
}
