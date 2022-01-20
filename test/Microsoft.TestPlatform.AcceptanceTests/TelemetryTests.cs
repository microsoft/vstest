// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    [TestClass]
    public class TelemetryTests : AcceptanceTestBase
    {
        private const string TELEMETRY_OPTEDIN = "VSTEST_TELEMETRY_OPTEDIN";
        private const string LOG_TELEMETRY = "VSTEST_LOGTELEMETRY";
        private const string LOG_TELEMETRY_PATH = "VSTEST_LOGTELEMETRY_PATH";

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

            using var workspace = new Workspace();
            var env = new Dictionary<string, string>
            {
                [LOG_TELEMETRY_PATH] = workspace.Path,
                [TELEMETRY_OPTEDIN] = "1",
                [LOG_TELEMETRY] = "1",
            };

            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath(), this.FrameworkArgValue, string.Empty, env);
            this.ValidateOutput("Execution", workspace);
        }

        private void DiscoverTests(string runnerFramework)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Telemetry API is not supported for .NetCore runner");
                return;
            }

            var assemblyPaths = this.GetAssetFullPath("SimpleTestProject2.dll");

            using var workspace = new Workspace();
            var env = new Dictionary<string, string>
            {
                [LOG_TELEMETRY_PATH] = workspace.Path,
                [TELEMETRY_OPTEDIN] = "1",
                [LOG_TELEMETRY] = "1",
            };

            this.InvokeVsTestForDiscovery(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, env);
            this.ValidateOutput("Discovery", workspace);
        }

        private void ValidateOutput(string command, Workspace workspace)
        {
            if (!Directory.Exists(workspace.Path))
            {
                Assert.Fail("Could not find the telemetry logs folder at {0}", workspace.Path);
            }

            bool isValid = false;
            var directory = new DirectoryInfo(workspace.Path);
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

            Assert.IsTrue(isValid);
        }
    }
}
