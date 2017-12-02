// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiscoverTests : AcceptanceTestBase
    {
        private const string Netcoreapp = "netcoreapp";
        private const string Message = "VsTestConsoleWrapper donot support .Net Core Runner";

        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private DiscoveryEventHandler discoveryEventHandler;
        private DiscoveryEventHandler2 discoveryEventHandler2;

        public DiscoverTests()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.discoveryEventHandler = new DiscoveryEventHandler();
            this.discoveryEventHandler2 = new DiscoveryEventHandler2();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingDiscoveryEventHandler1(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            this.vstestConsoleWrapper.DiscoverTests(this.GetTestAssemblies(), this.GetDefaultRunSettings(), this.discoveryEventHandler);

            // Assert.
            Assert.AreEqual(6, this.discoveryEventHandler.DiscoveredTestCases.Count);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedOut(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            this.vstestConsoleWrapper.DiscoverTests(
               this.GetTestAssemblies(),
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = false },
                this.discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, this.discoveryEventHandler2.DiscoveredTestCases.Count);
            Assert.AreEqual(0, this.discoveryEventHandler2.Metrics.Count);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedIn(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            this.vstestConsoleWrapper.DiscoverTests(this.GetTestAssemblies(), this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, this.discoveryEventHandler2.DiscoveredTestCases.Count);
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests));
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecByAllAdapters));
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForDiscovery));
            Assert.IsTrue(this.discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.DiscoveryState));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingEventHandler2AndBatchSize(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var discoveryEventHandlerForBatchSize = new DiscoveryEventHandlerForBatchSize();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
                                    <RunSettings>     
                                        <RunConfiguration>
                                        <BatchSize>3</BatchSize>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.vstestConsoleWrapper.DiscoverTests(
               this.GetTestAssemblies(),
                runSettingsXml,
                null,
                discoveryEventHandlerForBatchSize);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandlerForBatchSize.DiscoveredTestCases.Count);
            Assert.AreEqual(3, discoveryEventHandlerForBatchSize.batchSize);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingEventHandler1AndBatchSize(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            var discoveryEventHandlerForBatchSize = new DiscoveryEventHandlerForBatchSize();

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
                                    <RunSettings>     
                                        <RunConfiguration>
                                        <BatchSize>3</BatchSize>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.vstestConsoleWrapper.DiscoverTests(
               this.GetTestAssemblies(),
                runSettingsXml,
                discoveryEventHandlerForBatchSize);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandlerForBatchSize.DiscoveredTestCases.Count);
            Assert.AreEqual(3, discoveryEventHandlerForBatchSize.batchSize);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsUsingLiveUnitTesting(RunnerInfo runnerInfo)
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
        public void DiscoverTestsUsingSourceNavigation(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);

            this.vstestConsoleWrapper.DiscoverTests(
               this.GetTestAssemblies(),
                this.GetDefaultRunSettings(),
                this.discoveryEventHandler);

            // Assert.
            var testCase =
                this.discoveryEventHandler.DiscoveredTestCases.Where(dt => dt.FullyQualifiedName.Equals("SampleUnitTestProject.UnitTest1.PassingTest"));

            // Release builds optimize code, hence line numbers are different.
            if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(23, testCase.FirstOrDefault().LineNumber);
            }
            else
            {
                Assert.AreEqual(22, testCase.FirstOrDefault().LineNumber);
            }
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