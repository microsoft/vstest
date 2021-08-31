﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FrameworkTests : AcceptanceTestBase
    {

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void FrameworkArgumentShouldWork(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " ", $"/Framework:{this.FrameworkArgValue}");

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void FrameworkShortNameArgumentShouldWork(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " ", $"/Framework:{this.testEnvironment.TargetFramework}");

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        // framework runner not available on Linux
        [TestCategory("Windows-Review")]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void OnWrongFrameworkPassedTestRunShouldNotRun(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: resultsDir);
            if (runnerInfo.TargetFramework.Contains("netcore"))
            {
                arguments = string.Concat(arguments, " ", "/Framework:Framework45");
            }
            else
            {
                arguments = string.Concat(arguments, " ", "/Framework:FrameworkCore10");
            }
            this.InvokeVsTest(arguments);

            if (runnerInfo.TargetFramework.Contains("netcore"))
            {
                this.StdOutputContains("No test is available");
            }
            else
            {
                this.StdErrorContains("Test Run Aborted.");
            }

            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSpecificTestsShouldWorkWithFrameworkInCompatibleWarning(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " ", "/tests:PassingTest");
            arguments = string.Concat(arguments, " ", "/Framework:Framework40");

            this.InvokeVsTest(arguments);

            if (runnerInfo.TargetFramework.Contains("netcore"))
            {
                this.StdOutputContains("No test is available");
            }
            else
            {
                this.StdOutputContains("Following DLL(s) do not match current settings, which are .NETFramework,Version=v4.0 framework and X86 platform.");
                this.ValidateSummaryStatus(1, 0, 0);
            }

            TryRemoveDirectory(resultsDir);
        }
    }
}