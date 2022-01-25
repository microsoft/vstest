// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The Run Tests using VsTestConsoleWrapper API's
    /// </summary>
    [TestClass]
    public class RunTestsWithFilterTests : AcceptanceTestBase
    {
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private RunEventHandler runEventHandler;

        private void Setup()
        {
            vstestConsoleWrapper = GetVsTestConsoleWrapper();
            runEventHandler = new RunEventHandler();
        }

        [TestCleanup]
        public void Cleanup()
        {
            vstestConsoleWrapper?.EndSession();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithTestCaseFilter(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            var sources = new List<string>
                              {
                                  GetAssetFullPath("SimpleTestProject.dll")
                              };

            vstestConsoleWrapper.RunTests(
                sources,
                GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest" },
                runEventHandler);

            // Assert
            Assert.AreEqual(1, runEventHandler.TestResults.Count);
            Assert.AreEqual(TestOutcome.Passed, runEventHandler.TestResults.FirstOrDefault().Outcome);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithFastFilter(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            var sources = new List<string>
                              {
                                  GetAssetFullPath("SimpleTestProject.dll")
                              };

            vstestConsoleWrapper.RunTests(
                sources,
                GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest | FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest" },
                runEventHandler);

            // Assert
            Assert.AreEqual(2, runEventHandler.TestResults.Count);
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        private IList<string> GetTestAssemblies()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetAssetFullPath("SimpleTestProject.dll"),
                                         GetAssetFullPath("SimpleTestProject2.dll")
                                     };

            return testAssemblies;
        }
    }
}