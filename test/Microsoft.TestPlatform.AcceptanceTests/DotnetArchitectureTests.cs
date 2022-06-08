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
            yield return new object[] { entry[0], "net6.0", "6.0.100", "DOTNET_ROOT_X86" };
            yield return new object[] { entry[0], "net6.0", "6.0.100", "DOTNET_ROOT(x86)" };
            yield return new object[] { entry[0], "net6.0", "6.0.100", "DOTNET_ROOT" };

            yield return new object[] { entry[0], "net5.0", "5.0.100", "DOTNET_ROOT_X86" };
            yield return new object[] { entry[0], "net5.0", "5.0.100", "DOTNET_ROOT(x86)" };
            yield return new object[] { entry[0], "net5.0", "5.0.100", "DOTNET_ROOT" };
        }
    }

    [TestMethod]
    [DynamicData(nameof(GetRunnerAndDotnetRootEnvVar), DynamicDataSourceType.Method)]
    public void Run32BitsProcessFrom64BitsDotnet(RunnerInfo runnerInfo, string targetFramework, string sdkVersion, string dotnetRootEnvVarName)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        // We want some isolated directory because we are going to pin the SDK used to ensure
        // the dotnet directory detection mechanism is the correct one.
        var dllPath = GetIsolatedTestDllForFramework("ProjectLaunch32BitsProcess.dll", targetFramework);
        var isolatedDirectory = Path.GetDirectoryName(dllPath)!;
        File.WriteAllText(Path.Combine(isolatedDirectory, "global.json"), $@"{{
  ""sdk"": {{
    ""version"": ""{sdkVersion}""
  }},
  ""tools"": {{
    ""dotnet"": ""{sdkVersion}""
  }}
}}");

        var dotnetX86LocalPath = Path.Combine(_testEnvironment.ToolsDirectory, "dotnet_x86");
        var env = new Dictionary<string, string>
        {
            ["EXPECTED_ENV_VAR_NAME"] = dotnetRootEnvVarName,
            ["EXPECTED_ENV_VAR_VALUE"] = dotnetX86LocalPath,
        };
        InvokeDotnetTest(dllPath, env, useDotnetFromTools: true, workingDirectory: isolatedDirectory);

        ExitCodeEquals(0);
    }
}

#endif
