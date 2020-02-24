// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using global::TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// The Run Tests using VsTestConsoleWrapper API's
    /// </summary>
    [TestClass]
    public class RunTestsWithDifferentConfigurationTests : AcceptanceTestBase
    {
        private const string Netcoreapp = "netcoreapp";
        private const string Message = "VsTestConsoleWrapper does not support .Net Core Runner";

        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private RunEventHandler runEventHandler;

        private void Setup()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.runEventHandler = new RunEventHandler();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.vstestConsoleWrapper?.EndSession();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithTestAdapterPath(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

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
        public void RunTestsWithRunSettingsWithParallel(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            string runSettingsXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                        <MaxCpuCount>2</MaxCpuCount>
                                        </RunConfiguration>
                                    </RunSettings>";

            var testHostNames = new[] { "testhost", "testhost.x86", "dotnet" };
            int expectedNumOfProcessCreated = 2;

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testHostNames);

            this.vstestConsoleWrapper.RunTests(
                this.GetTestAssemblies(),
                runSettingsXml,
                this.runEventHandler);

            cts.Cancel();

            // Assert
            this.runEventHandler.EnsureSuccess();
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Result.Count,
                $"Number of '{ string.Join(", ", testHostNames) }' process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result.Count} ({ string.Join(", ", numOfProcessCreatedTask.Result) })");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void RunTestsWithTestSettings(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);
            this.Setup();

            var testsettingsFile = Path.Combine(Path.GetTempPath(), "tempsettings.testsettings");
            string testSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?><TestSettings></TestSettings>";

            File.WriteAllText(testsettingsFile, testSettingsXml, Encoding.UTF8);
            var runSettings = $"<RunSettings><RunConfiguration><TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion></RunConfiguration><MSTest><SettingsFile>" + testsettingsFile + "</SettingsFile></MSTest></RunSettings>";
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
        public void RunTestsWithX64Source(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject3.dll")
                              };


            int expectedNumOfProcessCreated = 1;
            var testhostProcessNames = new[] { "testhost", "dotnet" };

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts, testhostProcessNames);

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName = SampleUnitTestProject3.UnitTest1.WorkingDirectoryTest" },
                this.runEventHandler);

            cts.Cancel();

            // Assert
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            var numberOfProcessCreated = numOfProcessCreatedTask.Result;
            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numberOfProcessCreated.Count,
                $"Number of { string.Join(" ,", testhostProcessNames) } process created, expected: {expectedNumOfProcessCreated} actual: {numberOfProcessCreated.Count} ({ string.Join(", ", numberOfProcessCreated) })");
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