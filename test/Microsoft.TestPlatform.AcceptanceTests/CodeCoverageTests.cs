// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#if NET451
    using VisualStudio.Coverage.Analysis;
#endif

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
        private readonly string resultsDirectory;

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

            var coverageDocument = this.RunCodeCoverage(actualCoverageFile);
            if (testParameters.CheckSkippedMethods)
            {
                this.AssertSkippedMethod(coverageDocument);
            }

            // Microsoft.VisualStudio.Coverage.Analysis assembly not available for .NET Core.
#if NET451
            var coverageDs = this.CreateCoverageData(actualCoverageFile, testParameters.AssemblyName);
            this.ValidateCoverageData(coverageDs, testParameters.AssemblyName);
#endif
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

        private XmlDocument RunCodeCoverage(string coverageResult)
        {
            var codeCoveragePath = Path.Combine(
                IntegrationTestEnvironment.TestPlatformRootDirectory,
                @"artifacts\Debug\Intellitrace\Team Tools\Dynamic Code Coverage Tools");

            var codeCoverageExe = Path.Combine(
                codeCoveragePath,
                "CodeCoverage.exe");

            string xmlResult = Path.Combine(this.resultsDirectory, "result.xml");
            if (File.Exists(xmlResult))
            {
                File.Delete(xmlResult);
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = codeCoverageExe;
                process.StartInfo.WorkingDirectory = codeCoveragePath;
                process.StartInfo.Arguments = $"analyze /include_skipped_functions /include_skipped_modules /output:\"{xmlResult}\" \"{coverageResult}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;

                Console.WriteLine($"Starting {codeCoverageExe}");
                var watch = new Stopwatch();

                watch.Start();
                process.Start();

                process.WaitForExit();

                watch.Stop();
                Console.WriteLine($"Total execution time: {watch.Elapsed.Duration()}");

                Assert.IsTrue(0 == process.ExitCode, "Code Coverage analyze failed: " + process.StandardOutput.ReadToEnd());
            }

            XmlDocument coverage = new XmlDocument();
            coverage.Load(xmlResult);

            return coverage;
        }

        private void AssertSkippedMethod(XmlDocument document)
        {
            var module = document.DocumentElement.SelectSingleNode("//module[@name='codecoveragetest.dll']");
            Assert.IsNotNull(module);

            var coverage = double.Parse(module.Attributes["block_coverage"].Value);
            Assert.IsTrue(coverage > 40.0);

            var testSignFunction = module.SelectSingleNode("//skipped_function[@name='TestSign()']");
            Assert.IsNotNull(testSignFunction);
            Assert.AreEqual("name_excluded", testSignFunction.Attributes["reason"].Value);

            var testAbsFunction = module.SelectSingleNode("//function[@name='TestAbs()']");
            Assert.IsNotNull(testAbsFunction);
        }

#if NET451
        private CoverageDS CreateCoverageData(string coverageFile, string assemblyName)
        {
            using (var converageInfo = CoverageInfo.CreateFromFile(coverageFile))
            {
                return converageInfo.BuildDataSet();
            }
        }

        private void ValidateCoverageData(CoverageDS coverageDs, string assemblyName)
        {
            AssertModuleCoverageCollected(coverageDs, assemblyName);
            AssertSourceFileName(coverageDs);
        }

        private static void AssertSourceFileName(CoverageDS coverageDS)
        {
            var sourceFileNames = from sourceFilePath in coverageDS.GetSourceFiles()
                select Path.GetFileName(sourceFilePath);
            var expectedFileName = "UnitTest1.cs";
            CollectionAssert.Contains(
                sourceFileNames.ToArray(),
                expectedFileName,
                $"Code Coverage not collected for file: {expectedFileName}");
        }

        private void AssertModuleCoverageCollected(CoverageDS coverageDS, string assemblyName)
        {
            var moduleFound = false;
            for (int i = 0; i < coverageDS.Module.Count; i++)
            {
                var module = coverageDS.Module[i];
                if (module.ModuleName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    moduleFound = true;
                    break;
                }
            }

            Assert.IsTrue(moduleFound, $"Code coverage not collected for module: {assemblyName}");
        }
#endif

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

        private bool SkipIfRuningInCI(string message)
        {
            // Setting Console.ForegroundColor to newColor which will be used to determine whether
            // test command output is redirecting to file or writing to console.
            // If command output is redirecting to file, then Console.ForegroundColor can't be modified.
            // So that tests which assert Console.ForegroundColor should not run.
            var previousColor = Console.ForegroundColor;
            var newColor = previousColor == ConsoleColor.Gray
                ? ConsoleColor.Black
                : ConsoleColor.Blue;
            Console.ForegroundColor = newColor;
            if (Console.ForegroundColor != newColor)
            {
                Console.ForegroundColor = previousColor;
                Assert.Inconclusive(message);
            }

            Console.ForegroundColor = previousColor;

            return false;
        }
    }
}