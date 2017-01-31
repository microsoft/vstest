// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

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

        /// <summary>
        /// Maintains the current active execution request
        /// Assumption : There can only be one active execution request.
        /// </summary>
        private ITestRunRequest currentTestRunRequest;

        private readonly EventWaitHandle runRequestCreatedEventHandle = new AutoResetEvent(false);

        #region Constructor

        public TestRequestManager() :
            this(CommandLineOptions.Instance,
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

            // Always enable logging for discovery or run requests
            this.testLoggerManager.EnableLogging();

            if (!this.commandLineOptions.IsDesignMode)
            {
                var consoleLogger = new ConsoleLogger();
                consoleLogger.Initialize(this.testLoggerManager.LoggerEvents, null);
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
            this.testPlatform.Initialize(pathToAdditionalExtensions, false, true);
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
                    this.testLoggerManager?.RegisterDiscoveryEvents(discoveryRequest);
                    discoveryEventsRegistrar?.RegisterDiscoveryEvents(discoveryRequest);

                    this.testPlatformEventSource.DiscoveryRequestStart();

                    discoveryRequest.DiscoverAsync();
                    discoveryRequest.WaitForCompletion();

                    this.testPlatformEventSource.DiscoveryRequestStop();

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

            return success;
        }

        /// <summary>
        /// Run Tests with given a set of test cases.
        /// </summary>
        /// <param name="testRunRequestPayload">TestRun request Payload</param>
        /// <param name="testHostLauncher">TestHost Launcher for the run</param>
        /// <param name="testRunEventsRegistrar">event registrar for run events</param>
        /// <returns>True, if successful</returns>
        public bool RunTests(TestRunRequestPayload testRunRequestPayload, ITestHostLauncher testHostLauncher, ITestRunEventsRegistrar testRunEventsRegistrar)
        {
            TestRunCriteria runCriteria = null;
            if (testRunRequestPayload.Sources != null && testRunRequestPayload.Sources.Any())
            {
                runCriteria = new TestRunCriteria(
                                  testRunRequestPayload.Sources,
                                  this.commandLineOptions.BatchSize,
                                  testRunRequestPayload.KeepAlive,
                                  testRunRequestPayload.RunSettings,
                                  this.commandLineOptions.TestRunStatsEventTimeout,
                                  testHostLauncher);
                runCriteria.TestCaseFilter = this.commandLineOptions.TestCaseFilterValue;
            }
            else
            {
                runCriteria = new TestRunCriteria(
                                  testRunRequestPayload.TestCases,
                                  this.commandLineOptions.BatchSize,
                                  testRunRequestPayload.KeepAlive,
                                  testRunRequestPayload.RunSettings,
                                  this.commandLineOptions.TestRunStatsEventTimeout,
                                  testHostLauncher);
            }

            return this.RunTests(runCriteria, testRunEventsRegistrar);
        }

        /// <summary>
        /// Cancel the test run.
        /// </summary>
        public void CancelTestRun()
        {
            this.runRequestCreatedEventHandle.WaitOne(runRequestTimeout);
            this.currentTestRunRequest?.CancelAsync();
        }

        /// <summary>
        /// Aborts the test run.
        /// </summary>
        public void AbortTestRun()
        {
            this.runRequestCreatedEventHandle.WaitOne(runRequestTimeout);
            this.currentTestRunRequest?.Abort();
        }

        #endregion

        private bool RunTests(TestRunCriteria testRunCriteria, ITestRunEventsRegistrar testRunEventsRegistrar)
        {
            bool success = true;
            using (this.currentTestRunRequest = this.testPlatform.CreateTestRunRequest(testRunCriteria))
            {
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

                    this.testPlatformEventSource.ExecutionRequestStop();
                }
                catch (Exception ex)
                {
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
                    this.testLoggerManager.UnregisterTestRunEvents(this.currentTestRunRequest);
                    this.testRunResultAggregator.UnregisterTestRunEvents(this.currentTestRunRequest);
                    testRunEventsRegistrar?.UnregisterTestRunEvents(this.currentTestRunRequest);
                }
            }

            this.currentTestRunRequest = null;

            return success;
        }
    }
}