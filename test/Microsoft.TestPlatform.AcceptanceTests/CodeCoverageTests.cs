// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using global::TestPlatform.TestUtilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CodeCoverageTests : AcceptanceTestBase
    {
        public  CodeCoverageTests()
        {
            this.testEnvironment.portableRunner = true;
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void EnableCodeCoverageWithArguments(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            if (runnerInfo.RunnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Code coverage not supported for .NET core runner");
                return;
            }
            var projectName = "SimpleTestProject";
            var assemblyPath = this.GetAssetFullPath(projectName + ".dll");

            var arguments = string.Concat(assemblyPath, " /EnableCodeCoverage");
            var trxFilePath = Path.GetTempFileName();
            var resultsDirectory = Path.GetTempPath();
            arguments = string.Concat(arguments, " /logger:trx;logfilename=" + trxFilePath);
            arguments = string.Concat(arguments, " /ResultsDirectory:"+ resultsDirectory);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
            var expectedCoverageFile =
                Path.Combine(Path.GetDirectoryName(assemblyPath), "..", "..", "..", projectName + ".coverage");
            var actualCoverageFile = CodeCoverageTests.GetCoverageFileNameFromTrx(trxFilePath, resultsDirectory);
            Assert.IsTrue(File.Exists(actualCoverageFile), "Coverage file not found: {0}", actualCoverageFile);
            // TODO validate coverage file content,  which required using Microsoft.VisualStudio.Coverage.Analysis lib. 
            Directory.Delete(resultsDirectory, true);
            File.Delete(trxFilePath);

            var areIdentical = File.ReadAllBytes(expectedCoverageFile)
                .SequenceEqual(File.ReadAllBytes(actualCoverageFile));
            Assert.IsTrue(areIdentical, "Expected coverage file: {0} Actual coverage:{1} file are not identical.", expectedCoverageFile, actualCoverageFile);
        }

        private static string GetCoverageFileNameFromTrx(string trxFilePath, string resultsDirectory)
        {
            Assert.IsTrue(File.Exists(trxFilePath), "Trx file not found: {0}", trxFilePath);
            XmlDocument doc = new XmlDocument();
            doc.Load(new FileStream(trxFilePath, FileMode.Open, FileAccess.Read));
            var deploymentElements = doc.GetElementsByTagName("Deployment");
            Assert.IsTrue(deploymentElements.Count == 1, "None or more than one Deployment tags found in trx file:{0}", trxFilePath);
            var deploymentDir = deploymentElements[0].Attributes.GetNamedItem("runDeploymentRoot")?.Value;
            Assert.IsTrue(string.IsNullOrEmpty(deploymentDir) == false, "runDeploymentRoot attatribute not found in trx file:{0}", trxFilePath);
            var collectors = doc.GetElementsByTagName("Collector");

            string fileName = string.Empty;
            for (int i = 0; i < collectors.Count; i++)
            {
                if (string.Equals(collectors[i].Attributes.GetNamedItem("collectorDisplayName").Value, "Code Coverage", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = collectors[i].FirstChild?.FirstChild?.FirstChild?.Attributes.GetNamedItem("href")?.Value;
                }
            }

            Assert.IsTrue(string.IsNullOrEmpty(fileName) == false, "Coverage file name not found in trx file: {0}", trxFilePath);
            return Path.Combine(resultsDirectory, deploymentDir, "In", fileName);
        }

    }
}
