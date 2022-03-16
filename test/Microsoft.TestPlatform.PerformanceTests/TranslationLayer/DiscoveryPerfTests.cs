// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer;

[TestClass]
public class DiscoveryPerfTests : TelemetryPerfTestbase
{
    private readonly IVsTestConsoleWrapper _vstestConsoleWrapper;
    private readonly DiscoveryEventHandler2 _discoveryEventHandler2;

    public DiscoveryPerfTests()
    {
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        _discoveryEventHandler2 = new DiscoveryEventHandler2();
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void DiscoverMsTest10K()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _discoveryEventHandler2);

        PostTelemetry("DiscoverMsTest10K", _discoveryEventHandler2.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void DiscoverXunit10K()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _discoveryEventHandler2);

        PostTelemetry("DiscoverXunit10K", _discoveryEventHandler2.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void DiscoverNunit10K()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _discoveryEventHandler2);

        PostTelemetry("DiscoverNunit10K", _discoveryEventHandler2.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void DiscoverMsTest10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _discoveryEventHandler2);

        PostTelemetry("DiscoverMsTest10KWithDefaultAdaptersSkipped", _discoveryEventHandler2.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void DiscoverXunit10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _discoveryEventHandler2);

        PostTelemetry("DiscoverXunit10KWithDefaultAdaptersSkipped", _discoveryEventHandler2.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void DiscoverNunit10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _discoveryEventHandler2);

        PostTelemetry("DiscoverNunit10KWithDefaultAdaptersSkipped", _discoveryEventHandler2.Metrics);
    }
}
