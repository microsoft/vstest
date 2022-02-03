// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
[TestCategory("Windows-Review")]
public class CuitTest : AcceptanceTestBase
{
    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource]
    public void CuitRunAllTests(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        CuitRunAll(runnerInfo.RunnerFramework);
    }

    private void CuitRunAll(string runnerFramework)
    {
        if (runnerFramework.StartsWith("netcoreapp"))
        {
            Assert.Inconclusive("CUIT tests are not supported with .Netcore runner.");
            return;
        }

        var assemblyAbsolutePath = _testEnvironment.GetTestAsset("CUITTestProject.dll", "net451");
        using var tempDir = new TempDirectory();
        var arguments = PrepareArguments(assemblyAbsolutePath, string.Empty, string.Empty, FrameworkArgValue, resultsDirectory: tempDir.Path);

        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 0, 0);
    }
}
