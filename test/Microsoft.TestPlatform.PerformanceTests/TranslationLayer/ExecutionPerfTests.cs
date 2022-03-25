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
    private readonly IVsTestConsoleWrapper _vstestConsoleWrapper;
    private readonly RunEventHandler _runEventHandler;

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
            GetPerfAssetFullPath("MSTest10kPassing", "MSTest10kPassing.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _runEventHandler);

        Assert.AreEqual(10_000L, _runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);
        PostTelemetry(_runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunXunit10K()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("XUnit10kPassing", "XUnit10kPassing.dll", "net452"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _runEventHandler);

        Assert.AreEqual(10_000L, _runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);
        PostTelemetry(_runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunNunit10K()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("NUnit10kPassing", "NUnit10kPassing.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _runEventHandler);

        Assert.AreEqual(10_000L, _runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);
        PostTelemetry(_runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunMsTest10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("MSTest10kPassing", "MSTest10kPassing.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _runEventHandler);

        Assert.AreEqual(10_000L, _runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);
        PostTelemetry(_runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunXunit10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("XUnit10kPassing", "XUnit10kPassing.dll", "net452"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _runEventHandler);

        Assert.AreEqual(10_000L, _runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);
        PostTelemetry(_runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    public void RunNunit10KWithDefaultAdaptersSkipped()
    {
        var testAssemblies = new List<string>
        {
            GetPerfAssetFullPath("NUnit10kPassing", "NUnit10kPassing.dll"),
        };

        _vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, _runEventHandler);

        Assert.AreEqual(10_000L, _runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);
        PostTelemetry(_runEventHandler.Metrics);
    }
}
