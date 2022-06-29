// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class FrameworkTests : AcceptanceTestBase
{

    [TestMethod]
    [NetFullTargetFrameworkDataSourceAttribute]
    [NetCoreTargetFrameworkDataSourceAttribute]
    public void FrameworkArgumentShouldWork(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: TempDirectory.Path);
        arguments = string.Concat(arguments, " ", $"/Framework:{FrameworkArgValue}");

        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 1, 1);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSourceAttribute]
    [NetCoreTargetFrameworkDataSourceAttribute]
    public void FrameworkShortNameArgumentShouldWork(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: TempDirectory.Path);
        arguments = string.Concat(arguments, " ", $"/Framework:{_testEnvironment.TargetFramework}");

        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 1, 1);
    }

    [TestMethod]
    // framework runner not available on Linux
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSourceAttribute(useCoreRunner: false)]
    //[NetCoreTargetFrameworkDataSourceAttribute]
    public void OnWrongFrameworkPassedTestRunShouldNotRun(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: TempDirectory.Path);
        if (runnerInfo.TargetFramework.Contains("netcore"))
        {
            arguments = string.Concat(arguments, " ", "/Framework:Framework45");
        }
        else
        {
            arguments = string.Concat(arguments, " ", "/Framework:FrameworkCore10");
        }
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
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSourceAttribute]
    [NetCoreTargetFrameworkDataSourceAttribute]
    public void RunSpecificTestsShouldWorkWithFrameworkInCompatibleWarning(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: TempDirectory.Path);
        arguments = string.Concat(arguments, " ", "/tests:PassingTest");
        arguments = string.Concat(arguments, " ", "/Framework:Framework40");

        InvokeVsTest(arguments);

        if (runnerInfo.TargetFramework.Contains("netcore"))
        {
            StdOutputContains("No test is available");
        }
        else
        {
            StdOutputContains("Following DLL(s) do not match current settings, which are .NETFramework,Version=v4.0 framework and X64 platform.");
            ValidateSummaryStatus(1, 0, 0);
        }
    }
}
