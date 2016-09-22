// Copyright (c) Microsoft. All rights reserved.
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
        /// <returns>ITestDiscoveryManager object that can do discovery</returns>
        IProxyDiscoveryManager GetDiscoveryManager();

        /// <summary>
        /// Fetches the ExecutionManager for this engine. This manager would provide all functionality required for execution.
        /// </summary>
        /// <param name="testHostManager">Test host manager for current test run.</param>
        /// <param name="testRunCriteria">TestRunCriteria of the current test run</param>
        /// <returns>ITestExecutionManager object that can do execution</returns>
        IProxyExecutionManager GetExecutionManager(ITestHostManager testHostManager, TestRunCriteria testRunCriteria);

        /// <summary>
        /// Fetches the extension manager for this engine. This manager would provide extensibility features that this engine supports.
        /// </summary>
        /// <returns>ITestExtensionManager object that helps with extensibility</returns>
        ITestExtensionManager GetExtensionManager();

        /// <summary>
        /// Fetches the Test Host manager for this engine. This manager would provide extensibility features that this engine supports.
        /// </summary>
        /// <param name="architecture">Architecture of the test run</param>
        /// <returns>Launcher for the test host process</returns>
        ITestHostManager GetDefaultTestHostManager(Architecture architecture, Framework framework);
    }
}
