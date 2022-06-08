// Copyright (c) Microsoft Corporation. All rights reserved.
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

        Assert.IsTrue(string.IsNullOrEmpty(stderr));
    }
}
