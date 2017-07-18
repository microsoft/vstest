// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RunsettingsTests : AcceptanceTestBase
    {
        #region Runsettings precedence tests
        /// <summary>
        /// Command line run settings should have high precedence among settings file, cli runsettings and cli switches
        /// </summary>
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void CommandLineRunSettingsShouldWinAmongAllOptions(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var targetPlatform = "x86";
            var testhostProcessName = this.GetTestHostProcessName(targetPlatform);
            var expectedNumOfProcessCreated = GetExpectedNumOfProcessCreatedForWithoutParallel();

            // passing parallel
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "0" },
                                                         { "TargetFrameworkVersion", this.GetTargetFramworkForRunsettings() },
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            // passing different platform
            var additionalArgs = "/Platform:x64";

            var runSettingsArgs = String.Join(
                " ",
                new string[]
                    {
                        "RunConfiguration.MaxCpuCount=1",
                        string.Concat("RunConfiguration.TargetPlatform=",targetPlatform),
                        string.Concat("RunConfiguration.TargetFrameworkVersion=" , this.GetTargetFramworkForRunsettings()),
                        string.Concat("RunConfiguration.TestAdaptersPaths=" , this.GetTestAdapterPath())
                    });

            this.RunTestWithRunSettings(runConfigurationDictionary, runSettingsArgs, additionalArgs, testhostProcessName, expectedNumOfProcessCreated);
        }

        /// <summary>
        /// Command line run settings should have high precedence btween cli runsettings and cli switches.
        /// </summary>
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void CLIRunsettingsShouldWinBetweenCLISwitchesAndCLIRunsettings(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var targetPlatform = "x86";
            var testhostProcessName = this.GetTestHostProcessName(targetPlatform);
            var expectedNumOfProcessCreated = GetExpectedNumOfProcessCreatedForWithoutParallel();

            // Pass parallel
            var additionalArgs = "/Parallel";

            // Pass non parallel
            var runSettingsArgs = String.Join(
                " ",
                new string[]
                    {
                        "RunConfiguration.MaxCpuCount=1",
                        string.Concat("RunConfiguration.TargetPlatform=",targetPlatform),
                        string.Concat("RunConfiguration.TargetFrameworkVersion=" , this.GetTargetFramworkForRunsettings()),
                        string.Concat("RunConfiguration.TestAdaptersPaths=" , this.GetTestAdapterPath())
                    });

            this.RunTestWithRunSettings(null, runSettingsArgs, additionalArgs, testhostProcessName, expectedNumOfProcessCreated);
        }

        /// <summary>
        /// Command line switches should have high precedence if runsetting file and commandline switch specified
        /// </summary>
        /// <param name="runnerFramework"></param>
        /// <param name="targetFramework"></param>
        /// <param name="targetRuntime"></param>
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void CommandLineSwitchesShouldWinBetweenSettingsFileAndCommandLineSwitches(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var targetPlatform = "x86";
            var testhostProcessName = this.GetTestHostProcessName(targetPlatform);
            var expectedNumOfProcessCreated = GetExpectedNumOfProcessCreatedForWithoutParallel();

            // passing different platform
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "1" },
                                                          { "TargetPlatform", "x64" },
                                                         { "TargetFrameworkVersion", this.GetTargetFramworkForRunsettings() },
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            var additionalArgs = "/Platform:x86";

            this.RunTestWithRunSettings(runConfigurationDictionary, null, additionalArgs, testhostProcessName, expectedNumOfProcessCreated);
        }

        #endregion

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSettingsWithoutParallelAndPlatformX86(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var targetPlatform = "x86";
            var testhostProcessName = this.GetTestHostProcessName(targetPlatform);
            var expectedNumOfProcessCreated = GetExpectedNumOfProcessCreatedForWithoutParallel();

            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "1" },
                                                         { "TargetPlatform", targetPlatform },
                                                         { "TargetFrameworkVersion", this.GetTargetFramworkForRunsettings() },
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            this.RunTestWithRunSettings(runConfigurationDictionary, null, null, testhostProcessName, expectedNumOfProcessCreated);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSettingsParamsAsArguments(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var targetPlatform = "x86";
            var testhostProcessName = this.GetTestHostProcessName(targetPlatform);
            var expectedNumOfProcessCreated = GetExpectedNumOfProcessCreatedForWithoutParallel();

            var runSettingsArgs = String.Join(
                " ",
                new string[]
                    {
                        "RunConfiguration.MaxCpuCount=1",
                        string.Concat("RunConfiguration.TargetPlatform=",targetPlatform),
                        string.Concat("RunConfiguration.TargetFrameworkVersion=" , this.GetTargetFramworkForRunsettings()),
                        string.Concat("RunConfiguration.TestAdaptersPaths=" , this.GetTestAdapterPath())
                    });

            this.RunTestWithRunSettings(null, runSettingsArgs, null, testhostProcessName, expectedNumOfProcessCreated);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSettingsAndRunSettingsParamsAsArguments(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var targetPlatform = "x86";
            var testhostProcessName = this.GetTestHostProcessName(targetPlatform);
            var expectedNumOfProcessCreated = GetExpectedNumOfProcessCreatedForWithoutParallel();
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "2" },
                                                         { "TargetPlatform", targetPlatform },
                                                         { "TargetFrameworkVersion", this.GetTargetFramworkForRunsettings() },
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };

            var runSettingsArgs = String.Join(
                " ",
                new string[]
                    {
                        "RunConfiguration.MaxCpuCount=1",
                        string.Concat("RunConfiguration.TargetPlatform=",targetPlatform),
                        string.Concat("RunConfiguration.TargetFrameworkVersion=" , this.GetTargetFramworkForRunsettings()),
                        string.Concat("RunConfiguration.TestAdaptersPaths=" , this.GetTestAdapterPath())
                    });

            this.RunTestWithRunSettings(runConfigurationDictionary, runSettingsArgs, null, testhostProcessName, expectedNumOfProcessCreated);
        }

        // Randomly failing with error "The active test run was aborted. Reason: Destination array was not long enough.
        // Check destIndex and length, and the array's lower bounds. Test Run Failed."
        // Issue: https://github.com/Microsoft/vstest/issues/292
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSettingsWithParallelAndPlatformX64(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var targetPlatform = "x64";
            var testhostProcessName = this.GetTestHostProcessName(targetPlatform);
            var expectedProcessCreated = 2;
            if (!this.IsDesktopTargetFramework() && !this.IsDesktopRunner())
            {
                expectedProcessCreated = 3;
            }

            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "2" },
                                                         { "TargetPlatform", targetPlatform },
                                                         { "TargetFrameworkVersion", this.GetTargetFramworkForRunsettings()},
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            this.RunTestWithRunSettings(runConfigurationDictionary, null, null, testhostProcessName, expectedProcessCreated);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void TestAdapterPathFromRunSettings(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            var runsettingsFilePath = this.GetRunsettingsFilePath(runConfigurationDictionary);
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                string.Empty,
                runsettingsFilePath,
                this.FrameworkArgValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
            File.Delete(runsettingsFilePath);
        }

        private string GetRunsettingsFilePath(Dictionary<string, string> runConfigurationDictionary)
        {
            var runsettingsPath = Path.Combine(
                Path.GetTempPath(),
                "test_" + Guid.NewGuid() + ".runsettings");
            CreateRunSettingsFile(runsettingsPath, runConfigurationDictionary);
            return runsettingsPath;
        }

        private void RunTestWithRunSettings(Dictionary<string, string> runConfigurationDictionary,
            string runSettingsArgs, string additionalArgs, string testhostProcessName, int expectedNumOfProcessCreated)
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');

            var runsettingsPath = string.Empty;

            if (runConfigurationDictionary != null)
            {
                runsettingsPath = this.GetRunsettingsFilePath(runConfigurationDictionary);
            }

            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runsettingsPath, this.FrameworkArgValue);

            if (!string.IsNullOrWhiteSpace(runSettingsArgs))
            {
                arguments = string.Concat(arguments, " -- ", runSettingsArgs);
            }

            if (!string.IsNullOrWhiteSpace(additionalArgs))
            {
                arguments = string.Concat(arguments, " ", additionalArgs);
            }

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            // assert
            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result} args: {arguments} runner path: {this.GetConsoleRunnerPath()}");
            this.ValidateSummaryStatus(2, 2, 2);

            //cleanup
            if (!string.IsNullOrWhiteSpace(runsettingsPath))
            {
                File.Delete(runsettingsPath);
            }
        }

        private int GetExpectedNumOfProcessCreatedForWithoutParallel()
        {
            int expectedNumOfProcessCreated;
            if (this.IsDesktopTargetFramework())
            {
                expectedNumOfProcessCreated = 1;
            }
            else
            {
                expectedNumOfProcessCreated = this.IsDesktopRunner() ? 2 : 3;
            }
            return expectedNumOfProcessCreated;
        }
    }
}
