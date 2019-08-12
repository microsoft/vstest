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
        private string runsettingsPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".runsettings");

        [TestCleanup]
        public void TestCleanup()
        {
            if (File.Exists(runsettingsPath))
            {
                File.Delete(runsettingsPath);
            }
        }

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
        }

        #region LegacySettings Tests

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, useCoreRunner: false)]
        public void LegacySettingsWithPlatform(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssemblyPath = this.GetAssetFullPath("LegacySettingsUnitTestProject.dll");
            var testAssemblyDirectory = Path.GetDirectoryName(testAssemblyPath);

            var runsettingsXml = @"<RunSettings>
                                    <MSTest>
                                    <ForcedLegacyMode>true</ForcedLegacyMode>
                                    </MSTest>
                                    <LegacySettings>
                                      <Execution hostProcessPlatform=""x64"">
                                      </Execution>
                                    </LegacySettings>
                                   </RunSettings>";

            File.WriteAllText(this.runsettingsPath, runsettingsXml);

            var arguments = PrepareArguments(
               testAssemblyPath,
               string.Empty,
               this.runsettingsPath, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 0, 0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, useCoreRunner: false)]
        public void LegacySettingsWithScripts(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssemblyPath = this.GetAssetFullPath("LegacySettingsUnitTestProject.dll");
            var testAssemblyDirectory = Path.GetDirectoryName(testAssemblyPath);

            // Create the script files
            var guid = Guid.NewGuid();
            var setupScriptName = "setupScript_" + guid + ".bat";
            var setupScriptPath = Path.Combine(Path.GetTempPath(), setupScriptName);
            File.WriteAllText(setupScriptPath, @"echo > %temp%\ScriptTestingFile.txt");

            var cleanupScriptName = "cleanupScript_" + guid + ".bat";
            var cleanupScriptPath = Path.Combine(Path.GetTempPath(), cleanupScriptName);
            File.WriteAllText(cleanupScriptPath, @"del %temp%\ScriptTestingFile.txt");

            var runsettingsFormat = @"<RunSettings>
                                    <MSTest>
                                    <ForcedLegacyMode>true</ForcedLegacyMode>
                                    </MSTest>
                                    <LegacySettings>
                                         <Scripts setupScript=""{0}"" cleanupScript=""{1}"" />
                                    </LegacySettings>
                                   </RunSettings>";

            // Scripts have relative paths to temp directory where the runsettings is created.
            var runsettingsXml = string.Format(runsettingsFormat, setupScriptName, cleanupScriptName);

            File.WriteAllText(this.runsettingsPath, runsettingsXml);

            var arguments = PrepareArguments(
               testAssemblyPath,
               string.Empty,
               this.runsettingsPath, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:Name=ScriptsTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);

            // Validate cleanup script ran
            var scriptPath = Path.Combine(Path.GetTempPath() + "ScriptTestingFile.txt");
            Assert.IsFalse(File.Exists(scriptPath));

            // Cleanup script files
            File.Delete(setupScriptPath);
            File.Delete(cleanupScriptPath);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, useCoreRunner: false)]
        public void LegacySettingsWithDeploymentItem(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssemblyPath = this.GetAssetFullPath("LegacySettingsUnitTestProject.dll");
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
            File.WriteAllText(this.runsettingsPath, runsettingsXml);

            var arguments = PrepareArguments(
               testAssemblyPath,
               string.Empty,
               this.runsettingsPath, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:Name=DeploymentItemTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, useCoreRunner: false)]
        public void LegacySettingsTestTimeout(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var testAssemblyPath = this.GetAssetFullPath("LegacySettingsUnitTestProject.dll");
            var runsettingsXml = @"<RunSettings>
                                    <MSTest>
                                    <ForcedLegacyMode>true</ForcedLegacyMode>
                                    </MSTest>
                                    <LegacySettings>
                                        <Execution><Timeouts testTimeout=""2000"" />
                                        </Execution>
                                    </LegacySettings>
                                   </RunSettings>";
            File.WriteAllText(this.runsettingsPath, runsettingsXml);
            var arguments = PrepareArguments(testAssemblyPath, string.Empty, this.runsettingsPath, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:Name~TimeTest");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, useCoreRunner: false)]
        public void LegacySettingsAssemblyResolution(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var testAssemblyPath = this.GetAssetFullPath("LegacySettingsUnitTestProject.dll");
            var runsettingsFormat = @"<RunSettings>
                                    <MSTest><ForcedLegacyMode>true</ForcedLegacyMode></MSTest>
                                    <LegacySettings>
                                        <Execution>
                                         <TestTypeSpecific>
                                          <UnitTestRunConfig testTypeId=""13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b"">
                                           <AssemblyResolution>
                                              <TestDirectory useLoadContext=""true"" />
                                              <RuntimeResolution>
                                                  <Directory path=""{0}"" includeSubDirectories=""true"" />
                                              </RuntimeResolution>
                                           </AssemblyResolution>
                                          </UnitTestRunConfig>
                                         </TestTypeSpecific>
                                        </Execution>
                                    </LegacySettings>
                                   </RunSettings>";

            var testAssemblyDirectory = Path.Combine(this.testEnvironment.TestAssetsPath, "LegacySettingsUnitTestProject", "DependencyAssembly");
            var runsettingsXml = string.Format(runsettingsFormat, testAssemblyDirectory);

            File.WriteAllText(this.runsettingsPath, runsettingsXml);
            var arguments = PrepareArguments(testAssemblyPath, string.Empty, this.runsettingsPath, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:Name=DependencyTest");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 0, 0);
        }

        #endregion

        #region RunSettings With EnvironmentVariables Settings Tests

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void EnvironmentVariablesSettingsShouldSetEnvironmentVariables(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssemblyPath = this.GetAssetFullPath("EnvironmentVariablesTestProject.dll");

            var runsettingsXml = @"<RunSettings>
                                    <RunConfiguration>
                                      <EnvironmentVariables>
                                        <DOTNET_ROOT>C:\ProgramFiles\dotnet</DOTNET_ROOT>
                                      </EnvironmentVariables>
                                    </RunConfiguration>
                                   </RunSettings>";

            File.WriteAllText(this.runsettingsPath, runsettingsXml);

            var arguments = PrepareArguments(
               testAssemblyPath,
               string.Empty,
               this.runsettingsPath, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        #endregion

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
