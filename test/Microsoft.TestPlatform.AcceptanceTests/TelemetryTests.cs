// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using System;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

using VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TelemetryTests : AcceptanceTestBase
{
    private readonly string _resultPath;
    private readonly string _currentOptInStatus;
    private const string TelemetryOptedin = "VSTEST_TELEMETRY_OPTEDIN";
    private const string LogTelemetry = "VSTEST_LOGTELEMETRY";

    public TelemetryTests()
    {
        _resultPath = Path.GetTempPath() + "TelemetryLogs";

        // Get Current Opt In Status
        _currentOptInStatus = Environment.GetEnvironmentVariable(TelemetryOptedin);

        // Opt in the Telemetry
        Environment.SetEnvironmentVariable(TelemetryOptedin, "1");

        // Log the telemetry Data to file
        Environment.SetEnvironmentVariable(LogTelemetry, "1");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Opt out the Telemetry
        Environment.SetEnvironmentVariable(TelemetryOptedin, "0");

        // Set Current Opt in Status
        Environment.SetEnvironmentVariable(TelemetryOptedin, _currentOptInStatus);

        // Unset the environment variable
        Environment.SetEnvironmentVariable(LogTelemetry, "0");

        if (Directory.Exists(_resultPath))
        {
            Directory.Delete(_resultPath, true);
        }
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsShouldPublishMetrics(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        RunTests(runnerInfo.RunnerFramework);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    [NetCoreTargetFrameworkDataSource]
    public void DiscoverTestsShouldPublishMetrics(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        DiscoverTests(runnerInfo.RunnerFramework);
    }

    private void RunTests(string runnerFramework)
    {
        if (runnerFramework.StartsWith("netcoreapp"))
        {
            Assert.Inconclusive("Telemetry API is not supported for .NetCore runner");
            return;
        }

        var assemblyPaths = GetAssetFullPath("SimpleTestProject2.dll");

        InvokeVsTestForExecution(assemblyPaths, GetTestAdapterPath(), FrameworkArgValue, string.Empty);
        ValidateOutput("Execution");
    }

    private void DiscoverTests(string runnerFramework)
    {
        if (runnerFramework.StartsWith("netcoreapp"))
        {
            Assert.Inconclusive("Telemetry API is not supported for .NetCore runner");
            return;
        }

        var assemblyPaths = GetAssetFullPath("SimpleTestProject2.dll");

        InvokeVsTestForDiscovery(assemblyPaths, GetTestAdapterPath(), string.Empty, FrameworkArgValue);
        ValidateOutput("Discovery");
    }

    private void ValidateOutput(string command)
    {
        bool isValid = false;

        if (Directory.Exists(_resultPath))
        {
            var directory = new DirectoryInfo(_resultPath);
            var file = directory.GetFiles().OrderByDescending(f => f.CreationTime).First();

            string[] lines = File.ReadAllLines(file.FullName);

            foreach (var line in lines)
            {
                if (line.Contains(TelemetryDataConstants.TestExecutionCompleteEvent) && command.Equals("Execution", StringComparison.Ordinal))
                {
                    var isPresent = line.Contains(
                                        TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution)
                                    && line.Contains(TelemetryDataConstants.NumberOfAdapterUsedToRunTests)
                                    && line.Contains(TelemetryDataConstants.ParallelEnabledDuringExecution + '=' + "False")
                                    && line.Contains(TelemetryDataConstants.NumberOfSourcesSentForRun + '=' + "1")
                                    && line.Contains(TelemetryDataConstants.RunState + '=' + "Completed")
                                    && line.Contains(TelemetryDataConstants.TimeTakenByAllAdaptersInSec)
                                    && line.Contains(TelemetryDataConstants.TotalTestsRun + '=' + "3")
                                    && line.Contains(TelemetryDataConstants.TotalTestsRanByAdapter)
                                    && line.Contains(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter);

                    isValid = isPresent;
                    break;
                }
                else if (line.Contains(TelemetryDataConstants.TestDiscoveryCompleteEvent) && command.Equals("Discovery", StringComparison.Ordinal))
                {
                    var isPresent = line.Contains(TelemetryDataConstants.TotalTestsDiscovered + '=' + "3")
                                    && line.Contains(TelemetryDataConstants.ParallelEnabledDuringDiscovery + '=' + "False")
                                    && line.Contains(TelemetryDataConstants.TimeTakenInSecForDiscovery)
                                    && line.Contains(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec)
                                    && line.Contains(TelemetryDataConstants.TimeTakenInSecByAllAdapters)
                                    && line.Contains(TelemetryDataConstants.TotalTestsByAdapter)
                                    && line.Contains(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter)
                                    && line.Contains(TelemetryDataConstants.DiscoveryState + "=Completed")
                                    && line.Contains(TelemetryDataConstants.NumberOfSourcesSentForDiscovery + '=' + "1")
                                    && line.Contains(
                                        TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery)
                                    && line.Contains(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests);

                    isValid = isPresent;
                    break;
                }
            }
        }

        Assert.IsTrue(isValid);
    }
}