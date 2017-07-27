// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;

    class NoIsolationProxyexecutionManager : IProxyExecutionManager
    {
        private ITestHostManagerFactory testHostManagerFactory;
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoIsolationProxyexecutionManager"/> class.
        /// </summary>
        public NoIsolationProxyexecutionManager() : this(new TestHostManagerFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoIsolationProxyexecutionManager"/> class.
        /// </summary>
        /// <param name="testHostManagerFactory">
        /// Manager factory
        /// </param>
        protected NoIsolationProxyexecutionManager(ITestHostManagerFactory testHostManagerFactory)
        {
            this.testHostManagerFactory = testHostManagerFactory;
        }

        public void Initialize()
        {
            this.IsInitialized = true;
        }

        public int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            // Initialize extension before execution
            var executionManager = this.testHostManagerFactory.GetExecutionManager();

            executionManager.Initialize(Enumerable.Empty<string>());

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
                // [TODO]: we need to revisit to second-last argument if we will enable datacollector. 
                executionManager.StartTestRun(testRunCriteria.AdapterSourceMap, testRunCriteria.TestRunSettings, executionContext, null, eventHandler);
            }
            else
            {
                // [TODO]: we need to revisit to second-last argument if we will enable datacollector. 
                executionManager.StartTestRun(testRunCriteria.Tests, testRunCriteria.TestRunSettings, executionContext, null, eventHandler);
            }

            return 0;
        }

        /// <summary>
        /// Aborts the test operation.
        /// </summary>
        public void Abort()
        {
            this.testHostManagerFactory.GetExecutionManager().Abort();
        }

        /// <summary>
        /// Cancels the test run.
        /// </summary>
        public void Cancel()
        {
            this.testHostManagerFactory.GetExecutionManager().Cancel();
        }

        /// <summary>
        /// Closes the current test operation.
        /// This function is of no use in this context as we are not creating any testhost
        /// </summary>
        public void Close()
        {
        }
    }
}
