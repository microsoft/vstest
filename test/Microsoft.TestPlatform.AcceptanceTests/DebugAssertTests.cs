﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using VisualStudio.TestTools.UnitTesting;

using System;

[TestClass]
public class DebugAssertTests : AcceptanceTestBase
{
    [TestMethod]
    // this is core only, there is nothing we can do about Debug.Assert crashing the process on framework
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void RunningTestWithAFailingDebugAssertDoesNotCrashTheHostingProcess(RunnerInfo runnerInfo)
    {
        // when debugging this test in case it starts failing, be aware that the default behavior of Debug.Assert 
        // is to not crash the process when we are running in debug, and debugger is attached
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var resultsDir = GetResultsDirectory();
        var assemblyPath = BuildMultipleAssemblyPath("CrashingOnDebugAssertTestProject.dll").Trim('\"');
        var arguments = PrepareArguments(assemblyPath, null, null, FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
        InvokeVsTest(arguments);

        // this will have failed tests when our trace listener works and crash the testhost process when it does not
        // because crashing processes is what a failed Debug.Assert does by default, unless you have a debugger attached
        ValidateSummaryStatus(passedTestsCount: 4, failedTestsCount: 4, 0);
        StringAssert.Contains(StdOut, "threw exception: Microsoft.VisualStudio.TestPlatform.TestHost.DebugAssertException:");

        TryRemoveDirectory(resultsDir);
    }
}