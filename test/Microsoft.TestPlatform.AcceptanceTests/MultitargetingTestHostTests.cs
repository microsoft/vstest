﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using VisualStudio.TestTools.UnitTesting;

using System;

using static AcceptanceTestBase;

[TestClass]
public class MultitargetingTestHostTests : AcceptanceTestBase
{
    [TestMethod]
    [TestCategory("Windows-Review")]
    // the underlying test is using xUnit to avoid AppDomain enhancements in MSTest that make this pass even without multitargetting
    // xUnit supports net452 onwards, so that is why this starts at net452, I also don't test all framework versions
    [NetCoreRunner(Netfx45248)]
    [NetFrameworkRunner(Netfx45248)]
    public void RunningTestWithAFailingDebugAssertDoesNotCrashTheHostingProcess(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var resultsDir = GetResultsDirectory();

        var assemblyPath = BuildMultipleAssemblyPath("MultitargetedNetFrameworkProject.dll").Trim('\"');
        var arguments = PrepareArguments(assemblyPath, null, null, FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
        InvokeVsTest(arguments);

        ValidateSummaryStatus(passedTestsCount: 1, failedTestsCount: 0, 0);
        TryRemoveDirectory(resultsDir);
    }
}