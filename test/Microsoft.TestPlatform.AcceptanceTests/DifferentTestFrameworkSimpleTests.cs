// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DifferentTestFrameworkSimpleTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void ChutzpahRunAllTestExecution(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testJSFileAbsolutePath = Path.Combine(this.testEnvironment.TestAssetsPath, "test.js");
            var arguments = PrepareArguments(
                testJSFileAbsolutePath,
                this.GetTestAdapterPath(UnitTestFramework.Chutzpah),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void CPPRunAllTestExecution(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            CppRunAllTests(runnerInfo.RunnerFramework, "x86");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void CPPRunAllTestExecutionPlatformx64(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            CppRunAllTests(runnerInfo.RunnerFramework, "x64");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void WebTestRunAllTestsWithRunSettings(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var runSettingsFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".runsettings");

            //test the simulateThinkTimes setting for WebTestRunConfiguration in run settings
            var runSettingsXml = $@"<?xml version='1.0' encoding='utf-8'?>
                                <RunSettings>
                                    <WebTestRunConfiguration simulateThinkTimes='true' />
                                    <RunConfiguration>
                                        <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                    </RunConfiguration>
                                </RunSettings>";

            IntegrationTestBase.CreateRunSettingsFile(runSettingsFilePath, runSettingsXml);

            //minExecutionTimeInSeconds is set to 30 here as the web test has a request with think time equal to 30 seconds
            //therefore, the test will at least run for 30 seconds
            WebTestRunAllTests(runnerInfo.RunnerFramework, runSettingsFilePath, 30);
        }

        [TestMethod]
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

            var arguments = PrepareArguments(
                this.GetAssetFullPath("NUTestProject.dll"),
                this.GetTestAdapterPath(UnitTestFramework.NUnit),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void XUnitRunAllTestExecution(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            string testAssemblyPath = null;

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
                runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        private void CppRunAllTests(string runnerFramework, string platform)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("CPP tests not supported with .Netcore runner.");
                return;
            }

            string assemblyRelativePathFormat =
                @"microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\{0}\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            var assemblyRelativePath = platform.Equals("x64", StringComparison.OrdinalIgnoreCase)
                ? string.Format(assemblyRelativePathFormat, platform)
                : string.Format(assemblyRelativePathFormat, "");
            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            var arguments = PrepareArguments(
                assemblyAbsolutePath,
                string.Empty,
                string.Empty, this.FrameworkArgValue,
                this.testEnvironment.InIsolationValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        private void WebTestRunAllTests(string runnerFramework, string runSettingsFilePath=null, int minExecutionTimeInSeconds=0)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("WebTests tests not supported with .Netcore runner.");
                return;
            }

            string assemblyRelativePath =
                @"microsoft.testplatform.qtools.assets\2.0.0\contentFiles\any\any\WebTestAssets\WebTest1.webtest";

            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            var arguments = PrepareArguments(
                assemblyAbsolutePath,
                string.Empty,
                runSettingsFilePath, this.FrameworkArgValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0, minExecutionTimeInSeconds);
        }

        private void CodedWebTestRunAllTests(string runnerFramework)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("WebTests tests not supported with .Netcore runner.");
                return;
            }

            string assemblyRelativePath =
                @"microsoft.testplatform.qtools.assets\2.0.0\contentFiles\any\any\WebTestAssets\BingWebTest.dll";
            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            var arguments = PrepareArguments(
                assemblyAbsolutePath,
                string.Empty,
                string.Empty, this.FrameworkArgValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }
    }
}
