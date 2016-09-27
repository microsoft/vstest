// Copyright (c) Microsoft. All rights reserved.

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

        public VSTestForwardingApp(IEnumerable<string> argsToForward)
        {
            this.allArgs.Add("exec");
            this.allArgs.Add(this.GetVSTestExePath());
            this.allArgs.AddRange(argsToForward);
        }

        public int Execute()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = hostExe,
                Arguments = string.Join(" ", this.allArgs), // ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(allArgs),
                UseShellExecute = false
            };

            var process = new Process { StartInfo = processInfo };

            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }

        private string GetVSTestExePath()
        {
            return Path.Combine(AppContext.BaseDirectory, vsTestAppName);
        }
    }


}
