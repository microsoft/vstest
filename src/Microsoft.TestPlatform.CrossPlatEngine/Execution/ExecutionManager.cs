// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;

    /// <summary>
    /// Orchestrates test execution related functionality for the engine communicating with the test host process.
    /// </summary>
    public class ExecutionManager : IExecutionManager
    {
        private ITestRunEventsHandler testRunEventsHandler;

        private readonly ITestPlatformEventSource testPlatformEventSource;

        private BaseRunTests activeTestRun;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionManager"/> class.
        /// </summary>
        public ExecutionManager() : this(TestPlatformEventSource.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionManager"/> class.
        /// </summary>
        /// <param name="testPlatformEventSource">Test platform event source.</param>
        protected ExecutionManager(ITestPlatformEventSource testPlatformEventSource)
        {
            this.testPlatformEventSource = testPlatformEventSource;
        }

        #region IExecutionManager Implementation

        /// <summary>
        /// Initializes the execution manager.
        /// </summary>
        /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
        public void Initialize(IEnumerable<string> pathToAdditionalExtensions)
        {
            this.testPlatformEventSource.AdapterSearchStart();

            if (pathToAdditionalExtensions != null && pathToAdditionalExtensions.Any())
            {
                // Start using these additional extensions
                TestPluginCache.Instance.DefaultExtensionPaths = pathToAdditionalExtensions;
            }

            this.LoadExtensions();

            this.testPlatformEventSource.AdapterSearchStop();
        }

        /// <summary>
        /// Starts the test run
        /// </summary>
        /// <param name="adapterSourceMap"> The adapter Source Map.  </param>
        /// <param name="package">The user input test source(package) if it differ from actual test source otherwise null.</param>
        /// <param name="runSettings"> The run Settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        /// <param name="testCaseEventsHandler"> EventHandler for handling test cases level events from Engine. </param>
        /// <param name="runEventsHandler"> EventHandler for handling execution events from Engine.  </param>
        public void StartTestRun(
            Dictionary<string, IEnumerable<string>> adapterSourceMap,
            string package,
            string runSettings,
            TestExecutionContext testExecutionContext,
            ITestCaseEventsHandler testCaseEventsHandler,
            ITestRunEventsHandler runEventsHandler)
        {
            this.testRunEventsHandler = runEventsHandler;
            try
            {
                this.activeTestRun = new RunTestsWithSources(
                     adapterSourceMap,
                     package,
                     runSettings,
                     testExecutionContext,
                     testCaseEventsHandler,
                     runEventsHandler);

                this.activeTestRun.RunTests();
            }
            catch(Exception e)
            {
                this.testRunEventsHandler.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, e.ToString());
                this.Abort();
            }
            finally
            {
                this.activeTestRun = null;
            }
        }

        /// <summary>
        /// Starts the test run with tests.
        /// </summary>
        /// <param name="tests"> The test list. </param>
        /// <param name="package">The user input test source(package) if it differ from actual test source otherwise null.</param>
        /// <param name="runSettings"> The run Settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        /// <param name="testCaseEventsHandler"> EventHandler for handling test cases level events from Engine. </param>
        /// <param name="runEventsHandler"> EventHandler for handling execution events from Engine. </param>
        public void StartTestRun(
            IEnumerable<TestCase> tests,
            string package,
            string runSettings,
            TestExecutionContext testExecutionContext,
            ITestCaseEventsHandler testCaseEventsHandler,
            ITestRunEventsHandler runEventsHandler)
        {
            this.testRunEventsHandler = runEventsHandler;

            try
            {
                this.activeTestRun = new RunTestsWithTests(
                                         tests,
                                         package,
                                         runSettings,
                                         testExecutionContext,
                                         testCaseEventsHandler,
                                         runEventsHandler);

                this.activeTestRun.RunTests();
            }
            catch(Exception e)
            {
                this.testRunEventsHandler.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, e.ToString());
                this.Abort();
            }
            finally
            {
                this.activeTestRun = null;
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
