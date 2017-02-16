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
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

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

        private object syncobject = new object();

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
            this.testPlatform.Initialize(pathToAdditionalExtensions, false, true);
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
        /// <returns>True, if successful</returns>
        public bool DiscoverTests(DiscoveryRequestPayload discoveryPayload, ITestDiscoveryEventsRegistrar discoveryEventsRegistrar)
        {
            EqtTrace.Info("TestRequestManager.DiscoverTests: Discovery tests started.");

            bool success = false;

            // create discovery request
            var criteria = new DiscoveryCriteria(discoveryPayload.Sources, this.commandLineOptions.BatchSize, this.commandLineOptions.TestStatsEventTimeout, discoveryPayload.RunSettings);
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

            EqtTrace.Info("TestRequestManager.DiscoverTests: Discovery tests completed, sucessful: {0}.", success);
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
            EqtTrace.Info("TestRequestManager.RunTests: run tests started.");

            TestRunCriteria runCriteria = null;
            if (testRunRequestPayload.Sources != null && testRunRequestPayload.Sources.Any())
            {
                runCriteria = new TestRunCriteria(
                                  testRunRequestPayload.Sources,
                                  this.commandLineOptions.BatchSize,
                                  testRunRequestPayload.KeepAlive,
                                  testRunRequestPayload.RunSettings,
                                  this.commandLineOptions.TestStatsEventTimeout,
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
                                  this.commandLineOptions.TestStatsEventTimeout,
                                  testHostLauncher);
            }

            var success = this.RunTests(runCriteria, testRunEventsRegistrar);
            EqtTrace.Info("TestRequestManager.RunTests: run tests completed, sucessful: {0}.", success);
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

        private bool RunTests(TestRunCriteria testRunCriteria, ITestRunEventsRegistrar testRunEventsRegistrar)
        {
            // Make sure to run the run request inside a lock as the below section is not thread-safe
            // TranslationLayer can process faster as it directly gets the raw unserialized messages whereas 
            // below logic needs to deserialize and do some cleanup
            // While this section is cleaning up, TranslationLayer can trigger run causing multiple threads to run the below section at the same time
            lock (syncobject)
            {
                bool success = true;
                using (ITestRunRequest testRunRequest = this.testPlatform.CreateTestRunRequest(testRunCriteria))
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

                        this.testPlatformEventSource.ExecutionRequestStop();
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
    }
}