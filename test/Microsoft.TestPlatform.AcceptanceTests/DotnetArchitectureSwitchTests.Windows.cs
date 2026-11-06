// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETFRAMEWORK

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;

using NuGet.Versioning;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
[TestCategory("Windows-Review")]
public class DotnetArchitectureSwitchTestsWindowsOnly : AcceptanceTestBase
{
    [TestMethod]
    [DataRow("X64", "X86")]
    [DataRow("X86", "X64")]
    public void Use_EnvironmentVariables(string architectureFrom, string architectureTo)
    {
        SetTestEnvironment(_testEnvironment, new RunnerInfo { RunnerFramework = "netcoreapp2.1" });
        string dotnetPath = GetDownloadedDotnetMuxerFromTools(architectureFrom);
        string dotnetPathTo = GetDownloadedDotnetMuxerFromTools(architectureTo);
        var vstestConsolePath = GetDotnetRunnerPath();
        var dotnetRunnerPath = TempDirectory.CreateDirectory("dotnetrunner");
        TempDirectory.CopyDirectory(new DirectoryInfo(Path.GetDirectoryName(vstestConsolePath)!), dotnetRunnerPath);

        // Patch the runner
        string sdkVersion = GetLatestSdkVersion(dotnetPath);
        string runtimeConfigFile = Path.Combine(dotnetRunnerPath.FullName, "vstest.console.runtimeconfig.json");
        JObject patchRuntimeConfig = JObject.Parse(File.ReadAllText(runtimeConfigFile));
        patchRuntimeConfig!["runtimeOptions"]!["framework"]!["version"] = sdkVersion;
        File.WriteAllText(runtimeConfigFile, patchRuntimeConfig.ToString());

        var environmentVariables = new Dictionary<string, string?>
        {
            ["DOTNET_MULTILEVEL_LOOKUP"] = "0",
            ["DOTNET_CLI_HOME"] = CreateIsolatedTemplateEngineHome(TempDirectory.Path),
            [$"DOTNET_ROOT_{architectureTo}"] = Path.GetDirectoryName(dotnetPathTo)!,
            ["ExpectedArchitecture"] = architectureTo,
            ["ExpectedDotnetRoot"] = Path.GetDirectoryName(dotnetPathTo)!
        };
        ConfigureSdkEnvironment(environmentVariables, dotnetPath);
        ExecuteApplication(dotnetPath, "new mstest --no-restore", out _, out string templateError, out int templateExitCode, environmentVariables, TempDirectory.Path);
        Assert.AreEqual(0, templateExitCode, templateError);
        UseLocallyBuiltTestSdk(TempDirectory.Path);

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
        StringAssert.StartsWith(typeof(object).Assembly.Location, System.IO.Path.Combine(Environment.GetEnvironmentVariable(""ExpectedDotnetRoot"")!, ""shared"") + System.IO.Path.DirectorySeparatorChar);
    }
}");

        var testArguments = $"test --disable-build-servers -p:UseSharedCompilation=false -p:VsTestConsolePath=\"{Path.Combine(dotnetRunnerPath.FullName, Path.GetFileName(vstestConsolePath))}\" --arch {architectureTo.ToLower()} --diag:log.txt";
        ExecuteApplication(dotnetPath, testArguments, out string stdOut, out _, out int exitCode, environmentVariables, TempDirectory.Path);
        Assert.AreEqual(0, exitCode, stdOut);

        environmentVariables = new Dictionary<string, string?>
        {
            ["DOTNET_MULTILEVEL_LOOKUP"] = "0",
            ["DOTNET_ROOT"] = Path.GetDirectoryName(dotnetPathTo),
            [$"DOTNET_ROOT_{architectureTo}"] = null,
            ["ExpectedArchitecture"] = architectureTo,
            ["ExpectedDotnetRoot"] = Path.GetDirectoryName(dotnetPathTo)!
        };
        if (architectureTo == "X86")
        {
            // The legacy x86 apphost uses this name instead of DOTNET_ROOT on 64-bit Windows.
            environmentVariables["DOTNET_ROOT(x86)"] = Path.GetDirectoryName(dotnetPathTo);
        }
        ConfigureSdkEnvironment(environmentVariables, dotnetPath);

        ExecuteApplication(dotnetPath, testArguments, out stdOut, out _, out exitCode, environmentVariables, TempDirectory.Path);
        Assert.AreEqual(0, exitCode, stdOut);

        environmentVariables = new Dictionary<string, string?>
        {
            ["DOTNET_MULTILEVEL_LOOKUP"] = "0",
            [$"DOTNET_ROOT_{architectureTo}"] = Path.GetDirectoryName(dotnetPathTo),
            ["DOTNET_ROOT"] = "WE SHOULD PICK THE ABOVE ONE BEFORE FALLBACK TO DOTNET_ROOT",
            ["ExpectedArchitecture"] = architectureTo,
            ["ExpectedDotnetRoot"] = Path.GetDirectoryName(dotnetPathTo)!
        };
        if (architectureTo == "X86")
        {
            environmentVariables["DOTNET_ROOT(x86)"] = "DOTNET_ROOT_X86_MUST_TAKE_PRECEDENCE";
        }
        ConfigureSdkEnvironment(environmentVariables, dotnetPath);

        ExecuteApplication(dotnetPath, testArguments, out stdOut, out _, out exitCode, environmentVariables, TempDirectory.Path);
        Assert.AreEqual(0, exitCode, stdOut);
    }

    private static void ConfigureSdkEnvironment(IDictionary<string, string?> environment, string dotnetPath)
    {
        // MSBuild exports these to child processes. An x86 SDK must not load the outer x64 SDK's tasks.
        environment["MSBuildSDKsPath"] = null;
        environment["MSBuildExtensionsPath"] = null;
        environment["MSBUILD_EXE_PATH"] = null;
        environment["DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR"] = Path.GetDirectoryName(dotnetPath);
        environment["DOTNET_HOST_PATH"] = dotnetPath;
    }

    private string GetLatestSdkVersion(string dotnetPath)
        => Path.GetFileName(Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(dotnetPath)!, @"shared/Microsoft.NETCore.App"))
            .OrderByDescending(path => SemanticVersion.Parse(Path.GetFileName(path))).First());
}

#endif
