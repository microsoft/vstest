// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETFRAMEWORK

using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class DotnetArchitectureTests : AcceptanceTestBase
{
    public static IEnumerable<object[]> GetRunnerAndDotnetRootEnvVar()
    {
        var runnerDataSource = new NetCoreTargetFrameworkDataSource(useDesktopRunner: false);
        foreach (var entry in runnerDataSource.GetData(null))
        {
            yield return new object[] { entry[0], "DOTNET_ROOT(x86)", "dotnet_x86" };
            yield return new object[] { entry[0], "DOTNET_ROOT_X86", "dotnet_x86" };
            yield return new object[] { entry[0], "DOTNET_ROOT", "dotnet" };
            yield return new object[] { entry[0], "DOTNET_ROOT_X64", "dotnet" };
        }
    }

    [TestMethod]
    [DynamicData(nameof(GetRunnerAndDotnetRootEnvVar), DynamicDataSourceType.Method)]
    public void DotnetTestWithNet6ProjectLaunching32BitsProcess(RunnerInfo runnerInfo, string envVarName, string dotnetSubFolder)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        // We want some isolated directory because we are going to pin the SDK used to ensure
        // the dotnet directory detection mechanism is the correct one.
        var dllPath = GetIsolatedTestDllForFramework("ProjectLaunch32BitsProcess.dll", "net6.0");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(dllPath)!, "global.json"), @"{
  ""sdk"": {
    ""version"": ""6.0.100""
  },
  ""tools"": {
    ""dotnet"": ""6.0.100""
  }
}");

        var dotnetX86LocalPath = Path.Combine(_testEnvironment.ToolsDirectory, dotnetSubFolder);
        var env = new Dictionary<string, string>
        {
            // If we don't set DOTNET_ROOT/DOTNET_ROOT(x86), the test result depends on whether or not
            // there is a global x86 .NET installed.
            [envVarName] = dotnetX86LocalPath,
            // Used by test to assert which env var is supposed to be found.
            ["EXPECTED_ENV_VAR_NAME"] = envVarName,
            ["EXPECTED_ENV_VAR_VALUE"] = dotnetX86LocalPath,
        };
        InvokeDotnetTest(dllPath, env, useDotnetFromTools: true);

        ExitCodeEquals(0);
    }

    [TestMethod]
    [DynamicData(nameof(GetRunnerAndDotnetRootEnvVar), DynamicDataSourceType.Method)]
    public void DotnetTestWithNet5ProjectLaunching32BitsProcess(RunnerInfo runnerInfo, string envVarName, string dotnetSubFolder)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        // We want some isolated directory because we are going to pin the SDK used to ensure
        // the dotnet directory detection mechanism is the correct one.
        var dllPath = GetIsolatedTestDllForFramework("ProjectLaunch32BitsProcess.dll", "net5.0");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(dllPath)!, "global.json"), @"{
  ""sdk"": {
    ""version"": ""5.0.100""
  },
  ""tools"": {
    ""dotnet"": ""5.0.100""
  }
}");

        var dotnetX86LocalPath = Path.Combine(_testEnvironment.ToolsDirectory, dotnetSubFolder);
        var env = new Dictionary<string, string>
        {
            // If we don't set DOTNET_ROOT/DOTNET_ROOT(x86), the test result depends on whether or not
            // there is a global x86 .NET installed.
            [envVarName] = dotnetX86LocalPath,
            // Used by test to assert which env var is supposed to be found.
            ["EXPECTED_ENV_VAR_NAME"] = envVarName,
            ["EXPECTED_ENV_VAR_VALUE"] = dotnetX86LocalPath,
            //["COREHOST_TRACE"] = "1",
            //["COREHOST_TRACEFILE"] = @"C:\src\temp\net5.tx",
        };
        InvokeDotnetTest(dllPath, env, useDotnetFromTools: true);

        ExitCodeEquals(0);
    }
}

#endif
