// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

/// <summary>
/// The Run Tests using VsTestConsoleWrapper API's
/// </summary>
[TestClass]
public class RunTests : AcceptanceTestBase
{
    private IVsTestConsoleWrapper _vstestConsoleWrapper;
    private RunEventHandler _runEventHandler;

    private void Setup()
    {
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        _runEventHandler = new RunEventHandler();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _vstestConsoleWrapper?.EndSession();
    }

    [TestMethod]
    [TranslationLayerCompatibilityDataSource]
    public void RunAllTests(RunnerInfo runnerInfo, VSTestConsoleInfo vsTestConsoleInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo, vsTestConsoleInfo);
        
        var vstestConsoleWrapper = GetVsTestConsoleWrapper();
        var runEventHandler = new RunEventHandler();
        vstestConsoleWrapper.RunTests(GetTestAssemblies(), GetDefaultRunSettings(), runEventHandler);

        // Assert
        Assert.AreEqual(6, runEventHandler.TestResults.Count);
        Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    [DoNotParallelize]
    public void EndSessionShouldEnsureVstestConsoleProcessDies(RunnerInfo runnerInfo)
    {
        var numOfProcesses = Process.GetProcessesByName("vstest.console").Length;

        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        _vstestConsoleWrapper.RunTests(GetTestAssemblies(), GetDefaultRunSettings(), _runEventHandler);
        _vstestConsoleWrapper?.EndSession();

        // Assert
        // TODO: This still works reliably, but it is accidental. Correctly we should look at our "tree" of processes
        // but there is no such thing on Windows. We can still replicate it quite well. There is code for it in blame
        // hang collector.
        Assert.AreEqual(numOfProcesses, Process.GetProcessesByName("vstest.console").Length);

        _vstestConsoleWrapper = null;
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsWithTelemetryOptedIn(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        _vstestConsoleWrapper.RunTests(
            GetTestAssemblies(),
            GetDefaultRunSettings(),
            new TestPlatformOptions() { CollectMetrics = true },
            _runEventHandler);

        // Assert
        Assert.AreEqual(6, _runEventHandler.TestResults.Count);
        Assert.IsTrue(_runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
        Assert.IsTrue(_runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetFramework));
        Assert.IsTrue(_runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetOS));
        Assert.IsTrue(_runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForRun));
        Assert.IsTrue(_runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution));
        Assert.IsTrue(_runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.RunState));
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsWithTelemetryOptedOut(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        _vstestConsoleWrapper.RunTests(
            GetTestAssemblies(),
            GetDefaultRunSettings(),
            new TestPlatformOptions() { CollectMetrics = false },
            _runEventHandler);

        // Assert
        Assert.AreEqual(6, _runEventHandler.TestResults.Count);
        Assert.AreEqual(0, _runEventHandler.Metrics.Count);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsShouldThrowOnStackOverflowException(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        if (IntegrationTestEnvironment.BuildConfiguration.Equals("release", StringComparison.OrdinalIgnoreCase))
        {
            // On release, x64 builds, recursive calls may be replaced with loops (tail call optimization)
            Assert.Inconclusive("On StackOverflowException testhost not exited in release configuration.");
            return;
        }

        var source = new[] { GetAssetFullPath("SimpleTestProject3.dll") };

        _vstestConsoleWrapper.RunTests(
            source,
            GetDefaultRunSettings(),
            new TestPlatformOptions() { TestCaseFilter = "ExitWithStackoverFlow" },
            _runEventHandler);

        var errorMessage = runnerInfo.TargetFramework == "net451"
            ? $"The active test run was aborted. Reason: Test host process crashed : Process is terminated due to StackOverflowException.{Environment.NewLine}"
            : $"The active test run was aborted. Reason: Test host process crashed : Process is terminating due to StackOverflowException.{Environment.NewLine}";

        Assert.IsTrue(_runEventHandler.Errors.Contains(errorMessage));
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(useCoreRunner: false)]
    [NetCoreTargetFrameworkDataSource(useCoreRunner: false)]
    public void RunTestsShouldShowProperWarningOnNoTestsForTestCaseFilter(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var testAssemblyName = "SimpleTestProject2.dll";
        var source = new List<string>() { GetAssetFullPath(testAssemblyName) };

        var veryLongTestCaseFilter =
            "FullyQualifiedName=VeryLongTestCaseNameeeeeeeeeeeeee" +
            "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee" +
            "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee" +
            "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee" +
            "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";

        _vstestConsoleWrapper.RunTests(
            source,
            GetDefaultRunSettings(),
            new TestPlatformOptions() { TestCaseFilter = veryLongTestCaseFilter },
            _runEventHandler);

        var expectedFilter = veryLongTestCaseFilter.Substring(0, 256) + "...";

        // Assert
        StringAssert.StartsWith(_runEventHandler.LogMessage, $"No test matches the given testcase filter `{expectedFilter}` in");
        StringAssert.EndsWith(_runEventHandler.LogMessage, testAssemblyName);

        Assert.AreEqual(TestMessageLevel.Warning, _runEventHandler.TestMessageLevel);
    }

    private IList<string> GetTestAssemblies()
    {
        return new List<string>
        {
            GetAssetFullPath("SimpleTestProject.dll"),
            GetAssetFullPath("SimpleTestProject2.dll")
        };
    }
}
