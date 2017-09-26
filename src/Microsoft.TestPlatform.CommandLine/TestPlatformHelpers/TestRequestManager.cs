// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.TestPlatformHelpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
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
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using Microsoft.TestPlatform.CommandLine.Internal;
    using Microsoft.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.TestPlatform.CommandLine.Publisher;

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

        private const int runRequestTimeout = 5000;

        private bool telemetryOptedIn;

        /// <summary>
        /// Maintains the current active execution request
        /// Assumption : There can only be one active execution request.
        /// </summary>
        private ITestRunRequest currentTestRunRequest;

        private readonly EventWaitHandle runRequestCreatedEventHandle = new AutoResetEvent(false);

        private object syncobject = new object();

        #region Constructor

        public TestRequestManager()
            : this(
                CommandLineOptions.Instance,
                TestPlatformFactory.GetTestPlatform(),
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                TestPlatformEventSource.Instance)
        {
        }

        internal TestRequestManager(CommandLineOptions commandLineOptions, ITestPlatform testPlatform, TestLoggerManager testLoggerManager, TestRunResultAggregator testRunResultAggregator, ITestPlatformEventSource testPlatformEventSource)
        {
            this.testPlatform = testPlatform;
            this.commandLineOptions = commandLineOptions;
            this.testLoggerManager = testLoggerManager;
            this.testRunResultAggregator = testRunResultAggregator;
            this.testPlatformEventSource = testPlatformEventSource;
            this.telemetryOptedIn = IsTelemetryOptedIn();

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

        /// <summary>
        /// Initializes the extensions while probing additional paths.
        /// </summary>
        /// <param name="pathToAdditionalExtensions">Paths to Additional extensions</param>
        public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
        {
            EqtTrace.Info("TestRequestManager.InitializeExtensions: Initialize extensions started.");
            this.testPlatform.ClearExtensions();
            this.testPlatform.UpdateExtensions(pathToAdditionalExtensions, false);
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

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(discoveryPayload.RunSettings);
            var batchSize = runConfiguration.BatchSize;

            var runsettings = discoveryPayload.RunSettings;
            var requestData = this.GetRequestData(protocolConfig);
            var metricsPublisher = this.telemetryOptedIn ? (IMetricsPublisher)new MetricsPublisher() : new NoOpMetricsPublisher();

            if (this.UpdateRunSettingsIfRequired(runsettings, out string updatedRunsettings))
            {
                runsettings = updatedRunsettings;
            }

            // create discovery request
            var criteria = new DiscoveryCriteria(discoveryPayload.Sources, batchSize, this.commandLineOptions.TestStatsEventTimeout, runsettings);
            criteria.TestCaseFilter = this.commandLineOptions.TestCaseFilterValue;

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
                catch (Exception ex)
                {
                    if (ex is TestPlatformException ||
                        ex is SettingsException ||
                        ex is InvalidOperationException)
                    {
#if TODO
                        Utilities.RaiseTestRunError(testLoggerManager, null, ex);
#endif
                        success = false;
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    this.testLoggerManager?.UnregisterDiscoveryEvents(discoveryRequest);
                    discoveryEventsRegistrar?.UnregisterDiscoveryEvents(discoveryRequest);
                }
            }

            EqtTrace.Info("TestRequestManager.DiscoverTests: Discovery tests completed, successful: {0}.", success);
            this.testPlatformEventSource.DiscoveryRequestStop();

            // Publish the Metrics
            metricsPublisher.PublishMetrics(TelemetryDataConstants.TestDiscoveryCompleteEvent, requestData.MetricsCollection.Metrics);
            metricsPublisher.Dispose();

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

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(testRunRequestPayload.RunSettings);
            var batchSize = runConfiguration.BatchSize;

            TestRunCriteria runCriteria = null;
            var runsettings = testRunRequestPayload.RunSettings;
            var requestData = this.GetRequestData(protocolConfig);
            var metricsPublisher = this.telemetryOptedIn ? (IMetricsPublisher)new MetricsPublisher() : new NoOpMetricsPublisher();

            if (this.UpdateRunSettingsIfRequired(runsettings, out string updatedRunsettings))
            {
                runsettings = updatedRunsettings;
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
                runCriteria.TestCaseFilter = testRunRequestPayload.TestCaseFilter;
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

            var success = this.RunTests(requestData, runCriteria, testRunEventsRegistrar, protocolConfig);
            EqtTrace.Info("TestRequestManager.RunTests: run tests completed, sucessful: {0}.", success);
            this.testPlatformEventSource.ExecutionRequestStop();

            metricsPublisher.PublishMetrics(TelemetryDataConstants.TestExecutionCompleteEvent, requestData.MetricsCollection.Metrics);
            metricsPublisher.Dispose();

            return success;
        }

        /// <summary>
        /// Cancel the test run.
        /// </summary>
        public void CancelTestRun()
        {
            EqtTrace.Info("TestRequestManager.CancelTestRun: Sending cancel request.");

            this.runRequestCreatedEventHandle.WaitOne(runRequestTimeout);
            this.currentTestRunRequest?.CancelAsync();
        }

        /// <summary>
        /// Aborts the test run.
        /// </summary>
        public void AbortTestRun()
        {
            EqtTrace.Info("TestRequestManager.AbortTestRun: Sending abort request.");

            this.runRequestCreatedEventHandle.WaitOne(runRequestTimeout);
            this.currentTestRunRequest?.Abort();
        }

        #endregion

        private bool UpdateRunSettingsIfRequired(string runsettingsXml, out string updatedRunSettingsXml)
        {
            bool settingsUpdated = false;
            updatedRunSettingsXml = runsettingsXml;

            if (!string.IsNullOrEmpty(runsettingsXml))
            {
                // TargetFramework is full CLR. Set DesignMode based on current context.
                using (var stream = new StringReader(runsettingsXml))
                using (var reader = XmlReader.Create(stream, XmlRunSettingsUtilities.ReaderSettings))
                {
                    var document = new XmlDocument();
                    document.Load(reader);

                    var navigator = document.CreateNavigator();

                    // If user is already setting DesignMode via runsettings or CLI args; we skip.
                    var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);

                    if (!runConfiguration.DesignModeSet)
                    {
                        InferRunSettingsHelper.UpdateDesignMode(navigator, this.commandLineOptions.IsDesignMode);
                        settingsUpdated = true;
                    }

                    if (!runConfiguration.CollectSourceInformationSet)
                    {
                        InferRunSettingsHelper.UpdateCollectSourceInformation(navigator, this.commandLineOptions.ShouldCollectSourceInformation);
                        settingsUpdated = true;
                    }

                    if(InferRunSettingsHelper.TryGetDeviceXml(navigator, out string deviceXml))
                    {
                        InferRunSettingsHelper.UpdateTargetDeviceInformation(navigator, deviceXml);
                        settingsUpdated = true;
                    }

                    updatedRunSettingsXml = navigator.OuterXml;
                }
            }

            return settingsUpdated;
        }

        private bool RunTests(IRequestData requestData, TestRunCriteria testRunCriteria, ITestRunEventsRegistrar testRunEventsRegistrar, ProtocolConfig protocolConfig)
        {
            // Make sure to run the run request inside a lock as the below section is not thread-safe
            // TranslationLayer can process faster as it directly gets the raw unserialized messages whereas 
            // below logic needs to deserialize and do some cleanup
            // While this section is cleaning up, TranslationLayer can trigger run causing multiple threads to run the below section at the same time
            lock (syncobject)
            {
                bool success = true;
                using (ITestRunRequest testRunRequest = this.testPlatform.CreateTestRunRequest(requestData, testRunCriteria))
                {
                    this.currentTestRunRequest = testRunRequest;
                    this.runRequestCreatedEventHandle.Set();
                    try
                    {
                        this.testLoggerManager.RegisterTestRunEvents(testRunRequest);
                        this.testRunResultAggregator.RegisterTestRunEvents(testRunRequest);
                        testRunEventsRegistrar?.RegisterTestRunEvents(testRunRequest);

                        this.testPlatformEventSource.ExecutionRequestStart();

                        testRunRequest.ExecuteAsync();

                        // Wait for the run completion event
                        testRunRequest.WaitForCompletion();
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
                    finally
                    {
                        this.testLoggerManager.UnregisterTestRunEvents(testRunRequest);
                        this.testRunResultAggregator.UnregisterTestRunEvents(testRunRequest);
                        testRunEventsRegistrar?.UnregisterTestRunEvents(testRunRequest);
                    }
                }

                this.currentTestRunRequest = null;

                return success;
            }
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
        /// Gets Request Data
        /// </summary>
        /// <param name="protocolConfig"></param>
        /// <returns></returns>
        private IRequestData GetRequestData(ProtocolConfig protocolConfig)
        {
            return new RequestData
                       {
                           ProtocolConfig = protocolConfig,
                           MetricsCollection =
                               this.telemetryOptedIn
                                   ? (IMetricsCollection)new MetricsCollection()
                                   : new NoOpMetricsCollection(),
                           IsTelemetryOptedIn = this.telemetryOptedIn
                       };
        }
    }
}
