// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Cross Platform test engine entry point for the client.
    /// </summary>
    public class TestEngine : ITestEngine
    {
        #region Private Fields

        private ITestExtensionManager testExtensionManager;

        #endregion

        #region ITestEngine implementation

        /// <summary>
        /// Fetches the DiscoveryManager for this engine. This manager would provide all functionality required for discovery.
        /// </summary>
        /// <param name="testHostManager">
        /// Test host manager
        /// </param>
        /// <param name="discoveryCriteria">
        /// The discovery Criteria.
        /// </param>
        /// <returns>
        /// ITestDiscoveryManager object that can do discovery
        /// </returns>
        public IProxyDiscoveryManager GetDiscoveryManager(ITestRuntimeProvider testHostManager, DiscoveryCriteria discoveryCriteria)
        {
            var parallelLevel = this.VerifyParallelSettingAndCalculateParallelLevel(discoveryCriteria.Sources.Count(), discoveryCriteria.RunSettings);

            Func<IProxyDiscoveryManager> proxyDiscoveryManagerCreator = delegate
            {
                // Create a new HostProvider, to be associated with individual ProxyDiscoveryManager(&POM)
                var hostManager = this.GetDefaultTestHostManager(XmlRunSettingsUtilities.GetRunConfigurationNode(discoveryCriteria.RunSettings));
                hostManager.Initialize(TestSessionMessageLogger.Instance);

                return new ProxyDiscoveryManager(hostManager);
            };
            return !testHostManager.Shared ? new ParallelProxyDiscoveryManager(proxyDiscoveryManagerCreator, parallelLevel, sharedHosts: testHostManager.Shared) : proxyDiscoveryManagerCreator();
        }

        /// <summary>
        /// Fetches the ExecutionManager for this engine. This manager would provide all functionality required for execution.
        /// </summary>
        /// <param name="testHostManager">Test host manager.</param>
        /// <param name="testRunCriteria">Test run criterion.</param>
        /// <returns>
        /// ITestExecutionManager object that can do execution
        /// </returns>
        public IProxyExecutionManager GetExecutionManager(ITestRuntimeProvider testHostManager, TestRunCriteria testRunCriteria)
        {
            var distinctSources = GetDistinctNumberOfSources(testRunCriteria);
            var parallelLevel = this.VerifyParallelSettingAndCalculateParallelLevel(distinctSources, testRunCriteria.TestRunSettings);

            var isDataCollectorEnabled = XmlRunSettingsUtilities.IsDataCollectionEnabled(testRunCriteria.TestRunSettings);

            // SetupChannel ProxyExecutionManager with data collection if data collectors are specififed in run settings.
            Func<IProxyExecutionManager> proxyExecutionManagerCreator = delegate
            {
                // Create a new HostManager, to be associated with individual ProxyExecutionManager(&POM)
                var hostManager = this.GetDefaultTestHostManager(XmlRunSettingsUtilities.GetRunConfigurationNode(testRunCriteria.TestRunSettings));
                hostManager.Initialize(TestSessionMessageLogger.Instance);

                if (testRunCriteria.TestHostLauncher != null)
                {
                    hostManager.SetCustomLauncher(testRunCriteria.TestHostLauncher);
                }

                return isDataCollectorEnabled ? new ProxyExecutionManagerWithDataCollection(hostManager, new ProxyDataCollectionManager(testRunCriteria.TestRunSettings))
                                                : new ProxyExecutionManager(hostManager);
            };
                    
            // parallelLevel = 1 for desktop should go via else route.
            if (parallelLevel > 1 || !testHostManager.Shared)
            {
                return new ParallelProxyExecutionManager(proxyExecutionManagerCreator, parallelLevel, sharedHosts: testHostManager.Shared);
            }
            else
            {
                return proxyExecutionManagerCreator();
            }
        }

        /// <summary>
        /// Fetches the extension manager for this engine. This manager would provide extensibility features that this engine supports.
        /// </summary>
        /// <returns>ITestExtensionManager object that helps with extensibility</returns>
        public ITestExtensionManager GetExtensionManager()
        {
            return this.testExtensionManager ?? (this.testExtensionManager = new TestExtensionManager());
        }

        /// <summary>
        /// Retrieves the default test host manager for this engine.
        /// </summary>
        /// <param name="runConfiguration">
        /// The run Configuration.
        /// </param>
        /// <returns>
        /// An instance of the test host manager.
        /// </returns>
        public ITestRuntimeProvider GetDefaultTestHostManager(RunConfiguration runConfiguration)
        {
            var framework = runConfiguration.TargetFrameworkVersion;

            // This is expected to be called once every run so returning a new instance every time.
            if (framework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                || framework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new DotnetTestHostManager();
            }

            // Only share the manager if DisableAppDomain is "false"
            // meaning AppDomain is enabled and we can reuse the host for multiple sources
            return new DefaultTestHostManager(runConfiguration.TargetPlatform, framework, shared: !runConfiguration.DisableAppDomain);
        }

        #endregion

        private static int GetDistinctNumberOfSources(TestRunCriteria testRunCriteria)
        {
            // No point in creating more processes if number of sources is less than what user configured for
            int numSources = 1;
            if (testRunCriteria.HasSpecificTests)
            {
                numSources = new System.Collections.Generic.HashSet<string>(
                    testRunCriteria.Tests.Select((testCase) => testCase.Source)).Count;
            }
            else
            {
                numSources = testRunCriteria.Sources.Count();
            }

            return numSources;
        }

        /// <summary>
        /// Verifies Parallel Setting and returns parallel level to use based on the run criteria
        /// </summary>
        /// <param name="sourceCount">
        /// The source Count.
        /// </param>
        /// <param name="runSettings">
        /// The run Settings.
        /// </param>
        /// <returns>
        /// Parallel Level to use
        /// </returns>
        private int VerifyParallelSettingAndCalculateParallelLevel(int sourceCount, string runSettings)
        {
            // Default is 1
            int parallelLevelToUse = 1;
            try
            {
                // Check the User Parallel Setting
                int userParallelSetting = RunSettingsUtilities.GetMaxCpuCount(runSettings);
                parallelLevelToUse = userParallelSetting == 0 ? Environment.ProcessorCount : userParallelSetting;
                var enableParallel = parallelLevelToUse > 1;

                EqtTrace.Verbose("TestEngine: Initializing Parallel Execution as MaxCpuCount is set to: {0}", parallelLevelToUse);

                // Verify if the number of Sources is less than user setting of parallel
                // we should use number of sources as the parallel level, if sources count is less than parallel level
                if (enableParallel)
                {
                    parallelLevelToUse = Math.Min(sourceCount, parallelLevelToUse);

                    // If only one source, no need to use parallel service client
                    enableParallel = parallelLevelToUse > 1;

                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Verbose("TestEngine: ParallelExecution set to '{0}' as the parallel level is adjusted to '{1}' based on number of sources", enableParallel, parallelLevelToUse);
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("TestEngine: Error occured while initializing ParallelExecution: {0}", ex);
                EqtTrace.Warning("TestEngine: Defaulting to Sequential Execution");

                parallelLevelToUse = 1;
            }

            return parallelLevelToUse;
        }
    }
}
