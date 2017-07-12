// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using Microsoft.TestPlatform.Build.Utils;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Trace;

    public class VSTestForwardingApp
    {
        private const string hostExe = "dotnet";
        private readonly List<string> allArgs = new List<string>();
        private int activeProcessId;

        public VSTestForwardingApp(string vsTestExePath, IEnumerable<string> argsToForward)
        {
            this.allArgs.Add("exec");

            // Ensure that path to vstest.console is whitespace friendly. User may install
            // dotnet-cli to any folder containing whitespace (e.g. VS installs to program files).
            // Arguments are already whitespace friendly.
            this.allArgs.Add(ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(vsTestExePath));
            this.allArgs.AddRange(argsToForward);
        }

        public int Execute()
        {
            var processInfo = new ProcessStartInfo
                                  {
                                      FileName = hostExe,
                                      Arguments = string.Join(" ", this.allArgs),
                                      UseShellExecute = false,
                                  };

            Tracing.Trace("VSTest: Starting vstest.console...");
            Tracing.Trace("VSTest: Arguments: " + processInfo.FileName + " " + processInfo.Arguments);

            using (var activeProcess = new Process { StartInfo = processInfo })
            {
                activeProcess.Start();
                this.activeProcessId = activeProcess.Id;

                activeProcess.WaitForExit();
                Tracing.Trace("VSTest: Exit code: " + activeProcess.ExitCode);
                return activeProcess.ExitCode;
            }
        }

        public void Cancel()
        {
            try
            {
                Process.GetProcessById(activeProcessId).Kill();
            }
            catch(ArgumentException ex)
            {
                Tracing.Trace(string.Format("VSTest: Killing process throws ArgumentException with the following message {0}. It may be that process is not running", ex.Message));
            }
        }
    }
}
