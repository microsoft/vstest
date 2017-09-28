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
        private const string TELEMETRY_OPTEDIN = "VSTEST_TELEMETRY_OPTEDIN";
        private const string LOG_TELEMETRY = "VSTEST_LOGTELEMETRY";

        public TelemetryTests()
        {
            this.resultPath = Path.GetTempPath() + "TelemetryLogs";

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

            // Unset the environment varaible
            Environment.SetEnvironmentVariable(LOG_TELEMETRY, "0");

            if (Directory.Exists(this.resultPath))
            {
                Directory.Delete(this.resultPath, true);
            }
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework(inIsolation: true, inProcess: true)]
        [NETCORETargetFramework]
        public void RunTestsShouldPublishMetrics(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.RunTests(runnerInfo.RunnerFramework);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework(inIsolation: true, inProcess: true)]
        [NETCORETargetFramework]
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
            this.ValidateOutput();
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
            this.ValidateOutput();
        }

        private void ValidateOutput()
        {
            bool isValid = false;

            if (Directory.Exists(this.resultPath))
            {
                var directory = new DirectoryInfo(this.resultPath);
                var file = directory.GetFiles().OrderByDescending(f => f.CreationTime).First();

                string[] lines = File.ReadAllLines(file.FullName);
                foreach (var line in lines)
                {
                    if (line.Contains(TelemetryDataConstants.TestExecutionCompleteEvent))
                    {
                        var isPresent = line.Contains(TelemetryDataConstants.DataCollectorsEnabled)
                                        && line.Contains(
                                            TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution)
                                        && line.Contains(TelemetryDataConstants.NumberOfAdapterUsedToRunTests)
                                        && line.Contains(TelemetryDataConstants.ParallelEnabledDuringExecution)
                                        && line.Contains(TelemetryDataConstants.NumberOfSourcesSentForRun)
                                        && line.Contains(TelemetryDataConstants.RunState)
                                        && line.Contains(TelemetryDataConstants.TimeTakenByAllAdaptersInSec)
                                        && line.Contains(TelemetryDataConstants.TotalTestsRun)
                                        && line.Contains(TelemetryDataConstants.TotalTestsRanByAdapter)
                                        && line.Contains(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter);

                        isValid = isPresent;
                    }
                    else if (line.Contains(TelemetryDataConstants.TestDiscoveryCompleteEvent))
                    {
                        var isPresent = line.Contains(TelemetryDataConstants.TotalTestsDiscovered)
                                        && line.Contains(TelemetryDataConstants.ParallelEnabledDuringDiscovery)
                                        && line.Contains(TelemetryDataConstants.TimeTakenInSecForDiscovery)
                                        && line.Contains(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec)
                                        && line.Contains(TelemetryDataConstants.TimeTakenInSecByAllAdapters)
                                        && line.Contains(TelemetryDataConstants.TotalTestsByAdapter)
                                        && line.Contains(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter)
                                        && line.Contains(TelemetryDataConstants.DiscoveryState)
                                        && line.Contains(TelemetryDataConstants.NumberOfSourcesSentForDiscovery)
                                        && line.Contains(
                                            TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery)
                                        && line.Contains(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests);

                        isValid = isPresent;
                    }
                }
            }

            Assert.IsTrue(isValid);
        }
    }
}
