// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;
    using System.Threading;

#if NET46
    using System.Configuration;
#endif

    /// <summary>
    /// The base run tests.
    /// </summary>
    internal abstract class BaseRunTests
    {
        #region private fields

        private string runSettings;
        private TestExecutionContext testExecutionContext;
        private ITestRunEventsHandler testRunEventsHandler;
        private InProcDataCollectionExtensionManager inProcDataCollectionExtensionManager;
        private ITestRunCache testRunCache;

        /// <summary>
        /// Specifies that the test run cancellation is requested
        /// </summary>
        private volatile bool isCancellationRequested;

        /// <summary>
        /// Active executor which is executing the tests currently
        /// </summary>
        private ITestExecutor activeExecutor;
        private ITestCaseEventsHandler testCaseEventsHandler;
        private RunContext runContext;
        private FrameworkHandle frameworkHandle;

        private ICollection<string> executorUrisThatRanTests;
        private ITestPlatformEventSource testPlatformEventSource;

        /// <summary>
        /// Key in AppSettings section. Corresponding value used for setting tests execution thread apartment state.
        /// </summary>
        private const string ExecutionThreadApartmentStateKey = "ExecutionThreadApartmentState";

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRunTests"/> class.
        /// </summary>
        /// <param name="runSettings"> The run settings. </param>
        /// <param name="testExecutionContext"> The test execution context. </param>
        /// <param name="testCaseEventsHandler"> The test case events handler. </param>
        /// <param name="testRunEventsHandler"> The test run events handler. </param>
        /// <param name="testPlatformEventSource"></param>
        protected BaseRunTests(string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, ITestPlatformEventSource testPlatformEventSource)
        {
            this.runSettings = runSettings;
            this.testExecutionContext = testExecutionContext;
            this.testCaseEventsHandler = testCaseEventsHandler;
            this.testRunEventsHandler = testRunEventsHandler;

            this.isCancellationRequested = false;
            this.testPlatformEventSource = testPlatformEventSource;
            this.SetContext();
        }

        private void SetContext()
        {
            this.testRunCache = new TestRunCache(testExecutionContext.FrequencyOfRunStatsChangeEvent, testExecutionContext.RunStatsChangeEventTimeout, this.OnCacheHit);
            this.inProcDataCollectionExtensionManager = new InProcDataCollectionExtensionManager(runSettings, testRunCache);

            // Verify if datacollection is enabled and wrap the testcasehandler around to get the events 
            if (inProcDataCollectionExtensionManager.IsInProcDataCollectionEnabled)
            {
                this.testCaseEventsHandler = new TestCaseEventsHandler(inProcDataCollectionExtensionManager, this.testCaseEventsHandler);
            }
            else
            {
                // No need to call any methods on this, if inproc-datacollection is not enabled
                inProcDataCollectionExtensionManager = null;
            }

            this.runContext = new RunContext();
            this.runContext.RunSettings = RunSettingsUtilities.CreateAndInitializeRunSettings(this.runSettings);
            this.runContext.KeepAlive = this.testExecutionContext.KeepAlive;
            this.runContext.InIsolation = this.testExecutionContext.InIsolation;
            this.runContext.IsDataCollectionEnabled = this.testExecutionContext.IsDataCollectionEnabled;
            this.runContext.IsBeingDebugged = this.testExecutionContext.IsDebug;

            var runConfig = XmlRunSettingsUtilities.GetRunConfigurationNode(this.runSettings);
            this.runContext.TestRunDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfig);
            this.runContext.SolutionDirectory = RunSettingsUtilities.GetSolutionDirectory(runConfig);

            this.frameworkHandle = new FrameworkHandle(
                this.testCaseEventsHandler,
                this.testRunCache,
                this.testExecutionContext,
                this.testRunEventsHandler);

            this.executorUrisThatRanTests = new List<string>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the run settings.
        /// </summary>
        protected string RunSettings => this.runSettings;

        /// <summary>
        /// Gets the test execution context.
        /// </summary>
        protected TestExecutionContext TestExecutionContext => this.testExecutionContext;

        /// <summary>
        /// Gets the test run events handler.
        /// </summary>
        protected ITestRunEventsHandler TestRunEventsHandler => this.testRunEventsHandler;

        /// <summary>
        /// Gets the test run cache.
        /// </summary>
        protected ITestRunCache TestRunCache => this.testRunCache;

        protected bool IsCancellationRequested => this.isCancellationRequested;

        protected RunContext RunContext => this.runContext;

        protected FrameworkHandle FrameworkHandle => this.frameworkHandle;

        protected ICollection<string> ExecutorUrisThatRanTests => this.executorUrisThatRanTests;

        #endregion

        #region Public methods

        public void RunTests()
        {
            using (testRunCache)
            {
                TimeSpan elapsedTime = TimeSpan.Zero;

                Exception exception = null;
                bool isAborted = false;
                bool shutdownAfterRun = false;

                try
                {
                    // Call Session-Start event on in-proc datacollectors
                    this.inProcDataCollectionExtensionManager?.TriggerTestSessionStart();

                    elapsedTime = this.RunTestsInternal();

                    // Flush any results cached by in-proc manager
                    inProcDataCollectionExtensionManager?.FlushLastChunkResults();

                    // Check the adapter setting for shutting down this process after run
                    shutdownAfterRun = this.frameworkHandle.EnableShutdownAfterTestRun;
                }
                catch (Exception ex)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("BaseRunTests.RunTests: Failed to run the tests. Reason: {0}.", ex);
                    }

                    exception = new Exception(ex.Message, ex.InnerException);

                    isAborted = true;
                }
                finally
                {
                    // Trigger Session End on in-proc datacollectors
                    inProcDataCollectionExtensionManager?.TriggerTestSessionEnd();

                    try
                    {
                        // Send the test run complete event.
                        this.RaiseTestRunComplete(exception, this.isCancellationRequested, isAborted, shutdownAfterRun, elapsedTime);
                    }
                    catch (Exception ex2)
                    {
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error("BaseRunTests.RunTests: Failed to raise runCompletion error. Reason: {0}.", ex2);
                        }

                        // TODO: this does not crash the process currently because of the job queue.
                        // Let the process crash
                        throw;
                    }
                }
            }

            EqtTrace.Verbose("BaseRunTests.RunTests: Run is complete.");
        }

        internal void Abort()
        {
            EqtTrace.Verbose("BaseRunTests.Abort: Calling RaiseTestRunComplete");
            this.RaiseTestRunComplete(exception: null, canceled: this.isCancellationRequested, aborted: true, adapterHintToShutdownAfterRun: false, elapsedTime: TimeSpan.Zero);
        }

        /// <summary>
        /// Cancel the current run by setting cancellation token for active executor
        /// </summary>
        internal void Cancel()
        {
            ITestExecutor activeExecutor = this.activeExecutor;
            isCancellationRequested = true;
            if (activeExecutor != null)
            {
                Task.Run(() => CancelTestRunInternal(this.activeExecutor));
            }
        }

        private void CancelTestRunInternal(ITestExecutor executor)
        {
            try
            {
                activeExecutor.Cancel();
            }
            catch (Exception e)
            {
                EqtTrace.Info("{0}.Cancel threw an exception: {1} ", executor.GetType().FullName, e);
            }
        }

        #endregion

        #region Abstract methods

        protected abstract void BeforeRaisingTestRunComplete(bool exceptionsHitDuringRunTests);

        protected abstract IEnumerable<Tuple<Uri, string>> GetExecutorUriExtensionMap(IFrameworkHandle testExecutorFrameworkHandle, RunContext runContext);

        protected abstract void InvokeExecutor(LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor, Tuple<Uri, string> executorUriExtensionTuple, RunContext runContext, IFrameworkHandle frameworkHandle);

        #endregion

        #region Private methods

        private TimeSpan RunTestsInternal()
        {
            long totalTests = 0;

            var executorUriExtensionMap = this.GetExecutorUriExtensionMap(this.frameworkHandle, this.runContext);

            // Set on the logger the TreatAdapterErrorAsWarning setting from runsettings.
            this.SetAdapterLoggingSettings();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            this.testPlatformEventSource.ExecutionStart();
            var exceptionsHitDuringRunTests = this.RunTestInternalWithExecutors(
                executorUriExtensionMap,
                totalTests);

            stopwatch.Stop();
            this.testPlatformEventSource.ExecutionStop(this.testRunCache.TotalExecutedTests);
            this.BeforeRaisingTestRunComplete(exceptionsHitDuringRunTests);
            return stopwatch.Elapsed;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This methods must call all possible executors and not fail on crash in any executor.")]
        private bool RunTestInternalWithExecutors(IEnumerable<Tuple<Uri, string>> executorUriExtensionMap, long totalTests)
        {
            // Call the executor for each group of tests.
            var exceptionsHitDuringRunTests = false;

            foreach (var executorUriExtensionTuple in executorUriExtensionMap)
            {
                // Get the executor
                var extensionManager = this.GetExecutorExtensionManager(executorUriExtensionTuple.Item2);

                // Look up the executor.
                var executor = extensionManager.TryGetTestExtension(executorUriExtensionTuple.Item1);
                if (executor != null)
                {
                    try
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose(
                                "BaseRunTests.RunTestInternalWithExecutors: Running tests for {0}",
                                executor.Metadata.ExtensionUri);
                        }

                        // set the active executor
                        this.activeExecutor = executor.Value;

                        // If test run cancellation is requested, skip the next executor
                        if (this.isCancellationRequested)
                        {
                            break;
                        }
                        var currentTotalTests = this.testRunCache.TotalExecutedTests;
                        this.testPlatformEventSource.AdapterExecutionStart(executorUriExtensionTuple.Item1.AbsoluteUri);

                        // Run the tests.
#if NET46
                        RunInSTAThread(() => this.InvokeExecutor(executor, executorUriExtensionTuple, this.runContext, this.frameworkHandle));
#else
                        this.InvokeExecutor(executor, executorUriExtensionTuple, this.runContext, this.frameworkHandle);
#endif

                        this.testPlatformEventSource.AdapterExecutionStop(this.testRunCache.TotalExecutedTests - currentTotalTests);

                        // Identify whether the executor did run any tests at all  
                        if (this.testRunCache.TotalExecutedTests > totalTests)
                        {
                            this.executorUrisThatRanTests.Add(executorUriExtensionTuple.Item1.AbsoluteUri);
                            totalTests = this.testRunCache.TotalExecutedTests;
                        }

                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose(
                                "TestExecutionManager.RunTestInternalWithExecutors: Completed running tests for {0}",
                                executor.Metadata.ExtensionUri);
                        }
                    }
                    catch (Exception e)
                    {
                        exceptionsHitDuringRunTests = true;

                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error(
                                "TestExecutionManager.RunTestInternalWithExecutors: An exception occurred while invoking executor {0}. {1}.",
                                executorUriExtensionTuple.Item1,
                                e);
                        }

                        this.TestRunEventsHandler?.HandleLogMessage(
                            TestMessageLevel.Error,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                CrossPlatEngineResources.ExceptionFromRunTests,
                                executorUriExtensionTuple.Item1,
                                ExceptionUtilities.GetExceptionMessage(e)));
                    }
                    finally
                    {
                        this.activeExecutor = null;
                    }
                }
                else
                {
                    // Commenting this out because of a compatibility issue with Microsoft.Dotnet.ProjectModel released on nuGet.org.
                    //var runtimeVersion = string.Concat(PlatformServices.Default.Runtime.RuntimeType, " ",
                    //    PlatformServices.Default.Runtime.RuntimeVersion);
                    var runtimeVersion = " ";
                    this.TestRunEventsHandler?.HandleLogMessage(
                        TestMessageLevel.Warning,
                        string.Format(CultureInfo.CurrentUICulture, CrossPlatEngineResources.NoMatchingExecutor, executorUriExtensionTuple.Item1, runtimeVersion));
                }
            }

            return exceptionsHitDuringRunTests;
        }

        private TestExecutorExtensionManager GetExecutorExtensionManager(string extensionAssembly)
        {
            try
            {
                if (string.IsNullOrEmpty(extensionAssembly)
                    || string.Equals(extensionAssembly, ObjectModel.Constants.UnspecifiedAdapterPath))
                {
                    // full execution. Since the extension manager is cached this can be created multiple times without harming performance.
                    return TestExecutorExtensionManager.Create();
                }
                else
                {
                    return TestExecutorExtensionManager.GetExecutionExtensionManager(extensionAssembly);
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "BaseRunTests: GetExecutorExtensionManager: Exception occured while loading extensions {0}",
                    ex);

                return null;
            }
        }

        private void SetAdapterLoggingSettings()
        {
            // TODO: enable the below once runsettings is in.
            //var sessionMessageLogger = testExecutorFrameworkHandle as TestSessionMessageLogger;
            //if (sessionMessageLogger != null
            //        && testExecutionContext != null
            //        && testExecutionContext.TestRunConfiguration != null)
            //{
            //    sessionMessageLogger.TreatTestAdapterErrorsAsWarnings
            //        = testExecutionContext.TestRunConfiguration.TreatTestAdapterErrorsAsWarnings;
            //}
        }

        /// <summary>
        /// Raise the test run complete event.
        /// </summary>
        private void RaiseTestRunComplete(
            Exception exception,
            bool canceled,
            bool aborted,
            bool adapterHintToShutdownAfterRun,
            TimeSpan elapsedTime)
        {
            var runStats = this.testRunCache?.TestRunStatistics ?? new TestRunStatistics(new Dictionary<TestOutcome, long>());
            var lastChunk = this.testRunCache?.GetLastChunk() ?? new List<TestResult>();

            if (this.testRunEventsHandler != null)
            {
                Collection<AttachmentSet> attachments = this.frameworkHandle?.Attachments;
                var testRunCompleteEventArgs = new TestRunCompleteEventArgs(
                    runStats,
                    canceled,
                    aborted,
                    exception,
                    attachments,
                    elapsedTime);

                var testRunChangedEventArgs = new TestRunChangedEventArgs(runStats, lastChunk, Enumerable.Empty<TestCase>());

                this.testRunEventsHandler.HandleTestRunComplete(
                    testRunCompleteEventArgs,
                    testRunChangedEventArgs,
                    attachments,
                    this.executorUrisThatRanTests);
            }
            else
            {
                EqtTrace.Warning("Could not pass run completion as the callback is null. Aborted :{0}", aborted);
            }
        }

        private void OnCacheHit(TestRunStatistics testRunStats, ICollection<TestResult> results, ICollection<TestCase> inProgressTests)
        {
            if (this.testRunEventsHandler != null)
            {
                var testRunChangedEventArgs = new TestRunChangedEventArgs(testRunStats, results, inProgressTests);
                this.testRunEventsHandler.HandleTestRunStatsChange(testRunChangedEventArgs);
            }
            else
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("BaseRunTests.OnCacheHit: Unable to send TestRunStatsChange Event as TestRunEventsHandler is NULL");
                }
            }
        }

#if NET46
        private static void RunInSTAThread(Action func)
        {
            Exception exThrown = null;
            var thread = new Thread(() =>
            {
                try
                {
                    func();
                }
                catch (Exception e)
                {
                    exThrown = e;
                }
            });

            // .NetStandard 1.5 lib does not have ApartmentState - hence ifdef
            thread.SetApartmentState(GetApartmentStateAppSetting());
            thread.IsBackground = true;
            thread.Start();
            thread.Join();

            if (exThrown != null)
            {
                throw exThrown;
            }
        }

        /// <summary>
        /// Gets the apartmentState and sets the same on the thread that executes tests.
        /// </summary>
        private static ApartmentState GetApartmentStateAppSetting()
        {
            // Tests must be STA by default as customers who run UI tests, OLE tests are impacted otherwise - compat
            ApartmentState userApartmentState;
            string userConfiguredApartmentState = ConfigurationManager.AppSettings[ExecutionThreadApartmentStateKey];

            return  !string.IsNullOrWhiteSpace(userConfiguredApartmentState) && Enum.TryParse(userConfiguredApartmentState, out userApartmentState)
                ? userApartmentState : ApartmentState.STA;
        }
#endif

        #endregion
    }
}
