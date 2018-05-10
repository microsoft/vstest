﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void CommandLineRunSettingsShouldWinAmongAllOptions(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

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
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void CLIRunsettingsShouldWinBetweenCLISwitchesAndCLIRunsettings(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

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
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void CommandLineSwitchesShouldWinBetweenSettingsFileAndCommandLineSwitches(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

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

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSettingsWithoutParallelAndPlatformX86(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

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

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSettingsParamsAsArguments(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

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

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSettingsAndRunSettingsParamsAsArguments(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

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

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSettingsWithParallelAndPlatformX64(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

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

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void RunSettingsWithInvalidValueShouldLogError(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "TargetPlatform", "123" }
                                                 };
            var runsettingsFilePath = this.GetRunsettingsFilePath(runConfigurationDictionary);
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                string.Empty,
                runsettingsFilePath, this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);
            this.StdErrorContains(@"Settings file provided does not conform to required format. An error occurred while loading the settings. Error: Invalid setting 'RunConfiguration'. Invalid value '123' specified for 'TargetPlatform'.");
            File.Delete(runsettingsFilePath);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void TestAdapterPathFromRunSettings(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            var runsettingsFilePath = this.GetRunsettingsFilePath(runConfigurationDictionary);
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                string.Empty,
                runsettingsFilePath, this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
            File.Delete(runsettingsFilePath);
        }

        #region LegacySettings Tests

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, useOnlyDesktopRunner: true)]
        public void LegacySettingsWithScripts(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssemblyPath = this.GetAssetFullPath("MstestV1UnitTestProject.dll");
            var testAssemblyDirectory = Path.GetDirectoryName(testAssemblyPath);

            var setupScriptPath = Path.Combine(testAssemblyDirectory, "Scripts", "setup.bat");
            var cleanupScriptPath = Path.Combine(testAssemblyDirectory, "Scripts", "cleanup.bat");

            var runsettingsFormat = @"<RunSettings>
                                    <MSTest>
                                    <ForcedLegacyMode>true</ForcedLegacyMode>
                                    </MSTest>
                                    <LegacySettings>
                                         <Scripts setupScript=""{0}"" cleanupScript=""{1}"" />
                                    </LegacySettings>
                                   </RunSettings>";
            var runsettingsXml = string.Format(runsettingsFormat, setupScriptPath, cleanupScriptPath);

            string runsettingsFilePath = GetRunsettingsFilePath(runsettingsXml);

            var arguments = PrepareArguments(
               testAssemblyPath,
               string.Empty,
               runsettingsFilePath, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /tests:LegacySettingsScriptsTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);

            //Validate cleanup script ran
            var scriptPath = Path.Combine(Path.GetTempPath() + "ScriptTestingFile.txt");
            Assert.IsFalse(File.Exists(scriptPath));
            File.Delete(runsettingsFilePath);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, useOnlyDesktopRunner: true)]
        public void LegacySettingsWithDeploymentItem(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssemblyPath = this.GetAssetFullPath("MstestV1UnitTestProject.dll");
            var testAssemblyDirectory = Path.GetDirectoryName(testAssemblyPath);

            var deploymentItem = Path.Combine(testAssemblyDirectory, "Deployment", "DeploymentFile.xml");

            var runsettingsFormat = @"<RunSettings>
                                    <MSTest>
                                    <ForcedLegacyMode>true</ForcedLegacyMode>
                                    </MSTest>
                                    <LegacySettings>
                                         <Deployment>
                                            <DeploymentItem filename=""{0}"" />
                                         </Deployment>
                                    </LegacySettings>
                                   </RunSettings>";

            var runsettingsXml = string.Format(runsettingsFormat, deploymentItem);
            string runsettingsFilePath = GetRunsettingsFilePath(runsettingsXml);

            var arguments = PrepareArguments(
               testAssemblyPath,
               string.Empty,
               runsettingsFilePath, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /tests:LegacySettingsDeploymentItemTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
            File.Delete(runsettingsFilePath);
        }

        #endregion

        private string GetRunsettingsFilePath(string runsettingsXml)
        {
            var runsettingsPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".runsettings");
            if (File.Exists(runsettingsPath))
            {
                File.Delete(runsettingsPath);
            }

            using (var file = File.CreateText(runsettingsPath))
            {
                file.Write(runsettingsXml);
                file.Flush();
            }
            return runsettingsPath;
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

            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runsettingsPath, this.FrameworkArgValue, this.testEnvironment.InIsolationValue);

            if (!string.IsNullOrWhiteSpace(additionalArgs))
            {
                arguments = string.Concat(arguments, " ", additionalArgs);
            }

            if (!string.IsNullOrWhiteSpace(runSettingsArgs))
            {
                arguments = string.Concat(arguments, " -- ", runSettingsArgs);
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
