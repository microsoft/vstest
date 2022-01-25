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
        private readonly IVsTestConsoleWrapper vstestConsoleWrapper;
        private readonly DiscoveryEventHandler2 discoveryEventHandler2;

        public DiscoveryPerfTests()
        {
            vstestConsoleWrapper = GetVsTestConsoleWrapper();
            discoveryEventHandler2 = new DiscoveryEventHandler2();
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverMsTest10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, discoveryEventHandler2);

            PostTelemetry("DiscoverMsTest10K", discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverXunit10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, discoveryEventHandler2);

            PostTelemetry("DiscoverXunit10K", discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverNunit10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, discoveryEventHandler2);

            PostTelemetry("DiscoverNunit10K", discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverMsTest10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, discoveryEventHandler2);

            PostTelemetry("DiscoverMsTest10KWithDefaultAdaptersSkipped", discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverXunit10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, discoveryEventHandler2);

            PostTelemetry("DiscoverXunit10KWithDefaultAdaptersSkipped", discoveryEventHandler2.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void DiscoverNunit10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, discoveryEventHandler2);

            PostTelemetry("DiscoverNunit10KWithDefaultAdaptersSkipped", discoveryEventHandler2.Metrics);
        }
    }
}
