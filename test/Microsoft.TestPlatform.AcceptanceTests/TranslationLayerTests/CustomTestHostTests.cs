// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The Run Tests using VsTestConsoleWrapper API's
    /// </summary>
    [TestClass]
    public class CustomTestHostTests : AcceptanceTestBase
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
        public void RunTestsWithCustomTestHostLaunch(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            var customTestHostLauncher = new CustomTestHostLauncher();
            vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestAssemblies(), GetDefaultRunSettings(), runEventHandler, customTestHostLauncher);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
            Assert.IsTrue(customTestHostLauncher.ProcessId != -1);
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
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