// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    [TestClass]
    public class RunTestsWithTestsTests
    {
        private TestableTestRunCache testableTestRunCache;
        private TestExecutionContext testExecutionContext;
        private Mock<ITestRunEventsHandler> mockTestRunEventsHandler;

        private TestableRunTestsWithTests runTestsInstance;

        private IMetricsCollector metricsCollector;

        private const string RunTestsWithSourcesTestsExecutorUri = "executor://RunTestWithSourcesDiscoverer/";

        [TestInitialize]
        public void TestInit()
        {
            this.testableTestRunCache = new TestableTestRunCache();
            this.metricsCollector = new MetricsCollector();
            this.testExecutionContext = new TestExecutionContext(
                                frequencyOfRunStatsChangeEvent: 100,
                                runStatsChangeEventTimeout: TimeSpan.MaxValue,
                                inIsolation: false,
                                keepAlive: false,
                                isDataCollectionEnabled: false,
                                areTestCaseLevelEventsRequired: false,
                                hasTestRun: false,
                                isDebug: false,
                                testCaseFilter: null);
            this.mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
        }

        [TestMethod]
        public void GetExecutorUriExtensionMapShouldReturnExecutorUrisMapForTestCasesWithSameExecutorUri()
        {
            var tests = new List<TestCase>
            {
                new TestCase("A.C.M1", new Uri("e://d"), "s.dll"),
                new TestCase("A.C.M2", new Uri("e://d"), "s.dll")
            };

            this.runTestsInstance = new TestableRunTestsWithTests(
                tests,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.metricsCollector);

            var map = this.runTestsInstance.CallGetExecutorUriExtensionMap(new Mock<IFrameworkHandle>().Object, new RunContext());

            var expectedMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri("e://d"),
                    Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants.UnspecifiedAdapterPath)
            };

            CollectionAssert.AreEqual(expectedMap, map.ToList());
        }

        [TestMethod]
        public void GetExecutorUriExtensionMapShouldReturnExecutorUrisMapForTestCasesWithDifferentExecutorUri()
        {
            var tests = new List<TestCase>
            {
                new TestCase("A.C.M1", new Uri("e://d"), "s.dll"),
                new TestCase("A.C.M2", new Uri("e://d2"), "s.dll")
            };

            this.runTestsInstance = new TestableRunTestsWithTests(
                tests,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.metricsCollector);

            var map = this.runTestsInstance.CallGetExecutorUriExtensionMap(new Mock<IFrameworkHandle>().Object, new RunContext());

            var expectedMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri("e://d"),
                    Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants.UnspecifiedAdapterPath),
                new Tuple<Uri, string>(new Uri("e://d2"),
                    Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants.UnspecifiedAdapterPath)
            };

            CollectionAssert.AreEqual(expectedMap, map.ToList());
        }

        [TestMethod]
        public void InvokeExecutorShouldInvokeTestExecutorWithTheTests()
        {
            var tests = new List<TestCase>
            {
                new TestCase("A.C.M1", new Uri("e://d"), "s.dll")
            };
            
            var executorUriVsTestList = new Dictionary<Tuple<Uri, string>, List<TestCase>>();
            var executorUriExtensionTuple = new Tuple<Uri, string>(new Uri("e://d/"), "A.dll");
            executorUriVsTestList.Add(executorUriExtensionTuple, tests);

            this.runTestsInstance = new TestableRunTestsWithTests(
                tests,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                executorUriVsTestList,
                this.metricsCollector);
            
            var testExecutor = new RunTestsWithSourcesTests.RunTestWithSourcesExecutor();
            var extension = new LazyExtension<ITestExecutor, ITestExecutorCapabilities>(testExecutor, new TestExecutorMetadata("e://d/"));
            IEnumerable<TestCase> receivedTests = null;
            RunTestsWithSourcesTests.RunTestWithSourcesExecutor.RunTestsWithTestsCallback = (t, rc, fh) => { receivedTests = t; };

            this.runTestsInstance.CallInvokeExecutor(extension, executorUriExtensionTuple, null, null);

            Assert.IsNotNull(receivedTests);
            CollectionAssert.AreEqual(tests, receivedTests.ToList());
        }

        #region Testable Implemetations

        private class TestableRunTestsWithTests : RunTestsWithTests
        {
            public TestableRunTestsWithTests(IEnumerable<TestCase> testCases,
                string runSettings, TestExecutionContext testExecutionContext,
                ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler,
                IMetricsCollector metricsCollector)
                : base(
                    testCases, null, runSettings, testExecutionContext, testCaseEventsHandler,
                    testRunEventsHandler, metricsCollector)
            {
            }


            internal TestableRunTestsWithTests(IEnumerable<TestCase> testCases, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, Dictionary<Tuple<Uri, string>, List<TestCase>> executorUriVsTestList, IMetricsCollector metricsCollector)
                : base(
                    testCases, null, runSettings, testExecutionContext, testCaseEventsHandler,
                    testRunEventsHandler, executorUriVsTestList, metricsCollector)
            {
            }

            public IEnumerable<Tuple<Uri, string>> CallGetExecutorUriExtensionMap(
                IFrameworkHandle testExecutorFrameworkHandle, RunContext runContext)
            {
                return this.GetExecutorUriExtensionMap(testExecutorFrameworkHandle, runContext);
            }

            public void CallInvokeExecutor(LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
                Tuple<Uri, string> executorUriExtensionTuple, RunContext runContext, IFrameworkHandle frameworkHandle)
            {
                this.InvokeExecutor(executor, executorUriExtensionTuple, runContext, frameworkHandle);
            }
        }

        #endregion

    }
}
