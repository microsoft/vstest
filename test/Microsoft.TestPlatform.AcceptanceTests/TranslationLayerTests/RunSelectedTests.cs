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
        private List<string> testAssemblies;
        private RunEventHandler runEventHandler;
        private DiscoveryEventHandler discoveryEventHandler;

        public RunSelectedTests()
        {
            this.testAssemblies = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };

            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.runEventHandler = new RunEventHandler();
            this.discoveryEventHandler = new DiscoveryEventHandler();
        }

        [TestMethod]
        public void RunSelectedTestsWithoutTestPlatformOptions()
        {
            this.vstestConsoleWrapper.DiscoverTests(this.testAssemblies, this.GetDefaultRunSettings(), this.discoveryEventHandler);
            var testCases = this.discoveryEventHandler.DiscoveredTestCases;

            this.vstestConsoleWrapper.RunTests(testCases, this.GetDefaultRunSettings(), this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        public void RunSelectedTestsWithTestPlatformOptions()
        {
            this.vstestConsoleWrapper.DiscoverTests(this.testAssemblies, this.GetDefaultRunSettings(), this.discoveryEventHandler);
            var testCases = this.discoveryEventHandler.DiscoveredTestCases;

            this.vstestConsoleWrapper.RunTests(
                testCases,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = true },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetFramework));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetOS));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForRun));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.RunState));
        }
    }
}