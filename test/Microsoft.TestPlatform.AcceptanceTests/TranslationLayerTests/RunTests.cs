// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The Run Tests using VsTestConsoleWrapper API's
    /// </summary>
    [TestClass]
    public class RunTests : AcceptanceTestBase
    {
        private const string Netcoreapp = "netcoreapp";
        private const string Message = "VsTestConsoleWrapper donot support .Net Core Runner";

        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private RunEventHandler runEventHandler;

        public RunTests()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.runEventHandler = new RunEventHandler();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunAllTests(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies(), this.GetDefaultRunSettings(), this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithTelemetryOptedIn(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            this.vstestConsoleWrapper.RunTests(
                this.GetTestAssemblies(),
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = true },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetFramework));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetOS));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForRun));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.RunState));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithTelemetryOptedOut(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            this.vstestConsoleWrapper.RunTests(
                this.GetTestAssemblies(),
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = false },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(0, this.runEventHandler.Metrics.Count);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithTestCaseFilter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest" },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(TestOutcome.Passed, this.runEventHandler.TestResults.FirstOrDefault().Outcome);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithFastFilter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest | FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest" },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithSourceNavigation(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest" },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(TestOutcome.Passed, this.runEventHandler.TestResults.FirstOrDefault().Outcome);

            // Release builds optimize code, hence line numbers are different.
            if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(23, this.runEventHandler.TestResults.FirstOrDefault().TestCase.LineNumber);
            }
            else
            {
                Assert.AreEqual(22, this.runEventHandler.TestResults.FirstOrDefault().TestCase.LineNumber);
            }
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithTestAdapterPath(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var testAdapterPath = Directory.EnumerateFiles(this.GetTestAdapterPath(), "*.TestAdapter.dll").ToList();
            this.vstestConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });

            this.vstestConsoleWrapper.RunTests(
                this.GetTestAssemblies(),
                this.GetDefaultRunSettings(),
                this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithNunitAdapter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("NUTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                this.runEventHandler);

            var testCase =
                this.runEventHandler.TestResults.Where(tr => tr.TestCase.DisplayName.Equals("PassTestMethod1"));

            // Assert
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));

            // Release builds optimize code, hence line numbers are different.
            if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(11, testCase.FirstOrDefault().TestCase.LineNumber);
            }
            else
            {
                Assert.AreEqual(10, testCase.FirstOrDefault().TestCase.LineNumber);
            }
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithXunitAdapter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            // Xunit >= 2.2 won't support net451, Minimum target framework it supports is net452.
            string testAssemblyPath = null;
            if (this.testEnvironment.TargetFramework.Equals("net451"))
            {
                testAssemblyPath = testEnvironment.GetTestAsset("XUTestProject.dll", "net46");
            }
            else
            {
                testAssemblyPath = testEnvironment.GetTestAsset("XUTestProject.dll");
            }

            var sources = new List<string> { testAssemblyPath };
            var testAdapterPath = Directory.EnumerateFiles(this.GetTestAdapterPath(UnitTestFramework.XUnit), "*.TestAdapter.dll").ToList();
            this.vstestConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                this.runEventHandler);

            var testCase =
                this.runEventHandler.TestResults.Where(tr => tr.TestCase.DisplayName.Equals("xUnitTestProject.Class1.PassTestMethod1"));

            // Assert
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));

            // Release builds optimize code, hence line numbers are different.
            if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(16, testCase.FirstOrDefault().TestCase.LineNumber);
            }
            else
            {
                Assert.AreEqual(15, testCase.FirstOrDefault().TestCase.LineNumber);
            }
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithChutzpahAdapter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var sources = new List<string>
                              {
                                  Path.Combine(this.testEnvironment.TestAssetsPath, "test.js")
                              };

            var testAdapterPath = Directory.EnumerateFiles(this.GetTestAdapterPath(UnitTestFramework.Chutzpah), "*.TestAdapter.dll").ToList();
            this.vstestConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                this.runEventHandler);

            var testCase =
                this.runEventHandler.TestResults.Where(tr => tr.TestCase.DisplayName.Equals("TestMethod1"));

            // Assert
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(1, testCase.FirstOrDefault().TestCase.LineNumber);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        [DoNotParallelize]
        public void RunTestsWithRunSettingsWithParallel(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
                                    <RunSettings>     
                                        <RunConfiguration>
                                        <MaxCpuCount>2</MaxCpuCount>
                                        </RunConfiguration>
                                    </RunSettings>";

            string testhostProcessName = string.Empty;
            int expectedNumOfProcessCreated = 0;
            if (this.IsDesktopTargetFramework())
            {
                testhostProcessName = "testhost.x86";
                expectedNumOfProcessCreated = 2;
            }
            else
            {
                testhostProcessName = "dotnet";
                if (this.IsDesktopRunner())
                {
                    expectedNumOfProcessCreated = 2;
                }
                else
                {
                    // Include launcher dotnet.exe
                    expectedNumOfProcessCreated = 3;
                }
            }

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.vstestConsoleWrapper.RunTests(
                this.GetTestAssemblies(),
                runSettingsXml,
                this.runEventHandler);

            cts.Cancel();

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result}");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void RunTestsWithTestSettings(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var testsettingsFile = Path.Combine(Path.GetTempPath(), "tempsettings.testsettings");
            string testSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?><TestSettings></TestSettings>";

            File.WriteAllText(testsettingsFile, testSettingsXml, Encoding.UTF8);
            var runSettings = "<RunSettings><MSTest><SettingsFile>" + testsettingsFile + "</SettingsFile></MSTest></RunSettings>";
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("MstestV1UnitTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                runSettings,
                this.runEventHandler);

            // Assert
            Assert.AreEqual(5, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));

            File.Delete(testsettingsFile);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        [DoNotParallelize]
        public void RunTestsWithX64Source(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject3.dll")
                              };

            string testhostProcessName = string.Empty;
            int expectedNumOfProcessCreated = 0;
            if (this.IsDesktopTargetFramework())
            {
                testhostProcessName = "testhost";
                expectedNumOfProcessCreated = 1;
            }
            else
            {
                testhostProcessName = "dotnet";
                if (this.IsDesktopRunner())
                {
                    expectedNumOfProcessCreated = 1;
                }
                else
                {
                    // Include launcher dotnet.exe
                    expectedNumOfProcessCreated = 2;
                }
            }

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName = SampleUnitTestProject3.UnitTest1.WorkingDirectoryTest" },
                this.runEventHandler);

            cts.Cancel();

            // Assert
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result}");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithCustomTestHostLaunch(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var customTestHostLauncher = new CustomTestHostLauncher();
            this.vstestConsoleWrapper.RunTestsWithCustomTestHost(this.GetTestAssemblies(), this.GetDefaultRunSettings(), this.runEventHandler, customTestHostLauncher);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.IsTrue(customTestHostLauncher.ProcessId != -1);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithLiveUnitTesting(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
                                    <RunSettings>     
                                        <RunConfiguration>
                                        <DisableAppDomain>true</DisableAppDomain>
                                        <DisableParallelization>true</DisableParallelization>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.vstestConsoleWrapper.RunTests(
                this.GetTestAssemblies(),
                runSettingsXml,
                this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsShouldThrowOnStackOverflowException(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            if (IntegrationTestEnvironment.BuildConfiguration.Equals("release", StringComparison.OrdinalIgnoreCase))
            {
                // On release, x64 builds, recursive calls may be replaced with loops (tail call optimization)
                Assert.Inconclusive("On StackOverflowException testhost not exited in release configuration.");
                return;
            }

            var source = new List<string>()
                             {
                                 this.GetAssetFullPath("SimpleTestProject3.dll")
                             };

            this.vstestConsoleWrapper.RunTests(
                source,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "ExitWithStackoverFlow" },
                this.runEventHandler);

            var errorMessage = "The active test run was aborted. Reason: Process is terminated due to StackOverflowException.\r\n";
            if (runnerInfo.TargetFramework.StartsWith("netcoreapp2."))
            {
                errorMessage =
                    "The active test run was aborted. Reason: Process is terminating due to StackOverflowException.\r\n";
            }

            // Assert
            Assert.AreEqual(errorMessage, this.runEventHandler.LogMessage);
        }

        private IList<string> GetTestAssemblies()
        {
            var testAssemblies = new List<string>
                                     {
                                         this.GetAssetFullPath("SimpleTestProject.dll"),
                                         this.GetAssetFullPath("SimpleTestProject2.dll")
                                     };

            return testAssemblies;
        }
    }
}