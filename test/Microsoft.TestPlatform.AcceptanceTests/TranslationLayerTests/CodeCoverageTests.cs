// Copyright (c) Microsoft Corporation. All rights reserved.	
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Castle.Core.Internal;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    [TestClass]
    //Code coverage only supported on windows (based on the message in output)
    [TestCategory("Windows-Review")]
    public class CodeCoverageTests : CodeCoverageAcceptanceTestBase
    {
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private RunEventHandler runEventHandler;
        private TestRunAttachmentsProcessingEventHandler testRunAttachmentsProcessingEventHandler;

        private void Setup()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.runEventHandler = new RunEventHandler();
            this.testRunAttachmentsProcessingEventHandler = new TestRunAttachmentsProcessingEventHandler();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.vstestConsoleWrapper?.EndSession();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void TestRunWithCodeCoverage(RunnerInfo runnerInfo)
        {
            // arrange
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            // act
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies(), this.GetCodeCoverageRunSettings(1), new TestPlatformOptions { CollectMetrics = true }, this.runEventHandler);

            // assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);

            int expectedNumberOfAttachments = 1;
            Assert.AreEqual(expectedNumberOfAttachments, this.runEventHandler.Attachments.Count);

            AssertCoverageResults(this.runEventHandler.Attachments);

            Assert.AreEqual("e5f256dc-7959-4dd6-8e4f-c11150ab28e0", this.runEventHandler.Metrics["VS.TestPlatform.DataCollector.CorProfiler.datacollector://microsoft/CodeCoverage/2.0"]);
            Assert.AreEqual("e5f256dc-7959-4dd6-8e4f-c11150ab28e0", this.runEventHandler.Metrics["VS.TestPlatform.DataCollector.CoreClrProfiler.datacollector://microsoft/CodeCoverage/2.0"]);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void TestRunWithCodeCoverageUsingClrIe(RunnerInfo runnerInfo)
        {
            // arrange
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            // act
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies(), this.GetCodeCoverageRunSettings(1, true), new TestPlatformOptions { CollectMetrics = true }, this.runEventHandler);

            // assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);

            int expectedNumberOfAttachments = 1;
            Assert.AreEqual(expectedNumberOfAttachments, this.runEventHandler.Attachments.Count);

            AssertCoverageResults(this.runEventHandler.Attachments);

            Assert.AreEqual("324f817a-7420-4e6d-b3c1-143fbed6d855", this.runEventHandler.Metrics["VS.TestPlatform.DataCollector.CorProfiler.datacollector://microsoft/CodeCoverage/2.0"]);
            Assert.AreEqual("324f817a-7420-4e6d-b3c1-143fbed6d855", this.runEventHandler.Metrics["VS.TestPlatform.DataCollector.CoreClrProfiler.datacollector://microsoft/CodeCoverage/2.0"]);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void TestRunWithCodeCoverageParallel(RunnerInfo runnerInfo)
        {
            // arrange
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            // act
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies(), this.GetCodeCoverageRunSettings(4), new TestPlatformOptions { CollectMetrics = true }, this.runEventHandler);

            // assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.Attachments.Count);

            AssertCoverageResults(this.runEventHandler.Attachments);

            Assert.AreEqual("e5f256dc-7959-4dd6-8e4f-c11150ab28e0", this.runEventHandler.Metrics["VS.TestPlatform.DataCollector.CorProfiler.datacollector://microsoft/CodeCoverage/2.0"]);
            Assert.AreEqual("e5f256dc-7959-4dd6-8e4f-c11150ab28e0", this.runEventHandler.Metrics["VS.TestPlatform.DataCollector.CoreClrProfiler.datacollector://microsoft/CodeCoverage/2.0"]);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public async Task TestRunWithCodeCoverageAndAttachmentsProcessing(RunnerInfo runnerInfo)
        {
            // arrange
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Take(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Skip(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);

            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.Attachments.Count);

            // act
            await this.vstestConsoleWrapper.ProcessTestRunAttachmentsAsync(runEventHandler.Attachments, null, true, true, testRunAttachmentsProcessingEventHandler, CancellationToken.None);

            // Assert
            testRunAttachmentsProcessingEventHandler.EnsureSuccess();
            Assert.AreEqual(1, this.testRunAttachmentsProcessingEventHandler.Attachments.Count);

            AssertCoverageResults(this.testRunAttachmentsProcessingEventHandler.Attachments);

            Assert.IsFalse(testRunAttachmentsProcessingEventHandler.CompleteArgs.IsCanceled);
            Assert.IsNull(testRunAttachmentsProcessingEventHandler.CompleteArgs.Error);

            for (int i = 0; i < testRunAttachmentsProcessingEventHandler.ProgressArgs.Count; i++)
            {
                VisualStudio.TestPlatform.ObjectModel.Client.TestRunAttachmentsProcessingProgressEventArgs progressArgs = testRunAttachmentsProcessingEventHandler.ProgressArgs[i];
                Assert.AreEqual(1, progressArgs.CurrentAttachmentProcessorIndex);
                Assert.AreEqual(1, progressArgs.CurrentAttachmentProcessorUris.Count);
                Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", progressArgs.CurrentAttachmentProcessorUris.First().AbsoluteUri);
                Assert.AreEqual(1, progressArgs.AttachmentProcessorsCount);
                if (testRunAttachmentsProcessingEventHandler.ProgressArgs.Count == 2)
                {
                    Assert.AreEqual(i == 0 ? 50 : 100, progressArgs.CurrentAttachmentProcessorProgress);
                }
            }

            Assert.AreEqual("Completed", testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.AttachmentsProcessingState]);
            Assert.AreEqual(2L, testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.NumberOfAttachmentsSentForProcessing]);
            Assert.AreEqual(1L, testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.NumberOfAttachmentsAfterProcessing]);
            Assert.IsTrue(testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForAttachmentsProcessing));

            Assert.IsTrue(File.Exists(runEventHandler.Attachments.First().Attachments.First().Uri.LocalPath));
            Assert.IsFalse(File.Exists(runEventHandler.Attachments.Last().Attachments.First().Uri.LocalPath));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public async Task TestRunWithCodeCoverageAndAttachmentsProcessingNoMetrics(RunnerInfo runnerInfo)
        {
            // arrange
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Take(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Skip(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);

            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.Attachments.Count);

            // act
            await this.vstestConsoleWrapper.ProcessTestRunAttachmentsAsync(runEventHandler.Attachments, null, true, false, testRunAttachmentsProcessingEventHandler, CancellationToken.None);

            // Assert
            testRunAttachmentsProcessingEventHandler.EnsureSuccess();
            Assert.AreEqual(1, this.testRunAttachmentsProcessingEventHandler.Attachments.Count);

            AssertCoverageResults(this.testRunAttachmentsProcessingEventHandler.Attachments);

            Assert.IsFalse(testRunAttachmentsProcessingEventHandler.CompleteArgs.IsCanceled);
            Assert.IsNull(testRunAttachmentsProcessingEventHandler.CompleteArgs.Error);

            for (int i = 0; i < testRunAttachmentsProcessingEventHandler.ProgressArgs.Count; i++)
            {
                VisualStudio.TestPlatform.ObjectModel.Client.TestRunAttachmentsProcessingProgressEventArgs progressArgs = testRunAttachmentsProcessingEventHandler.ProgressArgs[i];
                Assert.AreEqual(1, progressArgs.CurrentAttachmentProcessorIndex);
                Assert.AreEqual(1, progressArgs.CurrentAttachmentProcessorUris.Count);
                Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", progressArgs.CurrentAttachmentProcessorUris.First().AbsoluteUri);
                Assert.AreEqual(1, progressArgs.AttachmentProcessorsCount);
                if (testRunAttachmentsProcessingEventHandler.ProgressArgs.Count == 2)
                {
                    Assert.AreEqual(i == 0 ? 50 : 100, progressArgs.CurrentAttachmentProcessorProgress);
                }
            }

            Assert.IsTrue(testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics.IsNullOrEmpty());

            Assert.IsTrue(File.Exists(runEventHandler.Attachments.First().Attachments.First().Uri.LocalPath));
            Assert.IsFalse(File.Exists(runEventHandler.Attachments.Last().Attachments.First().Uri.LocalPath));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public async Task TestRunWithCodeCoverageAndAttachmentsProcessingModuleDuplicated(RunnerInfo runnerInfo)
        {
            // arrange
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Take(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Skip(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Skip(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);

            Assert.AreEqual(9, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(3, this.runEventHandler.Attachments.Count);

            // act
            await this.vstestConsoleWrapper.ProcessTestRunAttachmentsAsync(runEventHandler.Attachments, null, true, true, testRunAttachmentsProcessingEventHandler, CancellationToken.None);

            // Assert
            testRunAttachmentsProcessingEventHandler.EnsureSuccess();
            Assert.AreEqual(1, this.testRunAttachmentsProcessingEventHandler.Attachments.Count);

            AssertCoverageResults(this.testRunAttachmentsProcessingEventHandler.Attachments);

            Assert.IsFalse(testRunAttachmentsProcessingEventHandler.CompleteArgs.IsCanceled);
            Assert.IsNull(testRunAttachmentsProcessingEventHandler.CompleteArgs.Error);

            for (int i = 0; i < testRunAttachmentsProcessingEventHandler.ProgressArgs.Count; i++)
            {
                VisualStudio.TestPlatform.ObjectModel.Client.TestRunAttachmentsProcessingProgressEventArgs progressArgs = testRunAttachmentsProcessingEventHandler.ProgressArgs[i];
                Assert.AreEqual(1, progressArgs.CurrentAttachmentProcessorIndex);
                Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", progressArgs.CurrentAttachmentProcessorUris.First().AbsoluteUri);
                Assert.AreEqual(1, progressArgs.AttachmentProcessorsCount);

                if (testRunAttachmentsProcessingEventHandler.ProgressArgs.Count == 3)
                {
                    Assert.AreEqual(i == 0 ? 33 : i == 1 ? 66 : 100, progressArgs.CurrentAttachmentProcessorProgress);
                }
            }

            Assert.AreEqual("Completed", testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.AttachmentsProcessingState]);
            Assert.AreEqual(3L, testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.NumberOfAttachmentsSentForProcessing]);
            Assert.AreEqual(1L, testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.NumberOfAttachmentsAfterProcessing]);
            Assert.IsTrue(testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForAttachmentsProcessing));

            Assert.IsTrue(File.Exists(runEventHandler.Attachments.First().Attachments.First().Uri.LocalPath));
            Assert.IsFalse(File.Exists(runEventHandler.Attachments.Last().Attachments.First().Uri.LocalPath));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public async Task EndSessionShouldEnsureVstestConsoleProcessDies(RunnerInfo runnerInfo)
        {
            var numOfProcesses = Process.GetProcessesByName("vstest.console").Length;

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Take(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Skip(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);

            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.Attachments.Count);

            await this.vstestConsoleWrapper.ProcessTestRunAttachmentsAsync(runEventHandler.Attachments, null, true, true, testRunAttachmentsProcessingEventHandler, CancellationToken.None);

            // act
            this.vstestConsoleWrapper?.EndSession();

            // Assert
            Assert.AreEqual(numOfProcesses, Process.GetProcessesByName("vstest.console").Length);

            this.vstestConsoleWrapper = null;
        }

        private IList<string> GetTestAssemblies()
        {
            return GetProjects().Select(p => this.GetAssetFullPath(p)).ToList();
        }

        private IList<string> GetProjects()
        {
            return new List<string> { "SimpleTestProject.dll", "SimpleTestProject2.dll" };
        }

        /// <summary>
        /// Default RunSettings
        /// </summary>
        /// <returns></returns>
        private string GetCodeCoverageRunSettings(int cpuCount, bool useClrIeInstrumentationEngine = false)
        {
            string runSettingsXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                            <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                            <TestAdaptersPaths>{this.GetNetStandardAdapterPath()}</TestAdaptersPaths>
                                            <MaxCpuCount>{cpuCount}</MaxCpuCount>
                                        </RunConfiguration>
                                        <DataCollectionRunSettings>
                                            <DataCollectors>
                                                <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
                                                    <Configuration>
                                                      <CLRIEInstrumentationNetCore>{useClrIeInstrumentationEngine}</CLRIEInstrumentationNetCore>
                                                      <CLRIEInstrumentationNetFramework>{useClrIeInstrumentationEngine}</CLRIEInstrumentationNetFramework>
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

        private void AssertCoverageResults(IList<AttachmentSet> attachments)
        {
            if (attachments.Count == 1)
            {
                var xmlCoverage = this.GetXmlCoverage(attachments.First().Attachments.First().Uri.LocalPath);

                foreach (var project in this.GetProjects())
                {
                    var moduleNode = this.GetModuleNode(xmlCoverage.DocumentElement, project.ToLower());
                    this.AssertCoverage(moduleNode, CodeCoverageAcceptanceTestBase.ExpectedMinimalModuleCoverage);
                }
            }
        }
    }
}
