﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Execution;

using System;
using System.Diagnostics;
using System.IO;
#if !NETCOREAPP1_0 && DEBUG
using System.Reflection;
#endif
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.Utilities;

internal static class DebuggerBreakpoint
{
    internal static void AttachVisualStudioDebugger(string environmentVariable)
    {
#if NETCOREAPP1_0 || !DEBUG
        return;
#else
        if (string.IsNullOrWhiteSpace(environmentVariable))
        {
            throw new ArgumentException($"'{nameof(environmentVariable)}' cannot be null or whitespace.", nameof(environmentVariable));
        }

        if (Debugger.IsAttached)
            return;

        var debugEnabled = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrEmpty(debugEnabled) && !debugEnabled.Equals("0", StringComparison.Ordinal))
        {
            int? vsPid = null;
            if (int.TryParse(debugEnabled, out int pid))
            {
                // The option is used to both enable and disable attaching (0 and 1)
                // and providing custom vs pid (any number higher than 1)
                vsPid = pid <= 1 ? null : (int?)pid;
            }

            if (vsPid == null)
            {
                ConsoleOutput.Instance.WriteLine("Attaching Visual Studio, either a parent or the one that was started first... To specify a VS instance to use, use the PID in the option, instead of 1. No breakpoints are automatically set.", OutputLevel.Information);
            }
            else
            {
                ConsoleOutput.Instance.WriteLine($"Attaching Visual Studio with PID {vsPid} to the process '{Process.GetCurrentProcess().ProcessName}({Process.GetCurrentProcess().Id})'... No breakpoints are automatically set.", OutputLevel.Information);
            }

            AttachVs(Process.GetCurrentProcess(), vsPid);

            Break();
        }
#endif
    }

    private static bool AttachVs(Process process, int? vsPid)
    {
#if NETCOREAPP1_0 || !DEBUG
        return false;
#else
        // The way we attach VS is not compatible with .NET Core 2.1 and .NET Core 3.1, but works in .NET Framework and .NET.
        // We could call the library code directly here for .NET, and .NET Framework, but then we would also need to package it
        // together with testhost. So instead we always run the executable, and pass path to it using env variable.

        const string env = "VSTEST_DEBUG_ATTACHVS_PATH";
        var vsAttachPath = Environment.GetEnvironmentVariable(env) ?? FindAttachVs();

        // Always set it so we propagate it to child processes even if it was not previously set.
        Environment.SetEnvironmentVariable(env, vsAttachPath);

        if (vsAttachPath == null)
        {
            throw new InvalidOperationException($"Cannot find AttachVS.exe tool.");
        }

        if (!File.Exists(vsAttachPath))
        {
            throw new InvalidOperationException($"Cannot start tool, path {vsAttachPath} does not exist.");
        }
        var attachVsProcess = Process.Start(vsAttachPath, $"{process.Id} {vsPid}");
        attachVsProcess.WaitForExit();

        return attachVsProcess.ExitCode == 0;
#endif
    }

    private static string FindAttachVs()
    {
#if NETCOREAPP1_0 || !DEBUG
        return null;
#else

        var fromPath = FindOnPath("AttachVS.exe");
        if (fromPath != null)
        {
            return fromPath;
        }

        var parent = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        while (parent != null)
        {
            var path = Path.Combine(parent, @"src\AttachVS\bin\Debug\net472\AttachVS.exe");
            if (File.Exists(path))
            {
                return path;
            }

            parent = Path.GetDirectoryName(parent);
        }

        return parent;
#endif
    }

    private static string FindOnPath(string exeName)
    {
        var paths = Environment.GetEnvironmentVariable("PATH").Split(';');
        foreach (var p in paths)
        {
            var path = Path.Combine(p, exeName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    internal static void WaitForDebugger(string environmentVariable)
    {
        if (Debugger.IsAttached)
            return;

        if (string.IsNullOrWhiteSpace(environmentVariable))
        {
            throw new ArgumentException($"'{nameof(environmentVariable)}' cannot be null or whitespace.", nameof(environmentVariable));
        }

        var debugEnabled = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
        {
            ConsoleOutput.Instance.WriteLine("Waiting for debugger attach...", OutputLevel.Information);

            var currentProcess = Process.GetCurrentProcess();
            ConsoleOutput.Instance.WriteLine(
                string.Format("Process Id: {0}, Name: {1}", currentProcess.Id, currentProcess.ProcessName),
                OutputLevel.Information);

            while (!Debugger.IsAttached)
            {
                Thread.Sleep(1000);
            }

            Break();
        }
    }

    internal static void WaitForNativeDebugger(string environmentVariable)
    {
        if (string.IsNullOrWhiteSpace(environmentVariable))
        {
            throw new ArgumentException($"'{nameof(environmentVariable)}' cannot be null or whitespace.", nameof(environmentVariable));
        }

        // Check if native debugging is enabled and OS is windows.
        var nativeDebugEnabled = Environment.GetEnvironmentVariable(environmentVariable);

        if (!string.IsNullOrEmpty(nativeDebugEnabled) && nativeDebugEnabled.Equals("1", StringComparison.Ordinal)
                                                      && new PlatformEnvironment().OperatingSystem.Equals(PlatformOperatingSystem.Windows))
        {
            while (!IsDebuggerPresent())
            {
                Task.Delay(1000).Wait();
            }

            BreakNative();
        }
    }

    private static void Break()
    {
        if (ShouldNotBreak())
        {
            return;
        }

        Debugger.Break();
    }

    private static bool ShouldNotBreak()
    {
        return Environment.GetEnvironmentVariable("VSTEST_DEBUG_NOBP")?.Equals("1") ?? false;
    }

    private static void BreakNative()
    {
        if (ShouldNotBreak())
        {
            return;
        }

        DebugBreak();
    }

    // Native APIs for enabling native debugging.
    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    internal static extern void DebugBreak();
}
