// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Cross Platform test engine entry point for the client.
    /// </summary>
    public class TestEngine : ITestEngine
    {
        #region Private Fields

        private readonly TestRuntimeProviderManager testHostProviderManager;
        private ITestExtensionManager testExtensionManager;
        private IProcessHelper processHelper;

        #endregion

        public TestEngine() : this(TestRuntimeProviderManager.Instance, new ProcessHelper())
        {
        }

        protected TestEngine(TestRuntimeProviderManager testHostProviderManager, IProcessHelper processHelper)
        {
            this.testHostProviderManager = testHostProviderManager;
            this.processHelper = processHelper;
        }

        #region ITestEngine implementation

        /// <summary>
        /// Fetches the DiscoveryManager for this engine. This manager would provide all functionality required for discovery.
        /// </summary>
        /// <param name="requestData">
        /// The request data for providing discovery services and data.
        /// </param>
        /// <param name="testHostManager">
        ///     Test host manager
        /// </param>
        /// <param name="discoveryCriteria">
        ///     The discovery Criteria.
        /// </param>
        /// <returns>
        /// ITestDiscoveryManager object that can do discovery
        /// </returns>
        public IProxyDiscoveryManager GetDiscoveryManager(IRequestData requestData, ITestRuntimeProvider testHostManager, DiscoveryCriteria discoveryCriteria)
        {
            var parallelLevel = this.VerifyParallelSettingAndCalculateParallelLevel(discoveryCriteria.Sources.Count(), discoveryCriteria.RunSettings);

            // Collecting IsParallel Enabled
            requestData.MetricsCollection.Add(TelemetryDataConstants.ParallelEnabledDuringDiscovery, parallelLevel > 1 ? "True" : "False");

            if (this.ShouldRunInNoIsolation(discoveryCriteria.RunSettings, parallelLevel > 1, false))
            {
                var isTelemetryOptedIn = requestData.IsTelemetryOptedIn;
                var newRequestData = this.GetRequestData(isTelemetryOptedIn);
                return new InProcessProxyDiscoveryManager(testHostManager, new TestHostManagerFactory(newRequestData));
            }

            Func<IProxyDiscoveryManager> proxyDiscoveryManagerCreator = delegate
            {
                var hostManager = this.testHostProviderManager.GetTestHostManagerByRunConfiguration(discoveryCriteria.RunSettings);
                hostManager?.Initialize(TestSessionMessageLogger.Instance, discoveryCriteria.RunSettings);

                return new ProxyDiscoveryManager(requestData, new TestRequestSender(requestData.ProtocolConfig, hostManager.GetTestHostConnectionInfo()), hostManager);
            };

            return !testHostManager.Shared ? new ParallelProxyDiscoveryManager(requestData, proxyDiscoveryManagerCreator, parallelLevel, sharedHosts: testHostManager.Shared) : proxyDiscoveryManagerCreator();
        }

        /// <summary>
        /// Fetches the ExecutionManager for this engine. This manager would provide all functionality required for execution.
        /// </summary>
        /// <param name="requestData">The request data for providing execution services and data</param>
        /// <param name="testHostManager">Test host manager.</param>
        /// <param name="testRunCriteria">Test run criterion.</param>
        /// <returns>
        /// ITestExecutionManager object that can do execution
        /// </returns>
        public IProxyExecutionManager GetExecutionManager(IRequestData requestData, ITestRuntimeProvider testHostManager, TestRunCriteria testRunCriteria)
        {
            var distinctSources = GetDistinctNumberOfSources(testRunCriteria);
            var parallelLevel = this.VerifyParallelSettingAndCalculateParallelLevel(distinctSources, testRunCriteria.TestRunSettings);

            // Collecting IsParallel Enabled
            requestData.MetricsCollection.Add(TelemetryDataConstants.ParallelEnabledDuringExecution, parallelLevel > 1 ? "True" : "False");

            var isDataCollectorEnabled = XmlRunSettingsUtilities.IsDataCollectionEnabled(testRunCriteria.TestRunSettings);

            var isInProcDataCollectorEnabled = XmlRunSettingsUtilities.IsInProcDataCollectionEnabled(testRunCriteria.TestRunSettings);

            if (this.ShouldRunInNoIsolation(testRunCriteria.TestRunSettings, parallelLevel > 1, isDataCollectorEnabled || isInProcDataCollectorEnabled))
            {
                var isTelemetryOptedIn = requestData.IsTelemetryOptedIn;
                var newRequestData = this.GetRequestData(isTelemetryOptedIn);
                return new InProcessProxyExecutionManager(testHostManager, new TestHostManagerFactory(newRequestData));
            }

            // SetupChannel ProxyExecutionManager with data collection if data collectors are specififed in run settings.
            Func<IProxyExecutionManager> proxyExecutionManagerCreator = delegate
            {
                // Create a new HostManager, to be associated with individual ProxyExecutionManager(&POM)
                var hostManager = this.testHostProviderManager.GetTestHostManagerByRunConfiguration(testRunCriteria.TestRunSettings);
                hostManager?.Initialize(TestSessionMessageLogger.Instance, testRunCriteria.TestRunSettings);

                if (testRunCriteria.TestHostLauncher != null)
                {
                    hostManager.SetCustomLauncher(testRunCriteria.TestHostLauncher);
                }

                var requestSender = new TestRequestSender(requestData.ProtocolConfig, hostManager.GetTestHostConnectionInfo());

                return isDataCollectorEnabled ? new ProxyExecutionManagerWithDataCollection(requestData, requestSender, hostManager, new ProxyDataCollectionManager(requestData, testRunCriteria.TestRunSettings, GetSourcesFromTestRunCriteria(testRunCriteria)))
                                                : new ProxyExecutionManager(requestData, requestSender, hostManager);
            };

            // parallelLevel = 1 for desktop should go via else route.
            if (parallelLevel > 1 || !testHostManager.Shared)
            {
                return new ParallelProxyExecutionManager(requestData, proxyExecutionManagerCreator, parallelLevel, sharedHosts: testHostManager.Shared);
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
        /// Fetches the logger manager for this engine. This manager will provide logger extensibility features that this engine supports.
        /// </summary>
        /// <param name="requestData">The request data for providing common execution services and data</param>
        /// <returns>ITestLoggerManager object that helps with logger extensibility.</returns>
        public ITestLoggerManager GetLoggerManager(IRequestData requestData)
        {
            return new TestLoggerManager(
                requestData,
                TestSessionMessageLogger.Instance, 
                new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
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

        private bool ShouldRunInNoIsolation(string runsettings, bool isParallelEnabled, bool isDataCollectorEnabled)
        {
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettings);

            if (runConfiguration.InIsolation)
            {
                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("TestEngine.ShouldRunInNoIsolation: running test in isolation");
                }
                return false;
            }

            // Run tests in isolation if run is authored using testsettings.
            if (InferRunSettingsHelper.IsTestSettingsEnabled(runsettings))
            {
                return false;
            }

            var currentProcessPath = this.processHelper.GetCurrentProcessFileName();

            // If running with the dotnet executable, then dont run in InProcess
            if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Return true if
            // 1) Not running in parallel
            // 2) Data collector is not enabled
            // 3) Target framework is x86 or anyCpu
            // 4) DisableAppDomain is false
            // 5) Not running in design mode
            // 6) target framework is NETFramework (Desktop test)
            if (!isParallelEnabled &&
                !isDataCollectorEnabled &&
                (runConfiguration.TargetPlatform == Architecture.X86 || runConfiguration.TargetPlatform == Architecture.AnyCPU) &&
                !runConfiguration.DisableAppDomain &&
                !runConfiguration.DesignMode &&
                runConfiguration.TargetFramework.Name.IndexOf("netframework", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("TestEngine.ShouldRunInNoIsolation: running test in process(inside vstest.console.exe process)");
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get Request Data on basis of Telemetry OptedIn or not
        /// </summary>
        /// <param name="isTelemetryOptedIn"></param>
        /// <returns></returns>
        private IRequestData GetRequestData(bool isTelemetryOptedIn)
        {
            return new RequestData
                       {
                           MetricsCollection = isTelemetryOptedIn
                                                   ? (IMetricsCollection)new MetricsCollection()
                                                   : new NoOpMetricsCollection(),
                           IsTelemetryOptedIn = isTelemetryOptedIn
                       };
        }

        /// <summary>
        /// Gets test sources from test run criteria
        /// </summary>
        /// <returns>test sources</returns>
        private IEnumerable<string> GetSourcesFromTestRunCriteria(TestRunCriteria testRunCriteria)
        {
            return testRunCriteria.HasSpecificTests ? testRunCriteria.Tests.Select(tc => tc.Source).Distinct() : testRunCriteria.Sources;
        }
    }
}
