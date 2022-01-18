// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;

    using ObjectModel.Client;

    internal class RunTestsWithTests : BaseRunTests
    {
        private IEnumerable<TestCase> testCases;

        private Dictionary<Tuple<Uri, string>, List<TestCase>> executorUriVsTestList;

        private ITestCaseEventsHandler testCaseEventsHandler;

        public RunTestsWithTests(IRequestData requestData, IEnumerable<TestCase> testCases, string package, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler)
            : this(requestData, testCases, package, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, null)
        {
        }

        /// <summary>
        /// Used for unit testing only.
        /// </summary>
        /// <param name="testCases"></param>
        /// <param name="package">The user input test source(package) if it differ from actual test source otherwise null.</param>
        /// <param name="testRunCache"></param>
        /// <param name="runSettings"></param>
        /// <param name="testExecutionContext"></param>
        /// <param name="testCaseEventsHandler"></param>
        /// <param name="testRunEventsHandler"></param>
        /// <param name="executorUriVsTestList"></param>
        internal RunTestsWithTests(IRequestData requestData, IEnumerable<TestCase> testCases, string package, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, Dictionary<Tuple<Uri, string>, List<TestCase>> executorUriVsTestList)
            : base(requestData, package, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, TestPlatformEventSource.Instance)
        {
            this.testCases = testCases;
            this.executorUriVsTestList = executorUriVsTestList;
            this.testCaseEventsHandler = testCaseEventsHandler;
        }

        protected override void BeforeRaisingTestRunComplete(bool exceptionsHitDuringRunTests)
        {
            // Do Nothing.
        }

        protected override IEnumerable<Tuple<Uri, string>> GetExecutorUriExtensionMap(IFrameworkHandle testExecutorFrameworkHandle, RunContext runContext)
        {
            this.executorUriVsTestList = this.GetExecutorVsTestCaseList(this.testCases);

            Debug.Assert(this.TestExecutionContext.TestCaseFilter == null, "TestCaseFilter should be null for specific tests.");
            runContext.FilterExpressionWrapper = null;

            return this.executorUriVsTestList.Keys;
        }

        protected override void InvokeExecutor(
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
            Tuple<Uri, string> executorUri,
            RunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            executor?.Value.RunTests(this.executorUriVsTestList[executorUri], runContext, frameworkHandle);
        }

        /// <inheritdoc />
        protected override bool ShouldAttachDebuggerToTestHost(
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
            Tuple<Uri, string> executorUri,
            RunContext runContext)
        {
            // If the adapter doesn't implement the new test executor interface we should attach to
            // the default test host by default to preserve old behavior.
            if (!(executor?.Value is ITestExecutor2 convertedExecutor))
            {
                return true;
            }

            return convertedExecutor.ShouldAttachToTestHost(this.executorUriVsTestList[executorUri], runContext);
        }

        /// <summary>
        /// Sends Session-End event on in-proc datacollectors
        /// </summary>
        protected override void SendSessionEnd()
        {
            this.testCaseEventsHandler?.SendSessionEnd();
        }

        /// <summary>
        /// Sends Session-Start event on in-proc datacollectors
        /// </summary>
        protected override void SendSessionStart()
        {
            // Send session start with test sources in property bag for session start event args.
            if (this.testCaseEventsHandler == null)
            {
                return;
            }

            var properties = new Dictionary<string, object>
            {
                { "TestSources", TestSourcesUtility.GetSources(this.testCases) }
            };

            this.testCaseEventsHandler.SendSessionStart(properties);
        }

        /// <summary>
        /// Returns the executor Vs TestCase list
        /// </summary>
        private Dictionary<Tuple<Uri, string>, List<TestCase>> GetExecutorVsTestCaseList(IEnumerable<TestCase> tests)
        {
            var result = new Dictionary<Tuple<Uri, string>, List<TestCase>>();
            foreach (var test in tests)
            {
                List<TestCase> testList;

                // TODO: Fill this in with the right extension value.
                var executorUriExtensionTuple = new Tuple<Uri, string>(
                    test.ExecutorUri,
                    Constants.UnspecifiedAdapterPath);

                if (result.TryGetValue(executorUriExtensionTuple, out testList))
                {
                    testList.Add(test);
                }
                else
                {
                    testList = new List<TestCase>
                    {
                        test
                    };
                    result.Add(executorUriExtensionTuple, testList);
                }
            }

            return result;
        }
    }
}
