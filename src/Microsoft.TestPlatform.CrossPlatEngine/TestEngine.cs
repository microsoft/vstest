// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
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
        /// <param name="testHostManager"></param>
        /// <returns>ITestDiscoveryManager object that can do discovery</returns>
        public IProxyDiscoveryManager GetDiscoveryManager(ITestHostManager testHostManager)
        {
            Func<IProxyDiscoveryManager> proxyDiscoveryManagerCreator = () => new ProxyDiscoveryManager(testHostManager);
            if (!testHostManager.Shared)
            {
                return new ParallelProxyDiscoveryManager(proxyDiscoveryManagerCreator, parallelLevel: Environment.ProcessorCount, reuseHosts: testHostManager.Shared);
            }
            else
            {
                return proxyDiscoveryManagerCreator();
            }
        }

        /// <summary>
        /// Fetches the ExecutionManager for this engine. This manager would provide all functionality required for execution.
        /// </summary>
        /// <param name="testHostManager">Test host manager.</param>
        /// <param name="testRunCriteria">Test run criterion.</param>
        /// <returns>
        /// ITestExecutionManager object that can do execution
        /// </returns>
        public IProxyExecutionManager GetExecutionManager(ITestHostManager testHostManager, TestRunCriteria testRunCriteria)
        {
            int parallelLevel = this.VerifyParallelSettingAndCalculateParallelLevel(testRunCriteria);

            var runconfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(testRunCriteria.TestRunSettings);
            var architecture = runconfiguration.TargetPlatform;
            var isDataCollectorEnabled = XmlRunSettingsUtilities.IsDataCollectionEnabled(testRunCriteria.TestRunSettings);

            // SetupChannel ProxyExecutionManager with data collection if data collectors are specififed in run settings.
            Func<IProxyExecutionManager> proxyExecutionManagerCreator =
                () =>
                    isDataCollectorEnabled
                        ? new ProxyExecutionManagerWithDataCollection(testHostManager, this.GetDataCollectionManager(architecture, testRunCriteria.TestRunSettings))
                        : new ProxyExecutionManager(testHostManager);

            if (parallelLevel > 1)
            {
                return new ParallelProxyExecutionManager(proxyExecutionManagerCreator, parallelLevel);
            }
            else if(!testHostManager.Shared)
            {
                // If testHostManager is not shared, do not reuse hosts
                return new ParallelProxyExecutionManager(proxyExecutionManagerCreator, parallelLevel: Environment.ProcessorCount, reuseHosts: false);
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
        /// <param name="architecture">The architecture we want the test host manager for.</param>
        /// <param name="framework">Framework for the test session.</param>
        /// <returns>An instance of the test host manager.</returns>
        public ITestHostManager GetDefaultTestHostManager(Architecture architecture, Framework framework)
        {
            // This is expected to be called once every run so returning a new instance every time.
            if (framework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                || framework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new DotnetTestHostManager();
            }

            return new DefaultTestHostManager(architecture, framework);
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
        /// <param name="testRunCriteria">Test Run Criteria</param>
        /// <returns>Parallel Level to use</returns>
        private int VerifyParallelSettingAndCalculateParallelLevel(TestRunCriteria testRunCriteria)
        {
            // Default is 1
            int parallelLevelToUse = 1;
            try
            {
                // Check the User Parallel Setting
                int userParallelSetting = RunSettingsUtilities.GetMaxCpuCount(testRunCriteria.TestRunSettings);
                parallelLevelToUse = userParallelSetting == 0 ? Environment.ProcessorCount : userParallelSetting;
                var enableParallel = parallelLevelToUse > 1;

                EqtTrace.Verbose("TestEngine: Initializing Parallel Execution as MaxCpuCount is set to: {0}", parallelLevelToUse);

                // Verify if the number of Sources is less than user setting of parallel
                // we should use number of sources as the parallel level, if sources count is less than parallel level
                if (enableParallel)
                {
                    parallelLevelToUse = Math.Min(GetDistinctNumberOfSources(testRunCriteria), parallelLevelToUse);

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

        private IProxyDataCollectionManager GetDataCollectionManager(Architecture architecture, string settingsXml)
        {
            try
            {
                return new ProxyDataCollectionManager(architecture, settingsXml);
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("TestEngine: Error occured while initializing DataCollection Process: {0}", ex);
                }

                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("TestEngine: Skipping Data Collection");
                }

                return null;
            }
        }
    }
}
