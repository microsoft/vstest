// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETFRAMEWORK

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Text.Json.Nodes;

using NuGet.Versioning;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
[TestCategory("Windows-Review")]
public class DotnetArchitectureSwitchTestsWindowsOnly : AcceptanceTestBase
{
    [TestMethod]
    [DataRow("X64", "X86")]
    // TODO: This test does not work on server, it occasionally fails with cryptic message around not being able to load MSBuild.Tasks.
    // [DataRow("X86", "X64")]
    public void Use_EnvironmentVariables(string architectureFrom, string architectureTo)
    {
        SetTestEnvironment(_testEnvironment, new RunnerInfo { RunnerFramework = RUNNER_NET });
        string dotnetPath = GetDownloadedDotnetMuxerFromTools(architectureFrom);
        string dotnetPathTo = GetDownloadedDotnetMuxerFromTools(architectureTo);
        var vstestConsolePath = GetDotnetRunnerPath();
        var dotnetRunnerPath = TempDirectory.CreateDirectory("dotnetrunner");
        TempDirectory.CopyDirectory(new DirectoryInfo(Path.GetDirectoryName(vstestConsolePath)!), dotnetRunnerPath);

        // Patch the runner
        string sdkVersion = GetLatestSdkVersion(dotnetPath);
        string runtimeConfigFile = Path.Combine(dotnetRunnerPath.FullName, "vstest.console.runtimeconfig.json");
        var patchRuntimeConfig = JsonNode.Parse(File.ReadAllText(runtimeConfigFile))!;
        patchRuntimeConfig["runtimeOptions"]!["framework"]!["version"] = sdkVersion;
        File.WriteAllText(runtimeConfigFile, patchRuntimeConfig.ToJsonString());

        var environmentVariables = new Dictionary<string, string?>
        {
            [$"DOTNET_ROOT_{architectureTo}"] = Path.GetDirectoryName(dotnetPathTo)!,
            ["ExpectedArchitecture"] = architectureTo
        };

        if (architectureTo == "X86")
        {
            environmentVariables.Add("DOTNET_ROOT(x86)", Path.GetDirectoryName(dotnetPathTo)!);
        }
        ExecuteApplication(dotnetPath, "new mstest", out _, out string _, out _, environmentVariables, TempDirectory.Path);

        // Patch test file
        File.WriteAllText(Path.Combine(TempDirectory.Path, "UnitTest1.cs"),
@"
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace cfebbc5339cf4c22854e79824e938c74;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
        Assert.AreEqual(Environment.GetEnvironmentVariable(""ExpectedArchitecture""), System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString());
    }
}");

        ExecuteApplication(dotnetPath, $"test -p:VsTestConsolePath=\"{Path.Combine(dotnetRunnerPath.FullName, Path.GetFileName(vstestConsolePath))}\" --arch {architectureTo.ToLower(CultureInfo.InvariantCulture)} --diag:log.txt", out string stdOut, out _, out int exitCode, environmentVariables, TempDirectory.Path);
        Assert.AreEqual(0, exitCode, stdOut);

        environmentVariables = new Dictionary<string, string?>
        {
            ["DOTNET_ROOT"] = Path.GetDirectoryName(dotnetPathTo),
            ["ExpectedArchitecture"] = architectureTo
        };

        if (architectureTo == "X86")
        {
            environmentVariables.Add("DOTNET_ROOT(x86)", Path.GetDirectoryName(dotnetPathTo)!);
        }
        ExecuteApplication(dotnetPath, $"test -p:VsTestConsolePath=\"{Path.Combine(dotnetRunnerPath.FullName, Path.GetFileName(vstestConsolePath))}\" --arch {architectureTo.ToLower(CultureInfo.InvariantCulture)} --diag:log.txt", out stdOut, out _, out exitCode, environmentVariables, TempDirectory.Path);
        Assert.AreEqual(0, exitCode, stdOut);

        environmentVariables = new Dictionary<string, string?>
        {
            [$"DOTNET_ROOT_{architectureTo}"] = Path.GetDirectoryName(dotnetPathTo),
            ["DOTNET_ROOT"] = "WE SHOULD PICK THE ABOVE ONE BEFORE FALLBACK TO DOTNET_ROOT",
            ["ExpectedArchitecture"] = architectureTo
        };

