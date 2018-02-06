// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LiveUnitTestingTests : AcceptanceTestBase
    {
        private const string Netcoreapp = "netcoreapp";
        private const string Message = "VsTestConsoleWrapper donot support .Net Core Runner";

        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private DiscoveryEventHandler discoveryEventHandler;
        private DiscoveryEventHandler2 discoveryEventHandler2;
        private RunEventHandler runEventHandler;

        public void Setup()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.discoveryEventHandler = new DiscoveryEventHandler();
            this.discoveryEventHandler2 = new DiscoveryEventHandler2();
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
        public void DiscoverTestsUsingLiveUnitTesting(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);
            this.Setup();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <DisableAppDomain>true</DisableAppDomain>
                                        <DisableParallelization>true</DisableParallelization>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.vstestConsoleWrapper.DiscoverTests(
               this.GetTestAssemblies(),
                runSettingsXml,
                this.discoveryEventHandler);

            // Assert
            Assert.AreEqual(6, this.discoveryEventHandler.DiscoveredTestCases.Count);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithLiveUnitTesting(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);
            this.Setup();

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