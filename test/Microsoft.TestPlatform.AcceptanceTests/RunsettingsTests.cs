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
        [CustomDataTestMethod]
        [NET46TargetFramework]
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
            this.RunTestWithRunSettings(runConfigurationDictionary, testhostProcessName, expectedNumOfProcessCreated);
        }

        [CustomDataTestMethod]
        [NET46TargetFramework]
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

            this.RunTestWithRunSettingsAndRunSettingsParamsAsArguments(null, runSettingsArgs, testhostProcessName, expectedNumOfProcessCreated);
        }

        [CustomDataTestMethod]
        [NET46TargetFramework]
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

            this.RunTestWithRunSettingsAndRunSettingsParamsAsArguments(runConfigurationDictionary, runSettingsArgs, testhostProcessName, expectedNumOfProcessCreated);
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

        [CustomDataTestMethod]
        [NET46TargetFramework]
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
            this.RunTestWithRunSettings(runConfigurationDictionary, testhostProcessName, expectedProcessCreated);
        }

        // Known issue https://github.com/Microsoft/vstest/issues/135
        [Ignore]
        [CustomDataTestMethod]
        [NET46TargetFramework]
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

        private void RunTestWithRunSettings(
            Dictionary<string, string> runConfigurationDictionary,
            string testhostProcessName,
            int expectedNumOfProcessCreated)
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var runsettingsPath = this.GetRunsettingsFilePath(runConfigurationDictionary);
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runsettingsPath, this.FrameworkArgValue);
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result} args: {arguments} runner path: {this.testEnvironment.GetConsoleRunnerPath()}");
            this.ValidateSummaryStatus(2, 2, 2);
            File.Delete(runsettingsPath);
        }

        private void RunTestWithRunSettingsAndRunSettingsParamsAsArguments(
            Dictionary<string, string> runConfigurationDictionary,
            string runSettingsArgs,
            string testhostProcessName,
            int expectedNumOfProcessCreated)
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var runsettingsPath = string.Empty;

            if (runConfigurationDictionary != null)
            {
                runsettingsPath = this.GetRunsettingsFilePath(runConfigurationDictionary);
            }

            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runsettingsPath, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " -- ", runSettingsArgs);
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result} args: {arguments} runner path: {this.testEnvironment.GetConsoleRunnerPath()}");
            this.ValidateSummaryStatus(2, 2, 2);
        }

        // runsetting with framework
    }
}
