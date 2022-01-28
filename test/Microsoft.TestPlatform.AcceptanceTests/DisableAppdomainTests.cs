// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.IO;

[TestClass]
[TestCategory("Windows")]
public class DisableAppdomainTests : AcceptanceTestBase
{
    [TestMethod]
    [TestCategory("Windows")]
    [NetFullTargetFrameworkDataSource]
    public void DisableAppdomainTest(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var diableAppdomainTest1 = _testEnvironment.GetTestAsset("DisableAppdomainTest1.dll", "net451");
        var diableAppdomainTest2 = _testEnvironment.GetTestAsset("DisableAppdomainTest2.dll", "net451");

        RunTests(runnerInfo.RunnerFramework, string.Format("{0}\" \"{1}", diableAppdomainTest1, diableAppdomainTest2), 2);
    }

    [TestMethod]
    [TestCategory("Windows")]
    [NetFullTargetFrameworkDataSource]
    public void NewtonSoftDependencyWithDisableAppdomainTest(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var newtonSoftDependnecyTest = _testEnvironment.GetTestAsset("NewtonSoftDependency.dll", "net451");

        RunTests(runnerInfo.RunnerFramework, newtonSoftDependnecyTest, 1);
    }

    private void RunTests(string runnerFramework, string testAssembly, int passedTestCount)
    {
        if (runnerFramework.StartsWith("netcoreapp"))
        {
            Assert.Inconclusive("This test is not meant for .netcore.");
            return;
        }

        var runConfigurationDictionary = new Dictionary<string, string>
        {
            { "DisableAppDomain", "true" }
        };

        using var tempDir = new TempDirectory();
        var arguments = PrepareArguments(
            testAssembly,
            string.Empty,
            GetRunsettingsFilePath(tempDir, runConfigurationDictionary),
            FrameworkArgValue, resultsDirectory: tempDir.Path);

        InvokeVsTest(arguments);
        ValidateSummaryStatus(passedTestCount, 0, 0);
    }

    private string GetRunsettingsFilePath(TempDirectory tempDir, Dictionary<string, string> runConfigurationDictionary)
    {
        var runsettingsPath = Path.Combine(tempDir.Path, "test_" + Guid.NewGuid() + ".runsettings");
        CreateRunSettingsFile(runsettingsPath, runConfigurationDictionary);
        return runsettingsPath;
    }
}
