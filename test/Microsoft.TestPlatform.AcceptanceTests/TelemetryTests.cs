// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryTests : AcceptanceTestBase
    {
        private readonly string resultPath;
        private string CurrentOptInStatus;
        private const string TELEMETRY_OPTEDIN = "VSTEST_TELEMETRY_OPTEDIN";
        private const string LOG_TELEMETRY = "VSTEST_LOGTELEMETRY";

        public TelemetryTests()
        {
            this.resultPath = Path.GetTempPath() + "TelemetryLogs";

            // Get Current Opt In Status
            CurrentOptInStatus = Environment.GetEnvironmentVariable(TELEMETRY_OPTEDIN);

            // Opt in the Telemetry
            Environment.SetEnvironmentVariable(TELEMETRY_OPTEDIN, "1");

            // Log the telemetry Data to file
            Environment.SetEnvironmentVariable(LOG_TELEMETRY, "1");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Opt out the Telemetry
            Environment.SetEnvironmentVariable(TELEMETRY_OPTEDIN, "0");

            // Set Current Opt in Status
            Environment.SetEnvironmentVariable(TELEMETRY_OPTEDIN, CurrentOptInStatus);

            // Unset the environment varaible
            Environment.SetEnvironmentVariable(LOG_TELEMETRY, "0");

            if (Directory.Exists(this.resultPath))
            {
                Directory.Delete(this.resultPath, true);
            }
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsShouldPublishMetrics(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.RunTests(runnerInfo.RunnerFramework);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsShouldPublishMetrics(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.DiscoverTests(runnerInfo.RunnerFramework);
        }

        private void RunTests(string runnerFramework)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Telemetry API is not supported for .NetCore runner");
                return;
            }

            var assemblyPaths = this.GetAssetFullPath("SimpleTestProject2.dll");

            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            this.ValidateOutput("Execution");
        }


        private void DiscoverTests(string runnerFramework)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Telemetry API is not supported for .NetCore runner");
                return;
            }

            var assemblyPaths = this.GetAssetFullPath("SimpleTestProject2.dll");

            this.InvokeVsTestForDiscovery(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            this.ValidateOutput("Discovery");
        }

        private void ValidateOutput(string command)
        {
            bool isValid = false;

            if (Directory.Exists(this.resultPath))
            {
                var directory = new DirectoryInfo(this.resultPath);
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
                                        && line.Contains(TelemetryDataConstants.DiscoveryState + "=" + "Completed")
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
}
