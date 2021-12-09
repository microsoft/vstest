﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    internal class InProcessProxyExecutionManager : IProxyExecutionManager
    {
        private ITestHostManagerFactory testHostManagerFactory;
        private IExecutionManager executionManager;
        private ITestRuntimeProvider testHostManager;

        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="InProcessProxyexecutionManager"/> class.
        /// </summary>
        /// <param name="testHostManager">
        /// The test Host Manager.
        /// </param>
        /// <param name="testHostManagerFactory">
        /// Manager factory
        /// </param>
        public InProcessProxyExecutionManager(ITestRuntimeProvider testHostManager, ITestHostManagerFactory testHostManagerFactory)
        {
            this.testHostManager = testHostManager;
            this.testHostManagerFactory = testHostManagerFactory;
            this.executionManager = this.testHostManagerFactory.GetExecutionManager();
        }

        /// <summary>
        /// Initialize adapters.
        /// <param name="skipDefaultAdapters">Skip default adapters flag.</param>
        /// </summary>
        public void Initialize(bool skipDefaultAdapters)
        {
        }

        /// <inheritdoc/>
        public int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            try
            {
                var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(testRunCriteria.TestRunSettings);
                var testPackages = new List<string>(testRunCriteria.HasSpecificSources ? testRunCriteria.Sources :
                                                    // If the test execution is with a test filter, group them by sources
                                                    testRunCriteria.Tests.GroupBy(tc => tc.Source).Select(g => g.Key));

                // This code should be in sync with ProxyExecutionManager.StartTestRun executionContext
                var executionContext = new TestExecutionContext(
                            testRunCriteria.FrequencyOfRunStatsChangeEvent,
                            testRunCriteria.RunStatsChangeEventTimeout,
                            inIsolation: runConfiguration.InIsolation,
                            keepAlive: testRunCriteria.KeepAlive,
                            isDataCollectionEnabled: false,
                            areTestCaseLevelEventsRequired: false,
                            hasTestRun: true,
                            isDebug: (testRunCriteria.TestHostLauncher != null && testRunCriteria.TestHostLauncher.IsDebug),
                            testCaseFilter: testRunCriteria.TestCaseFilter,
                            filterOptions: testRunCriteria.FilterOptions);

                // Initialize extension before execution
                this.InitializeExtensions(testPackages);

                if (testRunCriteria.HasSpecificSources)
                {
                    var runRequest = testRunCriteria.CreateTestRunCriteriaForSources(testHostManager, testRunCriteria.TestRunSettings, executionContext, testPackages);

                    Task.Run(() => executionManager.StartTestRun(runRequest.AdapterSourceMap, runRequest.Package,
                        runRequest.RunSettings, runRequest.TestExecutionContext, null, eventHandler));
                }
                else
                {
                    var runRequest = testRunCriteria.CreateTestRunCriteriaForTests(testHostManager, testRunCriteria.TestRunSettings, executionContext, testPackages);

                    Task.Run(() => executionManager.StartTestRun(runRequest.Tests, runRequest.Package,
                        runRequest.RunSettings, runRequest.TestExecutionContext, null, eventHandler));
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
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        public void Abort(ITestRunEventsHandler eventHandler)
        {
            Task.Run(() => this.testHostManagerFactory.GetExecutionManager().Abort(eventHandler));
        }

        /// <summary>
        /// Cancels the test run.
        /// </summary>
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        public void Cancel(ITestRunEventsHandler eventHandler)
        {
            Task.Run(() => this.testHostManagerFactory.GetExecutionManager().Cancel(eventHandler));
        }

        /// <summary>
        /// Closes the current test operation.
        /// This function is of no use in this context as we are not creating any testhost
        /// </summary>
        public void Close()
        {
        }


        private void InitializeExtensions(IEnumerable<string> sources)
        {
            var extensionsFromSource = this.testHostManager.GetTestPlatformExtensions(sources, Enumerable.Empty<string>());
            if (extensionsFromSource.Any())
            {
                TestPluginCache.Instance.UpdateExtensions(extensionsFromSource, false);
            }

            // We don't need to pass list of extension as we are running inside vstest.console and
            // it will use TestPluginCache of vstest.console
            executionManager.Initialize(Enumerable.Empty<string>(), null);
        }
    }
}
