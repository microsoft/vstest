// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// Running dotnet test + csproj and using MSBuild for the output.
/// </summary>
[TestClass]
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
        // The 1ES agent sets MSBUILDENSURESTDOUTFORTASKPROCESSES=1 in the environment. Microsoft.TestPlatform.targets
        // calls _VSTestConsole when "!$(VsTestUseMSBuildOutput) OR $(MSBUILDENSURESTDOUTFORTASKPROCESSES) == '1'", so
        // with that variable inherited the console logger runs first, fails the build on the failing test, and
        // _VSTestMSBuild - the target under test here - never runs at all. MSBuildLoggerCanBeDisabledByEnvironmentVariableProperty
        // below documents that this variable disables the MSBuild logger BY DESIGN, so a test that asks for the logger
        // to be ENABLED has to neutralize it. "0" is used rather than null so the value deterministically fails the
        // '== 1' comparison instead of relying on null-removal semantics of ProcessStartInfo.EnvironmentVariables.
        // Passing the value through ProcessStartInfo.EnvironmentVariables does NOT work, and build 3067671 measured
        // exactly why rather than inferring it. At -v:detailed MSBuild printed, from the very line that owns the
        // condition, "Microsoft.TestPlatform.targets(32,42): message : Property
        // 'MSBUILDENSURESTDOUTFORTASKPROCESSES' with value '1' expanded from the environment." So the child process
        // read '1', not the '0' set here, and consequently "_VSTestMSBuild" appeared 0 times in the whole log while
        // "_VSTestConsole" appeared 8 times and ended in "Done building target "_VSTestConsole" ... -- FAILED".
        // _VSTestConsole is called first, so once it fails the build stops and the target under test never runs.
        // The fix is MSBuild property precedence, not a different environment value: environment variables are the
        // LOWEST precedence source of a property, and a global property set with /p: overrides them and cannot be
        // overwritten by the environment. The targets read $(MSBUILDENSURESTDOUTFORTASKPROCESSES) as an ordinary
        // property, so a global property makes the '== 1' comparison deterministically false no matter who exports
        // the variable - the 1ES agent or the dotnet CLI. This only changes the value the CONDITION sees; MSBuild's
        // own stdout handling still reads the real environment variable, so engine behaviour is untouched.
        InvokeDotnetTest($@"{projectPath} -nodereuse:false -v:detailed /p:VsTestUseMSBuildOutput=true /p:MSBUILDENSURESTDOUTFORTASKPROCESSES=0 /p:PackageVersion={IntegrationTestEnvironment.LatestLocallyBuiltNugetVersion}", environmentVariables: new Dictionary<string, string?> { ["MSBUILDENSURESTDOUTFORTASKPROCESSES"] = "0" });

        // The output:
        // Determining projects to restore...
        //   Restored C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\SimpleTestProject.csproj (in 441 ms).
        //   SimpleTestProject -> C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\artifacts\bin\TestAssets\SimpleTestProject\Debug\net462\SimpleTestProject.dll
        //   SimpleTestProject -> C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\artifacts\bin\TestAssets\SimpleTestProject\Debug\netcoreapp3.1\SimpleTestProject.dll
        // C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\UnitTest1.cs(41): error VSTEST1: (FailingTest) SampleUnitTestProject.UnitTest1.FailingTest() Assert.AreEqual failed. Expected:<2>. Actual:<3>.  [C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\SimpleTestProject.csproj::TargetFramework=net462]
        // C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\UnitTest1.cs(41): error VSTEST1: (FailingTest) SampleUnitTestProject.UnitTest1.FailingTest() Assert.AreEqual failed. Expected:<2>. Actual:<3>.  [C:\Users\nohwnd\AppData\Local\Temp\vstest\xvoVt\SimpleTestProject.csproj::TargetFramework=netcoreapp3.1]

        StdOutputContains("error TESTERROR: FailingTest (");
        StdOutputContains("): Error Message: Assert.AreEqual failed. Expected:<ğğğ𦮙我們剛才從𓋴𓅓𓏏𓇏𓇌𓀀>. Actual:<not the same>.");
        StdOutputContains("at TerminalLoggerUnitTests.UnitTest1.FailingTest() in");
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
