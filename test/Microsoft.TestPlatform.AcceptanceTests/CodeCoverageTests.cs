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
        private const string StaticCodeCoverageTestSettingsContent =
            @"<?xml version='1.0' encoding='UTF-8'?>
              <TestSettings xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010' name='TestSettings1' id='bb640a13-3a47-4bc5-a7bf-00bfefc1d36e'>
                 <Description>These are default test settings for a local test run.</Description>
                 <Deployment enabled='false' />
                 <Execution>
                    <TestTypeSpecific>
                       <UnitTestRunConfig testTypeId='13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b'>
                          <AssemblyResolution>
                             <TestDirectory useLoadContext='true' />
                          </AssemblyResolution>
                       </UnitTestRunConfig>
                    </TestTypeSpecific>
                    <AgentRule name='LocalMachineDefaultRole'>
                       <DataCollectors>
                          <DataCollector uri='datacollector://microsoft/CodeCoverage/1.0' assemblyQualifiedName='Microsoft.VisualStudio.TestTools.CodeCoverage.CoveragePlugIn, Microsoft.VisualStudio.QualityTools.Plugins.CodeCoverage, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' friendlyName='Code Coverage (Visual Studio 2010)' >
                            <Configuration>
                                <CodeCoverage xmlns=''>
                                    <Regular>
                                        <CodeCoverageItem binaryFile='c:\src\vstest\test\TestAssets\MstestV1UnitTestProject\bin\Debug\net451\MstestV1UnitTestProject.dll' pdbFile='c:\src\vstest\test\TestAssets\MstestV1UnitTestProject\bin\Debug\net451\MstestV1UnitTestProject.pdb' instrumentInPlace='true' />
                                    </Regular>
                                </CodeCoverage>
                            </Configuration>
                        </DataCollector>
                       </DataCollectors>
                    </AgentRule>
                 </Execution>
                 <Properties />
              </TestSettings>";

        public  CodeCoverageTests()
        {
            this.testEnvironment.portableRunner = true;
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void EnableCodeCoverageWithArguments(RunnerInfo runnerInfo)
        {
            TestCodeCoverage(runnerInfo, " /EnableCodeCoverage /platform:x86", () => this.ValidateSummaryStatus(1, 1, 1));
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void EnableCodeCoverageWithPlatformX64(RunnerInfo runnerInfo)
        {
            TestCodeCoverage(runnerInfo, " /EnableCodeCoverage /platform:x64", () => this.ValidateSummaryStatus(1, 1, 1));
        }

        [Ignore("Static Code coverage not supported with XCopyable package. Static Code coverage need VSPerfMon.exe, Which is not ships with XCopyable package.")]
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void StaticCodeCoverage(RunnerInfo runnerInfo)
        {
            TestStaticCodeCoverage(runnerInfo, "x86");
        }

        private void TestStaticCodeCoverage(RunnerInfo runnerInfo, string platform)
        {
            var testsettingsFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".testsettings");
            File.AppendAllText(testsettingsFile, StaticCodeCoverageTestSettingsContent);
            TestCodeCoverage(runnerInfo, $" /platform:{platform} /settings:{testsettingsFile}", () => this.ValidateSummaryStatus(2, 2, 1), "MstestV1UnitTestProject");
            File.Delete(testsettingsFile);
        }

        private void TestCodeCoverage(RunnerInfo runnerInfo, string additionalArgs, Action validation, string projectName = "SimpleTestProject")
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            if (runnerInfo.RunnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Code coverage not supported for .NET core runner");
            }

            var assemblyPath = this.GetAssetFullPath(projectName + ".dll");

            var arguments = string.Concat(assemblyPath, additionalArgs);
            var trxFilePath = Path.GetTempFileName();
            var resultsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            arguments = string.Concat(arguments, " /logger:trx;logfilename=" + trxFilePath);
            arguments = string.Concat(arguments, " /ResultsDirectory:" + resultsDirectory);

            this.InvokeVsTest(arguments);
            validation();

            var actualCoverageFile = CodeCoverageTests.GetCoverageFileNameFromTrx(trxFilePath, resultsDirectory);
            Assert.IsTrue(File.Exists(actualCoverageFile), "Coverage file not found: {0}", actualCoverageFile);
            // TODO validate coverage file content,  which required using Microsoft.VisualStudio.Coverage.Analysis lib.

            Directory.Delete(resultsDirectory, true);
            File.Delete(trxFilePath);
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
                Assert.IsTrue(string.IsNullOrEmpty(fileName) == false, "Coverage file name not found in trx file: {0}", trxFilePath);
                return Path.Combine(resultsDirectory, deploymentDir, "In", fileName);
            }
        }

    }
}
