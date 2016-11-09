// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Defines the functionality of a test engine.
    /// </summary>
    public interface ITestEngine
    {
        /// <summary>
        /// Fetches the DiscoveryManager for this engine. This manager would provide all functionality required for discovery.
        /// </summary>
        /// <param name="testHostManager">Test host manager for the current test discovery.</param>
        /// <returns>ITestDiscoveryManager object that can do discovery</returns>
        IProxyDiscoveryManager GetDiscoveryManager(ITestHostManager testHostManager, DiscoveryCriteria discoveryCriteria);

        /// <summary>
        /// Fetches the ExecutionManager for this engine. This manager would provide all functionality required for execution.
        /// </summary>
        /// <param name="testHostManager">Test host manager for current test run.</param>
        /// <param name="testRunCriteria">TestRunCriteria of the current test run</param>
        /// <returns>ITestExecutionManager object that can do execution</returns>
        IProxyExecutionManager GetExecutionManager(ITestHostManager testHostManager, TestRunCriteria testRunCriteria);

        /// <summary>
        /// Fetches the extension manager for this engine. This manager would provide extensibility
        /// features that this engine supports.
        /// </summary>
        /// <returns>ITestExtensionManager object that helps with extensibility</returns>
        ITestExtensionManager GetExtensionManager();

        /// <summary>
        /// Fetches the Test Host manager for this engine. This manager would provide extensibility
        /// features that this engine supports.
        /// </summary>
        /// <param name="runConfiguration">RunConfiguration information which contains info like Architecture, Framework for the test run.</param>
        /// <returns>Launcher for the test host process</returns>
        ITestHostManager GetDefaultTestHostManager(RunConfiguration runConfiguration);
    }
}
