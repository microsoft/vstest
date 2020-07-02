// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Castle.Core.Internal;

    internal struct TestParameters
    {
        public enum SettingsType
        {
            None = 0,
            Default = 1,
            Custom = 2
        }

        public string AssemblyName { get; set; }

        public string TargetPlatform { get; set; }

        public SettingsType RunSettingsType { get; set; }

        public string RunSettingsPath { get; set; }

        public int ExpectedPassedTests { get; set; }

        public int ExpectedSkippedTests { get; set; }

        public int ExpectedFailedTests { get; set; }

        public bool CheckSkippedMethods { get; set; }
    }

    [TestClass]
    public class CodeCoverageTests : AcceptanceTestBase
    {
        /*
         * Below value is just safe coverage result for which all tests are passing.
         * Inspecting this value gives us confidence that there is no big drop in overall coverage.
         */
        private const double ExpectedMinimalModuleCoverage = 30.0;

        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private RunEventHandler runEventHandler;
        private TestRunAttachmentsProcessingEventHandler testRunAttachmentsProcessingEventHandler;

        private readonly string resultsDirectory;

        public CodeCoverageTests()
        {
            this.resultsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

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
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies(), this.GetCodeCoverageRunSettings(1), this.runEventHandler);

            // assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);

            int expectedNumberOfAttachments = testEnvironment.RunnerFramework.Equals(IntegrationTestBase.CoreRunnerFramework) &&
                testEnvironment.TargetFramework.Equals(IntegrationTestBase.CoreRunnerFramework) ? 2 : 1;
            Assert.AreEqual(expectedNumberOfAttachments, this.runEventHandler.Attachments.Count);

            AssertCoverageResults(this.runEventHandler.Attachments);
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
            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies(), this.GetCodeCoverageRunSettings(4), this.runEventHandler);

            // assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework) ? 1 : 2, this.runEventHandler.Attachments.Count);

            AssertCoverageResults(this.runEventHandler.Attachments);
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
            Assert.AreEqual(testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework) ? 1 : 2, this.testRunAttachmentsProcessingEventHandler.Attachments.Count);

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
            Assert.AreEqual(testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework) ? 1L : 2L, testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.NumberOfAttachmentsAfterProcessing]);
            Assert.IsTrue(testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForAttachmentsProcessing));

            Assert.IsTrue(File.Exists(runEventHandler.Attachments.First().Attachments.First().Uri.LocalPath));
            Assert.IsTrue(File.Exists(runEventHandler.Attachments.Last().Attachments.First().Uri.LocalPath) != testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework));
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
            Assert.AreEqual(testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework) ? 1 : 2, this.testRunAttachmentsProcessingEventHandler.Attachments.Count);

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
            Assert.IsTrue(File.Exists(runEventHandler.Attachments.Last().Attachments.First().Uri.LocalPath) != testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework));
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
            Assert.AreEqual(testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework) ? 1 : 3, this.testRunAttachmentsProcessingEventHandler.Attachments.Count);

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
            Assert.AreEqual(testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework) ? 1L : 3L, testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.NumberOfAttachmentsAfterProcessing]);
            Assert.IsTrue(testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForAttachmentsProcessing));

            Assert.IsTrue(File.Exists(runEventHandler.Attachments.First().Attachments.First().Uri.LocalPath));
            Assert.IsTrue(File.Exists(runEventHandler.Attachments.Last().Attachments.First().Uri.LocalPath) != testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework));
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public async Task TestRunWithCodeCoverageAndAttachmentsProcessingCancelled(RunnerInfo runnerInfo)
        {
            // arrange
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            this.Setup();

            if (!testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework)) return;

            this.vstestConsoleWrapper.RunTests(this.GetTestAssemblies().Take(1), this.GetCodeCoverageRunSettings(1), this.runEventHandler);
            Assert.AreEqual(3, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.Attachments.Count);

            List<AttachmentSet> attachments = Enumerable.Range(0, 1000).Select(i => this.runEventHandler.Attachments.First()).ToList();

            CancellationTokenSource cts = new CancellationTokenSource();

            Task attachmentsProcessing = this.vstestConsoleWrapper.ProcessTestRunAttachmentsAsync(attachments, null, true, true, testRunAttachmentsProcessingEventHandler, cts.Token);

            while (true)
            {
                try
                {
                    if (testRunAttachmentsProcessingEventHandler.ProgressArgs.Count >= 3)
                        break;
                }
                catch
                {
                    // ignore
                }
                await Task.Delay(100);
            }

            // act
            cts.Cancel();

            // Assert
            await attachmentsProcessing;
            testRunAttachmentsProcessingEventHandler.EnsureSuccess();

            Assert.AreEqual(1000, this.testRunAttachmentsProcessingEventHandler.Attachments.Count);

            Assert.IsTrue(testRunAttachmentsProcessingEventHandler.CompleteArgs.IsCanceled);
            Assert.IsNull(testRunAttachmentsProcessingEventHandler.CompleteArgs.Error);

            Assert.IsTrue((testEnvironment.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework) ? 3 : 0) <= testRunAttachmentsProcessingEventHandler.ProgressArgs.Count);
            for (int i = 0; i < testRunAttachmentsProcessingEventHandler.ProgressArgs.Count; i++)
            {
                VisualStudio.TestPlatform.ObjectModel.Client.TestRunAttachmentsProcessingProgressEventArgs progressArgs = testRunAttachmentsProcessingEventHandler.ProgressArgs[i];
                Assert.AreEqual(1, progressArgs.CurrentAttachmentProcessorIndex);
                Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", progressArgs.CurrentAttachmentProcessorUris.First().AbsoluteUri);
                Assert.AreEqual(1, progressArgs.AttachmentProcessorsCount);

                if (i == 0)
                {
                    Assert.AreEqual(0, progressArgs.CurrentAttachmentProcessorProgress);
                }
            }

            Assert.AreEqual("Canceled", testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.AttachmentsProcessingState]);
            Assert.AreEqual(1000L, testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.NumberOfAttachmentsSentForProcessing]);
            Assert.AreEqual(1000L, testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics[TelemetryDataConstants.NumberOfAttachmentsAfterProcessing]);
            Assert.IsTrue(testRunAttachmentsProcessingEventHandler.CompleteArgs.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForAttachmentsProcessing));

            Assert.IsTrue(File.Exists(runEventHandler.Attachments.First().Attachments.First().Uri.LocalPath));
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

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageWithCollectOptionForx86(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "SimpleTestProject.dll",
                TargetPlatform = "x86",
                RunSettingsPath = string.Empty,
                RunSettingsType = TestParameters.SettingsType.None,
                ExpectedPassedTests = 1,
                ExpectedSkippedTests = 1,
                ExpectedFailedTests = 1
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageWithCollectOptionForx64(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "SimpleTestProject.dll",
                TargetPlatform = "x64",
                RunSettingsPath = string.Empty,
                RunSettingsType = TestParameters.SettingsType.None,
                ExpectedPassedTests = 1,
                ExpectedSkippedTests = 1,
                ExpectedFailedTests = 1
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageX86WithRunSettings(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "SimpleTestProject.dll",
                TargetPlatform = "x86",
                RunSettingsPath = string.Empty,
                RunSettingsType = TestParameters.SettingsType.Default,
                ExpectedPassedTests = 1,
                ExpectedSkippedTests = 1,
                ExpectedFailedTests = 1
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageX64WithRunSettings(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "SimpleTestProject.dll",
                TargetPlatform = "x64",
                RunSettingsPath = string.Empty,
                RunSettingsType = TestParameters.SettingsType.Default,
                ExpectedPassedTests = 1,
                ExpectedSkippedTests = 1,
                ExpectedFailedTests = 1
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CodeCoverageShouldAvoidExclusionsX86(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "CodeCoverageTest.dll",
                TargetPlatform = "x86",
                RunSettingsPath = Path.Combine(
                    IntegrationTestEnvironment.TestPlatformRootDirectory,
                    @"scripts\vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.Custom,
                ExpectedPassedTests = 2,
                ExpectedSkippedTests = 0,
                ExpectedFailedTests = 0,
                CheckSkippedMethods = true
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CodeCoverageShouldAvoidExclusionsX64(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "CodeCoverageTest.dll",
                TargetPlatform = "x64",
                RunSettingsPath = Path.Combine(
                    IntegrationTestEnvironment.TestPlatformRootDirectory,
                    @"scripts\vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.Custom,
                ExpectedPassedTests = 2,
                ExpectedSkippedTests = 0,
                ExpectedFailedTests = 0,
                CheckSkippedMethods = true
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
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
        private string GetCodeCoverageRunSettings(int cpuCount)
        {
            string runSettingsXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                            <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                            <TestAdaptersPaths>{GetCodeCoveragePath()}</TestAdaptersPaths>
                                            <MaxCpuCount>{cpuCount}</MaxCpuCount>
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

        private void AssertCoverageResults(IList<AttachmentSet> attachments)
        {
            if (attachments.Count == 1)
            {
                var xmlCoverage = GetXmlCoverage(attachments.First().Attachments.First().Uri.LocalPath);

                foreach (var project in GetProjects())
                {
                    var moduleNode = GetModuleNode(xmlCoverage.DocumentElement, project.ToLower());
                    AssertCoverage(moduleNode, ExpectedMinimalModuleCoverage);
                }
            }
        }

        private XmlDocument GetXmlCoverage(string coverageResult)
        {
            var codeCoverageExe = this.GetCodeCoverageExePath();
            var output = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");

            var watch = new Stopwatch();

            Console.WriteLine($"Starting {codeCoverageExe}");
            watch.Start();
            var analyze = Process.Start(new ProcessStartInfo
            {
                FileName = codeCoverageExe,
                Arguments = $"analyze /include_skipped_functions /include_skipped_modules /output:\"{output}\" \"{coverageResult}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });

            string analysisOutput = analyze.StandardOutput.ReadToEnd();

            analyze.WaitForExit();
            watch.Stop();
            Console.WriteLine($"Total execution time: {watch.Elapsed.Duration()}");

            Assert.IsTrue(0 == analyze.ExitCode, $"Code Coverage analyze failed: {analysisOutput}");

            XmlDocument coverage = new XmlDocument();
            coverage.Load(output);
            return coverage;
        }

        private string GetCodeCoveragePath()
        {
            return Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "artifacts", IntegrationTestEnvironment.BuildConfiguration, "Microsoft.CodeCoverage");
        }

        private string GetCodeCoverageExePath()
        {
            return Path.Combine(GetCodeCoveragePath(), "CodeCoverage", "CodeCoverage.exe");
        }

        private XmlNode GetModuleNode(XmlNode node, string name)
        {
            return GetNode(node, "module", name);
        }

        private XmlNode GetNode(XmlNode node, string type, string name)
        {
            return node.SelectSingleNode($"//{type}[@name='{name}']");
        }

        private void AssertCoverage(XmlNode node, double expectedCoverage)
        {
            var coverage = double.Parse(node.Attributes["block_coverage"].Value);
            Console.WriteLine($"Checking coverage for {node.Name} {node.Attributes["name"].Value}. Expected at least: {expectedCoverage}. Result: {coverage}");
            Assert.IsTrue(coverage > expectedCoverage, $"Coverage check failed for {node.Name} {node.Attributes["name"].Value}. Expected at least: {expectedCoverage}. Found: {coverage}");
        }

        private void CollectCodeCoverage(RunnerInfo runnerInfo, TestParameters testParameters)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = this.CreateArguments(runnerInfo, testParameters, out var trxFilePath);

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(
                testParameters.ExpectedPassedTests,
                testParameters.ExpectedSkippedTests,
                testParameters.ExpectedFailedTests);

            var actualCoverageFile = CodeCoverageTests.GetCoverageFileNameFromTrx(trxFilePath, resultsDirectory);
            Console.WriteLine($@"Coverage file: {actualCoverageFile}  Results directory: {resultsDirectory} trxfile: {trxFilePath}");
            Assert.IsTrue(File.Exists(actualCoverageFile), "Coverage file not found: {0}", actualCoverageFile);

            var coverageDocument = this.GetXmlCoverage(actualCoverageFile);
            if (testParameters.CheckSkippedMethods)
            {
                this.AssertSkippedMethod(coverageDocument);
            }

            this.ValidateCoverageData(coverageDocument);

            Directory.Delete(this.resultsDirectory, true);
        }

        private string CreateArguments(
            RunnerInfo runnerInfo,
            TestParameters testParameters,
            out string trxFilePath)
        {
            var assemblyPaths = this.GetAssetFullPath(testParameters.AssemblyName);

            string traceDataCollectorDir = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory,
                $@"src\DataCollectors\TraceDataCollector\bin\{IntegrationTestEnvironment.BuildConfiguration}\netstandard2.0");

            string diagFileName = Path.Combine(this.resultsDirectory, "diaglog.txt");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty,
                this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDirectory}", $" /Diag:{diagFileName}",
                $" /TestAdapterPath:{traceDataCollectorDir}");
            arguments = string.Concat(arguments, $" /Platform:{testParameters.TargetPlatform}");

            trxFilePath = Path.Combine(this.resultsDirectory, Guid.NewGuid() + ".trx");
            arguments = string.Concat(arguments, " /logger:trx;logfilename=" + trxFilePath);

            var defaultRunSettingsPath = Path.Combine(
                IntegrationTestEnvironment.TestPlatformRootDirectory,
                @"scripts\vstest-codecoverage.runsettings");

            var runSettings = string.Empty;
            switch (testParameters.RunSettingsType)
            {
                case TestParameters.SettingsType.None:
                    runSettings = $" /collect:\"Code Coverage\"";
                    break;
                case TestParameters.SettingsType.Default:
                    runSettings = $" /settings:{defaultRunSettingsPath}";
                    break;
                case TestParameters.SettingsType.Custom:
                    runSettings = $" /settings:{testParameters.RunSettingsPath}";
                    break;
            }

            arguments = string.Concat(arguments, runSettings);

            return arguments;
        }

        private void AssertSkippedMethod(XmlDocument document)
        {
            var module = this.GetModuleNode(document.DocumentElement, "codecoveragetest.dll");
            Assert.IsNotNull(module);

            var coverage = double.Parse(module.Attributes["block_coverage"].Value);
            Assert.IsTrue(coverage > ExpectedMinimalModuleCoverage);

            var testSignFunction = this.GetNode(module, "skipped_function", "TestSign()");
            Assert.IsNotNull(testSignFunction);
            Assert.AreEqual("name_excluded", testSignFunction.Attributes["reason"].Value);

            var testAbsFunction = this.GetNode(module, "function", "TestAbs()");
            Assert.IsNotNull(testAbsFunction);
        }

        private void ValidateCoverageData(XmlDocument document)
        {
            var module = this.GetModuleNode(document.DocumentElement, "codecoveragetest.dll");
            Assert.IsNotNull(module);

            this.AssertCoverage(module, ExpectedMinimalModuleCoverage);
            this.AssertSourceFileName(module);
        }

        private void AssertSourceFileName(XmlNode module)
        {
            const string ExpectedFileName = "UnitTest1.cs";

            var found = false;
            var sourcesNode = module.SelectSingleNode("./source_files");
            foreach (XmlNode node in sourcesNode.ChildNodes)
            {
                if (node.Attributes["path"].Value.Contains(ExpectedFileName))
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found);
        }

        private static string GetCoverageFileNameFromTrx(string trxFilePath, string resultsDirectory)
        {
            Assert.IsTrue(File.Exists(trxFilePath), "Trx file not found: {0}", trxFilePath);
            XmlDocument doc = new XmlDocument();
            using (var trxStream = new FileStream(trxFilePath, FileMode.Open, FileAccess.Read))
            {
                doc.Load(trxStream);
                var deploymentElements = doc.GetElementsByTagName("Deployment");
                Assert.IsTrue(deploymentElements.Count == 1,
                    "None or more than one Deployment tags found in trx file:{0}", trxFilePath);
                var deploymentDir = deploymentElements[0].Attributes.GetNamedItem("runDeploymentRoot")?.Value;
                Assert.IsTrue(string.IsNullOrEmpty(deploymentDir) == false,
                    "runDeploymentRoot attribute not found in trx file:{0}", trxFilePath);
                var collectors = doc.GetElementsByTagName("Collector");

                string fileName = string.Empty;
                for (int i = 0; i < collectors.Count; i++)
                {
                    if (string.Equals(collectors[i].Attributes.GetNamedItem("collectorDisplayName").Value,
                        "Code Coverage", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = collectors[i].FirstChild?.FirstChild?.FirstChild?.Attributes.GetNamedItem("href")
                            ?.Value;
                    }
                }

                Assert.IsTrue(string.IsNullOrEmpty(fileName) == false, "Coverage file name not found in trx file: {0}",
                    trxFilePath);
                return Path.Combine(resultsDirectory, deploymentDir, "In", fileName);
            }
        }
    }
}