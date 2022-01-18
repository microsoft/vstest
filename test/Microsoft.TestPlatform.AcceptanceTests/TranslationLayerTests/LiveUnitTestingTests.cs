// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;

    [TestClass]
    public class LiveUnitTestingTests : AcceptanceTestBase
    {
        private TestConsoleWrapperContext wrapperContext;
        private DiscoveryEventHandler discoveryEventHandler;
        private RunEventHandler runEventHandler;

        public void Setup()
        {
            this.wrapperContext = this.GetVsTestConsoleWrapper();
            this.discoveryEventHandler = new DiscoveryEventHandler();
            this.runEventHandler = new RunEventHandler();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.wrapperContext?.VsTestConsoleWrapper?.EndSession();
        }


        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingLiveUnitTesting(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <DisableAppDomain>true</DisableAppDomain>
                                        <DisableParallelization>true</DisableParallelization>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.wrapperContext.VsTestConsoleWrapper.DiscoverTests(
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
            this.Setup();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <DisableAppDomain>true</DisableAppDomain>
                                        <DisableParallelization>true</DisableParallelization>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.wrapperContext.VsTestConsoleWrapper.RunTests(
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