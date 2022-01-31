// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MultitargetingTestHostTests : AcceptanceTestBase
{
    [TestMethod]
    [TestCategory("Windows-Review")]
    // the underlying test is using xUnit to avoid AppDomain enhancements in MSTest that make this pass even without multitargetting
    // xUnit supports net452 onwards, so that is why this starts at net452, I also don't test all framework versions
    [NetCoreRunner(NETFX452_48)]
    [NetFrameworkRunner(NETFX452_48)]
    public void RunningTestWithAFailingDebugAssertDoesNotCrashTheHostingProcess(RunnerInfo runnerInfo)
    {
        AcceptanceTestBase.SetTestEnvironment(_testEnvironment, runnerInfo);
        using var tempDir = new TempDirectory();

        var assemblyPath = BuildMultipleAssemblyPath("MultitargetedNetFrameworkProject.dll").Trim('\"');
        var arguments = PrepareArguments(assemblyPath, null, null, FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: tempDir.Path);
        InvokeVsTest(arguments);

        ValidateSummaryStatus(passedTestsCount: 1, failedTestsCount: 0, 0);
    }
}
