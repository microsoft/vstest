// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace zombie_child_process;

/// <summary>
/// This test simulates the Selenium WebDriver scenario where testhost starts a child process
/// (e.g., Edge Driver) that outlives testhost. The child inherits testhost's stderr pipe handle
/// (UseShellExecute=false on .NET Core). After testhost exits, vstest.console's parameterless
/// WaitForExit() would block indefinitely waiting for the pipe EOF that never comes because the
/// child still holds the handle.
///
/// The ProcessHelper's WaitForExitAsync(cts) timeout protects against this hang.
/// This test exists to verify that protection does not regress.
/// </summary>
[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestPassesWhileChildHoldsStderrPipe()
    {
        // Resolve path to sleeping-child exe using the same output structure.
        // Our DLL: artifacts/bin/TestAssets/zombie-child-process/{config}/{tfm}/zombie-child-process.dll
        // Child:   artifacts/bin/TestAssets/sleeping-child/{config}/{tfm}/sleeping-child{.exe}
        var assemblyDir = Path.GetDirectoryName(typeof(UnitTest1).Assembly.Location)!;
        var testAssetsDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));
        var configDir = new DirectoryInfo(assemblyDir).Parent!.Name;
        var tfmDir = new DirectoryInfo(assemblyDir).Name;

        var childName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "sleeping-child.exe"
            : "sleeping-child";
        var childPath = Path.Combine(testAssetsDir, "sleeping-child", configDir, tfmDir, childName);

        if (!File.Exists(childPath))
        {
            Assert.Inconclusive($"sleeping-child not found at: {childPath}");
        }

        // Start the child process with UseShellExecute=false so it INHERITS testhost's
        // stdio pipe handles (the default on .NET Core). This is the key mechanism that
        // causes the "zombie pipe" issue — the child holds the stderr pipe handle open
        // even after testhost exits.
        var psi = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? childPath
                : "dotnet",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.Arguments = $"{childPath} 30000";
        }
        else
        {
            psi.Arguments = "30000";
        }

        var child = Process.Start(psi);
        Assert.IsNotNull(child, "Failed to start sleeping-child process.");

        // Write the child PID to stderr so the integration test can verify it was started
        // and clean it up afterward.
        Console.Error.WriteLine($"ZOMBIE_CHILD_PID={child.Id}");

        // Pass immediately — do NOT wait for the child.
        // This simulates a Selenium test that starts Edge Driver and returns.
        // Testhost will exit, but the child keeps running, holding the pipe.
    }
}
