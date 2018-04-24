// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
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

        public CodeCoverageTests()
        {
            this.resultsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void CollectCodeCoverage(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            if (runnerInfo.RunnerFramework.Equals(IntegrationTestBase.DesktopRunnerFramework))
            {
                Assert.Inconclusive("Skip CollectCodeCoverage test for Desktop runner.");
            }
            var assemblyPaths = this.GetAssetFullPath("SimpleTestProject.dll");
            string runSettings = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory,
                @"scripts\vstest.runsettings");

            string traceDataCollectorDir = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory,
                $@"src\DataCollectors\TraceDataCollector\bin\{
                        IntegrationTestEnvironment.BuildConfiguration
                    }\netstandard2.0");

            string diagFileName = Path.Combine(this.resultsDirectory, "diaglog.txt");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runSettings,
                this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDirectory}", $" /Diag:{diagFileName}",
                $" /TestAdapterPath:{traceDataCollectorDir}", " /Collect:\"Code Coverage\"");
            var trxFilePath = Path.Combine(this.resultsDirectory, Guid.NewGuid() + ".trx");
            arguments = string.Concat(arguments, " /logger:trx;logfilename=" + trxFilePath);

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);

            var actualCoverageFile = CodeCoverageTests.GetCoverageFileNameFromTrx(trxFilePath, resultsDirectory);
            Console.WriteLine($@"Coverage file: {actualCoverageFile}  Results directory: {resultsDirectory} trxfile: {trxFilePath}");
            Assert.IsTrue(File.Exists(actualCoverageFile), "Coverage file not found: {0}", actualCoverageFile);

            this.ValidateCoverageFile(actualCoverageFile);

            Directory.Delete(this.resultsDirectory, true);
        }


        private void ValidateCoverageFile(string coverageFile)
        {
#if NET451
            using (var converageInfo = CoverageInfo.CreateFromFile(coverageFile))
            {
                CoverageDS coverageDS = converageInfo.BuildDataSet();

                Assert.AreEqual(2, coverageDS.GetSourceFiles().Count);
                Assert.AreEqual("UnitTest1.cs", Path.GetFileName(coverageDS.GetSourceFiles()[1]));
            }
#endif
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
                    "runDeploymentRoot attatribute not found in trx file:{0}", trxFilePath);
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