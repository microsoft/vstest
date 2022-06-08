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
        // Test is based on reproducer from following SDK issue:
        // https://github.com/dotnet/sdk/issues/22647
        var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "TestProcess32.exe",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        var envVarName = Environment.GetEnvironmentVariable("EXPECTED_ENV_VAR_NAME");
        Assert.IsNotNull(envVarName, "Calling process didn't set EXPECTED_ENV_VAR_NAME.");
        var envVarValue = Environment.GetEnvironmentVariable("EXPECTED_ENV_VAR_VALUE");
        Assert.IsNotNull(envVarValue, "Calling process didn't set EXPECTED_ENV_VAR_VALUE.");
        // Set the DOTNET_ROOT* env variable so that the 32bits process can locate dotnet
        // even if there is no global installation.
        process.StartInfo.EnvironmentVariables[envVarName] = envVarValue;

        process.Start();
        var stderr = process.StandardError.ReadToEnd();
        var stdout = process.StandardOutput.ReadToEnd();

        Console.WriteLine($"32bit stdout: {stdout}");
        Console.WriteLine($"32bit err: {stderr}");

        Assert.IsTrue(string.IsNullOrEmpty(stderr),
            $"There was some error in process run: {stderr}");
    }
}
