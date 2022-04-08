// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class DotnetTestTests : AcceptanceTestBase
{
    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void RunDotnetTestWithCsproj(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetIsolatedTestAsset("SimpleTestProject.csproj");
        InvokeDotnetTest($@"{projectPath} --logger:""Console;Verbosity=normal""");

        // ensure our dev version is used
        StdOutputContains("-dev");
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

        // ensure our dev version is used
        StdOutputContains("-dev");
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
        InvokeDotnetTest($@"{projectPath} --logger:""Console;Verbosity=normal"" -- TestRunParameters.Parameter(name =\""weburl\"", value=\""http://localhost//def\"")");

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
}
