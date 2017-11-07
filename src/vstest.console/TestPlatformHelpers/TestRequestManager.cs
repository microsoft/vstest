// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;

    /// <summary>
    /// Defines the TestRequestManger which can fire off discovery and test run requests
    /// </summary>
    internal class TestRequestManager : ITestRequestManager
    {
        private ITestPlatform testPlatform;

        private CommandLineOptions commandLineOptions;

        private TestLoggerManager testLoggerManager;

        private ITestPlatformEventSource testPlatformEventSource;

        private TestRunResultAggregator testRunResultAggregator;

        private static ITestRequestManager testRequestManagerInstance;

        private InferHelper inferHelper;

        private const int runRequestTimeout = 5000;

        private bool telemetryOptedIn;

        /// <summary>
        /// Maintains the current active execution request
        /// Assumption : There can only be one active execution request.
        /// </summary>
        private ITestRunRequest currentTestRunRequest;

        private readonly EventWaitHandle runRequestCreatedEventHandle = new AutoResetEvent(false);

        private object syncobject = new object();
        private object syncDispose = new object();

        private Task<IMetricsPublisher> metricsPublisher;

        private bool isDisposed;

        #region Constructor

        public TestRequestManager()
            : this(
                  CommandLineOptions.Instance,
                  TestPlatformFactory.GetTestPlatform(),
                  TestLoggerManager.Instance,
                  TestRunResultAggregator.Instance,
                  TestPlatformEventSource.Instance,
                  new InferHelper(new AssemblyMetadataProvider()),
                  MetricsPublisherFactory.GetMetricsPublisher(IsTelemetryOptedIn(), CommandLineOptions.Instance.IsDesignMode))
        {
        }

        internal TestRequestManager(CommandLineOptions commandLineOptions, ITestPlatform testPlatform, TestLoggerManager testLoggerManager, TestRunResultAggregator testRunResultAggregator, ITestPlatformEventSource testPlatformEventSource, InferHelper inferHelper, Task<IMetricsPublisher> metricsPublisher)
        {
            this.testPlatform = testPlatform;
            this.commandLineOptions = commandLineOptions;
            this.testLoggerManager = testLoggerManager;
            this.testRunResultAggregator = testRunResultAggregator;
            this.testPlatformEventSource = testPlatformEventSource;
            this.inferHelper = inferHelper;
            this.metricsPublisher = metricsPublisher;

            // Always enable logging for discovery or run requests
            this.testLoggerManager.EnableLogging();

            if (!this.commandLineOptions.IsDesignMode)
            {
                var consoleLogger = new ConsoleLogger();
                this.testLoggerManager.AddLogger(consoleLogger, ConsoleLogger.ExtensionUri, null);
            }
        }

        #endregion

        public static ITestRequestManager Instance
        {
            get
            {
                if (testRequestManagerInstance == null)
                {
                    testRequestManagerInstance = new TestRequestManager();
                }

                return testRequestManagerInstance;
            }
        }

        #region ITestRequestManager

        /// <inheritdoc />
        public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions, bool skipExtensionFilters)
        {
            // It is possible for an Editor/IDE to keep running the runner in design mode for long duration.
            // We clear the extensions cache to ensure the extensions don't get reused across discovery/run
            // requests.
            EqtTrace.Info("TestRequestManager.InitializeExtensions: Initialize extensions started.");
            this.testPlatform.ClearExtensions();
            this.testPlatform.UpdateExtensions(pathToAdditionalExtensions, skipExtensionFilters);
            EqtTrace.Info("TestRequestManager.InitializeExtensions: Initialize extensions completed.");
        }

        /// <summary>
        /// Resets the command options
        /// </summary>
        public void ResetOptions()
        {
            this.commandLineOptions.Reset();
        }

        /// <summary>
        /// Discover Tests given a list of sources, run settings.
        /// </summary>
        /// <param name="discoveryPayload">Discovery payload</param>
        /// <param name="discoveryEventsRegistrar">EventHandler for discovered tests</param>
        /// <param name="protocolConfig">Protocol related information</param>
        /// <returns>True, if successful</returns>
        public bool DiscoverTests(DiscoveryRequestPayload discoveryPayload, ITestDiscoveryEventsRegistrar discoveryEventsRegistrar, ProtocolConfig protocolConfig)
        {
            EqtTrace.Info("TestRequestManager.DiscoverTests: Discovery tests started.");

            bool success = false;
            var runsettings = discoveryPayload.RunSettings;

            if (discoveryPayload.TestPlatformOptions != null)
            {
                this.telemetryOptedIn = discoveryPayload.TestPlatformOptions.CollectMetrics;
            }

            var requestData = this.GetRequestData(protocolConfig);
            if (this.UpdateRunSettingsIfRequired(runsettings, discoveryPayload.Sources?.ToList(), out string updatedRunsettings))
            {
                runsettings = updatedRunsettings;
            }

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettings);
            var batchSize = runConfiguration.BatchSize;

            if (requestData.IsTelemetryOptedIn)
            {
                // Collect Metrics
                this.CollectMetrics(requestData, runConfiguration);

                // Collect Commands
                this.LogCommandsTelemetryPoints(requestData);
            }

            // create discovery request
            var criteria = new DiscoveryCriteria(discoveryPayload.Sources, batchSize, this.commandLineOptions.TestStatsEventTimeout, runsettings);
            criteria.TestCaseFilter = this.commandLineOptions.TestCaseFilterValue;

            try
            {
                using (IDiscoveryRequest discoveryRequest = this.testPlatform.CreateDiscoveryRequest(requestData, criteria))
                {
                    try
                    {
                        this.testLoggerManager?.RegisterDiscoveryEvents(discoveryRequest);
                        discoveryEventsRegistrar?.RegisterDiscoveryEvents(discoveryRequest);

                        this.testPlatformEventSource.DiscoveryRequestStart();

                        discoveryRequest.DiscoverAsync();
                        discoveryRequest.WaitForCompletion();

                        success = true;
                    }

                    finally
                    {
                        this.testLoggerManager?.UnregisterDiscoveryEvents(discoveryRequest);
                        discoveryEventsRegistrar?.UnregisterDiscoveryEvents(discoveryRequest);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is TestPlatformException ||
                    ex is SettingsException ||
                    ex is InvalidOperationException)
                {
                    LoggerUtilities.RaiseTestRunError(testLoggerManager, null, ex);
                    success = false;
                }
                else
                {
                    throw;
                }
            }

            EqtTrace.Info("TestRequestManager.DiscoverTests: Discovery tests completed, successful: {0}.", success);
            this.testPlatformEventSource.DiscoveryRequestStop();

            // Posts the Discovery Complete event.
            this.metricsPublisher.Result.PublishMetrics(TelemetryDataConstants.TestDiscoveryCompleteEvent, requestData.MetricsCollection.Metrics);

            return success;
        }

        /// <summary>
        /// Run Tests with given a set of test cases.
        /// </summary>
        /// <param name="testRunRequestPayload">TestRun request Payload</param>
        /// <param name="testHostLauncher">TestHost Launcher for the run</param>
        /// <param name="testRunEventsRegistrar">event registrar for run events</param>
        /// <param name="protocolConfig">Protocol related information</param>
        /// <returns>True, if successful</returns>
        public bool RunTests(TestRunRequestPayload testRunRequestPayload, ITestHostLauncher testHostLauncher, ITestRunEventsRegistrar testRunEventsRegistrar, ProtocolConfig protocolConfig)
        {
            EqtTrace.Info("TestRequestManager.RunTests: run tests started.");

            TestRunCriteria runCriteria = null;
            var runsettings = testRunRequestPayload.RunSettings;

            if (testRunRequestPayload.TestPlatformOptions != null)
            {
                this.telemetryOptedIn = testRunRequestPayload.TestPlatformOptions.CollectMetrics;
            }

            var requestData = this.GetRequestData(protocolConfig);

            // Get sources to auto detect fx and arch for both run selected or run all scenario.
            var sources = GetSources(testRunRequestPayload);

            if (this.UpdateRunSettingsIfRequired(runsettings, sources, out string updatedRunsettings))
            {
                runsettings = updatedRunsettings;
            }

            if (InferRunSettingsHelper.IsTestSettingsEnabled(runsettings))
            {
                bool throwException = false;
                if (this.commandLineOptions.EnableCodeCoverage)
                {
                    var dataCollectorsFriendlyNames = XmlRunSettingsUtilities.GetDataCollectorsFriendlyName(runsettings);
                    if (dataCollectorsFriendlyNames.Count >= 2)
                    {
                        throwException = true;
                    }

                }
                else if (XmlRunSettingsUtilities.IsDataCollectionEnabled(runsettings))
                {
                    throwException = true;
                }

                if (throwException)
                {
                    throw new SettingsException(string.Format(Resources.RunsettingsWithDCErrorMessage, runsettings));
                }
            }

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettings);
            var batchSize = runConfiguration.BatchSize;

            if (requestData.IsTelemetryOptedIn)
            {
                // Collect Metrics
                this.CollectMetrics(requestData, runConfiguration);

                // Collect Commands
                this.LogCommandsTelemetryPoints(requestData);
            }

            if (!commandLineOptions.IsDesignMode)
            {
                // Generate fakes settings only for command line scenarios. In case of
                // Editors/IDEs, this responsibility is with the caller.
                GenerateFakesUtilities.GenerateFakesSettings(this.commandLineOptions, this.commandLineOptions.Sources.ToList(), ref runsettings);
            }

            if (testRunRequestPayload.Sources != null && testRunRequestPayload.Sources.Any())
            {
                runCriteria = new TestRunCriteria(
                                  testRunRequestPayload.Sources,
                                  batchSize,
                                  testRunRequestPayload.KeepAlive,
                                  runsettings,
                                  this.commandLineOptions.TestStatsEventTimeout,
                                  testHostLauncher);
                runCriteria.TestCaseFilter = testRunRequestPayload.TestPlatformOptions?.TestCaseFilter;
                runCriteria.FilterOptions = testRunRequestPayload.TestPlatformOptions?.FilterOptions;
            }
            else
            {
                runCriteria = new TestRunCriteria(
                                  testRunRequestPayload.TestCases,
                                  batchSize,
                                  testRunRequestPayload.KeepAlive,
                                  runsettings,
                                  this.commandLineOptions.TestStatsEventTimeout,
                                  testHostLauncher);
            }

            var success = this.RunTests(requestData, runCriteria, testRunEventsRegistrar);
            EqtTrace.Info("TestRequestManager.RunTests: run tests completed, sucessful: {0}.", success);
            this.testPlatformEventSource.ExecutionRequestStop();

            // Post the run complete event
            this.metricsPublisher.Result.PublishMetrics(TelemetryDataConstants.TestExecutionCompleteEvent, requestData.MetricsCollection.Metrics);

            return success;
        }

        /// <summary>
        /// Cancel the test run.
        /// </summary>
        public void CancelTestRun()
        {
            EqtTrace.Info("TestRequestManager.CancelTestRun: Sending cancel request.");

            lock (syncDispose)
            {
                this.runRequestCreatedEventHandle.WaitOne(runRequestTimeout);
                this.currentTestRunRequest?.CancelAsync();
            }
        }

        /// <summary>
        /// Aborts the test run.
        /// </summary>
        public void AbortTestRun()
        {
            EqtTrace.Info("TestRequestManager.AbortTestRun: Sending abort request.");

            lock (syncDispose)
            {
                this.runRequestCreatedEventHandle.WaitOne(runRequestTimeout);
                this.currentTestRunRequest?.Abort();
            }
        }

        #endregion

        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.metricsPublisher.Result.Dispose();
                }

                this.isDisposed = true;
            }
        }

        private bool UpdateRunSettingsIfRequired(string runsettingsXml, List<string> sources, out string updatedRunSettingsXml)
        {
            bool settingsUpdated = false;
            updatedRunSettingsXml = runsettingsXml;
            IDictionary<string, Architecture> sourcePlatforms = new Dictionary<string, Architecture>();
            IDictionary<string, Framework> sourceFrameworks = new Dictionary<string, Framework>();

            if (!string.IsNullOrEmpty(runsettingsXml))
            {
                // TargetFramework is full CLR. Set DesignMode based on current context.
                using (var stream = new StringReader(runsettingsXml))
                using (var reader = XmlReader.Create(stream, XmlRunSettingsUtilities.ReaderSettings))
                {
                    var document = new XmlDocument();
                    document.Load(reader);

                    var navigator = document.CreateNavigator();

                    var inferedFramework = inferHelper.AutoDetectFramework(sources, sourceFrameworks);
                    Framework chosenFramework;
                    var inferedPlatform = inferHelper.AutoDetectArchitecture(sources, sourcePlatforms);
                    Architecture chosenPlatform;

                    // Update frmaework and platform if required. For commandline scenario update happens in ArgumentProcessor.
                    bool updateFramework = IsAutoFrameworkDetectRequired(navigator, out chosenFramework);
                    bool updatePlatform = IsAutoPlatformDetectRequired(navigator, out chosenPlatform);

                    if (updateFramework)
                    {
                        InferRunSettingsHelper.UpdateTargetFramework(document, inferedFramework?.ToString(), overwrite: true);
                        chosenFramework = inferedFramework;
                        settingsUpdated = true;
                    }

                    if (updatePlatform)
                    {
                        InferRunSettingsHelper.UpdateTargetPlatform(document, inferedPlatform.ToString(), overwrite: true);
                        chosenPlatform = inferedPlatform;
                        settingsUpdated = true;
                    }

                    var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(chosenPlatform, chosenFramework, sourcePlatforms, sourceFrameworks, out var incompatibleSettingWarning);

                    if (!string.IsNullOrEmpty(incompatibleSettingWarning))
                    {
                        EqtTrace.Info(incompatibleSettingWarning);
                        LoggerUtilities.RaiseTestRunWarning(this.testLoggerManager, this.testRunResultAggregator, incompatibleSettingWarning);
                    }

                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Info("Compatible sources list : ");
                        EqtTrace.Info(string.Join("\n", compatibleSources.ToArray()));
                    }

                    // If user is already setting DesignMode via runsettings or CLI args; we skip.
                    var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);

                    if (!runConfiguration.DesignModeSet)
                    {
                        InferRunSettingsHelper.UpdateDesignMode(document, this.commandLineOptions.IsDesignMode);
                        settingsUpdated = true;
                    }

                    if (!runConfiguration.CollectSourceInformationSet)
                    {
                        InferRunSettingsHelper.UpdateCollectSourceInformation(document, this.commandLineOptions.ShouldCollectSourceInformation);
                        settingsUpdated = true;
                    }

                    if (InferRunSettingsHelper.TryGetDeviceXml(navigator, out string deviceXml))
                    {
                        InferRunSettingsHelper.UpdateTargetDevice(document, deviceXml);
                        settingsUpdated = true;
                    }

                    updatedRunSettingsXml = navigator.OuterXml;
                }
            }

            return settingsUpdated;
        }

        private bool RunTests(IRequestData requestData, TestRunCriteria testRunCriteria, ITestRunEventsRegistrar testRunEventsRegistrar)
        {
            // Make sure to run the run request inside a lock as the below section is not thread-safe
            // TranslationLayer can process faster as it directly gets the raw unserialized messages whereas 
            // below logic needs to deserialize and do some cleanup
            // While this section is cleaning up, TranslationLayer can trigger run causing multiple threads to run the below section at the same time
            lock (syncobject)
            {
                bool success = true;

                try
                {
                    this.currentTestRunRequest = this.testPlatform.CreateTestRunRequest(requestData, testRunCriteria);
                    this.runRequestCreatedEventHandle.Set();

                    try
                    {
                        this.testLoggerManager.RegisterTestRunEvents(this.currentTestRunRequest);
                        this.testRunResultAggregator.RegisterTestRunEvents(this.currentTestRunRequest);
                        testRunEventsRegistrar?.RegisterTestRunEvents(this.currentTestRunRequest);

                        this.testPlatformEventSource.ExecutionRequestStart();

                        this.currentTestRunRequest.ExecuteAsync();

                        // Wait for the run completion event
                        this.currentTestRunRequest.WaitForCompletion();
                    }
                    finally
                    {
                        this.testLoggerManager.UnregisterTestRunEvents(this.currentTestRunRequest);
                        this.testRunResultAggregator.UnregisterTestRunEvents(this.currentTestRunRequest);
                        testRunEventsRegistrar?.UnregisterTestRunEvents(this.currentTestRunRequest);

                        lock(syncDispose)
                        {
                            this.currentTestRunRequest.Dispose();
                            this.currentTestRunRequest = null;
                        }
                    }

                }
                catch (Exception ex)
                {
                    EqtTrace.Error("TestRequestManager.RunTests: failed to run tests: {0}", ex);
                    if (ex is TestPlatformException ||
                        ex is SettingsException ||
                        ex is InvalidOperationException)
                    {
                        LoggerUtilities.RaiseTestRunError(this.testLoggerManager, this.testRunResultAggregator, ex);
                        success = false;
                    }
                    else
                    {
                        throw;
                    }
                }

                return success;
            }
        }

        private bool IsAutoFrameworkDetectRequired(XPathNavigator navigator, out Framework chosenFramework)
        {
            bool required = true;
            chosenFramework = null;
            if (commandLineOptions.IsDesignMode)
            {
                bool isValidFx =
                    InferRunSettingsHelper.TryGetFrameworkXml(navigator, out var frameworkFromrunsettingsXml);
                required = !isValidFx || string.IsNullOrWhiteSpace(frameworkFromrunsettingsXml);
                if (!required)
                {
                    chosenFramework = Framework.FromString(frameworkFromrunsettingsXml);
                }
            }
            else if (!commandLineOptions.IsDesignMode && commandLineOptions.FrameworkVersionSpecified)
            {
                required = false;
                chosenFramework = commandLineOptions.TargetFrameworkVersion;
            }

            return required;
        }

        private bool IsAutoPlatformDetectRequired(XPathNavigator navigator, out Architecture chosenPlatform)
        {
            bool required = true;
            chosenPlatform = Architecture.Default;
            if (commandLineOptions.IsDesignMode)
            {
                bool isValidPlatform = InferRunSettingsHelper.TryGetPlatformXml(navigator, out var platformXml);
                required = !isValidPlatform || string.IsNullOrWhiteSpace(platformXml);
                if (!required)
                {
                    chosenPlatform = (Architecture)Enum.Parse(typeof(Architecture), platformXml, true);
                }
            }
            else if (!commandLineOptions.IsDesignMode && commandLineOptions.ArchitectureSpecified)
            {
                required = false;
                chosenPlatform = commandLineOptions.TargetArchitecture;
            }

            return required;
        }

        /// <summary>
        /// Collect Metrics
        /// </summary>
        /// <param name="requestData">Request Data for common Discovery/Execution Services</param>
        /// <param name="runConfiguration">RunConfiguration</param>
        private void CollectMetrics(IRequestData requestData, RunConfiguration runConfiguration)
        {
            // Collecting Target Framework.
            requestData.MetricsCollection.Add(TelemetryDataConstants.TargetFramework, runConfiguration.TargetFrameworkVersion.Name);

            // Collecting Target Platform.
            requestData.MetricsCollection.Add(TelemetryDataConstants.TargetPlatform, runConfiguration.TargetPlatform.ToString());

            // Collecting Max Cpu count.
            requestData.MetricsCollection.Add(TelemetryDataConstants.MaxCPUcount, runConfiguration.MaxCpuCount);

            // Collecting Target Device. Here, it will be updated run settings so, target device will be under runconfiguration only.
            var targetDevice = runConfiguration.TargetDevice;
            if (string.IsNullOrEmpty(targetDevice))
            {
                requestData.MetricsCollection.Add(TelemetryDataConstants.TargetDevice, "Local Machine");
            }
            else if (targetDevice.Equals("Device", StringComparison.Ordinal) || targetDevice.Contains("Emulator"))
            {
                requestData.MetricsCollection.Add(TelemetryDataConstants.TargetDevice, targetDevice);
            }
            else
            {
                // For IOT scenarios
                requestData.MetricsCollection.Add(TelemetryDataConstants.TargetDevice, "Other");
            }

            // Collecting TestPlatform Version
            requestData.MetricsCollection.Add(TelemetryDataConstants.TestPlatformVersion, Product.Version);

            // Collecting TargetOS
            requestData.MetricsCollection.Add(TelemetryDataConstants.TargetOS, new PlatformEnvironment().OperatingSystemVersion);
        }

        /// <summary>
        /// Checks whether Telemetry opted in or not. 
        /// By Default opting out
        /// </summary>
        /// <returns>Returns Telemetry Opted out or not</returns>
        private static bool IsTelemetryOptedIn()
        {
            var telemetryStatus = Environment.GetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN");
            return !string.IsNullOrEmpty(telemetryStatus) && telemetryStatus.Equals("1", StringComparison.Ordinal);
        }

        /// <summary>
        /// Log Command Line switches for Telemetry purposes
        /// </summary>
        /// <param name="requestData">Request Data providing common discovery/execution services.</param>
        private void LogCommandsTelemetryPoints(IRequestData requestData)
        {
            var commandsUsed = new List<string>();

            var parallel = this.commandLineOptions.Parallel;
            if (parallel)
            {
                commandsUsed.Add("/Parallel");
            }

            var platform = this.commandLineOptions.ArchitectureSpecified;
            if (platform)
            {
                commandsUsed.Add("/Platform");
            }

            var enableCodeCoverage = this.commandLineOptions.EnableCodeCoverage;
            if (enableCodeCoverage)
            {
                commandsUsed.Add("/EnableCodeCoverage");
            }

            var inIsolation = this.commandLineOptions.InIsolation;
            if (inIsolation)
            {
                commandsUsed.Add("/InIsolation");
            }

            var useVsixExtensions = this.commandLineOptions.UseVsixExtensions;
            if (useVsixExtensions)
            {
                commandsUsed.Add("/UseVsixExtensions");
            }

            var frameworkVersionSpecified = this.commandLineOptions.FrameworkVersionSpecified;
            if (frameworkVersionSpecified)
            {
                commandsUsed.Add("/Framework");
            }

            var settings = this.commandLineOptions.SettingsFile;
            if (!string.IsNullOrEmpty(settings))
            {
                var extension = Path.GetExtension(settings);
                if (string.Equals(extension, ".runsettings", StringComparison.OrdinalIgnoreCase))
                {
                    commandsUsed.Add("/settings//.RunSettings");
                }
                else if (string.Equals(extension, ".testsettings", StringComparison.OrdinalIgnoreCase))
                {
                    commandsUsed.Add("/settings//.TestSettings");
                }
                else if (string.Equals(extension, ".vsmdi", StringComparison.OrdinalIgnoreCase))
                {
                    commandsUsed.Add("/settings//.vsmdi");
                }
                else if (string.Equals(extension, ".testrunConfig", StringComparison.OrdinalIgnoreCase))
                {
                    commandsUsed.Add("/settings//.testrunConfig");
                }
            }

            requestData.MetricsCollection.Add(TelemetryDataConstants.CommandLineSwitches, string.Join(",", commandsUsed.ToArray()));
        }

        /// <summary>
        /// Gets Request Data
        /// </summary>
        /// <param name="protocolConfig">Protocol Config</param>
        /// <returns></returns>
        private IRequestData GetRequestData(ProtocolConfig protocolConfig)
        {
            return new RequestData
            {
                ProtocolConfig = protocolConfig,
                MetricsCollection =
                               this.telemetryOptedIn || IsTelemetryOptedIn()
                                   ? (IMetricsCollection)new MetricsCollection()
                                   : new NoOpMetricsCollection(),
                IsTelemetryOptedIn = this.telemetryOptedIn || IsTelemetryOptedIn()
            };
        }

        private List<String> GetSources(TestRunRequestPayload testRunRequestPayload)
        {
            List<string> sources = new List<string>();
            if (testRunRequestPayload.Sources != null && testRunRequestPayload.Sources.Count > 0)
            {
                sources = testRunRequestPayload.Sources;
            }
            else if (testRunRequestPayload.TestCases != null && testRunRequestPayload.TestCases.Count > 0)
            {
                ISet<string> sourcesSet = new HashSet<string>();
                foreach (var testCase in testRunRequestPayload.TestCases)
                {
                    sourcesSet.Add(testCase.Source);
                }
                sources = sourcesSet.ToList();
            }
            return sources;
        }
    }
}
