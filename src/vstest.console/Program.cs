// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Main entry point for the command line runner.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point. Hands off execution to the executor class.
        /// </summary>
        /// <param name="args">Arguments provided on the command line.</param>
        /// <returns>0 if everything was successful and 1 otherwise.</returns>
        public static int Main(string[] args)
        {
            var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_RUNNER_DEBUG");
            if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
            {
                ConsoleOutput.Instance.WriteLine("Waiting for debugger attach...", OutputLevel.Information);

                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                ConsoleOutput.Instance.WriteLine(
                    string.Format("Process Id: {0}, Name: {1}", currentProcess.Id, currentProcess.ProcessName),
                    OutputLevel.Information);

                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Threading.Thread.Sleep(1000);
                }

                System.Diagnostics.Debugger.Break();
            }

            return new Executor(ConsoleOutput.Instance).Execute(args);
        }
    }
}
