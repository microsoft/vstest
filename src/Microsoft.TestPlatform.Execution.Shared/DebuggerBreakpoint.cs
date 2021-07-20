using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.Execution
{
    internal static class DebuggerBreakpoint
    {
        internal static void WaitForDebugger(string environmentVariable)
        {
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

                Debugger.Break();
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

                DebugBreak();
            }
        }

        // Native APIs for enabling native debugging.
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        internal static extern void DebugBreak();
    }
}
