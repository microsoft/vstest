// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Windows-Review")]
    public class DifferentTestFrameworkSimpleTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void ChutzpahRunAllTestExecution(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();
            var testJSFileAbsolutePath = Path.Combine(this.testEnvironment.TestAssetsPath, "test.js");
            var arguments = PrepareArguments(testJSFileAbsolutePath, this.GetTestAdapterPath(UnitTestFramework.Chutzpah), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);

            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void CPPRunAllTestExecution(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            CppRunAllTests(runnerInfo.RunnerFramework, "x86");
        }

        [TestMethod]
        [TestCategory("Windows-Review")]
        [NetFullTargetFrameworkDataSource]
        public void CPPRunAllTestExecutionPlatformx64(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            CppRunAllTests(runnerInfo.RunnerFramework, "x64");
        }

        [TestMethod]
        [TestCategory("Windows-Review")]
        [NetFullTargetFrameworkDataSource]
        public void WebTestRunAllTestsWithRunSettings(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var runSettingsFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".runsettings");

            //test the iterationCount setting for WebTestRunConfiguration in run settings
            var runSettingsXml = $@"<?xml version='1.0' encoding='utf-8'?>
                                <RunSettings>
                                    <WebTestRunConfiguration iterationCount='5' />
                                    <RunConfiguration>
                                        <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                    </RunConfiguration>
                                </RunSettings>";

            IntegrationTestBase.CreateRunSettingsFile(runSettingsFilePath, runSettingsXml);

            //minWebTestResultFileSizeInKB is set to 150 here as the web test has a iteration count set to 5
            //therefore, the test will run for 5 iterations resulting in web test result file size of at least 150 KB
            WebTestRunAllTests(runnerInfo.RunnerFramework, runSettingsFilePath, 150);
        }

        [TestMethod]
        [TestCategory("Windows-Review")]
        [NetFullTargetFrameworkDataSource]
        public void CodedWebTestRunAllTests(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            CodedWebTestRunAllTests(runnerInfo.RunnerFramework);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void NUnitRunAllTestExecution(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var resultsDir = GetResultsDirectory();
            var arguments = PrepareArguments(
                this.GetAssetFullPath("NUTestProject.dll"),
                this.GetTestAdapterPath(UnitTestFramework.NUnit),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDir);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);

            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void XUnitRunAllTestExecution(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            string testAssemblyPath;
            // Xunit >= 2.2 won't support net451, Minimum target framework it supports is net452.
            if (this.testEnvironment.TargetFramework.Equals("net451"))
            {
                testAssemblyPath = testEnvironment.GetTestAsset("XUTestProject.dll", "net46");
            }
            else
            {
                testAssemblyPath = testEnvironment.GetTestAsset("XUTestProject.dll");
            }

            var arguments = PrepareArguments(
                testAssemblyPath,
                this.GetTestAdapterPath(UnitTestFramework.XUnit),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDir);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);

            TryRemoveDirectory(resultsDir);
        }

        private void CppRunAllTests(string runnerFramework, string platform)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("CPP tests not supported with .Netcore runner.");
                return;
            }

            var resultsDir = GetResultsDirectory();
            string assemblyRelativePathFormat =
                @"microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\{0}\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            var assemblyRelativePath = platform.Equals("x64", StringComparison.OrdinalIgnoreCase)
                ? string.Format(assemblyRelativePathFormat, platform)
                : string.Format(assemblyRelativePathFormat, "");
            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            var arguments = PrepareArguments(assemblyAbsolutePath, string.Empty, string.Empty, this.FrameworkArgValue, this.testEnvironment.InIsolationValue, resultsDirectory: resultsDir);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);

            TryRemoveDirectory(resultsDir);
        }

        private void WebTestRunAllTests(string runnerFramework, string runSettingsFilePath = null, int minWebTestResultFileSizeInKB = 0)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("WebTests tests not supported with .Netcore runner.");
                return;
            }

            string assemblyRelativePath =
                @"microsoft.testplatform.qtools.assets\2.0.0\contentFiles\any\any\WebTestAssets\WebTest1.webtest";

            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            var resultsDirectory = GetResultsDirectory();
            var arguments = PrepareArguments(
                assemblyAbsolutePath,
                string.Empty,
                runSettingsFilePath, this.FrameworkArgValue, string.Empty, resultsDirectory);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);

            if (minWebTestResultFileSizeInKB > 0)
            {
                var dirInfo = new DirectoryInfo(resultsDirectory);
                var webtestResultFile = "WebTest1.webtestResult";
                var files = dirInfo.GetFiles(webtestResultFile, SearchOption.AllDirectories);
                Assert.IsTrue(files.Length > 0, $"File {webtestResultFile} not found under results directory {resultsDirectory}");

                var fileSizeInKB = files[0].Length / 1024;
                Assert.IsTrue(fileSizeInKB > minWebTestResultFileSizeInKB, $"Size of the file {webtestResultFile} is {fileSizeInKB} KB. It is not greater than {minWebTestResultFileSizeInKB} KB indicating iterationCount in run settings not honored.");
            }

            TryRemoveDirectory(resultsDirectory);
        }

        private void CodedWebTestRunAllTests(string runnerFramework)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("WebTests tests not supported with .Netcore runner.");
                return;
            }

            var resultsDir = GetResultsDirectory();
            string assemblyRelativePath =
                @"microsoft.testplatform.qtools.assets\2.0.0\contentFiles\any\any\WebTestAssets\BingWebTest.dll";
            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            var arguments = PrepareArguments(
                assemblyAbsolutePath,
                string.Empty,
                string.Empty, this.FrameworkArgValue, resultsDirectory: resultsDir);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);

            TryRemoveDirectory(resultsDir);
        }
    }
}
