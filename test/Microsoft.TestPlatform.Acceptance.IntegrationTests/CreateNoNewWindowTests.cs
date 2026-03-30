// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class CreateNoNewWindowTests : AcceptanceTestBase
{
    private const string TestAssetName = "ConsoleWindowCheck.dll";

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(useDesktopRunner: true)]
    public void WhenCreateNoNewWindowIsFalseTestHostHasConsoleWindow(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var assemblyPaths = GetAssetFullPath(TestAssetName);

        var runsettingsXml = "<RunSettings><RunConfiguration>"
            + "<CreateNoNewWindow>false</CreateNoNewWindow>"
            + "</RunConfiguration></RunSettings>";
        var runsettingsPath = GetRunsettingsFilePath(runsettingsXml);

        var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
        arguments += $" /settings:{runsettingsPath}";

        InvokeVsTest(arguments);

        ExitCodeEquals(0);
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(useDesktopRunner: true)]
    public void WhenCreateNoNewWindowIsTrueTestHostHasNoConsoleWindow(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var assemblyPaths = GetAssetFullPath(TestAssetName);

        var runsettingsXml = "<RunSettings><RunConfiguration>"
            + "<CreateNoNewWindow>true</CreateNoNewWindow>"
            + "</RunConfiguration></RunSettings>";
        var runsettingsPath = GetRunsettingsFilePath(runsettingsXml);

        var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
        arguments += $" /settings:{runsettingsPath}";

        InvokeVsTest(arguments);

        ExitCodeEquals(1);
        StdErrorContains("HAS_CONSOLE_WINDOW=False");
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(useDesktopRunner: true)]
    public void WhenCreateNoNewWindowIsNotSetDefaultIsTrueAndTestHostHasNoConsoleWindow(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var assemblyPaths = GetAssetFullPath(TestAssetName);

        var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);

        InvokeVsTest(arguments);

        ExitCodeEquals(1);
        StdErrorContains("HAS_CONSOLE_WINDOW=False");
    }

    private string GetRunsettingsFilePath(string runsettingsXml)
    {
        var runsettingsPath = System.IO.Path.Combine(TempDirectory.Path, "test.runsettings");
        System.IO.File.WriteAllText(runsettingsPath, runsettingsXml);
        return runsettingsPath;
    }
}
