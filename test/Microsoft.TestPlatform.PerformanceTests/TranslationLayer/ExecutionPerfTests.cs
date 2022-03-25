﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

using Microsoft.TestPlatform.PerformanceTests.PerfInstrumentation;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer;

[TestClass]
public class ExecutionPerfTests : TelemetryPerfTestbase
{
    [TestMethod]
    [TestCategory("TelemetryPerf")]
    [DataRow("MSTest1Passing", 1)]
    [DataRow("MSTest100Passing", 100)]
    [DataRow("MSTest1000Passing", 1000)]
    [DataRow("MSTest10kPassing", 10_000)]
    [DataRow("NUnit1Passing", 1)]
    [DataRow("NUnit100Passing", 100)]
    [DataRow("NUnit1000Passing", 1000)]
    [DataRow("NUnit10kPassing", 10_000)]
    [DataRow("XUnit1Passing", 1)]
    [DataRow("XUnit100Passing", 100)]
    [DataRow("XUnit1000Passing", 1000)]
    [DataRow("XUnit10kPassing", 10_000)]
    public void RunTests(string projectName, long expectedNumberOfTests)
    {
        var framework = projectName.StartsWith("XUnit") ? "net452" : "net451";
        var runEventHandler = new RunEventHandler();
        TestPlatformOptions options = new() { CollectMetrics = true };

        var perfAnalyzer = new PerfAnalyzer();
        try
        {
            perfAnalyzer.EnableProvider();
            var vstestConsoleWrapper = GetVsTestConsoleWrapper(logFileDir: null, traceLevel: System.Diagnostics.TraceLevel.Off);

            vstestConsoleWrapper.RunTests(GetPerfAssetFullPath(projectName, framework), GetDefaultRunSettings(), options, runEventHandler);
            vstestConsoleWrapper.EndSession();
        }
        finally
        {
            perfAnalyzer.DisableProvider(wait: expectedNumberOfTests > 100);
        }

        Assert.AreEqual(expectedNumberOfTests, runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);

        perfAnalyzer.AnalyzeEventsData();
        var summary = perfAnalyzer.SummarizeExecution();

        summary.ForEach(measurement => runEventHandler.Metrics[measurement.Key] = measurement.Value);
        PostTelemetry(runEventHandler.Metrics);
    }

    [TestMethod]
    [TestCategory("TelemetryPerf")]
    [DataRow("MSTest1Passing", 1)]
    [DataRow("MSTest100Passing", 100)]
    [DataRow("MSTest1000Passing", 1000)]
    [DataRow("MSTest10kPassing", 10_000)]
    [DataRow("NUnit1Passing", 1)]
    [DataRow("NUnit100Passing", 100)]
    [DataRow("NUnit1000Passing", 1000)]
    [DataRow("NUnit10kPassing", 10_000)]
    [DataRow("XUnit1Passing", 1)]
    [DataRow("XUnit100Passing", 100)]
    [DataRow("XUnit1000Passing", 1000)]
    [DataRow("XUnit10kPassing", 10_000)]
    public void RunTestsWithDefaultAdaptersSkipped(string projectName, long expectedNumberOfTests)
    {
        var framework = projectName.StartsWith("XUnit") ? "net452" : "net451";
        var runEventHandler = new RunEventHandler();
        TestPlatformOptions options = new()
        {
            CollectMetrics = true,
            SkipDefaultAdapters = true, // <-- skipping adapters
        };

        var perfAnalyzer = new PerfAnalyzer();
        try
        {
            perfAnalyzer.EnableProvider();
            var vstestConsoleWrapper = GetVsTestConsoleWrapper(logFileDir: null, traceLevel: System.Diagnostics.TraceLevel.Off);
            vstestConsoleWrapper.RunTests(GetPerfAssetFullPath(projectName, framework), GetDefaultRunSettings(), options, runEventHandler);
            vstestConsoleWrapper.EndSession();
        }
        finally
        {
            perfAnalyzer.DisableProvider(wait: expectedNumberOfTests > 100);
        }

        Assert.AreEqual(expectedNumberOfTests, runEventHandler.Metrics[TelemetryDataConstants.TotalTestsRun]);

        perfAnalyzer.AnalyzeEventsData();
        var summary = perfAnalyzer.SummarizeExecution();

        summary.ForEach(measurement => runEventHandler.Metrics[measurement.Key] = measurement.Value);
        PostTelemetry(runEventHandler.Metrics);
    }
}
