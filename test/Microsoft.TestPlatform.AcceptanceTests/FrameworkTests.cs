// Copyright (c) Microsoft Corporation. All rights reserved.
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
            SetTestEnvironment(testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " ", $"/Framework:{FrameworkArgValue}");

            InvokeVsTest(arguments);
            ValidateSummaryStatus(1, 1, 1);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void FrameworkShortNameArgumentShouldWork(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " ", $"/Framework:{testEnvironment.TargetFramework}");

            InvokeVsTest(arguments);
            ValidateSummaryStatus(1, 1, 1);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        // framework runner not available on Linux
        [TestCategory("Windows-Review")]
        [NetFullTargetFrameworkDataSource(useCoreRunner: false)]
        //[NetCoreTargetFrameworkDataSource]
        public void OnWrongFrameworkPassedTestRunShouldNotRun(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: resultsDir);
            arguments = runnerInfo.TargetFramework.Contains("netcore")
                ? string.Concat(arguments, " ", "/Framework:Framework45")
                : string.Concat(arguments, " ", "/Framework:FrameworkCore10");
            InvokeVsTest(arguments);

            if (runnerInfo.TargetFramework.Contains("netcore"))
            {
                StdOutputContains("No test is available");
            }
            else
            {
                // This test indirectly tests that we abort when incorrect framework is forced on a DLL, the failure message with the new fallback 
                // is uglier than then one before that suggests (incorrectly) to install Microsoft.NET.Test.Sdk into the project, which would work,
                // but would not solve the problem. In either cases we should improve the message later.
                StdErrorContains("Test Run Failed.");
            }

            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSpecificTestsShouldWorkWithFrameworkInCompatibleWarning(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " ", "/tests:PassingTest");
            arguments = string.Concat(arguments, " ", "/Framework:Framework40");

            InvokeVsTest(arguments);

            if (runnerInfo.TargetFramework.Contains("netcore"))
            {
                StdOutputContains("No test is available");
            }
            else
            {
                StdOutputContains("Following DLL(s) do not match current settings, which are .NETFramework,Version=v4.0 framework and X86 platform.");
                ValidateSummaryStatus(1, 0, 0);
            }

            TryRemoveDirectory(resultsDir);
        }
    }
}