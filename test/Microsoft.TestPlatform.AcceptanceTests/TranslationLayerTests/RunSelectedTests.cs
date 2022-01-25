// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RunSelectedTests : AcceptanceTestBase
    {
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private RunEventHandler runEventHandler;
        private DiscoveryEventHandler discoveryEventHandler;

        private void Setup()
        {
            vstestConsoleWrapper = GetVsTestConsoleWrapper();
            runEventHandler = new RunEventHandler();
            discoveryEventHandler = new DiscoveryEventHandler();
        }

        [TestCleanup]
        public void Cleanup()
        {
            vstestConsoleWrapper?.EndSession();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithoutTestPlatformOptions(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            vstestConsoleWrapper.DiscoverTests(GetTestAssemblies(), GetDefaultRunSettings(), discoveryEventHandler);
            var testCases = discoveryEventHandler.DiscoveredTestCases;

            vstestConsoleWrapper.RunTests(testCases, GetDefaultRunSettings(), runEventHandler);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithTestPlatformOptions(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            vstestConsoleWrapper.DiscoverTests(GetTestAssemblies(), GetDefaultRunSettings(), discoveryEventHandler);
            var testCases = discoveryEventHandler.DiscoveredTestCases;

            vstestConsoleWrapper.RunTests(
                testCases,
                GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = true },
                runEventHandler);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetFramework));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetOS));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForRun));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.RunState));
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