// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest;

[TestClass]
public class Test32Bit
{
    [TestMethod]
    public void TheTest()
    {
        // Test is based on reproducer from following SDK issue: https://github.com/dotnet/sdk/issues/22647
        // We cannot run the test in an automatic/coded way because we need to have .NET 5 and 6 SDKs installed
        // which leads to full disk capacity on CI.
        // As this change of behavior is quite important we want to make sure we can rerun such manual test
        // later on so we are keeping it here.
        // Repro steps:
        // 1. Build project
        // 2. Set $env:DOTNET_ROOT_ENV_VAR_NAME = "DOTNET_ROOT(x86)"
        // 3. Set $env:DOTNET_ROOT_ENV_VAR_VALUE to a 32bits dotnet installation directory for the matching TFM
        //    (e.g. C:\src\vstest\tools\dotnet_x86)
        // 4. Add a global.json pinning the .NET version to 5 or 6
        // 5. Run 'dotnet test ./bin/<Config>/<Pinned_TFM>/ProjectLaunch32BitsProcess.dll'
        // 6. Test should be succesful
        // 7. Repeat steps 2 to 5 with $env:DOTNET_ROOT_ENV_VAR_NAME = "DOTNET_ROOT". Although we are running a
        //    32 bits process, the dotnet detection logic is set to fallback to this variable.
        // 8. Repeat steps 2 to 5 with $env:DOTNET_ROOT_ENV_VAR_NAME = "DOTNET_ROOT_X86".
        //    This should work for .NET 6 but fail for .NET 5 (if you don't have a global .NET 5 SDK installed),
        //    as this new variable is not understood by the .NET 5 detection algorithm.
        // Debugging tips:
        // Use the following environment variables to understand how is .NET being resolved.
        // COREHOST_TRACE = 1
        // COREHOST_TRACEFILE = "C:\fxr.tx"
        var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "TestProcess32.exe",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        var envVarName = Environment.GetEnvironmentVariable("DOTNET_ROOT_ENV_VAR_NAME");
        Assert.IsNotNull(envVarName, "Calling process didn't set DOTNET_ROOT_ENV_VAR_NAME.");
        var envVarValue = Environment.GetEnvironmentVariable("DOTNET_ROOT_ENV_VAR_VALUE");
        Assert.IsNotNull(envVarValue, "Calling process didn't set DOTNET_ROOT_ENV_VAR_VALUE.");
        // Set the DOTNET_ROOT* env variable so that the 32bits process can locate dotnet
        // even if there is no global installation.
        process.StartInfo.EnvironmentVariables[envVarName] = envVarValue;
        process.StartInfo.EnvironmentVariables["DOTNET_ROOT_ENV_VAR_NAME"] = envVarName;
        // Ensure multi-level lookup is disabled so that we don't fallback to machine-wide installation
        process.StartInfo.EnvironmentVariables["DOTNET_MULTILEVEL_LOOKUP"] = "0";

        process.Start();
        var stderr = process.StandardError.ReadToEnd();
        var stdout = process.StandardOutput.ReadToEnd();

        Console.WriteLine($"32bit stdout: {stdout}");
        Console.WriteLine($"32bit err: {stderr}");

        Assert.IsTrue(string.IsNullOrEmpty(stderr),
            $"There was some error in process run: {stderr}");
    }
}
