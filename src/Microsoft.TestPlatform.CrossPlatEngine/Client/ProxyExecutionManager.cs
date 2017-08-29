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
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Constants = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Constants;

    /// <summary>
    /// Orchestrates test execution operations for the engine communicating with the client.
    /// </summary>
    internal class ProxyExecutionManager : ProxyOperationManager, IProxyExecutionManager
    {
        private readonly ITestRuntimeProvider testHostManager;
        private IDataSerializer dataSerializer;
        private CancellationTokenSource cancellationTokenSource;
        private bool isCommunicationEstablished;

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; } = false;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class. 
        /// </summary>
        /// <param name="requestSender">Test request sender instance.</param>
        /// <param name="testHostManager">Test host manager for this proxy.</param>
        public ProxyExecutionManager(ITestRequestSender requestSender, ITestRuntimeProvider testHostManager) : this(requestSender, testHostManager, JsonDataSerializer.Instance, Constants.ClientConnectionTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class. 
        /// Constructor with Dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="requestSender">Request Sender instance</param>
        /// <param name="testHostManager">Test host manager instance</param>
        /// <param name="dataSerializer"></param>
        /// <param name="clientConnectionTimeout">The client Connection Timeout</param>
        internal ProxyExecutionManager(ITestRequestSender requestSender, ITestRuntimeProvider testHostManager, IDataSerializer dataSerializer, int clientConnectionTimeout)
            : base(requestSender, testHostManager, clientConnectionTimeout)
        {
            this.testHostManager = testHostManager;
            this.dataSerializer = dataSerializer;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.isCommunicationEstablished = false;
        }

        #endregion

        #region IProxyExecutionManager implementation.

        /// <summary>
        /// Ensure that the Execution component of engine is ready for execution usually by loading extensions.
        /// </summary>
        public virtual void Initialize()
        {
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
            try
            {
                EqtTrace.Verbose("ProxyExecutionManager: Test host is always Lazy initialize.");
                var testSources = testRunCriteria.Sources;

                // If the test execution is with a test filter, group them by sources
                if (testRunCriteria.HasSpecificTests)
                {
                    testSources = testRunCriteria.Tests.GroupBy(tc => tc.Source).Select(g => g.Key);
                }

                this.isCommunicationEstablished = this.SetupChannel(testSources, this.cancellationTokenSource.Token);

                if (this.isCommunicationEstablished)
                {
                    if (this.cancellationTokenSource.IsCancellationRequested)
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("ProxyExecutionManager.StartTestRun: Canceling the current run after getting cancelation request.");
                        }
                        throw new TestPlatformException(Resources.Resources.CancelationRequested);
                    }

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
                        testCaseFilter: testRunCriteria.TestCaseFilter);

                    // This is workaround for the bug https://github.com/Microsoft/vstest/issues/970
                    var runsettings = this.RemoveNodesFromRunsettingsIfRequired(testRunCriteria.TestRunSettings, (testMessageLevel, message) => { this.LogMessage(testMessageLevel, message, eventHandler); });

                    var actualTestSources = this.testHostManager.GetTestSources(testSources);

                    // For netcore/fullclr both packages and sources are same thing, 
                    // For UWP the actual source(exe) differs from input source(.appxrecipe) which we call package.
                    // So in such models we check if they differ, then we pass this info to test host to update TestCase source with package info,
                    // since this is needed by IDE's to map a TestCase to project.
                    var testSourcesDiffer = testSources.Except(actualTestSources).Any();

                    if (testRunCriteria.HasSpecificSources)
                    {
                        // Allow TestRuntimeProvider to update source map, this is required for remote scenarios.
                        // If we run for specific tests, then we expect the test case object to contain correct source path for remote scenario as well
                        if(testSourcesDiffer)
                        {
                            this.UpdateTestSources(testRunCriteria.Sources, testRunCriteria.AdapterSourceMap);
                        }

                        var runRequest = new TestRunCriteriaWithSources(
                            testRunCriteria.AdapterSourceMap,
                            testSourcesDiffer ? testSources : null,
                            runsettings,
                            executionContext);

                        this.RequestSender.StartTestRun(runRequest, eventHandler);
                    }
                    else
                    {
                        // In UWP scenario TestCase object contains the package as source, which is not actual test source for adapters, 
                        // so update test case before sending them.
                        // This approach will fail, once a package concept is introduced, where a package can contain multiple test sources.
                        if (testSourcesDiffer)
                        {
                            testRunCriteria.Tests.ToList().ForEach(tc => tc.Source = actualTestSources.FirstOrDefault());
                        }

                        var runRequest = new TestRunCriteriaWithTests(
                            testRunCriteria.Tests,
                            testSourcesDiffer ? testSources : null,
                            runsettings,
                            executionContext);

                        this.RequestSender.StartTestRun(runRequest, eventHandler);
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("ProxyExecutionManager.StartTestRun: Failed to start test run: {0}", exception);
                this.LogMessage(TestMessageLevel.Error, exception.Message, eventHandler);

                // Send a run complete to caller. Similar logic is also used in ParallelProxyExecutionManager.StartTestRunOnConcurrentManager
                // Aborted is `true`: in case of parallel run (or non shared host), an aborted message ensures another execution manager
                // created to replace the current one. This will help if the current execution manager is aborted due to irreparable error
                // and the test host is lost as well.
                var completeArgs = new TestRunCompleteEventArgs(null, false, true, exception, new Collection<AttachmentSet>(), TimeSpan.Zero);
                eventHandler.HandleTestRunComplete(completeArgs, null, null, null);
            }

            return 0;
        }

        /// <summary>
        /// Cancels the test run.
        /// </summary>
        public virtual void Cancel()
        {
            // Cancel fast, try to stop testhost deployment/launch
            this.cancellationTokenSource.Cancel();
            if (this.isCommunicationEstablished)
            {
                this.RequestSender.SendTestRunCancel();
            }
        }

        /// <summary>
        /// Aborts the test run.
        /// </summary>
        public void Abort()
        {
            this.RequestSender.SendTestRunAbort();
        }

        #endregion

        private void LogMessage(TestMessageLevel testMessageLevel, string message, ITestRunEventsHandler eventHandler)
        {
            // Log to vs ide test output
            var testMessagePayload = new TestMessagePayload { MessageLevel = testMessageLevel, Message = message };
            var rawMessage = this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            eventHandler.HandleRawMessage(rawMessage);

            // Log to vstest.console
            eventHandler.HandleLogMessage(testMessageLevel, message);
        }

        private void InitializeExtensions(IEnumerable<string> sources)
        {
            var extensions = new List<string>();

            if (TestPluginCache.Instance.PathToExtensions != null)
            {
                extensions.AddRange(TestPluginCache.Instance.PathToExtensions.Where(ext => ext.EndsWith(TestPlatformConstants.TestAdapterEndsWithPattern, StringComparison.OrdinalIgnoreCase)));
            }

            extensions.AddRange(TestPluginCache.Instance.DefaultExtensionPaths);
            var sourceList = sources.ToList();
            var platformExtensions = this.testHostManager.GetTestPlatformExtensions(sourceList, extensions).ToList();

            // Only send this if needed.
            if (platformExtensions.Any())
            {
                this.RequestSender.InitializeExecution(platformExtensions, TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
            }
        }
    }
}