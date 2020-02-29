// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#if NET451
    using VisualStudio.Coverage.Analysis;
#endif

    [TestClass]
    public class CodeCoverageTests : AcceptanceTestBase
    {
        private readonly string resultsDirectory;
        private readonly string assemblyName = "SimpleTestProject.dll";
        public CodeCoverageTests()
        {
            this.resultsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageWithCollectOptionForx86(RunnerInfo runnerInfo)
        {
            this.CollectCodeCoverage(runnerInfo, "x86", withRunsettings: false);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageWithCollectOptionForx64(RunnerInfo runnerInfo)
        {
            this.CollectCodeCoverage(runnerInfo, "x64", withRunsettings: false);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageX86WithRunSettings(RunnerInfo runnerInfo)
        {
            this.CollectCodeCoverage(runnerInfo, "x86", withRunsettings: true);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageX64WithRunSettings(RunnerInfo runnerInfo)
        {
            this.CollectCodeCoverage(runnerInfo, "x64", withRunsettings: true);
        }

        private void CollectCodeCoverage(RunnerInfo runnerInfo, string targetPlatform, bool withRunsettings)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = CreateArguments(runnerInfo, targetPlatform, withRunsettings, out var trxFilePath);

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);

            var actualCoverageFile = CodeCoverageTests.GetCoverageFileNameFromTrx(trxFilePath, resultsDirectory);
            Console.WriteLine($@"Coverage file: {actualCoverageFile}  Results directory: {resultsDirectory} trxfile: {trxFilePath}");
            Assert.IsTrue(File.Exists(actualCoverageFile), "Coverage file not found: {0}", actualCoverageFile);

            // Microsoft.VisualStudio.Coverage.Analysis assembly not available for .NET Core.
#if NET451
            this.ValidateCoverageData(actualCoverageFile);
#endif
            Directory.Delete(this.resultsDirectory, true);
        }

        private string CreateArguments(RunnerInfo runnerInfo, string targetPlatform, bool withRunsettings,
            out string trxFilePath)
        {
            var assemblyPaths = this.GetAssetFullPath(assemblyName);
            string runSettings = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory,
                @"scripts\vstest-codecoverage.runsettings");

            string traceDataCollectorDir = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory,
                $@"src\DataCollectors\TraceDataCollector\bin\{IntegrationTestEnvironment.BuildConfiguration}\netstandard2.0");

            string diagFileName = Path.Combine(this.resultsDirectory, "diaglog.txt");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty,
                this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDirectory}", $" /Diag:{diagFileName}",
                $" /TestAdapterPath:{traceDataCollectorDir}");
            arguments = string.Concat(arguments, $" /Platform:{targetPlatform}");

            trxFilePath = Path.Combine(this.resultsDirectory, Guid.NewGuid() + ".trx");
            arguments = string.Concat(arguments, " /logger:trx;logfilename=" + trxFilePath);

            if (withRunsettings)
            {
                arguments = string.Concat(arguments, $" /settings:{runSettings}");
            }
            else
            {
                // With /collect:"Code Coverage" option.
                arguments = string.Concat(arguments, $" /collect:\"Code Coverage\"");
            }

            return arguments;
        }

#if NET451
        private void ValidateCoverageData(string coverageFile)
        {
            using (var converageInfo = CoverageInfo.CreateFromFile(coverageFile))
            {
                CoverageDS coverageDs = converageInfo.BuildDataSet();
                AssertModuleCoverageCollected(coverageDs);
                AssertSourceFileName(coverageDs);
            }
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

        private void AssertModuleCoverageCollected(CoverageDS coverageDS)
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