// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The Run Tests using VsTestConsoleWrapper API's
    /// </summary>
    [TestClass]
    public class RunTestsWithFilterTests : AcceptanceTestBase
    {
        private const string Netcoreapp = "netcoreapp";
        private const string Message = "VsTestConsoleWrapper donot support .Net Core Runner";

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
        public void RunTestsWithTestCaseFilter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);
            this.Setup();

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
            this.Setup();

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