// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

    using Constants = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Constants;

    /// <summary>
    /// Orchestrates test execution operations for the engine communicating with the client.
    /// </summary>
    internal class ProxyExecutionManager : ProxyOperationManager, IProxyExecutionManager
    {
        private readonly ITestHostProvider testHostManager;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class. 
        /// </summary>
        /// <param name="testHostManager">Test host manager for this proxy.</param>
        public ProxyExecutionManager(ITestHostProvider testHostManager) : this(new TestRequestSender(), testHostManager, Constants.ClientConnectionTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManager"/> class. 
        /// Constructor with Dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="requestSender">Request Sender instance</param>
        /// <param name="testHostManager">Test host manager instance</param>
        /// <param name="clientConnectionTimeout">The client Connection Timeout</param>
        internal ProxyExecutionManager(ITestRequestSender requestSender, ITestHostProvider testHostManager, int clientConnectionTimeout)
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
                this.InitializeExtensions(Enumerable.Empty<string>());
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
            try
            {
                if (!this.testHostManager.Shared)
                {
                    // Non shared test host requires test source information to launch. Provide the sources
                    // information and create the channel.
                    EqtTrace.Verbose("ProxyExecutionManager: Test host is non shared. Lazy initialize.");
                    var testSources = testRunCriteria.Sources;

                    // If the test execution is with a test filter, group them by sources
                    if (testRunCriteria.HasSpecificTests)
                    {
                        testSources = testRunCriteria.Tests.GroupBy(tc => tc.Source).Select(g => g.Key);
                    }

                    this.InitializeExtensions(testSources);
                }

                this.SetupChannel(testRunCriteria.Sources);

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
            }
            catch (Exception exception)
            {
                EqtTrace.Error("ProxyExecutionManager.StartTestRun: Failed to start test run: {0}", exception);
                var completeArgs = new TestRunCompleteEventArgs(null, false, false, exception, new Collection<AttachmentSet>(), TimeSpan.Zero);
                eventHandler.HandleLogMessage(TestMessageLevel.Error, exception.Message);
                eventHandler.HandleTestRunComplete(completeArgs, null, null, null);
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
        public void Abort()
        {
            this.RequestSender.SendTestRunAbort();
        }

        #endregion

        private void InitializeExtensions(IEnumerable<string> sources)
        {
            var sourceList = sources.ToList();
            var extensions = this.testHostManager.GetTestPlatformExtensions(sourceList).ToList();
            if (TestPluginCache.Instance.PathToAdditionalExtensions != null)
            {
                extensions.AddRange(TestPluginCache.Instance.PathToAdditionalExtensions);
            }

            // Only send this if needed.
            if (extensions.Count > 0)
            {
                this.SetupChannel(sourceList);

                this.RequestSender.InitializeExecution(extensions, TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
            }
        }
    }
}