// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer
{
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;

    [TestClass]
    public class DiscoveryPerfTests : TelemetryPerfTestbase
    {
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private DiscoveryEventHandler2 discoveryEventHandler2;
        
        public DiscoveryPerfTests()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.discoveryEventHandler2 = new DiscoveryEventHandler2();
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverMsTest10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.DiscoverTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.discoveryEventHandler2);

            this.PostTelemetry("DiscoveryMsTest10K", this.discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverXunit10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.DiscoverTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.discoveryEventHandler2);

            this.PostTelemetry("DiscoverXunit10K", this.discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverNunit10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.DiscoverTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.discoveryEventHandler2);

            this.PostTelemetry("DiscoverNunit10K", this.discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverMsTest10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.DiscoverTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, this.discoveryEventHandler2);

            this.PostTelemetry("DiscoveryMsTest10K", this.discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverXunit10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.DiscoverTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, this.discoveryEventHandler2);

            this.PostTelemetry("DiscoverXunit10K", this.discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverNunit10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.DiscoverTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, this.discoveryEventHandler2);

            this.PostTelemetry("DiscoverNunit10K", this.discoveryEventHandler2.Metrics);
        }
    }
}
