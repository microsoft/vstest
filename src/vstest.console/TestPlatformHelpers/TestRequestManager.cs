// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Defines the TestRequestManger which can fire off discovery and test run requests
    /// </summary>
    internal class TestRequestManager : ITestRequestManager
    {
        private ITestPlatform testPlatform;

        private CommandLineOptions commandLineOptions;

        private TestLoggerManager testLoggerManager;

        private TestRunResultAggregator testRunResultAggregator;

        private static ITestRequestManager testRequestManagerInstance;

        private const int runRequestTimeout = 5000;

        /// <summary>
        /// Maintains the current active execution request
        /// Assumption : There can only be one active execution request.
        /// </summary>
        private ITestRunRequest currentTestRunRequest;

        private EventWaitHandle runRequestCreatedEventHandle = new AutoResetEvent(false);

        #region Constructor

        public TestRequestManager() :
            this(CommandLineOptions.Instance,
            TestPlatformFactory.GetTestPlatform(),
            TestLoggerManager.Instance,
            TestRunResultAggregator.Instance)
        {
        }

        internal TestRequestManager(CommandLineOptions commandLineOptions,
            ITestPlatform testPlatform,
            TestLoggerManager testLoggerManager,
            TestRunResultAggregator testRunResultAggregator)
        {
            this.testPlatform = testPlatform;
            this.commandLineOptions = commandLineOptions;
            this.testLoggerManager = testLoggerManager;
            this.testRunResultAggregator = testRunResultAggregator;

            // Always enable logging for discovery or run requests
            this.testLoggerManager.EnableLogging();

            // TODO: Is this required for design mode
            // Add console logger as a listener to logger events.
            var consoleLogger = new ConsoleLogger();
            consoleLogger.Initialize(this.testLoggerManager.LoggerEvents, null);
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
        /// Initializes the extensions while probing additional paths
        /// </summary>
        /// <param name="pathToAdditionalExtensions">Paths to Additional extensions</param>
        public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
        {
            testPlatform.Initialize(pathToAdditionalExtensions, false, true);
        }

        /// <summary>
        /// Resets the command options
        /// </summary>
        public void ResetOptions()
        {
            this.commandLineOptions.Reset();
        }

        /// <summary>
        /// Discover Tests given a list of sources, runsettings
        /// </summary>
        /// <param name="discoveryPayload">Discovery payload</param>
        /// <param name="discoveredTestEvent">EventHandler for discovered tests</param>
        /// <returns>True, if successful</returns>
        public bool DiscoverTests(DiscoveryRequestPayload discoveryPayload, ITestDiscoveryEventsRegistrar discoveryEventsRegistrar)
        {
            bool success = false;

            // create discovery request
            var criteria = new DiscoveryCriteria(discoveryPayload.Sources, this.commandLineOptions.BatchSize, TimeSpan.MaxValue, discoveryPayload.RunSettings);
            using (IDiscoveryRequest discoveryRequest = this.testPlatform.CreateDiscoveryRequest(criteria))
            {
                try
                {
                    testLoggerManager?.RegisterDiscoveryEvents(discoveryRequest);
                    discoveryEventsRegistrar?.RegisterDiscoveryEvents(discoveryRequest);

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
                    testLoggerManager?.UnregisterDiscoveryEvents(discoveryRequest);
                    discoveryEventsRegistrar?.UnregisterDiscoveryEvents(discoveryRequest);
                }
            }

            return success;
        }

        /// <summary>
        /// Run Tests with given a set of testcases
        /// </summary>
        /// <param name="testRunRequestPayload">TestRun request Payload</param>
        /// <param name="testHostLauncher">TestHost Launcher for the run</param>
        /// <param name="testRunEventsRegistrar">event registrar for run events</param>
        /// <returns>True, if sucessful</returns>
        public bool RunTests(TestRunRequestPayload testRunRequestPayload, ITestHostLauncher testHostLauncher, ITestRunEventsRegistrar testRunEventsRegistrar)
        {
            TestRunCriteria runCriteria = null;
            if (testRunRequestPayload.Sources != null && testRunRequestPayload.Sources.Count() > 0)
            {
                var adapterSourceMap = AdapterSourceMapUtilities.GetTestRunnerAndAssemblyInfo(testRunRequestPayload.Sources);

                runCriteria = new TestRunCriteria(adapterSourceMap,
                        commandLineOptions.BatchSize,
                        testRunRequestPayload.KeepAlive,
                        testRunRequestPayload.RunSettings,
                        commandLineOptions.TestRunStatsEventTimeout,
                        testHostLauncher);
                runCriteria.TestCaseFilter = commandLineOptions.TestCaseFilterValue;
            }
            else
            {
                runCriteria = new TestRunCriteria(testRunRequestPayload.TestCases, commandLineOptions.BatchSize, testRunRequestPayload.KeepAlive,
                    testRunRequestPayload.RunSettings, commandLineOptions.TestRunStatsEventTimeout, testHostLauncher);
            }

            return RunTests(runCriteria, testRunEventsRegistrar);
        }

        /// <summary>
        /// Cancel the test run
        /// </summary>
        public void CancelTestRun()
        {
            this.runRequestCreatedEventHandle.WaitOne(runRequestTimeout);
            this.currentTestRunRequest?.CancelAsync();
        }

        public void AbortTestRun()
        {
            this.runRequestCreatedEventHandle.WaitOne(runRequestTimeout);
            this.currentTestRunRequest?.Abort();
        }

        #endregion

        private bool RunTests(TestRunCriteria testRunCriteria, ITestRunEventsRegistrar testRunEventsRegistrar)
        {
            bool success = true;
            using (this.currentTestRunRequest = testPlatform.CreateTestRunRequest(testRunCriteria))
            {
                this.runRequestCreatedEventHandle.Set();
                try
                {
                    testLoggerManager.RegisterTestRunEvents(currentTestRunRequest);
                    testRunResultAggregator.RegisterTestRunEvents(currentTestRunRequest);
                    testRunEventsRegistrar?.RegisterTestRunEvents(currentTestRunRequest);

                    currentTestRunRequest.ExecuteAsync();

                    // Wait for the run completion event
                    currentTestRunRequest.WaitForCompletion();
                }
                catch (Exception ex)
                {
                    if (ex is TestPlatformException ||
                        ex is SettingsException ||
                        ex is InvalidOperationException)
                    {
                        LoggerUtilities.RaiseTestRunError(testLoggerManager, testRunResultAggregator, ex);
                        success = false;
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    testLoggerManager.UnregisterTestRunEvents(currentTestRunRequest);
                    testRunResultAggregator.UnregisterTestRunEvents(currentTestRunRequest);
                    testRunEventsRegistrar?.UnregisterTestRunEvents(currentTestRunRequest);
                }
            }
            this.currentTestRunRequest = null;

            return success;
        }
    }
}