        if (architectureTo == "X86")
        {
            environmentVariables.Add("DOTNET_ROOT(x86)", Path.GetDirectoryName(dotnetPathTo)!);
        }
        ExecuteApplication(dotnetPath, $"test -p:VsTestConsolePath=\"{Path.Combine(dotnetRunnerPath.FullName, Path.GetFileName(vstestConsolePath))}\" --arch {architectureTo.ToLower(CultureInfo.InvariantCulture)} --diag:log.txt", out stdOut, out _, out exitCode, environmentVariables, TempDirectory.Path);
        Assert.AreEqual(0, exitCode, stdOut);
    }

    // Regression test for https://github.com/microsoft/vstest/issues/16151
    //
    // When the architecture specific roots (DOTNET_ROOT_X86 / DOTNET_ROOT(x86)) are not set and only the
    // architecture-less DOTNET_ROOT is set, pointing at an x64 installation, the net8 testhost.x86.exe apphost
    // falls back to DOTNET_ROOT (DOTNET_ROOT_X86 -> DOTNET_ROOT since .NET 6) and loads the x64 hostfxr.dll
    // into the 32-bit process, aborting the run with HRESULT 0x800700C1 (ERROR_BAD_EXE_FORMAT). vstest.console
    // must detect that DOTNET_ROOT points at a different architecture than the testhost and scrub it before
    // launching the testhost, so the apphost never loads the mismatched hostfxr.dll.
    [TestMethod]
    public void X86Testhost_DoesNotLoadMismatchedHostfxr_WhenOnlyArchlessDotnetRootIsSetToX64()
    {
        SetTestEnvironment(_testEnvironment, new RunnerInfo { RunnerFramework = RUNNER_NET });
        string dotnetX64 = GetDownloadedDotnetMuxerFromTools("X64");
        var vstestConsolePath = GetDotnetRunnerPath();
        var dotnetRunnerPath = TempDirectory.CreateDirectory("dotnetrunner");
        TempDirectory.CopyDirectory(new DirectoryInfo(Path.GetDirectoryName(vstestConsolePath)!), dotnetRunnerPath);

        // Patch the runner to run on the x64 runtime we resolved above.
        string sdkVersion = GetLatestSdkVersion(dotnetX64);
        string runtimeConfigFile = Path.Combine(dotnetRunnerPath.FullName, "vstest.console.runtimeconfig.json");
        var patchRuntimeConfig = JsonNode.Parse(File.ReadAllText(runtimeConfigFile))!;
        patchRuntimeConfig["runtimeOptions"]!["framework"]!["version"] = sdkVersion;
        File.WriteAllText(runtimeConfigFile, patchRuntimeConfig.ToJsonString());

        ExecuteApplication(dotnetX64, "new mstest", out _, out _, out _, new Dictionary<string, string?>(), TempDirectory.Path);

        // Only the architecture-less DOTNET_ROOT is set, and it points at the x64 installation. The architecture
        // specific overrides are cleared (the test runner sets DOTNET_ROOT(x86) in the surrounding environment),
        // so without the fix the x86 testhost would fall back to the x64 DOTNET_ROOT and load the x64 hostfxr.dll.
        var environmentVariables = new Dictionary<string, string?>
        {
            ["DOTNET_ROOT"] = Path.GetDirectoryName(dotnetX64),
            ["DOTNET_ROOT(x86)"] = string.Empty,
            ["DOTNET_ROOT_X86"] = string.Empty,
        };

        ExecuteApplication(
            dotnetX64,
            $"test -p:VsTestConsolePath=\"{Path.Combine(dotnetRunnerPath.FullName, Path.GetFileName(vstestConsolePath))}\" --arch x86 --diag:log.txt",
            out string stdOut,
            out string stdError,
            out _,
            environmentVariables,
            TempDirectory.Path);

        // The x86 testhost must not try to load the x64 hostfxr.dll. Per the issue, an honest "runtime not found"
        // diagnostic is acceptable; loading the wrong-architecture hostfxr.dll (0x800700C1) is the bug.
        Assert.DoesNotContain(
            "0x800700C1",
            stdOut + stdError,
            $"x86 testhost loaded the x64 hostfxr.dll (issue 16151).\nStdOut:\n{stdOut}\nStdError:\n{stdError}");
    }

    private static string GetLatestSdkVersion(string dotnetPath)
    {
        var folders = Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(dotnetPath)!, @"shared/Microsoft.NETCore.App")).Select(f => new
        {
            FullName = f,
            SemanticVersion = SemanticVersion.Parse(new DirectoryInfo(f).Name)
        }).OrderByDescending(x => x.SemanticVersion).ToList();
        return Path.GetFileName(folders.First().FullName);
    }
}

#endif
