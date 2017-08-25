// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    internal class InProcessProxyExecutionManager : IProxyExecutionManager
    {
        private ITestHostManagerFactory testHostManagerFactory;
        IExecutionManager executionManager;
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="InProcessProxyexecutionManager"/> class.
        /// </summary>
        public InProcessProxyExecutionManager() : this(new TestHostManagerFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InProcessProxyexecutionManager"/> class.
        /// </summary>
        /// <param name="testHostManagerFactory">
        /// Manager factory
        /// </param>
        internal InProcessProxyExecutionManager(ITestHostManagerFactory testHostManagerFactory)
        {
            this.testHostManagerFactory = testHostManagerFactory;
            this.executionManager = this.testHostManagerFactory.GetExecutionManager();
        }

        /// <summary>
        /// Initialize adapters.
        /// </summary>
        public void Initialize()
        {
            if (!this.IsInitialized)
            {
                // We don't need to pass list of extension as we are running inside vstest.console and
                // it will use TestPluginCache of vstest.console
                executionManager.Initialize(Enumerable.Empty<string>());
                this.IsInitialized = true;
            }
        }

        /// <inheritdoc/>
        public int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            try
            {
                // Initialize extension before execution if not already initialized
                this.Initialize();

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
                    Task.Run(() => executionManager.StartTestRun(testRunCriteria.AdapterSourceMap, testRunCriteria.TestRunSettings, executionContext, null, eventHandler));
                }
                else
                {
                    // [TODO]: we need to revisit to second-last argument if we will enable datacollector. 
                    Task.Run(() => executionManager.StartTestRun(testRunCriteria.Tests, testRunCriteria.TestRunSettings, executionContext, null, eventHandler));
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("InProcessProxyexecutionManager.StartTestRun: Failed to start test run: {0}", exception);

                // Send exception message.
                eventHandler.HandleLogMessage(TestMessageLevel.Error, exception.ToString());

                // Send a run complete to caller.
                var completeArgs = new TestRunCompleteEventArgs(null, false, true, exception, new Collection<AttachmentSet>(), TimeSpan.Zero);
                eventHandler.HandleTestRunComplete(completeArgs, null, null, null);
            }

            return 0;
        }

        /// <summary>
        /// Aborts the test operation.
        /// </summary>
        public void Abort()
        {
            Task.Run(() => this.testHostManagerFactory.GetExecutionManager().Abort());
        }

        /// <summary>
        /// Cancels the test run.
        /// </summary>
        public void Cancel()
        {
            Task.Run(() => this.testHostManagerFactory.GetExecutionManager().Cancel());
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
