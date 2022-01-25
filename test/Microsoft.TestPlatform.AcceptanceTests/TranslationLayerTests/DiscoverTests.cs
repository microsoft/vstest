// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class DiscoverTests : AcceptanceTestBase
    {
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private DiscoveryEventHandler discoveryEventHandler;
        private DiscoveryEventHandler2 discoveryEventHandler2;

        public void Setup()
        {
            vstestConsoleWrapper = GetVsTestConsoleWrapper();
            discoveryEventHandler = new DiscoveryEventHandler();
            discoveryEventHandler2 = new DiscoveryEventHandler2();
        }

        [TestCleanup]
        public void Cleanup()
        {
            vstestConsoleWrapper?.EndSession();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingDiscoveryEventHandler1(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);

            Setup();

            vstestConsoleWrapper.DiscoverTests(GetTestAssemblies(), GetDefaultRunSettings(), discoveryEventHandler);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandler.DiscoveredTestCases.Count);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedOut(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            vstestConsoleWrapper.DiscoverTests(
               GetTestAssemblies(),
                GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = false },
                discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandler2.DiscoveredTestCases.Count);
            Assert.AreEqual(0, discoveryEventHandler2.Metrics.Count);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedIn(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            vstestConsoleWrapper.DiscoverTests(GetTestAssemblies(), GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandler2.DiscoveredTestCases.Count);
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests));
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecByAllAdapters));
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForDiscovery));
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.DiscoveryState));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingEventHandler2AndBatchSize(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            var discoveryEventHandlerForBatchSize = new DiscoveryEventHandlerForBatchSize();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <BatchSize>3</BatchSize>
                                        </RunConfiguration>
                                    </RunSettings>";

            vstestConsoleWrapper.DiscoverTests(
               GetTestAssemblies(),
                runSettingsXml,
                null,
                discoveryEventHandlerForBatchSize);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandlerForBatchSize.DiscoveredTestCases.Count);
            Assert.AreEqual(3, discoveryEventHandlerForBatchSize.BatchSize);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingEventHandler1AndBatchSize(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            var discoveryEventHandlerForBatchSize = new DiscoveryEventHandlerForBatchSize();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <BatchSize>3</BatchSize>
                                        </RunConfiguration>
                                    </RunSettings>";

            vstestConsoleWrapper.DiscoverTests(
               GetTestAssemblies(),
                runSettingsXml,
                discoveryEventHandlerForBatchSize);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandlerForBatchSize.DiscoveredTestCases.Count);
            Assert.AreEqual(3, discoveryEventHandlerForBatchSize.BatchSize);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingSourceNavigation(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            vstestConsoleWrapper.DiscoverTests(
               GetTestAssemblies(),
                GetDefaultRunSettings(),
                discoveryEventHandler);

            // Assert.
            var testCase =
                discoveryEventHandler.DiscoveredTestCases.Where(dt => dt.FullyQualifiedName.Equals("SampleUnitTestProject.UnitTest1.PassingTest"));

            // Release builds optimize code, hence line numbers are different.
            if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(23, testCase.FirstOrDefault().LineNumber);
            }
            else
            {
                Assert.AreEqual(22, testCase.FirstOrDefault().LineNumber);
            }
        }

        [TestMethod]
        // flaky on the desktop runner, desktop framework combo
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource]
        public void CancelTestDiscovery(RunnerInfo runnerInfo)
        {
            // Setup
            var testAssemblies = new List<string>
                                     {
                                         GetAssetFullPath("DiscoveryTestProject.dll"),
                                         GetAssetFullPath("SimpleTestProject.dll"),
                                         GetAssetFullPath("SimpleTestProject2.dll")
                                     };

            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            var discoveredTests = new List<TestCase>();
            var discoveryEvents = new Mock<ITestDiscoveryEventsHandler>();
            discoveryEvents.Setup((events) => events.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback
                ((IEnumerable<TestCase> testcases) => discoveredTests.AddRange(testcases));

            // Act
            var discoveryTask = Task.Run(() => vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), discoveryEvents.Object));

            Task.Delay(2000).Wait();
            vstestConsoleWrapper.CancelDiscovery();
            discoveryTask.Wait();

            // Assert.
            int discoveredSources = discoveredTests.Select((testcase) => testcase.Source).Distinct().Count();
            Assert.AreNotEqual(testAssemblies.Count, discoveredSources, "All test assemblies discovered");
        }

        private IList<string> GetTestAssemblies()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetAssetFullPath("SimpleTestProject.dll"),
                                         GetAssetFullPath("SimpleTestProject2.dll")
                                     };

            return testAssemblies;
        }
    }
}