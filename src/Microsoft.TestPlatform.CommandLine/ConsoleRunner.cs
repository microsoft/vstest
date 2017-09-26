// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    public static class ConsoleRunner
    {
        public static int Start(string[] args)
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
