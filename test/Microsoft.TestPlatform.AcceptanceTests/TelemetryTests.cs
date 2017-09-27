// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;

    [TestClass]
    public class TelemetryTests : AcceptanceTestBase
    {
        private const string TELEMETRY_OPTEDIN = "VSTEST_TELEMETRY_OPTEDIN";
        private const string UserRoot = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\Telemetry\Channels";
        private const string SubKey = @"Software\Microsoft\VisualStudio\Telemetry\Channels";
        private const string Pathfolder = "VSTelemetryLog";
        private const string AsiKeyName = "asi";
        private const string AiasimovKeyName = "aiasimov";
        private const string AivortexKeyName = "aivortex";
        private const string FileLoggerName = "fileLogger";

        public TelemetryTests()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable(TELEMETRY_OPTEDIN, "1");

            // Setting registry keys to not send telemetry at backend.
            Registry.SetValue(UserRoot, AsiKeyName, 0, RegistryValueKind.DWord);
            Registry.SetValue(UserRoot, AiasimovKeyName, 0, RegistryValueKind.DWord);
            Registry.SetValue(UserRoot, AivortexKeyName, 0, RegistryValueKind.DWord);

            // Logging the telemetry in file
            Registry.SetValue(UserRoot, FileLoggerName, 1, RegistryValueKind.DWord);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Removing the Keys
            Registry.CurrentUser.DeleteSubKey(SubKey);

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable(TELEMETRY_OPTEDIN, "0");
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework(inIsolation: true, inProcess: true)]
        [NETCORETargetFramework]
        public void RunTestsShouldPublishMetrics(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.RunTests(runnerInfo.RunnerFramework);
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

        private void ValidateOutput()
        {
            bool isValid = false;
            string path = System.IO.Path.GetTempPath() + Pathfolder;

            if (Directory.Exists(path))
            {
                var directory = new DirectoryInfo(path);
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
                        continue;
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
                        continue;
                    }
                }
            }

            Assert.IsTrue(isValid);
        }
    }
}
