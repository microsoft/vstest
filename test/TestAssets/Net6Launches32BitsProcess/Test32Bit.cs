﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            RedirectStandardOutput = true
        };

        process.Start();
        var stderr = process.StandardError.ReadToEnd();
        var stdout = process.StandardOutput.ReadToEnd();

        Console.WriteLine($"32bit stdout: {stdout}");
        Console.WriteLine($"32bit err: {stderr}");

        Assert.IsTrue(string.IsNullOrEmpty(stderr),
            $"There was some error in process run: {stderr}");
        Assert.IsTrue(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")),
            "Env var DOTNET_ROOT was found.");
        Assert.IsTrue(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)")),
            "Env var DOTNET_ROOT(x86) was found.");
        Assert.IsTrue(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT_X86")),
            "Env var DOTNET_ROOT_X86 was found.");
        Assert.IsFalse(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT_X64")),
            "Env var DOTNET_ROOT_X64 was not found.");
    }
}
