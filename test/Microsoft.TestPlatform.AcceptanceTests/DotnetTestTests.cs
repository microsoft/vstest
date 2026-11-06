// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class DotnetTestTests : AcceptanceTestBase
{
    private string GetBuiltConsoleVersion()
        => System.Diagnostics.FileVersionInfo.GetVersionInfo(GetDotnetRunnerPath()).ProductVersion!.Split('-')[0];

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void RunDotnetTestWithCsproj(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetIsolatedTestAsset("SimpleTestProject.csproj");
        InvokeDotnetTest($@"{projectPath} --framework {_testEnvironment.TargetFramework} /p:VSTestNoLogo=false --logger:""Console;Verbosity=normal""");

        StdOutputContains("Test Execution Command Line Tool Version " + GetBuiltConsoleVersion());
        ValidateSummaryStatus(1, 1, 1);
        ExitCodeEquals(1);
    }

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void RunDotnetTestWithDll(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var assemblyPath = BuildMultipleAssemblyPath("SimpleTestProject.dll");
        InvokeDotnetTest($@"{assemblyPath} --logger:""Console;Verbosity=normal""");

        StdOutputContains("Test Execution Command Line Tool Version " + GetBuiltConsoleVersion());
        ValidateSummaryStatus(1, 1, 1);
        ExitCodeEquals(1);
    }

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void RunDotnetTestWithCsprojPassInlineSettings(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetIsolatedTestAsset("ParametrizedTestProject.csproj");
        InvokeDotnetTest($@"{projectPath} --framework {_testEnvironment.TargetFramework} /p:VSTestNoLogo=false --logger:""Console;Verbosity=normal"" -- TestRunParameters.Parameter(name =\""weburl\"", value=\""http://localhost//def\"")");

        ValidateSummaryStatus(1, 0, 0);
        ExitCodeEquals(0);
    }

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void RunDotnetTestWithDllPassInlineSettings(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var assemblyPath = BuildMultipleAssemblyPath("ParametrizedTestProject.dll");
        InvokeDotnetTest($@"{assemblyPath} --logger:""Console;Verbosity=normal"" -- TestRunParameters.Parameter(name=\""weburl\"", value=\""http://localhost//def\"")");

        ValidateSummaryStatus(1, 0, 0);
        ExitCodeEquals(0);
    }

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void RunDotnetTestWithNativeDll(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        string assemblyRelativePath = @"microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\x64\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
        var assemblyAbsolutePath = Path.Combine(_testEnvironment.PackageDirectory, assemblyRelativePath);

        InvokeDotnetTest($@"{assemblyAbsolutePath} --logger:""Console;Verbosity=normal""");

        ValidateSummaryStatus(1, 1, 0);
        ExitCodeEquals(1);
    }
}
