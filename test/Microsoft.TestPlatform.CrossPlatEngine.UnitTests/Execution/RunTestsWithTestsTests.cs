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
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations;

    [TestClass]
    public class RunTestsWithTestsTests
    {
        private TestableTestRunCache testableTestRunCache;
        private TestExecutionContext testExecutionContext;
        private Mock<ITestRunEventsHandler> mockTestRunEventsHandler;
        private TestableRunTestsWithTests runTestsInstance;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;

        private const string RunTestsWithSourcesTestsExecutorUri = "executor://RunTestWithSourcesDiscoverer/";

        [TestInitialize]
        public void TestInit()
        {
            testableTestRunCache = new TestableTestRunCache();
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData = new Mock<IRequestData>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);
            testExecutionContext = new TestExecutionContext(
                                frequencyOfRunStatsChangeEvent: 100,
                                runStatsChangeEventTimeout: TimeSpan.MaxValue,
                                inIsolation: false,
                                keepAlive: false,
                                isDataCollectionEnabled: false,
                                areTestCaseLevelEventsRequired: false,
                                hasTestRun: false,
                                isDebug: false,
                                testCaseFilter: null,
                                filterOptions: null);
            mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
        }

        [TestMethod]
        public void GetExecutorUriExtensionMapShouldReturnExecutorUrisMapForTestCasesWithSameExecutorUri()
        {
            var tests = new List<TestCase>
            {
                new TestCase("A.C.M1", new Uri("e://d"), "s.dll"),
                new TestCase("A.C.M2", new Uri("e://d"), "s.dll")
            };

            runTestsInstance = new TestableRunTestsWithTests(
                tests,
                null,
                testExecutionContext,
                null,
                mockTestRunEventsHandler.Object,
                mockRequestData.Object);

            var map = runTestsInstance.CallGetExecutorUriExtensionMap(new Mock<IFrameworkHandle>().Object, new RunContext());

            var expectedMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri("e://d"),
                    Constants.UnspecifiedAdapterPath)
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

            runTestsInstance = new TestableRunTestsWithTests(
                tests,
                null,
                testExecutionContext,
                null,
                mockTestRunEventsHandler.Object,
                mockRequestData.Object);

            var map = runTestsInstance.CallGetExecutorUriExtensionMap(new Mock<IFrameworkHandle>().Object, new RunContext());

            var expectedMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri("e://d"),
                    Constants.UnspecifiedAdapterPath),
                new Tuple<Uri, string>(new Uri("e://d2"),
                    Constants.UnspecifiedAdapterPath)
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

            runTestsInstance = new TestableRunTestsWithTests(
                tests,
                null,
                testExecutionContext,
                null,
                mockTestRunEventsHandler.Object,
                executorUriVsTestList,
                mockRequestData.Object);

            var testExecutor = new RunTestsWithSourcesTests.RunTestWithSourcesExecutor();
            var extension = new LazyExtension<ITestExecutor, ITestExecutorCapabilities>(testExecutor, new TestExecutorMetadata("e://d/"));
            IEnumerable<TestCase> receivedTests = null;
            RunTestsWithSourcesTests.RunTestWithSourcesExecutor.RunTestsWithTestsCallback = (t, rc, fh) => receivedTests = t;

            runTestsInstance.CallInvokeExecutor(extension, executorUriExtensionTuple, null, null);

            Assert.IsNotNull(receivedTests);
            CollectionAssert.AreEqual(tests, receivedTests.ToList());
        }

        [TestMethod]
        public void SendSessionStartShouldCallSessionStartWithCorrectTestSources()
        {
            var tests = new List<TestCase>
            {
                new TestCase("A.C.M1", new Uri("e://d"), "s.dll")
            };
            var mockTestCaseEventsHandler = new Mock<ITestCaseEventsHandler>();

            runTestsInstance = new TestableRunTestsWithTests(
                tests,
                null,
                testExecutionContext,
                mockTestCaseEventsHandler.Object,
                mockTestRunEventsHandler.Object,
                mockRequestData.Object);

            runTestsInstance.CallSendSessionStart();

            mockTestCaseEventsHandler.Verify(x => x.SendSessionStart(It.Is<IDictionary<String, object>>(
                y => y.ContainsKey("TestSources")
                && ((IEnumerable<string>)y["TestSources"]).Contains("s.dll")
            )));
        }

        [TestMethod]
        public void SendSessionEndShouldCallSessionEnd()
        {
            var tests = new List<TestCase>
            {
                new TestCase("A.C.M1", new Uri("e://d"), "s.dll")
            };
            var mockTestCaseEventsHandler = new Mock<ITestCaseEventsHandler>();

            runTestsInstance = new TestableRunTestsWithTests(
                tests,
                null,
                testExecutionContext,
                mockTestCaseEventsHandler.Object,
                mockTestRunEventsHandler.Object,
                mockRequestData.Object);

            runTestsInstance.CallSendSessionEnd();

            mockTestCaseEventsHandler.Verify(x => x.SendSessionEnd());
        }

        #region Testable Implementations

        private class TestableRunTestsWithTests : RunTestsWithTests
        {
            public TestableRunTestsWithTests(IEnumerable<TestCase> testCases,
                string runSettings, TestExecutionContext testExecutionContext,
                ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler,
                IRequestData requestData)
                : base(requestData, testCases, null, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler)
            {
            }


            internal TestableRunTestsWithTests(IEnumerable<TestCase> testCases, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, Dictionary<Tuple<Uri, string>, List<TestCase>> executorUriVsTestList, IRequestData requestData)
                : base(
requestData, testCases, null, runSettings, testExecutionContext,
                    testCaseEventsHandler, testRunEventsHandler, executorUriVsTestList)
            {
            }

            public IEnumerable<Tuple<Uri, string>> CallGetExecutorUriExtensionMap(
                IFrameworkHandle testExecutorFrameworkHandle, RunContext runContext)
            {
                return GetExecutorUriExtensionMap(testExecutorFrameworkHandle, runContext);
            }

            public void CallInvokeExecutor(LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
                Tuple<Uri, string> executorUriExtensionTuple, RunContext runContext, IFrameworkHandle frameworkHandle)
            {
                InvokeExecutor(executor, executorUriExtensionTuple, runContext, frameworkHandle);
            }

            public void CallSendSessionStart()
            {
                SendSessionStart();
            }

            public void CallSendSessionEnd()
            {
                SendSessionEnd();
            }
        }

        #endregion

    }
}
