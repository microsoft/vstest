// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using ObjectModel;
    using ObjectModel.Client;

    internal class RunTestsWithTests : BaseRunTests
    {
        private IEnumerable<TestCase> testCases;

        private Dictionary<Tuple<Uri, string>, List<TestCase>> executorUriVsTestList;

        public RunTestsWithTests(IEnumerable<TestCase> testCases, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler)
            : this(testCases, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, null)
        {
        }

        /// <summary>
        /// Used for unit testing only.
        /// </summary>
        /// <param name="testCases"></param>
        /// <param name="testRunCache"></param>
        /// <param name="runSettings"></param>
        /// <param name="testExecutionContext"></param>
        /// <param name="testCaseEventsHandler"></param>
        /// <param name="testRunEventsHandler"></param>
        /// <param name="executorUriVsTestList"></param>
        internal RunTestsWithTests(IEnumerable<TestCase> testCases, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, Dictionary<Tuple<Uri, string>, List<TestCase>> executorUriVsTestList)
            : base(runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler)
        {
            this.testCases = testCases;
            this.executorUriVsTestList = executorUriVsTestList;
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

        protected override void InvokeExecutor(LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor, Tuple<Uri, string> executorUri, RunContext runContext, IFrameworkHandle frameworkHandle)
        {
            executor?.Value.RunTests(this.executorUriVsTestList[executorUri], runContext, frameworkHandle);
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

                // Todo aajohn Fill this in with the right extension value.
                var executorUriExtensionTuple = new Tuple<Uri, string>(
                    test.ExecutorUri,
                    ObjectModel.Constants.UnspecifiedAdapterPath);

                if (result.TryGetValue(executorUriExtensionTuple, out testList))
                {
                    testList.Add(test);
                }
                else
                {
                    testList = new List<TestCase>();
                    testList.Add(test);
                    result.Add(executorUriExtensionTuple, testList);
                }
            }

            return result;
        }
    }
}
