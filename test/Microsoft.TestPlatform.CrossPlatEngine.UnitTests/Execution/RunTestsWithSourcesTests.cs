// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestableImplementations;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class RunTestsWithSourcesTests
    {
        private TestableTestRunCache testableTestRunCache;
        private TestExecutionContext testExecutionContext;
        private Mock<ITestRunEventsHandler> mockTestRunEventsHandler;

        private TestableRunTestsWithSources runTestsInstance;

        private IRequestData requestData;

        internal const string RunTestsWithSourcesTestsExecutorUri = "executor://RunTestWithSourcesDiscoverer/";

        [TestInitialize]
        public void TestInit()
        {
            this.testableTestRunCache = new TestableTestRunCache();
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
            this.requestData = new RequestData(new NoOpMetricsCollection());

            TestPluginCacheTests.SetupMockExtensions(
                new string[] { typeof(RunTestsWithSourcesTests).GetTypeInfo().Assembly.Location },
                () => { });

            TestPluginCache.Instance.DiscoverTestExtensions<TestExecutorPluginInformation, ITestExecutor>(TestPlatformConstants.TestAdapterEndsWithPattern);
            TestPluginCache.Instance.DiscoverTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(TestPlatformConstants.TestAdapterEndsWithPattern);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            RunTestWithSourcesExecutor.RunTestsWithSourcesCallback = null;
            TestPluginCacheTests.ResetExtensionsCache();
        }

        [TestMethod]
        public void BeforeRaisingTestRunCompleteShouldWarnIfNoTestsAreRun()
        {
            var adapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            adapterSourceMap.Add("a", new List<string> {"a", "aa"});
            adapterSourceMap.Add("b", new List<string> { "b", "ab" });

            var executorUriVsSourceList = new Dictionary<Tuple<Uri, string>, IEnumerable<string>>();
            executorUriVsSourceList.Add(new Tuple<Uri, string>(new Uri("e://d/"), "A.dll"), new List<string> {"s1.dll "});

            this.runTestsInstance = new TestableRunTestsWithSources(
                adapterSourceMap,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                executorUriVsSourceList,
                this.requestData);
            
            this.runTestsInstance.CallBeforeRaisingTestRunComplete(false);

            var messageFormat =
                "No test is available in {0}. Make sure that test discoverer & executors are registered and platform & framework version settings are appropriate and try again.";
            var message = string.Format(messageFormat, "a aa b ab");
            this.mockTestRunEventsHandler.Verify(treh => treh.HandleLogMessage(TestMessageLevel.Warning, message),
                Times.Once);
        }

        [TestMethod]
        public void GetExecutorUriExtensionMapShouldReturnEmptyOnInvalidSources()
        {
            var adapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            adapterSourceMap.Add("a", new List<string> { "a", "aa" });

            this.runTestsInstance = new TestableRunTestsWithSources(
                adapterSourceMap,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.requestData);

            var executorUris = this.runTestsInstance.CallGetExecutorUriExtensionMap(new Mock<IFrameworkHandle>().Object, new RunContext());

            Assert.IsNotNull(executorUris);
            Assert.AreEqual(0, executorUris.Count());
        }

        [TestMethod]
        public void GetExecutorUriExtensionMapShouldReturnDefaultExecutorUrisForTheDiscoverersDefined()
        {
            var assemblyLocation = typeof (RunTestsWithSourcesTests).GetTypeInfo().Assembly.Location;

            var adapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            adapterSourceMap.Add("a", new List<string> {"a", "aa"});
            adapterSourceMap.Add(assemblyLocation, new List<string> {assemblyLocation});

            this.runTestsInstance = new TestableRunTestsWithSources(
                adapterSourceMap,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.requestData);

            var executorUris = this.runTestsInstance.CallGetExecutorUriExtensionMap(
                new Mock<IFrameworkHandle>().Object, new RunContext());

            Assert.IsNotNull(executorUris);
            CollectionAssert.Contains(executorUris.ToArray(),
                new Tuple<Uri, string>(new Uri("executor://RunTestWithSourcesDiscoverer"), assemblyLocation));
        }

        [TestMethod]
        public void InvokeExecutorShouldInvokeTestExecutorWithTheSources()
        {
            var adapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            adapterSourceMap.Add("a", new List<string> { "a", "aa" });
            adapterSourceMap.Add("b", new List<string> { "b", "ab" });

            var executorUriVsSourceList = new Dictionary<Tuple<Uri, string>, IEnumerable<string>>();
            var executorUriExtensionTuple = new Tuple<Uri, string>(new Uri("e://d/"), "A.dll");
            executorUriVsSourceList.Add(executorUriExtensionTuple, new List<string> { "s1.dll " });

            this.runTestsInstance = new TestableRunTestsWithSources(
                adapterSourceMap,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                executorUriVsSourceList,
                this.requestData);

            var testExecutor = new RunTestWithSourcesExecutor();
            var extension = new LazyExtension<ITestExecutor, ITestExecutorCapabilities>(testExecutor, new TestExecutorMetadata("e://d/"));
            IEnumerable<string> receivedSources = null;
            RunTestWithSourcesExecutor.RunTestsWithSourcesCallback = (sources, rc, fh) => { receivedSources = sources; };

            this.runTestsInstance.CallInvokeExecutor(extension, executorUriExtensionTuple, null, null);

            Assert.IsNotNull(receivedSources);
            CollectionAssert.AreEqual(new List<string> {"s1.dll "}, receivedSources.ToList());
        }

        [TestMethod]
        public void RunTestsShouldRunTestsForTheSourcesSpecified()
        {
            var assemblyLocation = typeof(RunTestsWithSourcesTests).GetTypeInfo().Assembly.Location;

            var adapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            adapterSourceMap.Add("a", new List<string> { "a", "aa" });
            adapterSourceMap.Add(assemblyLocation, new List<string> { assemblyLocation });

            this.runTestsInstance = new TestableRunTestsWithSources(
                adapterSourceMap,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.requestData);

            bool isExecutorCalled = false;
            RunTestWithSourcesExecutor.RunTestsWithSourcesCallback = (s, rc, fh) => { isExecutorCalled = true; };
            
            this.runTestsInstance.RunTests();

            Assert.IsTrue(isExecutorCalled);
            this.mockTestRunEventsHandler.Verify(
                treh => treh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()), Times.Once);
        }

        #region Testable Implemetations

        private class TestableRunTestsWithSources : RunTestsWithSources
        {
            public TestableRunTestsWithSources(Dictionary<string, IEnumerable<string>> adapterSourceMap, string runSettings, 
                TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, IRequestData requestData)
                : base(requestData, adapterSourceMap, null, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler)
            {
            }

            internal TestableRunTestsWithSources(Dictionary<string, IEnumerable<string>> adapterSourceMap, string runSettings, 
                TestExecutionContext testExecutionContext,
                ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, Dictionary<Tuple<Uri, string>, IEnumerable<string>> executorUriVsSourceList, IRequestData requestData)
                : base(requestData, adapterSourceMap, null, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, executorUriVsSourceList)
            {
            }

            public void CallBeforeRaisingTestRunComplete(bool exceptionsHitDuringRunTests)
            {
                this.BeforeRaisingTestRunComplete(exceptionsHitDuringRunTests);
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

        [FileExtension(".dll")]
        [DefaultExecutorUri(RunTestsWithSourcesTestsExecutorUri)]
        private class RunTestWithSourcesDiscoverer : ITestDiscoverer
        {
            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                throw new NotImplementedException();
            }
        }

        [ExtensionUri(RunTestsWithSourcesTestsExecutorUri)]
        internal class RunTestWithSourcesExecutor : ITestExecutor
        {
            public static Action<IEnumerable<string>, IRunContext, IFrameworkHandle> RunTestsWithSourcesCallback { get; set; }
            public static Action<IEnumerable<TestCase>, IRunContext, IFrameworkHandle> RunTestsWithTestsCallback { get; set; }

            public void Cancel()
            {
                throw new NotImplementedException();
            }

            public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
            {
                RunTestsWithSourcesCallback?.Invoke(sources, runContext, frameworkHandle);
            }

            public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
            {
                RunTestsWithTestsCallback?.Invoke(tests, runContext, frameworkHandle);
            }
        }

        #endregion
    }
}
