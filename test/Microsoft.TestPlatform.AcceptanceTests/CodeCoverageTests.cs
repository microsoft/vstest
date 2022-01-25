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
            Custom = 2,
            XmlOutput = 3,
            CoberturaOutput = 4
        }

        public string AssemblyName { get; set; }

        public string TargetPlatform { get; set; }

        public SettingsType RunSettingsType { get; set; }

        public string RunSettingsPath { get; set; }

        public int ExpectedPassedTests { get; set; }

        public int ExpectedSkippedTests { get; set; }

        public int ExpectedFailedTests { get; set; }

        public bool CheckSkipped { get; set; }
    }

    [TestClass]
    //Code coverage only supported on windows (based on the message in output)
    [TestCategory("Windows-Review")]
    public class CodeCoverageTests : CodeCoverageAcceptanceTestBase
    {
        private readonly string resultsDirectory;

        public CodeCoverageTests()
        {
            resultsDirectory = GetResultsDirectory();
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

            CollectCodeCoverage(runnerInfo, parameters);
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

            CollectCodeCoverage(runnerInfo, parameters);
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

            CollectCodeCoverage(runnerInfo, parameters);
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

            CollectCodeCoverage(runnerInfo, parameters);
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
                    @"scripts", "vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.Custom,
                ExpectedPassedTests = 3,
                ExpectedSkippedTests = 0,
                ExpectedFailedTests = 0,
                CheckSkipped = true
            };

            CollectCodeCoverage(runnerInfo, parameters);
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
                    @"scripts", "vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.Custom,
                ExpectedPassedTests = 3,
                ExpectedSkippedTests = 0,
                ExpectedFailedTests = 0,
                CheckSkipped = true
            };

            CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageSpecifyOutputFormatXml(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "SimpleTestProject.dll",
                TargetPlatform = "x64",
                RunSettingsPath = string.Empty,
                RunSettingsType = TestParameters.SettingsType.XmlOutput,
                ExpectedPassedTests = 1,
                ExpectedSkippedTests = 1,
                ExpectedFailedTests = 1
            };

            CollectCodeCoverage(runnerInfo, parameters);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void CollectCodeCoverageSpecifyOutputFormatCoberturaOverrideRunSettingsConfiguration(RunnerInfo runnerInfo)
        {
            var parameters = new TestParameters()
            {
                AssemblyName = "SimpleTestProject.dll",
                TargetPlatform = "x64",
                RunSettingsPath = Path.Combine(
                    IntegrationTestEnvironment.TestPlatformRootDirectory,
                    @"scripts", "vstest-codecoverage2.runsettings"),
                RunSettingsType = TestParameters.SettingsType.CoberturaOutput,
                ExpectedPassedTests = 1,
                ExpectedSkippedTests = 1,
                ExpectedFailedTests = 1
            };

            CollectCodeCoverage(runnerInfo, parameters);
        }

        private void CollectCodeCoverage(RunnerInfo runnerInfo, TestParameters testParameters)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);

            var arguments = CreateArguments(runnerInfo, testParameters, out var trxFilePath);

            InvokeVsTest(arguments);

            ValidateSummaryStatus(
                testParameters.ExpectedPassedTests,
                testParameters.ExpectedSkippedTests,
                testParameters.ExpectedFailedTests);

            var actualCoverageFile = GetCoverageFileNameFromTrx(trxFilePath, resultsDirectory);
            Console.WriteLine($@"Coverage file: {actualCoverageFile}  Results directory: {resultsDirectory} trxfile: {trxFilePath}");
            Assert.IsTrue(File.Exists(actualCoverageFile), "Coverage file not found: {0}", actualCoverageFile);

            if (testParameters.RunSettingsType == TestParameters.SettingsType.XmlOutput)
            {
                Assert.IsTrue(actualCoverageFile.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase));
            }
            else if (testParameters.RunSettingsType == TestParameters.SettingsType.CoberturaOutput)
            {
                Assert.IsTrue(actualCoverageFile.EndsWith(".cobertura.xml", StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                Assert.IsTrue(actualCoverageFile.EndsWith(".coverage", StringComparison.InvariantCultureIgnoreCase));
            }

            var coverageDocument = GetXmlCoverage(actualCoverageFile);
            if (testParameters.CheckSkipped)
            {
                AssertSkippedMethod(coverageDocument);
            }

            ValidateCoverageData(coverageDocument, testParameters.AssemblyName, testParameters.RunSettingsType != TestParameters.SettingsType.CoberturaOutput);

            Directory.Delete(resultsDirectory, true);
        }

        private string CreateArguments(
            RunnerInfo runnerInfo,
            TestParameters testParameters,
            out string trxFilePath)
        {
            var assemblyPaths = GetAssetFullPath(testParameters.AssemblyName);

            string traceDataCollectorDir = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory,
                "artifacts", IntegrationTestEnvironment.BuildConfiguration, "Microsoft.CodeCoverage");

            string diagFileName = Path.Combine(resultsDirectory, "diaglog.txt");
            var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty,
                FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory);
            arguments = string.Concat(arguments, $" /Diag:{diagFileName}",
                $" /TestAdapterPath:{traceDataCollectorDir}");
            arguments = string.Concat(arguments, $" /Platform:{testParameters.TargetPlatform}");

            trxFilePath = Path.Combine(resultsDirectory, Guid.NewGuid() + ".trx");
            arguments = string.Concat(arguments, " /logger:trx;logfilename=" + trxFilePath);

            var defaultRunSettingsPath = Path.Combine(
                IntegrationTestEnvironment.TestPlatformRootDirectory,
                @"scripts", "vstest-codecoverage.runsettings");

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
                case TestParameters.SettingsType.XmlOutput:
                    runSettings = $" /collect:\"Code Coverage;Format=Xml\"";
                    if (!string.IsNullOrWhiteSpace(testParameters.RunSettingsPath))
                    {
                        runSettings += $" /settings:{testParameters.RunSettingsPath}";
                    }
                    break;
                case TestParameters.SettingsType.CoberturaOutput:
                    runSettings = $" /collect:\"Code Coverage;Format=Cobertura\"";
                    if (!string.IsNullOrWhiteSpace(testParameters.RunSettingsPath))
                    {
                        runSettings += $" /settings:{testParameters.RunSettingsPath}";
                    }
                    break;
            }

            arguments = string.Concat(arguments, runSettings);

            return arguments;
        }

        private void AssertSkippedMethod(XmlDocument document)
        {
            var module = GetModuleNode(document.DocumentElement, "codecoveragetest.dll");
            Assert.IsNotNull(module);

            var coverage = double.Parse(module.Attributes["block_coverage"].Value);
            Assert.IsTrue(coverage > ExpectedMinimalModuleCoverage);

            var testSignFunction = GetNode(module, "skipped_function", "TestSign()");
            Assert.IsNotNull(testSignFunction);
            Assert.AreEqual("name_excluded", testSignFunction.Attributes["reason"].Value);

            var skippedTestMethod = GetNode(module, "skipped_function", "__CxxPureMSILEntry_Test()");
            Assert.IsNotNull(skippedTestMethod);
            Assert.AreEqual("name_excluded", skippedTestMethod.Attributes["reason"].Value);

            var testAbsFunction = GetNode(module, "function", "TestAbs()");
            Assert.IsNotNull(testAbsFunction);
        }

        private void ValidateCoverageData(XmlDocument document, string moduleName, bool validateSourceFileNames)
        {
            var module = GetModuleNode(document.DocumentElement, moduleName.ToLower());

            if (module == null)
            {
                module = GetModuleNode(document.DocumentElement, moduleName);
            }
            Assert.IsNotNull(module);

            AssertCoverage(module, ExpectedMinimalModuleCoverage);

            // In case of cobertura report. Cobertura report has different format.
            if (validateSourceFileNames)
            {
                AssertSourceFileName(module);
            }
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
    }
}