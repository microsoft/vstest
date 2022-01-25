// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;

    [TestClass]
    public class LiveUnitTestingTests : AcceptanceTestBase
    {
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private DiscoveryEventHandler discoveryEventHandler;
        private DiscoveryEventHandler2 discoveryEventHandler2;
        private RunEventHandler runEventHandler;

        public void Setup()
        {
            vstestConsoleWrapper = GetVsTestConsoleWrapper();
            discoveryEventHandler = new DiscoveryEventHandler();
            discoveryEventHandler2 = new DiscoveryEventHandler2();
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
        public void DiscoverTestsUsingLiveUnitTesting(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <DisableAppDomain>true</DisableAppDomain>
                                        <DisableParallelization>true</DisableParallelization>
                                        </RunConfiguration>
                                    </RunSettings>";

            vstestConsoleWrapper.DiscoverTests(
               GetTestAssemblies(),
                runSettingsXml,
                discoveryEventHandler);

            // Assert
            Assert.AreEqual(6, discoveryEventHandler.DiscoveredTestCases.Count);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithLiveUnitTesting(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            Setup();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <DisableAppDomain>true</DisableAppDomain>
                                        <DisableParallelization>true</DisableParallelization>
                                        </RunConfiguration>
                                    </RunSettings>";

            vstestConsoleWrapper.RunTests(
                GetTestAssemblies(),
                runSettingsXml,
                runEventHandler);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
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