// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Xml;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        public string TraceDataCollectorPath { get; set; }

        public string RunSettingsPath { get; set; }

        public SettingsType RunSettingsType { get; set; }

        public int ExpectedPassedTests { get; set; }

        public int ExpectedSkippedTests { get; set; }

        public int ExpectedFailedTests { get; set; }

        public bool CheckSkipped { get; set; }
    }

    [TestClass]
    public class CodeCoverageTests : CodeCoverageAcceptanceTestBase
    {
        private readonly string resultsDirectory;
        private readonly string vstestTraceDataCollector = Path.Combine(
            IntegrationTestEnvironment.TestPlatformRootDirectory,
            $@"src\DataCollectors\TraceDataCollector\bin\{IntegrationTestEnvironment.BuildConfiguration}\netstandard2.0");
        private readonly string vsTraceDataCollector = Path.Combine(
            IntegrationTestEnvironment.TestPlatformRootDirectory,
            $@"packages\microsoft.internal.testplatform.extensions\16.8.0-preview-3933530\contentFiles\any\any\Extensions"
            );

        public CodeCoverageTests()
        {
            this.resultsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
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
                TraceDataCollectorPath = vstestTraceDataCollector,
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
                TraceDataCollectorPath = vstestTraceDataCollector,
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
                TraceDataCollectorPath = vstestTraceDataCollector,
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
                TraceDataCollectorPath = vstestTraceDataCollector,
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
                TraceDataCollectorPath = vstestTraceDataCollector,
                RunSettingsPath = Path.Combine(
                    IntegrationTestEnvironment.TestPlatformRootDirectory,
                    @"scripts\vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.Custom,
                ExpectedPassedTests = 3,
                ExpectedSkippedTests = 0,
                ExpectedFailedTests = 0,
                CheckSkipped = true
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
                TraceDataCollectorPath = vstestTraceDataCollector,
                RunSettingsPath = Path.Combine(
                    IntegrationTestEnvironment.TestPlatformRootDirectory,
                    @"scripts\vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.Custom,
                ExpectedPassedTests = 3,
                ExpectedSkippedTests = 0,
                ExpectedFailedTests = 0,
                CheckSkipped = true
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CodeCoverageShouldAvoidExclusionsX86VS(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "CodeCoverageTest.dll",
                TargetPlatform = "x86",
                TraceDataCollectorPath = vsTraceDataCollector,
                RunSettingsPath = Path.Combine(
                    IntegrationTestEnvironment.TestPlatformRootDirectory,
                    @"scripts\vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.Custom,
                ExpectedPassedTests = 3,
                ExpectedSkippedTests = 0,
                ExpectedFailedTests = 0,
                CheckSkipped = true
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CodeCoverageShouldAvoidExclusionsX64VS(RunnerInfo runnerInfo)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
            else
            {
                System.Diagnostics.Debugger.Break();
            }

            var parameters = new TestParameters()
            {
                AssemblyName = "CodeCoverageTest.dll",
                TargetPlatform = "x64",
                TraceDataCollectorPath = vsTraceDataCollector,
                RunSettingsPath = Path.Combine(
                    IntegrationTestEnvironment.TestPlatformRootDirectory,
                    @"scripts\vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.Custom,
                ExpectedPassedTests = 3,
                ExpectedSkippedTests = 0,
                ExpectedFailedTests = 0,
                CheckSkipped = true
            };

            this.CollectCodeCoverage(runnerInfo, parameters);
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
            if (testParameters.CheckSkipped)
            {
                this.AssertSkippedMethod(coverageDocument);
            }

            this.ValidateCoverageData(coverageDocument, testParameters.AssemblyName);

            Directory.Delete(this.resultsDirectory, true);
        }

        private string CreateArguments(
            RunnerInfo runnerInfo,
            TestParameters testParameters,
            out string trxFilePath)
        {
            var assemblyPaths = this.GetAssetFullPath(testParameters.AssemblyName);

            string traceDataCollectorDir = testParameters.TraceDataCollectorPath;

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
            Assert.IsTrue(coverage > CodeCoverageAcceptanceTestBase.ExpectedMinimalModuleCoverage);

            var testSignFunction = this.GetNode(module, "skipped_function", "TestSign()");
            Assert.IsNotNull(testSignFunction);
            Assert.AreEqual("name_excluded", testSignFunction.Attributes["reason"].Value);

            var skippedTestMethod = this.GetNode(module, "skipped_function", "__CxxPureMSILEntry_Test()");
            Assert.IsNotNull(skippedTestMethod);
            Assert.AreEqual("name_excluded", skippedTestMethod.Attributes["reason"].Value);

            var testAbsFunction = this.GetNode(module, "function", "TestAbs()");
            Assert.IsNotNull(testAbsFunction);
        }

        private void ValidateCoverageData(XmlDocument document, string moduleName)
        {
            var module = this.GetModuleNode(document.DocumentElement, moduleName.ToLower());
            Assert.IsNotNull(module);

            this.AssertCoverage(module, CodeCoverageAcceptanceTestBase.ExpectedMinimalModuleCoverage);
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