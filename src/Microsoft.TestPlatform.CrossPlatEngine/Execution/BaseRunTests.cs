// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// The base run tests.
    /// </summary>
    internal abstract class BaseRunTests
    {
        #region private fields

        private string runSettings;
        private TestExecutionContext testExecutionContext;
        private ITestRunEventsHandler testRunEventsHandler;
        private ITestEventsPublisher testEventsPublisher;
        private ITestRunCache testRunCache;
        private string package;
        private IRequestData requestData;

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
        /// To create thread in given apartment state.
        /// </summary>
        private IThread platformThread;

        /// <summary>
        /// The Run configuration. To determine framework and execution thread apartment state.
        /// </summary>
        private RunConfiguration runConfiguration;

        /// <summary>
        /// The Serializer to clone testcase object in case of user input test source is package. E.g UWP scenario(appx/build.appxrecipe).
        /// </summary>
        private IDataSerializer dataSerializer;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRunTests"/> class.
        /// </summary>
        /// <param name="requestData">The request data for providing common execution services and data</param>
        /// <param name="package">The user input test source(package) if it differs from actual test source otherwise null.</param>
        /// <param name="runSettings">The run settings.</param>
        /// <param name="testExecutionContext">The test execution context.</param>
        /// <param name="testCaseEventsHandler">The test case events handler.</param>
        /// <param name="testRunEventsHandler">The test run events handler.</param>
        /// <param name="testPlatformEventSource">Test platform event source.</param>
        protected BaseRunTests(
            IRequestData requestData,
            string package,
            string runSettings,
            TestExecutionContext testExecutionContext,
            ITestCaseEventsHandler testCaseEventsHandler,
            ITestRunEventsHandler testRunEventsHandler,
            ITestPlatformEventSource testPlatformEventSource)
            : this(
                requestData,
                package,
                runSettings,
                testExecutionContext,
                testCaseEventsHandler,
                testRunEventsHandler,
                testPlatformEventSource,
                testCaseEventsHandler as ITestEventsPublisher,
                new PlatformThread(),
                JsonDataSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRunTests"/> class.
        /// </summary>
        /// <param name="requestData">Provides services and data for execution</param>
        /// <param name="package">The user input test source(package) list if it differs from actual test source otherwise null.</param>
        /// <param name="runSettings">The run settings.</param>
        /// <param name="testExecutionContext">The test execution context.</param>
        /// <param name="testCaseEventsHandler">The test case events handler.</param>
        /// <param name="testRunEventsHandler">The test run events handler.</param>
        /// <param name="testPlatformEventSource">Test platform event source.</param>
        /// <param name="testEventsPublisher">Publisher for test events.</param>
        /// <param name="platformThread">Platform Thread.</param>
        /// <param name="dataSerializer">Data Serializer for cloning TestCase and test results object.</param>
        protected BaseRunTests(
            IRequestData requestData,
            string package,
            string runSettings,
            TestExecutionContext testExecutionContext,
            ITestCaseEventsHandler testCaseEventsHandler,
            ITestRunEventsHandler testRunEventsHandler,
            ITestPlatformEventSource testPlatformEventSource,
            ITestEventsPublisher testEventsPublisher,
            IThread platformThread,
            IDataSerializer dataSerializer)
        {
            this.package = package;
            this.runSettings = runSettings;
            this.testExecutionContext = testExecutionContext;
            this.testCaseEventsHandler = testCaseEventsHandler;
            this.testRunEventsHandler = testRunEventsHandler;
            this.requestData = requestData;

            this.isCancellationRequested = false;
            this.testPlatformEventSource = testPlatformEventSource;
            this.testEventsPublisher = testEventsPublisher;
            this.platformThread = platformThread;
            this.dataSerializer = dataSerializer;
            this.SetContext();
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
            using (this.testRunCache)
            {
                TimeSpan elapsedTime = TimeSpan.Zero;

                Exception exception = null;
                bool isAborted = false;
                bool shutdownAfterRun = false;

                try
                {
                    // Call Session-Start event on in-proc datacollectors
                    this.SendSessionStart();

                    elapsedTime = this.RunTestsInternal();

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
                    this.SendSessionEnd();

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

                        // TODO : this does not crash the process currently because of the job queue.
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
            // Note: Test host delegates the cancellation to active executor and doesn't call HandleTestRunComplete in cancel request.
            // Its expected from active executor to respect the cancel request and thus return from RunTests quickly (canceling the tests).
            this.isCancellationRequested = true;

            if (this.activeExecutor == null)
            {
                return;
            }

            if (this.NotRequiredSTAThread() || !this.TryToRunInSTAThread(() => this.CancelTestRunInternal(this.activeExecutor), false))
            {
                Task.Run(() => this.CancelTestRunInternal(this.activeExecutor));
            }
        }

        #region Abstract methods

        protected abstract void BeforeRaisingTestRunComplete(bool exceptionsHitDuringRunTests);

        protected abstract IEnumerable<Tuple<Uri, string>> GetExecutorUriExtensionMap(
            IFrameworkHandle testExecutorFrameworkHandle,
            RunContext runContext);

        protected abstract void InvokeExecutor(
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
            Tuple<Uri, string> executorUriExtensionTuple,
            RunContext runContext,
            IFrameworkHandle frameworkHandle);

        /// <summary>
        /// Asks the adapter about attaching the debugger to the default test host.
        /// </summary>
        /// <param name="executor">The executor used to run the tests.</param>
        /// <param name="executorUriExtensionTuple">The executor URI.</param>
        /// <param name="runContext">The run context.</param>
        /// <returns>
        /// <see cref="true"/> if must attach the debugger to the default test host,
        /// <see cref="false"/> otherwise.
        /// </returns>
        protected abstract bool ShouldAttachDebuggerToTestHost(
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
            Tuple<Uri, string> executorUriExtensionTuple,
            RunContext runContext);

        protected abstract void SendSessionStart();

        protected abstract void SendSessionEnd();

        #endregion

        private void CancelTestRunInternal(ITestExecutor executor)
        {
            try
            {
                executor.Cancel();
            }
            catch (Exception e)
            {
                EqtTrace.Info("{0}.Cancel threw an exception: {1} ", executor.GetType().FullName, e);
            }
        }
        #endregion

        #region Private methods

        private void SetContext()
        {
            this.testRunCache = new TestRunCache(this.testExecutionContext.FrequencyOfRunStatsChangeEvent, this.testExecutionContext.RunStatsChangeEventTimeout, this.OnCacheHit);

            this.runContext = new RunContext();
            this.runContext.RunSettings = RunSettingsUtilities.CreateAndInitializeRunSettings(this.runSettings);
            this.runContext.KeepAlive = this.testExecutionContext.KeepAlive;
            this.runContext.InIsolation = this.testExecutionContext.InIsolation;
            this.runContext.IsDataCollectionEnabled = this.testExecutionContext.IsDataCollectionEnabled;
            this.runContext.IsBeingDebugged = this.testExecutionContext.IsDebug;

            var runConfig = XmlRunSettingsUtilities.GetRunConfigurationNode(this.runSettings);
            this.runContext.TestRunDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfig);
            this.runContext.SolutionDirectory = RunSettingsUtilities.GetSolutionDirectory(runConfig);
            this.runConfiguration = runConfig;

            this.frameworkHandle = new FrameworkHandle(
                this.testCaseEventsHandler,
                this.testRunCache,
                this.testExecutionContext,
                this.testRunEventsHandler);
            this.frameworkHandle.TestRunMessage += this.OnTestRunMessage;

            this.executorUrisThatRanTests = new List<string>();
        }

        private void OnTestRunMessage(object sender, TestRunMessageEventArgs e)
        {
            this.testRunEventsHandler.HandleLogMessage(e.Level, e.Message);
        }

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
            // Collecting Total Number of Adapters Discovered in Machine.
            this.requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution, executorUriExtensionMap.Count());

            var attachedToTestHost = false;
            var executorCache = new Dictionary<string, LazyExtension<ITestExecutor, ITestExecutorCapabilities>>();
            foreach (var executorUriExtensionTuple in executorUriExtensionMap)
            {
                // Avoid processing the same executor twice.
                if (executorCache.ContainsKey(executorUriExtensionTuple.Item1.AbsoluteUri))
                {
                    continue;
                }

                // Get the extension manager.
                var extensionManager = this.GetExecutorExtensionManager(executorUriExtensionTuple.Item2);

                // Look up the executor.
                var executor = extensionManager.TryGetTestExtension(executorUriExtensionTuple.Item1);
                if (executor == null)
                {
                    // Commenting this out because of a compatibility issue with Microsoft.Dotnet.ProjectModel released on nuGet.org.
                    // this.activeExecutor = null;
                    // var runtimeVersion = string.Concat(PlatformServices.Default.Runtime.RuntimeType, " ",	
                    // PlatformServices.Default.Runtime.RuntimeVersion);
                    var runtimeVersion = " ";
                    this.TestRunEventsHandler?.HandleLogMessage(
                        TestMessageLevel.Warning,
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatEngineResources.NoMatchingExecutor,
                            executorUriExtensionTuple.Item1.AbsoluteUri,
                            runtimeVersion));

                    continue;
                }

                // Cache the executor.
                executorCache.Add(executorUriExtensionTuple.Item1.AbsoluteUri, executor);

                // Check if we actually have to attach to the default test host.
                if (!this.runContext.IsBeingDebugged || attachedToTestHost)
                {
                    // We already know we should attach to the default test host, simply continue.
                    continue;
                }

                // If there's at least one adapter in the filtered adapters list that doesn't
                // implement the new test executor interface, we should attach to the default test
                // host by default.
                // Same goes if all adapters implement the new test executor interface but at
                // least one of them needs the test platform to attach to the default test host.
                if (!(executor.Value is ITestExecutor2)
                    || this.ShouldAttachDebuggerToTestHost(executor, executorUriExtensionTuple, this.runContext))
                {
                    EqtTrace.Verbose("Attaching to default test host.");

                    attachedToTestHost = true;
                    var pid = Process.GetCurrentProcess().Id;
                    if (!this.frameworkHandle.AttachDebuggerToProcess(pid))
                    {
                        EqtTrace.Warning(
                            string.Format(
                                CultureInfo.CurrentUICulture,
                                CrossPlatEngineResources.AttachDebuggerToDefaultTestHostFailure,
                                pid));
                    }
                }
            }


            // Call the executor for each group of tests.
            var exceptionsHitDuringRunTests = false;
            var executorsFromDeprecatedLocations = false;
            double totalTimeTakenByAdapters = 0;
            foreach (var executorUriExtensionTuple in executorUriExtensionMap)
            {
                // Get the executor from the cache.
                if (!executorCache.TryGetValue(executorUriExtensionTuple.Item1.AbsoluteUri, out var executor))
                {
                    continue;
                }

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

                    var timeStartNow = DateTime.UtcNow;

                    var currentTotalTests = this.testRunCache.TotalExecutedTests;
                    this.testPlatformEventSource.AdapterExecutionStart(executorUriExtensionTuple.Item1.AbsoluteUri);

                    // Run the tests.
                    if (this.NotRequiredSTAThread() || !this.TryToRunInSTAThread(() => this.InvokeExecutor(executor, executorUriExtensionTuple, this.runContext, this.frameworkHandle), true))
                    {
                        this.InvokeExecutor(executor, executorUriExtensionTuple, this.runContext, this.frameworkHandle);
                    }

                    this.testPlatformEventSource.AdapterExecutionStop(this.testRunCache.TotalExecutedTests - currentTotalTests);

                    var totalTimeTaken = DateTime.UtcNow - timeStartNow;

                    // Identify whether the executor did run any tests at all
                    if (this.testRunCache.TotalExecutedTests > totalTests)
                    {
                        this.executorUrisThatRanTests.Add(executorUriExtensionTuple.Item1.AbsoluteUri);

                        // Collecting Total Tests Ran by each Adapter
                        var totalTestRun = this.testRunCache.TotalExecutedTests - totalTests;
                        this.requestData.MetricsCollection.Add(string.Format("{0}.{1}", TelemetryDataConstants.TotalTestsRanByAdapter, executorUriExtensionTuple.Item1.AbsoluteUri), totalTestRun);

                        if (!CrossPlatEngine.Constants.DefaultAdapters.Contains(executor.Metadata.ExtensionUri, StringComparer.OrdinalIgnoreCase))
                        {
                            var executorLocation = executor.Value.GetType().GetTypeInfo().Assembly.GetAssemblyLocation();

                            executorsFromDeprecatedLocations |= Path.GetDirectoryName(executorLocation).Equals(CrossPlatEngine.Constants.DefaultAdapterLocation);
                        }

                        totalTests = this.testRunCache.TotalExecutedTests;
                    }

                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(
                            "BaseRunTests.RunTestInternalWithExecutors: Completed running tests for {0}",
                            executor.Metadata.ExtensionUri);
                    }

                    // Collecting Time Taken by each executor Uri
                    this.requestData.MetricsCollection.Add(string.Format("{0}.{1}", TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter, executorUriExtensionTuple.Item1.AbsoluteUri), totalTimeTaken.TotalSeconds);
                    totalTimeTakenByAdapters += totalTimeTaken.TotalSeconds;
                }
                catch (Exception e)
                {
                    exceptionsHitDuringRunTests = true;

                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error(
                            "BaseRunTests.RunTestInternalWithExecutors: An exception occurred while invoking executor {0}. {1}.",
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

            // Collecting Total Time Taken by Adapters
            this.requestData.MetricsCollection.Add(TelemetryDataConstants.TimeTakenByAllAdaptersInSec, totalTimeTakenByAdapters);

            if (executorsFromDeprecatedLocations)
            {
                this.TestRunEventsHandler?.HandleLogMessage(TestMessageLevel.Warning, string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.DeprecatedAdapterPath));
            }

            return exceptionsHitDuringRunTests;
        }

        private bool NotRequiredSTAThread()
        {
            return this.runConfiguration.ExecutionThreadApartmentState != PlatformApartmentState.STA;
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
                    "BaseRunTests: GetExecutorExtensionManager: Exception occurred while loading extensions {0}",
                    ex);

                return null;
            }
        }

        private void SetAdapterLoggingSettings()
        {
            // TODO: enable the below once runsettings is in.
            // var sessionMessageLogger = testExecutorFrameworkHandle as TestSessionMessageLogger;
            // if (sessionMessageLogger != null
            //        && testExecutionContext != null
            //        && testExecutionContext.TestRunConfiguration != null)
            // {
            //    sessionMessageLogger.TreatTestAdapterErrorsAsWarnings
            //        = testExecutionContext.TestRunConfiguration.TreatTestAdapterErrorsAsWarnings;
            // }
        }

        private void RaiseTestRunComplete(
            Exception exception,
            bool canceled,
            bool aborted,
            bool adapterHintToShutdownAfterRun,
            TimeSpan elapsedTime)
        {
            var runStats = this.testRunCache?.TestRunStatistics ?? new TestRunStatistics(new Dictionary<TestOutcome, long>());
            var lastChunkTestResults = this.testRunCache?.GetLastChunk() ?? new List<TestResult>();

            if (this.testRunEventsHandler != null)
            {
                // Collecting Total Tests Run
                this.requestData.MetricsCollection.Add(TelemetryDataConstants.TotalTestsRun, runStats.ExecutedTests);

                // Collecting Test Run State
                this.requestData.MetricsCollection.Add(TelemetryDataConstants.RunState, canceled ? "Canceled" : (aborted ? "Aborted" : "Completed"));

                // Collecting Number of Adapters Used to run tests.
                this.requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAdapterUsedToRunTests, this.ExecutorUrisThatRanTests.Count());

                if (lastChunkTestResults.Any() && this.IsTestSourceIsPackage())
                {
                    this.UpdateTestCaseSourceToPackage(lastChunkTestResults, null, out lastChunkTestResults, out var updatedTestCases);
                }

                var testRunChangedEventArgs = new TestRunChangedEventArgs(runStats, lastChunkTestResults, Enumerable.Empty<TestCase>());

                // Adding Metrics along with Test Run Complete Event Args
                Collection<AttachmentSet> attachments = this.frameworkHandle?.Attachments;
                var testRunCompleteEventArgs = new TestRunCompleteEventArgs(
                    runStats,
                    canceled,
                    aborted,
                    exception,
                    attachments,
                    elapsedTime);
                testRunCompleteEventArgs.Metrics = this.requestData.MetricsCollection.Metrics;

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

        private bool IsTestSourceIsPackage()
        {
            return string.IsNullOrEmpty(this.package) == false;
        }

        private void OnCacheHit(TestRunStatistics testRunStats, ICollection<TestResult> results, ICollection<TestCase> inProgressTestCases)
        {
            if (this.testRunEventsHandler != null)
            {
                if (this.IsTestSourceIsPackage())
                {
                    this.UpdateTestCaseSourceToPackage(results, inProgressTestCases, out results, out inProgressTestCases);
                }

                var testRunChangedEventArgs = new TestRunChangedEventArgs(testRunStats, results, inProgressTestCases);
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

        private bool TryToRunInSTAThread(Action action, bool waitForCompletion)
        {
            bool success = true;
            try
            {
                EqtTrace.Verbose("BaseRunTests.TryToRunInSTAThread: Using STA thread to call adapter API.");
                this.platformThread.Run(action, PlatformApartmentState.STA, waitForCompletion);
            }
            catch (ThreadApartmentStateNotSupportedException ex)
            {
                success = false;
                EqtTrace.Warning("BaseRunTests.TryToRunInSTAThread: Failed to run in STA thread: {0}", ex);
                this.TestRunEventsHandler.HandleLogMessage(
                    TestMessageLevel.Warning,
                    string.Format(CultureInfo.CurrentUICulture, Resources.ExecutionThreadApartmentStateNotSupportedForFramework, this.runConfiguration.TargetFramework.ToString()));
            }

            return success;
        }

        private void UpdateTestCaseSourceToPackage(
            ICollection<TestResult> testResults,
            ICollection<TestCase> inProgressTestCases,
            out ICollection<TestResult> updatedTestResults,
            out ICollection<TestCase> updatedInProgressTestCases)
        {
            EqtTrace.Verbose("BaseRunTests.UpdateTestCaseSourceToPackage: Update source details for testResults and testCases.");

            updatedTestResults = this.UpdateTestResults(testResults, this.package);
            updatedInProgressTestCases = this.UpdateInProgressTests(inProgressTestCases, this.package);
        }

        private ICollection<TestResult> UpdateTestResults(ICollection<TestResult> testResults, string package)
        {
            ICollection<TestResult> updatedTestResults = new List<TestResult>();

            foreach (var testResult in testResults)
            {
                var updatedTestResult = this.dataSerializer.Clone<TestResult>(testResult);
                updatedTestResult.TestCase.Source = package;
                updatedTestResults.Add(updatedTestResult);
            }

            return updatedTestResults;
        }

        private ICollection<TestCase> UpdateInProgressTests(ICollection<TestCase> inProgressTestCases, string package)
        {
            if (inProgressTestCases == null)
            {
                return null;
            }

            ICollection<TestCase> updatedTestCases = new List<TestCase>();
            foreach (var inProgressTestCase in inProgressTestCases)
            {
                var updatedTestCase = this.dataSerializer.Clone<TestCase>(inProgressTestCase);
                updatedTestCase.Source = package;
                updatedTestCases.Add(updatedTestCase);
            }

            return updatedTestCases;
        }

        #endregion
    }
}