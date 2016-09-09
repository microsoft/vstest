// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;

    /// <summary>
    /// Orchestrates test execution related functionality for the engine communicating with the test host process.
    /// </summary>
    public class ExecutionManager : IExecutionManager
    {
        private ITestRunEventsHandler testRunEventsHandler;

        private ITestPlatformEventSource testPlatformEventSource;

        private BaseRunTests activeTestRun;

        protected ExecutionManager(ITestPlatformEventSource testPlatformEventSource):this()
        {
            this.testPlatformEventSource = testPlatformEventSource;
        }

        public ExecutionManager()
        {
            this.testPlatformEventSource = TestPlatformEventSource.Instance;
        }

        #region IExecutionManager Implementation

        /// <summary>
        /// Initializes the execution manager.
        /// </summary>
        /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
        public void Initialize(IEnumerable<string> pathToAdditionalExtensions)
        {
            this.testPlatformEventSource.AdapterSearchStart();
            // Start using these additional extensions
            TestPluginCache.Instance.UpdateAdditionalExtensions(pathToAdditionalExtensions, shouldLoadOnlyWellKnownExtensions: false);
            this.LoadExtensions();
            this.testPlatformEventSource.AdapterSearchStop();
        }

        /// <summary>
        /// Starts the test run
        /// </summary>
        /// <param name="adapterSourceMap"> The adapter Source Map.  </param>
        /// <param name="runSettings"> The run Settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        /// <param name="testCaseEventsHandler"> EventHandler for handling test cases level events from Engine. </param>
        /// <param name="runEventsHandler"> EventHandler for handling execution events from Engine.  </param>
        public void StartTestRun(Dictionary<string, IEnumerable<string>> adapterSourceMap, string runSettings, TestExecutionContext testExecutionContext,
            ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler runEventsHandler)
        {
            this.testRunEventsHandler = runEventsHandler;
            try
            {
                activeTestRun = new RunTestsWithSources(
                     adapterSourceMap,
                     runSettings,
                     testExecutionContext,
                     testCaseEventsHandler,
                     runEventsHandler);

                activeTestRun.RunTests();
            }
            finally
            {
                activeTestRun = null;
            }
        }

        /// <summary>
        /// Starts the test run with tests.
        /// </summary>
        /// <param name="tests"> The test list. </param>
        /// <param name="runSettings"> The run Settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        /// <param name="testCaseEventsHandler"> EventHandler for handling test cases level events from Engine. </param>
        /// <param name="runEventsHandler"> EventHandler for handling execution events from Engine. </param>
        public void StartTestRun(IEnumerable<TestCase> tests, string runSettings, TestExecutionContext testExecutionContext, 
            ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler runEventsHandler)
        {
            this.testRunEventsHandler = runEventsHandler;

            try
            {
                activeTestRun = new RunTestsWithTests(tests,
                    runSettings,
                    testExecutionContext,
                    testCaseEventsHandler,
                    runEventsHandler);

                activeTestRun.RunTests();
            }
            finally
            {
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
