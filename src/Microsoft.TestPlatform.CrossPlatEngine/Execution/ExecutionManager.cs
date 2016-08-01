// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using ObjectModel.Utilities;
    /// <summary>
    /// Orchestrates test execution related functionality for the engine communicating with the test host process.
    /// </summary>
    public class ExecutionManager : IExecutionManager
    {
        private ITestRunEventsHandler testRunEventsHandler;

        private BaseRunTests activeTestRun;

        #region IExecutionManager Implementation

        /// <summary>
        /// Initializes the execution manager.
        /// </summary>
        /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
        public void Initialize(IEnumerable<string> pathToAdditionalExtensions)
        {
            // Start using these additional extensions
            TestPluginCache.Instance.UpdateAdditionalExtensions(pathToAdditionalExtensions, shouldLoadOnlyWellKnownExtensions: false);
            this.LoadExtensions();
        }

        /// <summary>
        /// Starts the test run
        /// </summary>
        /// <param name="adapterSourceMap"> The adapter Source Map.  </param>
        /// <param name="runSettings"> The run Settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        /// <param name="testCaseEvents"> EventHandler for handling test cases level events from Engine. </param>
        /// <param name="runEventsHandler"> EventHandler for handling execution events from Engine.  </param>
        public void StartTestRun(Dictionary<string, IEnumerable<string>> adapterSourceMap, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEvents, ITestRunEventsHandler runEventsHandler)
        {
            this.testRunEventsHandler = runEventsHandler;
            var testCaseEventsHandler = testCaseEvents;
            var inProcDataCollectionExtensionManager = new InProcDataCollectionExtensionManager(runSettings);

            try
            {
                if (inProcDataCollectionExtensionManager.IsInProcDataCollectionEnabled)
                {
                    testCaseEventsHandler = new TestCaseEventsHandler(inProcDataCollectionExtensionManager, testCaseEvents);
                    inProcDataCollectionExtensionManager.TriggerTestSessionStart();
                }

                using (var testRunCache = new TestRunCache(
                    testExecutionContext.FrequencyOfRunStatsChangeEvent,
                    testExecutionContext.RunStatsChangeEventTimeout,
                    this.OnCacheHit))
                {
                    activeTestRun = new RunTestsWithSources(
                         adapterSourceMap,
                         testRunCache,
                         runSettings,
                         testExecutionContext,
                         testCaseEventsHandler,
                         runEventsHandler);
                    activeTestRun.RunTests();
                }
            }
            finally
            {
                if (inProcDataCollectionExtensionManager.IsInProcDataCollectionEnabled)
                {
                    inProcDataCollectionExtensionManager.TriggerTestSessionEnd();
                }
                activeTestRun = null;
            }
        }

        /// <summary>
        /// Starts the test run with tests.
        /// </summary>
        /// <param name="tests"> The test list. </param>
        /// <param name="runSettings"> The run Settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        /// <param name="testCaseEvents"> EventHandler for handling test cases level events from Engine. </param>
        /// <param name="runEventsHandler"> EventHandler for handling execution events from Engine. </param>
        public void StartTestRun(IEnumerable<TestCase> tests, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEvents, ITestRunEventsHandler runEventsHandler)
        {
            this.testRunEventsHandler = runEventsHandler;
            var testCaseEventsHandler = testCaseEvents;
            var inProcDataCollectionExtensionManager = new InProcDataCollectionExtensionManager(runSettings);

            try
            {
                if (inProcDataCollectionExtensionManager.IsInProcDataCollectionEnabled)
                {
                    testCaseEventsHandler = new TestCaseEventsHandler(inProcDataCollectionExtensionManager, testCaseEvents);
                    inProcDataCollectionExtensionManager.TriggerTestSessionStart();
                }

                using (var testRunCache = new TestRunCache(
                    testExecutionContext.FrequencyOfRunStatsChangeEvent,
                    testExecutionContext.RunStatsChangeEventTimeout,
                    this.OnCacheHit))
                {
                    var runTestsWithTests = new RunTestsWithTests(tests, testRunCache, runSettings, testExecutionContext, testCaseEventsHandler, runEventsHandler);
                    activeTestRun = runTestsWithTests;
                    runTestsWithTests.RunTests();
                }
            }
            finally
            {
                if (inProcDataCollectionExtensionManager.IsInProcDataCollectionEnabled)
                {
                    inProcDataCollectionExtensionManager.TriggerTestSessionEnd();
                }
                activeTestRun = null;
            }
        }

        /// <summary>
        /// Cancel the test execution.
        /// </summary>
        public void Cancel()
        {
            if (this.activeTestRun == null)
            {
                var testRunCompleteEventArgs = new TestRunCompleteEventArgs(null, true, false, null, null, TimeSpan.Zero);
                this.testRunEventsHandler.HandleTestRunComplete(testRunCompleteEventArgs, null, null, null);
            }
            else
            {
                this.activeTestRun.Cancel();
            }
        }

        /// <summary>
        /// Aborts the test execution.
        /// </summary>
        public void Abort()
        {
            if (this.activeTestRun == null)
            {
                var testRunCompleteEventArgs = new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero);
                this.testRunEventsHandler.HandleTestRunComplete(testRunCompleteEventArgs, null, null, null);
            }
            else
            {
                this.activeTestRun.Abort();
            }

        }

        #endregion

        #region private methods
        
        private void OnCacheHit(TestRunStatistics testRunStats, ICollection<TestResult> results, ICollection<TestCase> inProgressTests)
        {
            if (this.testRunEventsHandler != null)
            {
                var testRunChangedEventArgs = new TestRunChangedEventArgs(testRunStats, results, inProgressTests);
                this.testRunEventsHandler.HandleTestRunStatsChange(testRunChangedEventArgs);
            }
            else
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("Could not pass the message for changes in test run statistics as the callback is null.");
                }
            }
        }

        private void LoadExtensions()
        {
            try
            {
                // Load the extensions on creation so that we dont have to spend time during first execution.
                EqtTrace.Verbose("TestExecutorService: Loading the extensions");

                TestDiscoveryExtensionManager.LoadAndInitializeAllExtensions(false);

                EqtTrace.Verbose("TestExecutorService: Loaded the discoverers");

                TestExecutorExtensionManager.LoadAndInitializeAllExtensions(false);

                EqtTrace.Verbose("TestExecutorService: Loaded the executors");

                SettingsProviderExtensionManager.LoadAndInitializeAllExtensions(false);

                EqtTrace.Verbose("TestExecutorService: Loaded the settings providers");
                EqtTrace.Info("TestExecutorService: Loaded the extensions");
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("TestExecutorWebService: Exception occured while calling test connection. {0}", ex);
                }
            }
        }
        #endregion
    }
}
