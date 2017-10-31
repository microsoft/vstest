// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RunSelectedTests : AcceptanceTestBase
    {
        [TestMethod]
        public void RunSelectedTestsWithoutTestPlatformOptions()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();
            var discoveryEventHandler = new DiscoveryEventHandler();

            vsConsoleWrapper.DiscoverTests(sources, this.GetDefaultRunSettings(), discoveryEventHandler);
            var testCases = discoveryEventHandler.DiscoveredTestCases;

            vsConsoleWrapper.RunTests(testCases, this.GetDefaultRunSettings(), runEventHandler);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        public void RunSelectedTestsWithTestPlatformOptions()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();
            var discoveryEventHandler = new DiscoveryEventHandler();

            vsConsoleWrapper.DiscoverTests(sources, this.GetDefaultRunSettings(), discoveryEventHandler);
            var testCases = discoveryEventHandler.DiscoveredTestCases;

            vsConsoleWrapper.RunTests(
                testCases,
                this.GetDefaultRunSettings(),
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
    }
}