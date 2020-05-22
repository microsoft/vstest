// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The Run Tests using VsTestConsoleWrapper API's
    /// </summary>
    [TestClass]
    public class FinalizeMultiTestRunsTests : AcceptanceTestBase
    {
        private IVsTestConsoleWrapper2 vstestConsoleWrapper;
        private RunEventHandler runEventHandler;
        private MultiTestRunsFinalizationEventHandler multiTestRunsFinalizationEventHandler;

        private void Setup()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.runEventHandler = new RunEventHandler();
            this.multiTestRunsFinalizationEventHandler = new MultiTestRunsFinalizationEventHandler();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.vstestConsoleWrapper?.EndSession();
        }

        [TestMethod]
        //[NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunAllTests(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Take(1), this.GetCodeCoverageRunSettings(), this.runEventHandler);
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Skip(1), this.GetCodeCoverageRunSettings(), this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.Attachments.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));

            this.vstestConsoleWrapper.FinalizeMultiTestRuns(runEventHandler.Attachments, multiTestRunsFinalizationEventHandler);
            Assert.AreEqual(1, this.multiTestRunsFinalizationEventHandler.Attachments.Count);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void EndSessionShouldEnsureVstestConsoleProcessDies(RunnerInfo runnerInfo)
        {
            var numOfProcesses = Process.GetProcessesByName("vstest.console").Length;

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies(), this.GetDefaultRunSettings(), this.runEventHandler);
            this.vstestConsoleWrapper?.EndSession();

            // Assert
            Assert.AreEqual(numOfProcesses, Process.GetProcessesByName("vstest.console").Length);

            this.vstestConsoleWrapper = null;
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestsWithTelemetryOptedIn(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

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
            this.Setup();

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
        public void RunTestsShouldThrowOnStackOverflowException(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            if (IntegrationTestEnvironment.BuildConfiguration.Equals("release", StringComparison.OrdinalIgnoreCase))
            {
                // On release, x64 builds, recursive calls may be replaced with loops (tail call optimization)
                Assert.Inconclusive("On StackOverflowException testhost not exited in release configuration.");
                return;
            }

            var source = new[] { this.GetAssetFullPath("SimpleTestProject3.dll") };

            this.vstestConsoleWrapper.RunTests(
                source,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "ExitWithStackoverFlow" },
                this.runEventHandler);

            var errorMessage = runnerInfo.TargetFramework == "net451"
                ? "The active test run was aborted. Reason: Test host process crashed : Process is terminated due to StackOverflowException.\r\n"
                : "The active test run was aborted. Reason: Test host process crashed : Process is terminating due to StackOverflowException.\r\n";

            Assert.AreEqual(errorMessage, this.runEventHandler.LogMessage);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useCoreRunner: false)]
        [NetCoreTargetFrameworkDataSource(useCoreRunner: false)]
        public void RunTestsShouldShowProperWarningOnNoTestsForTestCaseFilter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            var testAssemblyName = "SimpleTestProject2.dll";
            var source = new List<string>() { this.GetAssetFullPath(testAssemblyName) };

            var veryLongTestCaseFilter =
                "FullyQualifiedName=VeryLongTestCaseNameeeeeeeeeeeeee" +
                "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee" +
                "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee" +
                "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee" +
                "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";

            this.vstestConsoleWrapper.RunTests(
                source,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = veryLongTestCaseFilter },
                this.runEventHandler);

            var expectedFilter = veryLongTestCaseFilter.Substring(0, 256) + "...";

            // Assert
            StringAssert.StartsWith(this.runEventHandler.LogMessage, $"No test matches the given testcase filter `{expectedFilter}` in");
            StringAssert.EndsWith(this.runEventHandler.LogMessage, testAssemblyName);

            Assert.AreEqual(TestMessageLevel.Warning, this.runEventHandler.TestMessageLevel);
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

        /// <summary>
        /// Default RunSettings
        /// </summary>
        /// <returns></returns>
        public string GetCodeCoverageRunSettings()
        {
            string traceDataCollectorDir = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, $@"src\DataCollectors\TraceDataCollector\bin\{IntegrationTestEnvironment.BuildConfiguration}\netstandard2.0");
            if (this.testEnvironment.TargetFramework.Equals(IntegrationTestBase.DesktopRunnerFramework))
            {
                traceDataCollectorDir = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, $@"artifacts\{IntegrationTestEnvironment.BuildConfiguration}\Microsoft.CodeCoverage");
            }            

            string runSettingsXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                            <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                            <TestAdaptersPaths>{traceDataCollectorDir}</TestAdaptersPaths>
                                        </RunConfiguration>
                                        <DataCollectionRunSettings>
                                            <DataCollectors>
                                                <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
                                                    <Configuration>
                                                      <CodeCoverage>
                                                        <ModulePaths>
                                                          <Exclude>
                                                            <ModulePath>.*CPPUnitTestFramework.*</ModulePath>
                                                          </Exclude>
                                                        </ModulePaths>

                                                        <!-- We recommend you do not change the following values: -->
                                                        <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>
                                                        <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>
                                                        <CollectFromChildProcesses>True</CollectFromChildProcesses>
                                                        <CollectAspDotNet>False</CollectAspDotNet>
                                                      </CodeCoverage>
                                                    </Configuration>
                                                </DataCollector>
                                            </DataCollectors>
                                        </DataCollectionRunSettings>
                                    </RunSettings>";
            return runSettingsXml;
        }
    }
}