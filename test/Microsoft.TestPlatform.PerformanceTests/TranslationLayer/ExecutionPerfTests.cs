// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer;

[TestClass]
public class ExecutionPerfTests : TelemetryPerfTestbase
{
    private IVsTestConsoleWrapper _vstestConsoleWrapper;
    private RunEventHandler _runEventHandler;

    public ExecutionPerfTests()
    {
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        _runEventHandler = new RunEventHandler();
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunMsTest10K()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _runEventHandler);

        PostTelemetry("RunMsTest10K", _runEventHandler.Metrics);

        // TODO: Assert on the basics for every test. And also add some check for the actual timing to see if we are fast or slow?
        Assert.AreEqual(10_000L, _runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunXunit10K()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _runEventHandler);

        PostTelemetry("RunXunit10K", _runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunNunit10K()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _runEventHandler);

        PostTelemetry("RunNunit10K", _runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunMsTest10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _runEventHandler);

        PostTelemetry("RunMsTest10KWithDefaultAdaptersSkipped", _runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunXunit10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _runEventHandler);

        PostTelemetry("RunXunit10KWithDefaultAdaptersSkipped", _runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunNunit10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _runEventHandler);

        PostTelemetry("RunNunit10KWithDefaultAdaptersSkipped", _runEventHandler.Metrics);
    }
}
