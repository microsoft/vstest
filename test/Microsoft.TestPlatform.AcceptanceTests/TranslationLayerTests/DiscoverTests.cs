// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System.Collections.Generic;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiscoverTests : AcceptanceTestBase
    {
        private List<string> testAssemblies;
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private DiscoveryEventHandler discoveryEventHandler;
        private DiscoveryEventHandler2 discoveryEventHandler2;

        public DiscoverTests()
        {
            this.testAssemblies = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };

            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.discoveryEventHandler = new DiscoveryEventHandler();
            this.discoveryEventHandler2 = new DiscoveryEventHandler2();
        }

        [TestMethod]
        public void DiscoverTestsUsingDiscoveryEventHandler1()
        {
            this.vstestConsoleWrapper.DiscoverTests(this.testAssemblies, this.GetDefaultRunSettings(), this.discoveryEventHandler);

            // Assert.
            Assert.AreEqual(6, this.discoveryEventHandler.DiscoveredTestCases.Count);
        }

        [TestMethod]
        public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedOut()
        {
            this.vstestConsoleWrapper.DiscoverTests(
                this.testAssemblies,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = false },
                this.discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, this.discoveryEventHandler2.DiscoveredTestCases.Count);
        }

        [TestMethod]
        public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedIn()
        {
            this.vstestConsoleWrapper.DiscoverTests(this.testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, this.discoveryEventHandler2.DiscoveredTestCases.Count);
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests));
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecByAllAdapters));
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForDiscovery));
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.DiscoveryState));
        }

        [TestMethod]
        public void DiscoverTestsUsingEventHandler2AndBatchSize()
        {
            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
                                    <RunSettings>     
                                        <RunConfiguration>
                                        <BatchSize>3</BatchSize>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.vstestConsoleWrapper.DiscoverTests(
                this.testAssemblies,
                runSettingsXml,
                new TestPlatformOptions { CollectMetrics = false },
                this.discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, this.discoveryEventHandler2.DiscoveredTestCases.Count);
        }


        [TestMethod]
        public void DiscoverTestsUsingEventHandler1AndBatchSize()
        {
            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
                                    <RunSettings>     
                                        <RunConfiguration>
                                        <BatchSize>3</BatchSize>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.vstestConsoleWrapper.DiscoverTests(
                this.testAssemblies,
                runSettingsXml,
                this.discoveryEventHandler);

            // Assert.
            Assert.AreEqual(6, this.discoveryEventHandler.DiscoveredTestCases.Count);
        }
    }
}