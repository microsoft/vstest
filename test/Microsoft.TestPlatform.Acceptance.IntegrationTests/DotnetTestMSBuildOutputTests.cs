﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// Running dotnet test + csproj and using MSBuild for the output.
/// </summary>
[TestClass]
[Ignore(
"""
    Ignored because we need to update to dotnet SDK 9.0.100-alpha.1.24073.1 or newer,
    but that depends on newer version of MSBuild sdk that is not published yet, and so our build fails in Signing validation.
    We need that upgrade because older SDK is hardcoding the same ENV variable that we are using to disable this functionality.
    So when we patch the targets and build dll in the current SDK this new functionality is always disabled and we cannot test it with older SDK.
""")]
public class DotnetTestMSBuildOutputTests : AcceptanceTestBase
{
    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void MSBuildLoggerCanBeEnabledByBuildPropertyAndDoesNotEatSpecialChars(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetIsolatedTestAsset("TerminalLoggerTestProject.csproj");
        InvokeDotnetTest($@"{projectPath} -nodereuse:false /p:VsTestUseMSBuildOutput=true /p:PackageVersion={IntegrationTestEnvironment.LatestLocallyBuiltNugetVersion}");

        // The output:
        // Determining projects to restore...
        //   Restored C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\SimpleTestProject.csproj (in 441 ms).
        //   SimpleTestProject -> C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\artifacts\bin\TestAssets\SimpleTestProject\Debug\net462\SimpleTestProject.dll
        //   SimpleTestProject -> C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\artifacts\bin\TestAssets\SimpleTestProject\Debug\netcoreapp3.1\SimpleTestProject.dll
        // C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\UnitTest1.cs(41): error VSTEST1: (FailingTest) SampleUnitTestProject.UnitTest1.FailingTest() Assert.AreEqual failed. Expected:<2>. Actual:<3>.  [C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\SimpleTestProject.csproj::TargetFramework=net462]
        // C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\UnitTest1.cs(41): error VSTEST1: (FailingTest) SampleUnitTestProject.UnitTest1.FailingTest() Assert.AreEqual failed. Expected:<2>. Actual:<3>.  [C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\SimpleTestProject.csproj::TargetFramework=netcoreapp3.1]

        StdOutputContains("TerminalLoggerUnitTests.UnitTest1.FailingTest() Assert.AreEqual failed. Expected:<ğğğ𦮙我們剛才從𓋴𓅓𓏏𓇏𓇌𓀀 (System.String)>. Actual:<3 (System.Int32)>.");
        // We are sending those as low prio messages, they won't show up on screen but will be in binlog.
        //StdOutputContains("passed PassingTest");
        //StdOutputContains("skipped SkippingTest");
        ExitCodeEquals(1);
    }

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void MSBuildLoggerCanBeDisabledByBuildProperty(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetIsolatedTestAsset("TerminalLoggerTestProject.csproj");
        InvokeDotnetTest($@"{projectPath} -nodereuse:false /p:VsTestUseMSBuildOutput=false /p:PackageVersion={IntegrationTestEnvironment.LatestLocallyBuiltNugetVersion}");

        // Check that we see the summary that is printed from the console logger, meaning the new output is disabled.
        StdOutputContains("Failed! - Failed: 1, Passed: 1, Skipped: 1, Total: 3, Duration:");
        // We are sending those as low prio messages, they won't show up on screen but will be in binlog.
        //StdOutputContains("passed PassingTest");
        //StdOutputContains("skipped SkippingTest");
        ExitCodeEquals(1);
    }


    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void MSBuildLoggerCanBeDisabledByEnvironmentVariableProperty(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetIsolatedTestAsset("TerminalLoggerTestProject.csproj");
        InvokeDotnetTest($@"{projectPath} -nodereuse:false /p:PackageVersion={IntegrationTestEnvironment.LatestLocallyBuiltNugetVersion}", environmentVariables: new Dictionary<string, string?> { ["MSBUILDENSURESTDOUTFORTASKPROCESSES"] = "1" });

        // Check that we see the summary that is printed from the console logger, meaning the new output is disabled.
        StdOutputContains("Failed! - Failed: 1, Passed: 1, Skipped: 1, Total: 3, Duration:");

        ExitCodeEquals(1);
    }
}
