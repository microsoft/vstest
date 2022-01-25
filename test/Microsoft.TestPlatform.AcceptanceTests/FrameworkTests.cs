﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using Microsoft.TestPlatform.TestUtilities;
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
        using var tempDir = new TempDirectory();

        var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: tempDir.Path);
        arguments = string.Concat(arguments, " ", $"/Framework:{this.FrameworkArgValue}");

        this.InvokeVsTest(arguments);
        this.ValidateSummaryStatus(1, 1, 1);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void FrameworkShortNameArgumentShouldWork(RunnerInfo runnerInfo)
    {
        AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
        using var tempDir = new TempDirectory();

        var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: tempDir.Path);
        arguments = string.Concat(arguments, " ", $"/Framework:{this.testEnvironment.TargetFramework}");

        this.InvokeVsTest(arguments);
        this.ValidateSummaryStatus(1, 1, 1);
    }

    [TestMethod]
    // framework runner not available on Linux
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(useCoreRunner: false)]
    //[NetCoreTargetFrameworkDataSource]
    public void OnWrongFrameworkPassedTestRunShouldNotRun(RunnerInfo runnerInfo)
    {
        AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
        using var tempDir = new TempDirectory();

        var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: tempDir.Path);
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
            // This test indirectly tests that we abort when incorrect framework is forced on a DLL, the failure message with the new fallback
            // is uglier than then one before that suggests (incorrectly) to install Microsoft.NET.Test.Sdk into the project, which would work,
            // but would not solve the problem. In either cases we should improve the message later.
            this.StdErrorContains("Test Run Failed.");
        }
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunSpecificTestsShouldWorkWithFrameworkInCompatibleWarning(RunnerInfo runnerInfo)
    {
        AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
        using var tempDir = new TempDirectory();

        var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, string.Empty, resultsDirectory: tempDir.Path);
        arguments = string.Concat(arguments, " ", "/tests:PassingTest");
        arguments = string.Concat(arguments, " ", "/Framework:Framework40");

        InvokeVsTest(arguments);

        if (runnerInfo.TargetFramework.Contains("netcore"))
        {
            this.StdOutputContains("No test is available");
        }
        else
        {
            this.StdOutputContains("Following DLL(s) do not match current settings, which are .NETFramework,Version=v4.0 framework and X86 platform.");
            this.ValidateSummaryStatus(1, 0, 0);
        }
    }
}
