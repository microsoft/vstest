// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;

    using Constants = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Constants;

    /// <summary>
    /// Orchestrates test execution operations for the engine communicating with the client.
    /// </summary>
    internal class ProxyExecutionManager : ProxyOperationManager, IProxyExecutionManager
    {
        private readonly ITestHostManager testHostManager;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class. 
        /// </summary>
        /// <param name="testHostManager">Test host manager for this proxy.</param>
        public ProxyExecutionManager(ITestHostManager testHostManager) : this(new TestRequestSender(), testHostManager, Constants.ClientConnectionTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class. 
        /// Constructor with Dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="requestSender">Request Sender instance</param>
        /// <param name="testHostManager">Test host manager instance</param>
        /// <param name="clientConnectionTimeout">The client Connection Timeout</param>
        internal ProxyExecutionManager(ITestRequestSender requestSender, ITestHostManager testHostManager, int clientConnectionTimeout)
            : base(requestSender, testHostManager, clientConnectionTimeout)
        {
            this.testHostManager = testHostManager;
        }

        #endregion

        #region IProxyExecutionManager implementation.

        /// <summary>
        /// Ensure that the Execution component of engine is ready for execution usually by loading extensions.
        /// </summary>
        public virtual void Initialize()
        {
            if (this.testHostManager.Shared)
            {
                // Shared test hosts don't require test source information to launch. Start them early
                // to allow fail fast.
                EqtTrace.Verbose("ProxyExecutionManager: Test host is shared. SetupChannel it early.");
                this.InitializeExtensions(this.testHostManager);
            }
        }

        /// <summary>
        /// Starts the test run
        /// </summary>
        /// <param name="testRunCriteria"> The settings/options for the test run. </param>
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        /// <returns> The process id of the runner executing tests. </returns>
        public virtual int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            if (!this.testHostManager.Shared)
            {
                // Non shared test host requires test source information to launch. SetupChannel them now.
                EqtTrace.Verbose("ProxyExecutionManager: Test host is non shared. Lazy initialize.");
                this.InitializeExtensions(this.testHostManager);
            }

            this.SetupChannel(this.testHostManager);

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

            if (testRunCriteria.HasSpecificSources)
            {
                var runRequest = new TestRunCriteriaWithSources(
                    testRunCriteria.AdapterSourceMap,
                    testRunCriteria.TestRunSettings,
                    executionContext);

                this.RequestSender.StartTestRun(runRequest, eventHandler);
            }
            else
            {
                var runRequest = new TestRunCriteriaWithTests(
                    testRunCriteria.Tests,
                    testRunCriteria.TestRunSettings,
                    executionContext);

                this.RequestSender.StartTestRun(runRequest, eventHandler);
            }

            return 0;
        }

        /// <summary>
        /// Cancels the test run.
        /// </summary>
        public virtual void Cancel()
        {
            this.RequestSender.SendTestRunCancel();
        }

        /// <summary>
        /// Aborts the test run.
        /// </summary>
        public override void Abort()
        {
            this.RequestSender.SendTestRunAbort();
        }

        /// <inheritdoc cref="System.IDisposable.Dispose"/>
        public override void Dispose()
        {
            this.RequestSender?.EndSession();
            base.Dispose();
        }

        #endregion

        private void InitializeExtensions(ITestHostManager testHostManager)
        {
            // Only send this if needed.
            if (TestPluginCache.Instance.PathToAdditionalExtensions != null
                && TestPluginCache.Instance.PathToAdditionalExtensions.Any())
            {
                this.SetupChannel(testHostManager);

                this.RequestSender.InitializeExecution(
                    TestPluginCache.Instance.PathToAdditionalExtensions,
                    TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
            }
        }
    }
}