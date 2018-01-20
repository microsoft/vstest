// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The Run Tests using VsTestConsoleWrapper API's
    /// </summary>
    [TestClass]
    public class DifferentTestFrameworkSimpleTests : AcceptanceTestBase
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
        public void RunTestsWithNunitAdapter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);
            this.Setup();

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
            this.Setup();

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
        public void RunTestsWithChutzpahAdapter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);
            this.Setup();

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