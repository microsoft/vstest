// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Utilities;
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
    internal class ProxyExecutionManager : IProxyExecutionManager, IBaseProxy, ITestRunEventsHandler2
    {
        private readonly TestSessionInfo testSessionInfo = null;
        readonly Func<string, ProxyExecutionManager, ProxyOperationManager> proxyOperationManagerCreator;

        private ITestRuntimeProvider testHostManager;
        private IRequestData requestData;

        private readonly IFileHelper fileHelper;
        private readonly IDataSerializer dataSerializer;
        private bool isCommunicationEstablished;

        private ProxyOperationManager proxyOperationManager = null;
        private ITestRunEventsHandler baseTestRunEventsHandler;
        private bool skipDefaultAdapters;
        private readonly bool debugEnabledForTestSession = false;

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource
        {
            get { return proxyOperationManager.CancellationTokenSource; }
            set { proxyOperationManager.CancellationTokenSource = value; }
        }
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info.</param>
        /// <param name="proxyOperationManagerCreator">The proxy operation manager creator.</param>
        /// <param name="debugEnabledForTestSession">
        /// A flag indicating if debugging should be enabled or not.
        /// </param>
        public ProxyExecutionManager(
            TestSessionInfo testSessionInfo,
            Func<string, ProxyExecutionManager, ProxyOperationManager> proxyOperationManagerCreator,
            bool debugEnabledForTestSession)
        {
            // Filling in test session info and proxy information.
            this.testSessionInfo = testSessionInfo;
            this.proxyOperationManagerCreator = proxyOperationManagerCreator;

            // This should be set to enable debugging when we have test session info available.
            this.debugEnabledForTestSession = debugEnabledForTestSession;

            requestData = null;
            testHostManager = null;
            dataSerializer = JsonDataSerializer.Instance;
            fileHelper = new FileHelper();
            isCommunicationEstablished = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class.
        /// </summary>
        /// 
        /// <param name="requestData">
        /// The request data for providing services and data for run.
        /// </param>
        /// <param name="requestSender">Test request sender instance.</param>
        /// <param name="testHostManager">Test host manager for this proxy.</param>
        public ProxyExecutionManager(
            IRequestData requestData,
            ITestRequestSender requestSender,
            ITestRuntimeProvider testHostManager) :
            this(
                requestData,
                requestSender,
                testHostManager,
                JsonDataSerializer.Instance,
                new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class.
        /// </summary>
        /// 
        /// <remarks>
        /// Constructor with dependency injection. Used for unit testing.
        /// </remarks>
        /// 
        /// <param name="requestData">The request data for common services and data for run.</param>
        /// <param name="requestSender">Request sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        /// <param name="dataSerializer">Data serializer instance.</param>
        /// <param name="fileHelper">File helper instance.</param>
        internal ProxyExecutionManager(
            IRequestData requestData,
            ITestRequestSender requestSender,
            ITestRuntimeProvider testHostManager,
            IDataSerializer dataSerializer,
            IFileHelper fileHelper)
        {
            this.testHostManager = testHostManager;
            this.dataSerializer = dataSerializer;
            isCommunicationEstablished = false;
            this.requestData = requestData;
            this.fileHelper = fileHelper;

            // Create a new proxy operation manager.
            proxyOperationManager = new ProxyOperationManager(requestData, requestSender, testHostManager, this);
        }

        #endregion

        #region IProxyExecutionManager implementation.

        /// <inheritdoc/>
        public virtual void Initialize(bool skipDefaultAdapters)
        {
            this.skipDefaultAdapters = skipDefaultAdapters;
            IsInitialized = true;
        }

        /// <inheritdoc/>
        public virtual int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            if (proxyOperationManager == null)
            {
                // In case we have an active test session, we always prefer the already
                // created proxies instead of the ones that need to be created on the spot.
                var sources = testRunCriteria.HasSpecificTests
                    ? TestSourcesUtility.GetSources(testRunCriteria.Tests)
                    : testRunCriteria.Sources;

                proxyOperationManager = proxyOperationManagerCreator(
                    sources.First(),
                    this);

                testHostManager = proxyOperationManager.TestHostManager;
                requestData = proxyOperationManager.RequestData;
            }

            baseTestRunEventsHandler = eventHandler;
            try
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("ProxyExecutionManager: Test host is always Lazy initialize.");
                }

                var testSources = new List<string>(
                    testRunCriteria.HasSpecificSources
                    ? testRunCriteria.Sources
                    // If the test execution is with a test filter, group them by sources.
                    : testRunCriteria.Tests.GroupBy(tc => tc.Source).Select(g => g.Key));

                isCommunicationEstablished = proxyOperationManager.SetupChannel(
                    testSources,
                    testRunCriteria.TestRunSettings);

                if (isCommunicationEstablished)
                {
                    proxyOperationManager.CancellationTokenSource.Token.ThrowTestPlatformExceptionIfCancellationRequested();

                    InitializeExtensions(testSources);

                    // This code should be in sync with InProcessProxyExecutionManager.StartTestRun
                    // execution context.
                    var executionContext = new TestExecutionContext(
                        testRunCriteria.FrequencyOfRunStatsChangeEvent,
                        testRunCriteria.RunStatsChangeEventTimeout,
                        inIsolation: false,
                        keepAlive: testRunCriteria.KeepAlive,
                        isDataCollectionEnabled: false,
                        areTestCaseLevelEventsRequired: false,
                        hasTestRun: true,
                        // Debugging should happen if there's a custom test host launcher present
                        // and is in debugging mode, or if the debugging is enabled in case the
                        // test session info is present.
                        isDebug:
                            (testRunCriteria.TestHostLauncher != null && testRunCriteria.TestHostLauncher.IsDebug)
                            || debugEnabledForTestSession,
                        testCaseFilter: testRunCriteria.TestCaseFilter,
                        filterOptions: testRunCriteria.FilterOptions);

                    // This is workaround for the bug https://github.com/Microsoft/vstest/issues/970
                    var runsettings = proxyOperationManager.RemoveNodesFromRunsettingsIfRequired(
                        testRunCriteria.TestRunSettings,
                        (testMessageLevel, message) => LogMessage(testMessageLevel, message));

                    if (testRunCriteria.HasSpecificSources)
                    {
                        var runRequest = testRunCriteria.CreateTestRunCriteriaForSources(
                            testHostManager,
                            runsettings,
                            executionContext,
                            testSources);
                        proxyOperationManager.RequestSender.StartTestRun(runRequest, this);
                    }
                    else
                    {
                        var runRequest = testRunCriteria.CreateTestRunCriteriaForTests(
                            testHostManager,
                            runsettings,
                            executionContext,
                            testSources);
                        proxyOperationManager.RequestSender.StartTestRun(runRequest, this);
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("ProxyExecutionManager.StartTestRun: Failed to start test run: {0}", exception);

                // Log error message to design mode and CLI.
                // TestPlatformException is expected exception, log only the message.
                // For other exceptions, log the stacktrace as well.
                var errorMessage = exception is TestPlatformException ? exception.Message : exception.ToString();
                var testMessagePayload = new TestMessagePayload()
                {
                    MessageLevel = TestMessageLevel.Error,
                    Message = errorMessage
                };
                HandleRawMessage(dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload));
                LogMessage(TestMessageLevel.Error, errorMessage);

                // Send a run complete to caller. Similar logic is also used in
                // ParallelProxyExecutionManager.StartTestRunOnConcurrentManager.
                //
                // Aborted is `true`: in case of parallel run (or non shared host), an aborted
                // message ensures another execution manager created to replace the current one.
                // This will help if the current execution manager is aborted due to irreparable
                // error and the test host is lost as well.
                var completeArgs = new TestRunCompleteEventArgs(null, false, true, null, new Collection<AttachmentSet>(), new Collection<InvokedDataCollector>(), TimeSpan.Zero);
                var testRunCompletePayload = new TestRunCompletePayload { TestRunCompleteArgs = completeArgs };
                HandleRawMessage(dataSerializer.SerializePayload(MessageType.ExecutionComplete, testRunCompletePayload));
                HandleTestRunComplete(completeArgs, null, null, null);
            }

            return 0;
        }

        /// <inheritdoc/>
        public virtual void Cancel(ITestRunEventsHandler eventHandler)
        {
            // Just in case ExecuteAsync isn't called yet, set the eventhandler.
            if (baseTestRunEventsHandler == null)
            {
                baseTestRunEventsHandler = eventHandler;
            }

            // Do nothing if the proxy is not initialized yet.
            if (proxyOperationManager == null)
            {
                return;
            }

            // Cancel fast, try to stop testhost deployment/launch.
            proxyOperationManager.CancellationTokenSource.Cancel();
            if (isCommunicationEstablished)
            {
                proxyOperationManager.RequestSender.SendTestRunCancel();
            }
        }

        /// <inheritdoc/>
        public void Abort(ITestRunEventsHandler eventHandler)
        {
            // Just in case ExecuteAsync isn't called yet, set the eventhandler.
            if (baseTestRunEventsHandler == null)
            {
                baseTestRunEventsHandler = eventHandler;
            }

            // Do nothing if the proxy is not initialized yet.
            if (proxyOperationManager == null)
            {
                return;
            }

            // Cancel fast, try to stop testhost deployment/launch.
            proxyOperationManager.CancellationTokenSource.Cancel();

            if (isCommunicationEstablished)
            {
                proxyOperationManager.RequestSender.SendTestRunAbort();
            }
        }

        /// <inheritdoc/>
        public void Close()
        {
            // Do nothing if the proxy is not initialized yet.
            if (proxyOperationManager == null)
            {
                return;
            }

            // When no test session is being used we don't share the testhost
            // between test discovery and test run. The testhost is closed upon
            // successfully completing the operation it was spawned for.
            //
            // In contrast, the new workflow (using test sessions) means we should keep
            // the testhost alive until explicitly closed by the test session owner.
            if (testSessionInfo == null)
            {
                proxyOperationManager.Close();
                return;
            }

            TestSessionPool.Instance.ReturnProxy(testSessionInfo, proxyOperationManager.Id);
        }

        /// <inheritdoc/>
        public virtual int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            return baseTestRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);
        }

        /// <inheritdoc />
        public bool AttachDebuggerToProcess(int pid)
        {
            return ((ITestRunEventsHandler2)baseTestRunEventsHandler).AttachDebuggerToProcess(pid);
        }

        /// <inheritdoc/>
        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            baseTestRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, lastChunkArgs, runContextAttachments, executorUris);
        }

        /// <inheritdoc/>
        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            baseTestRunEventsHandler.HandleTestRunStatsChange(testRunChangedArgs);
        }

        /// <inheritdoc/>
        public void HandleRawMessage(string rawMessage)
        {
            var message = dataSerializer.DeserializeMessage(rawMessage);

            if (string.Equals(message.MessageType, MessageType.ExecutionComplete))
            {
                Close();
            }

            baseTestRunEventsHandler.HandleRawMessage(rawMessage);
        }

        /// <inheritdoc/>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            baseTestRunEventsHandler.HandleLogMessage(level, message);
        }

        #endregion

        #region IBaseProxy implementation.
        /// <inheritdoc/>
        public virtual TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
        {
            // Update Telemetry Opt in status because by default in Test Host Telemetry is opted out
            var telemetryOptedIn = proxyOperationManager.RequestData.IsTelemetryOptedIn ? "true" : "false";
            testProcessStartInfo.Arguments += " --telemetryoptedin " + telemetryOptedIn;
            return testProcessStartInfo;
        }
        #endregion

        /// <summary>
        /// Ensures that the engine is ready for test operations. Usually includes starting up the
        /// test host process.
        /// </summary>
        /// 
        /// <param name="sources">List of test sources.</param>
        /// <param name="runSettings">Run settings to be used.</param>
        /// 
        /// <returns>
        /// Returns true if the communication is established b/w runner and host, false otherwise.
        /// </returns>
        public virtual bool SetupChannel(IEnumerable<string> sources, string runSettings)
        {
            return proxyOperationManager.SetupChannel(sources, runSettings);
        }

        private void LogMessage(TestMessageLevel testMessageLevel, string message)
        {
            // Log to vs ide test output.
            var testMessagePayload = new TestMessagePayload { MessageLevel = testMessageLevel, Message = message };
            var rawMessage = dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            HandleRawMessage(rawMessage);

            // Log to vstest.console.
            HandleLogMessage(testMessageLevel, message);
        }

        private void InitializeExtensions(IEnumerable<string> sources)
        {
            var extensions = TestPluginCache.Instance.GetExtensionPaths(TestPlatformConstants.TestAdapterEndsWithPattern, skipDefaultAdapters);

            // Filter out non existing extensions.
            var nonExistingExtensions = extensions.Where(extension => !fileHelper.Exists(extension));
            if (nonExistingExtensions.Any())
            {
                LogMessage(TestMessageLevel.Warning, string.Format(Resources.Resources.NonExistingExtensions, string.Join(",", nonExistingExtensions)));
            }

            var sourceList = sources.ToList();
            var platformExtensions = testHostManager.GetTestPlatformExtensions(sourceList, extensions.Except(nonExistingExtensions));

            // Only send this if needed.
            if (platformExtensions.Any())
            {
                proxyOperationManager.RequestSender.InitializeExecution(platformExtensions);
            }
        }
    }
}
