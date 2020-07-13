// Copyright (c) Microsoft Corporation. All rights reserved.	
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

        protected string GetCodeCoveragePath()
        {
            return Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "artifacts", IntegrationTestEnvironment.BuildConfiguration, "Microsoft.CodeCoverage");
        }

        protected string GetCodeCoverageExePath()
        {
            return Path.Combine(this.GetCodeCoveragePath(), "CodeCoverage", "CodeCoverage.exe");
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
    }
}
