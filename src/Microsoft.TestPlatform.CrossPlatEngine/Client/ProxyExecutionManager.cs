// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// Orchestrates test execution operations for the engine communicating with the client.
    /// </summary>
    internal class ProxyExecutionManager : ProxyOperationManager, IProxyExecutionManager, ITestRunEventsHandler2
    {
        private readonly ITestRuntimeProvider testHostManager;
        private IDataSerializer dataSerializer;
        private bool isCommunicationEstablished;
        private IRequestData requestData;
        private ITestRunEventsHandler baseTestRunEventsHandler;
        private bool skipDefaultAdapters;
        private readonly IFileHelper fileHelper;

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; } = false;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class.
        /// </summary>
        /// <param name="requestData">The Request Data for providing services and data for Run.</param>
        /// <param name="requestSender">Test request sender instance.</param>
        /// <param name="testHostManager">Test host manager for this proxy.</param>
        public ProxyExecutionManager(IRequestData requestData, ITestRequestSender requestSender, ITestRuntimeProvider testHostManager) :
            this(requestData, requestSender, testHostManager, JsonDataSerializer.Instance, new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class.
        /// Constructor with Dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="requestData">The Request Data for Common services and data for Run.</param>
        /// <param name="requestSender">Request Sender instance</param>
        /// <param name="testHostManager">Test host manager instance</param>
        /// <param name="dataSerializer"></param>
        internal ProxyExecutionManager(IRequestData requestData, ITestRequestSender requestSender,
            ITestRuntimeProvider testHostManager, IDataSerializer dataSerializer, IFileHelper fileHelper)
            : base(requestData, requestSender, testHostManager)
        {
            this.testHostManager = testHostManager;
            this.dataSerializer = dataSerializer;
            this.isCommunicationEstablished = false;
            this.requestData = requestData;
            this.fileHelper = fileHelper;
        }

        #endregion

        #region IProxyExecutionManager implementation.

        /// <summary>
        /// Ensure that the Execution component of engine is ready for execution usually by loading extensions.
        /// <param name="skipDefaultAdapters">Skip default adapters flag.</param>
        /// </summary>
        public virtual void Initialize(bool skipDefaultAdapters)
        {
            this.skipDefaultAdapters = skipDefaultAdapters;
            this.IsInitialized = true;
        }

        /// <summary>
        /// Starts the test run
        /// </summary>
        /// <param name="testRunCriteria"> The settings/options for the test run. </param>
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        /// <returns> The process id of the runner executing tests. </returns>
        public virtual int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            this.baseTestRunEventsHandler = eventHandler;

            try
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("ProxyExecutionManager: Test host is always Lazy initialize.");
                }

                var testSources = new List<string>(testRunCriteria.HasSpecificSources ? testRunCriteria.Sources :
                                                    // If the test execution is with a test filter, group them by sources
                                                    testRunCriteria.Tests.GroupBy(tc => tc.Source).Select(g => g.Key));

                this.isCommunicationEstablished = this.SetupChannel(testSources, testRunCriteria.TestRunSettings);

                if (this.isCommunicationEstablished)
                {
                    this.CancellationTokenSource.Token.ThrowTestPlatformExceptionIfCancellationRequested();

                    this.InitializeExtensions(testSources);

                    // This code should be in sync with InProcessProxyExecutionManager.StartTestRun executionContext
                    var executionContext = new TestExecutionContext(
                        testRunCriteria.FrequencyOfRunStatsChangeEvent,
                        testRunCriteria.RunStatsChangeEventTimeout,
                        inIsolation: false,
                        keepAlive: testRunCriteria.KeepAlive,
                        isDataCollectionEnabled: false,
                        areTestCaseLevelEventsRequired: false,
                        hasTestRun: true,
                        isDebug: (testRunCriteria.TestHostLauncher != null && testRunCriteria.TestHostLauncher.IsDebug),
                        testCaseFilter: testRunCriteria.TestCaseFilter,
                        filterOptions: testRunCriteria.FilterOptions);

                    // This is workaround for the bug https://github.com/Microsoft/vstest/issues/970
                    var runsettings = this.RemoveNodesFromRunsettingsIfRequired(testRunCriteria.TestRunSettings, (testMessageLevel, message) => { this.LogMessage(testMessageLevel, message); });

                    if (testRunCriteria.HasSpecificSources)
                    {
                        var runRequest = testRunCriteria.CreateTestRunCriteriaForSources(testHostManager, runsettings, executionContext, testSources);
                        this.RequestSender.StartTestRun(runRequest, this);
                    }
                    else
                    {
                        var runRequest = testRunCriteria.CreateTestRunCriteriaForTests(testHostManager, runsettings, executionContext, testSources);
                        this.RequestSender.StartTestRun(runRequest, this);
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("ProxyExecutionManager.StartTestRun: Failed to start test run: {0}", exception);

                // Log error message to design mode and CLI.
                // TestPlatformException is expected exception, log only the message
                // For other exceptions, log the stacktrace as well
                var errorMessage = exception is TestPlatformException ? exception.Message : exception.ToString();
                var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = errorMessage };
                this.HandleRawMessage(this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload));
                this.LogMessage(TestMessageLevel.Error, errorMessage);

                // Send a run complete to caller. Similar logic is also used in ParallelProxyExecutionManager.StartTestRunOnConcurrentManager
                // Aborted is `true`: in case of parallel run (or non shared host), an aborted message ensures another execution manager
                // created to replace the current one. This will help if the current execution manager is aborted due to irreparable error
                // and the test host is lost as well.
                var completeArgs = new TestRunCompleteEventArgs(null, false, true, null, new Collection<AttachmentSet>(), TimeSpan.Zero);
                var testRunCompletePayload = new TestRunCompletePayload { TestRunCompleteArgs = completeArgs };
                this.HandleRawMessage(this.dataSerializer.SerializePayload(MessageType.ExecutionComplete, testRunCompletePayload));
                this.HandleTestRunComplete(completeArgs, null, null, null);
            }

            return 0;
        }

        /// <summary>
        /// Cancels the test run.
        /// </summary>
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        public virtual void Cancel(ITestRunEventsHandler eventHandler)
        {
            // Just in case ExecuteAsync isn't called yet, set the eventhandler
            if (this.baseTestRunEventsHandler == null)
            {
                this.baseTestRunEventsHandler = eventHandler;
            }

            // Cancel fast, try to stop testhost deployment/launch
            this.CancellationTokenSource.Cancel();
            if (this.isCommunicationEstablished)
            {
                this.RequestSender.SendTestRunCancel();
            }
        }

        /// <inheritdoc/>
        public virtual int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            return this.baseTestRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);
        }

        /// <inheritdoc />
        public bool AttachDebuggerToProcess(int pid)
        {
            return ((ITestRunEventsHandler2)this.baseTestRunEventsHandler).AttachDebuggerToProcess(pid);
        }

        /// <summary>
        /// Aborts the test run.
        /// </summary>
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        public void Abort(ITestRunEventsHandler eventHandler)
        {
            // Just in case ExecuteAsync isn't called yet, set the eventhandler
            if (this.baseTestRunEventsHandler == null)
            {
                this.baseTestRunEventsHandler = eventHandler;
            }

            // Cancel fast, try to stop testhost deployment/launch
            this.CancellationTokenSource.Cancel();

            if (this.isCommunicationEstablished)
            {
                this.RequestSender.SendTestRunAbort();
            }
        }

        /// <inheritdoc/>
        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            this.baseTestRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, lastChunkArgs, runContextAttachments, executorUris);
        }

        /// <inheritdoc/>
        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            this.baseTestRunEventsHandler.HandleTestRunStatsChange(testRunChangedArgs);
        }

        /// <inheritdoc/>
        public void HandleRawMessage(string rawMessage)
        {
            var message = this.dataSerializer.DeserializeMessage(rawMessage);

            if (string.Equals(message.MessageType, MessageType.ExecutionComplete))
            {
                this.Close();
            }

            this.baseTestRunEventsHandler.HandleRawMessage(rawMessage);
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.baseTestRunEventsHandler.HandleLogMessage(level, message);
        }

        #endregion

        private void LogMessage(TestMessageLevel testMessageLevel, string message)
        {
            // Log to vs ide test output
            var testMessagePayload = new TestMessagePayload { MessageLevel = testMessageLevel, Message = message };
            var rawMessage = this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            this.HandleRawMessage(rawMessage);

            // Log to vstest.console
            this.HandleLogMessage(testMessageLevel, message);
        }

        private void InitializeExtensions(IEnumerable<string> sources)
        {
            var extensions = TestPluginCache.Instance.GetExtensionPaths(TestPlatformConstants.TestAdapterEndsWithPattern, this.skipDefaultAdapters);

            // Filter out non existing extensions
            var nonExistingExtensions = extensions.Where(extension => !this.fileHelper.Exists(extension));
            if (nonExistingExtensions.Any())
            {
                this.LogMessage(TestMessageLevel.Warning, string.Format(Resources.Resources.NonExistingExtensions, string.Join(",", nonExistingExtensions)));
            }

            var sourceList = sources.ToList();
            var platformExtensions = this.testHostManager.GetTestPlatformExtensions(sourceList, extensions.Except(nonExistingExtensions));

            // Only send this if needed.
            if (platformExtensions.Any())
            {
                this.RequestSender.InitializeExecution(platformExtensions);
            }
        }
    }
}
