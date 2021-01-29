﻿// Copyright (c) Microsoft Corporation. All rights reserved.	
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class CodeCoverageAcceptanceTestBase : AcceptanceTestBase
    {
        /*
         * Below value is just safe coverage result for which all tests are passing.
         * Inspecting this value gives us confidence that there is no big drop in overall coverage.
         */
        protected const double ExpectedMinimalModuleCoverage = 30.0;

        protected string GetNetStandardAdapterPath()
        {
            return Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "artifacts", IntegrationTestEnvironment.BuildConfiguration, "Microsoft.CodeCoverage");
        }

        protected string GetNetFrameworkAdapterPath()
        {
            return Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "artifacts", IntegrationTestEnvironment.BuildConfiguration, "net451", "win7-x64", "Extensions");
        }

        protected string GetCodeCoverageExePath()
        {
            return Path.Combine(this.GetNetStandardAdapterPath(), "CodeCoverage", "CodeCoverage.exe");
        }

        protected XmlNode GetModuleNode(XmlNode node, string name)
        {
            return this.GetNode(node, "module", name);
        }

        protected XmlNode GetNode(XmlNode node, string type, string name)
        {
            return node.SelectSingleNode($"//{type}[@name='{name}']");
        }

        protected XmlDocument GetXmlCoverage(string coverageResult)
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

        protected void AssertCoverage(XmlNode node, double expectedCoverage)
        {
            var coverage = double.Parse(node.Attributes["block_coverage"].Value);
            Console.WriteLine($"Checking coverage for {node.Name} {node.Attributes["name"].Value}. Expected at least: {expectedCoverage}. Result: {coverage}");
            Assert.IsTrue(coverage > expectedCoverage, $"Coverage check failed for {node.Name} {node.Attributes["name"].Value}. Expected at least: {expectedCoverage}. Found: {coverage}");
        }

        protected static string GetCoverageFileNameFromTrx(string trxFilePath, string resultsDirectory)
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
