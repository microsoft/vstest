// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class VSTestForwardingApp
    {
        private const string hostExe = "dotnet";
        private const string vsTestAppName = "vstest.console.dll";
        private readonly List<string> allArgs = new List<string>();

        private bool traceEnabled;

        public VSTestForwardingApp(IEnumerable<string> argsToForward)
        {
            this.allArgs.Add("exec");

            // Ensure that path to vstest.console is whitespace friendly. User may install
            // dotnet-cli to any folder containing whitespace (e.g. VS installs to program files).
            // Arguments are already whitespace friendly.
            this.allArgs.Add("\"" + GetVSTestExePath() + "\"");
            this.allArgs.AddRange(argsToForward);

            var traceEnabledValue = Environment.GetEnvironmentVariable("VSTEST_TRACE_BUILD");
            this.traceEnabled = !string.IsNullOrEmpty(traceEnabledValue) && traceEnabledValue.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        public int Execute()
        {
            var processInfo = new ProcessStartInfo
                                  {
                                      FileName = hostExe,
                                      Arguments = string.Join(" ", this.allArgs),
                                      UseShellExecute = false,
                                      CreateNoWindow = true,
                                      RedirectStandardError = true,
                                      RedirectStandardOutput = true
                                  };

            this.Trace("VSTest: Starting vstest.console...");
            this.Trace("VSTest: Arguments: " + processInfo.Arguments);

            using (var process = new Process { StartInfo = processInfo })
            {
                process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                this.Trace("VSTest: Exit code: " + process.ExitCode);
                return process.ExitCode;
            }
        }

        private static string GetVSTestExePath()
        {
            return Path.Combine(AppContext.BaseDirectory, vsTestAppName);
        }

        private void Trace(string message)
        {
            if (this.traceEnabled)
            {
                Console.WriteLine(message);
            }
        }
    }
}
