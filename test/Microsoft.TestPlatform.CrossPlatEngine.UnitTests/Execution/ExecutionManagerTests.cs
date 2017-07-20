// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Common.UnitTests.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.Utilities;

    using static RunTestsWithSourcesTests;

    [TestClass]
    public class ExecutionManagerTests
    {
        private ExecutionManager executionManager;

        [TestInitialize]
        public void TestInit()
        {
            this.executionManager = new ExecutionManager();

            TestPluginCache.Instance = null;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            RunTestWithSourcesExecutor.RunTestsWithSourcesCallback = null;
            RunTestWithSourcesExecutor.RunTestsWithTestsCallback = null;

            TestDiscoveryExtensionManager.Destroy();
            TestExecutorExtensionManager.Destroy();
            SettingsProviderExtensionManager.Destroy();
        }

        [TestMethod]
        public void InitializeShouldLoadAndInitializeAllExtension()
        {
            var commonAssemblyLocation = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;

            TestPluginCacheTests.SetupMockExtensions(
                new string[] { commonAssemblyLocation },
                () => { });


            this.executionManager.Initialize(new List<string> { commonAssemblyLocation });

            Assert.IsNotNull(TestPluginCache.Instance.TestExtensions);

            // Discoverers
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.TestDiscoverers.Count > 0);
            var allDiscoverers = TestDiscoveryExtensionManager.Create().Discoverers;

            foreach (var discoverer in allDiscoverers)
            {
                Assert.IsTrue(discoverer.IsExtensionCreated);
            }

            // Executors
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.TestExecutors.Count > 0);
            var allExecutors = TestExecutorExtensionManager.Create().TestExtensions;

            foreach (var executor in allExecutors)
            {
                Assert.IsTrue(executor.IsExtensionCreated);
            }

            // Settings Providers
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.TestSettingsProviders.Count > 0);
            var settingsProviders = SettingsProviderExtensionManager.Create().SettingsProvidersMap.Values;

            foreach (var provider in settingsProviders)
            {
                Assert.IsTrue(provider.IsExtensionCreated);
            }
        }

        [TestMethod]
        public void StartTestRunShouldRunTestsInTheProvidedSources()
        {
            var assemblyLocation = typeof(ExecutionManagerTests).GetTypeInfo().Assembly.Location;
            TestPluginCacheTests.SetupMockExtensions(
                new string[] { assemblyLocation },
                () => { });
            TestPluginCache.Instance.DiscoverTestExtensions<TestExecutorPluginInformation, ITestExecutor>(TestPlatformConstants.TestAdapterEndsWithPattern);
            TestPluginCache.Instance.DiscoverTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(TestPlatformConstants.TestAdapterEndsWithPattern);


            var adapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            adapterSourceMap.Add(assemblyLocation, new List<string> { assemblyLocation });

            var testExecutionContext = new TestExecutionContext(
                                           frequencyOfRunStatsChangeEvent: 1,
                                           runStatsChangeEventTimeout: TimeSpan.MaxValue,
                                           inIsolation: false,
                                           keepAlive: false,
                                           isDataCollectionEnabled: false,
                                           areTestCaseLevelEventsRequired: false,
                                           hasTestRun: false,
                                           isDebug: false,
                                           testCaseFilter: null);

            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            var isExecutorCalled = false;
            RunTestWithSourcesExecutor.RunTestsWithSourcesCallback = (s, rc, fh) =>
                {
                    isExecutorCalled = true;
                    var tr =
                        new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(
                            new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(
                                "A.C.M",
                                new Uri("e://d/"),
                                "A.dll"));
                    fh.RecordResult(tr);
                };

            this.executionManager.StartTestRun(adapterSourceMap, null, testExecutionContext, null, mockTestRunEventsHandler.Object);

            Assert.IsTrue(isExecutorCalled);
            mockTestRunEventsHandler.Verify(
                treh => treh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()), Times.Once);

            // Also verify that run stats are passed through.
            mockTestRunEventsHandler.Verify(treh => treh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldRunTestsForTheProvidedTests()
        {
            var assemblyLocation = typeof(ExecutionManagerTests).GetTypeInfo().Assembly.Location;

            var tests = new List<TestCase>
            {
                new TestCase("A.C.M1", new Uri(RunTestsWithSourcesTestsExecutorUri), assemblyLocation)
            };

            var testExecutionContext = new TestExecutionContext(
                                           frequencyOfRunStatsChangeEvent: 1,
                                           runStatsChangeEventTimeout: TimeSpan.MaxValue,
                                           inIsolation: false,
                                           keepAlive: false,
                                           isDataCollectionEnabled: false,
                                           areTestCaseLevelEventsRequired: false,
                                           hasTestRun: false,
                                           isDebug: false,
                                           testCaseFilter: null);

            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            var isExecutorCalled = false;
            RunTestWithSourcesExecutor.RunTestsWithTestsCallback = (s, rc, fh) =>
            {
                isExecutorCalled = true;
                var tr =
                    new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(
                        new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(
                            "A.C.M",
                            new Uri(RunTestsWithSourcesTestsExecutorUri),
                            "A.dll"));
                fh.RecordResult(tr);
            };
            TestPluginCacheTests.SetupMockExtensions(new string[] { assemblyLocation }, () => { });


            this.executionManager.StartTestRun(tests, null, testExecutionContext, null, mockTestRunEventsHandler.Object);

            Assert.IsTrue(isExecutorCalled);
            mockTestRunEventsHandler.Verify(
                treh => treh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()), Times.Once);

            // Also verify that run stats are passed through.
            mockTestRunEventsHandler.Verify(treh => treh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once);
        }
    }
}
