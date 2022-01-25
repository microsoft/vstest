﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using VisualStudio.TestTools.UnitTesting;

[TestClass]
[TestCategory("Windows-Review")]
public class ExecutionThreadApartmentStateTests : AcceptanceTestBase
{
    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    public void UiTestShouldPassIfApartmentStateIsSta(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var resultsDir = GetResultsDirectory();

        var assemblyPaths = BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
        var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
        arguments = string.Concat(arguments, " /testcasefilter:UITestMethod");
        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 0, 0);

        TryRemoveDirectory(resultsDir);
    }

    [TestMethod]
    [NetCoreTargetFrameworkDataSource]
    public void WarningShouldBeShownWhenValueIsStaForNetCore(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var resultsDir = GetResultsDirectory();

        var assemblyPaths =
            BuildMultipleAssemblyPath("SimpleTestProject2.dll").Trim('\"');
        var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
        arguments = string.Concat(arguments, " /testcasefilter:PassingTest2 -- RunConfiguration.ExecutionThreadApartmentState=STA");
        InvokeVsTest(arguments);
        StdOutputContains("ExecutionThreadApartmentState option not supported for framework:");
        ValidateSummaryStatus(1, 0, 0);

        TryRemoveDirectory(resultsDir);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    public void UiTestShouldFailWhenDefaultApartmentStateIsMta(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var resultsDir = GetResultsDirectory();

        var assemblyPaths =
            BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
        var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
        arguments = string.Concat(arguments, " /testcasefilter:UITestMethod -- RunConfiguration.ExecutionThreadApartmentState=MTA");
        InvokeVsTest(arguments);
        ValidateSummaryStatus(0, 1, 0);

        TryRemoveDirectory(resultsDir);
    }

    [Ignore(@"Issue with TestSessionTimeout:  https://github.com/Microsoft/vstest/issues/980")]
    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    public void CancelTestExectionShouldWorkWhenApartmentStateIsSta(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var resultsDir = GetResultsDirectory();

        var assemblyPaths =
            BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
        var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
        arguments = string.Concat(arguments, " /tests:UITestWithSleep1,UITestMethod -- RunConfiguration.ExecutionThreadApartmentState=STA RunConfiguration.TestSessionTimeout=2000");
        InvokeVsTest(arguments);
        StdOutputContains("Canceling test run: test run timeout of");
        ValidateSummaryStatus(1, 0, 0);

        TryRemoveDirectory(resultsDir);
    }
}