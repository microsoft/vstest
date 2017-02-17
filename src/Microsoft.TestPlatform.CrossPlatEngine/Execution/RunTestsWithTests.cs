// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using ObjectModel;
    using ObjectModel.Client;
    using CommunicationUtilities;
    using ObjectModel.Utilities;

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
            : base(runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, TestPlatformEventSource.Instance)
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
#if NET46
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runContext.RunSettings.SettingsXml);
            if (runConfiguration.DisableAppDomain)
            {
                var testCases = this.executorUriVsTestList[executorUri];

                var sourceTestCaseMap = new Dictionary<string, List<TestCase>>();
                foreach (var test in testCases)
                {
                    var source = test.Source;
                    if (!sourceTestCaseMap.ContainsKey(source))
                    {
                        sourceTestCaseMap.Add(source, new List<TestCase>() { test });
                    }
                    else
                    {
                        sourceTestCaseMap[source].Add(test);
                    }
                }

                foreach (var sourceTests in sourceTestCaseMap)
                {
                    AppDomain appDomain = null;
                    try
                    {
                        appDomain = AppDomainHelper.CreateAppDomain(sourceTests.Key);

                        var proxyFrameworkHandle = new FrameworkHandleProxy(frameworkHandle);

                        var appDomainFrameworkHandle = AppDomainHelper.CreateObjectInNewDomain<FrameworkHandleInAppDomain>(appDomain, proxyFrameworkHandle, runContext.RunSettings.SettingsXml);

                        var adapterManager = AppDomainHelper.CreateObjectInNewDomain<AdapterManager>(appDomain, executor.Value.GetType().ToString(), executor.Value.GetType().Assembly.FullName);

                        var testCasesString = JsonDataSerializer.Instance.Serialize<List<TestCase>>(sourceTests.Value);
                        adapterManager.InvokeTestRun(testCasesString, runContext.RunSettings.SettingsXml, appDomainFrameworkHandle, runContext.IsBeingDebugged);
                    }
                    finally
                    {
                        if (appDomain != null) AppDomain.Unload(appDomain);
                    }
                }

                return;
            }
#endif
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

                // TODO: Fill this in with the right extension value.
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
