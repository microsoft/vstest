// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.TestPlatform.Build.Resources;
    using Microsoft.TestPlatform.Build.Trace;

    public class VSTestForwardingTask : Task, ITestTask
    {
        private const string toolExe = "dotnet";
        private int activeProcessId;

        public ITaskItem TestFileFullPath { get; set; }
        public string VSTestSetting { get; set; }
        public ITaskItem[] VSTestTestAdapterPath { get; set; }
        public string VSTestFramework { get; set; }
        public string VSTestPlatform { get; set; }
        public string VSTestTestCaseFilter { get; set; }
        public string[] VSTestLogger { get; set; }
        public bool VSTestListTests { get; set; }
        public string VSTestDiag { get; set; }
        public string[] VSTestCLIRunSettings { get; set; }
        [Required]
        public ITaskItem VSTestConsolePath { get; set; }
        public ITaskItem VSTestResultsDirectory { get; set; }
        public string VSTestVerbosity { get; set; }
        public string[] VSTestCollect { get; set; }
        public bool VSTestBlame { get; set; }
        public bool VSTestBlameCrash { get; set; }
        public string VSTestBlameCrashDumpType { get; set; }
        public bool VSTestBlameCrashCollectAlways { get; set; }
        public bool VSTestBlameHang { get; set; }
        public string VSTestBlameHangDumpType { get; set; }
        public string VSTestBlameHangTimeout { get; set; }
        public ITaskItem VSTestTraceDataCollectorDirectoryPath { get; set; }
        public bool VSTestNoLogo { get; set; }

        public override bool Execute()
        {
            var traceEnabledValue = Environment.GetEnvironmentVariable("VSTEST_BUILD_TRACE");
            Tracing.traceEnabled = !string.IsNullOrEmpty(traceEnabledValue) && traceEnabledValue.Equals("1", StringComparison.OrdinalIgnoreCase);

            var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_BUILD_DEBUG");
            if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
            {
                Console.WriteLine("Waiting for debugger attach...");

                var currentProcess = Process.GetCurrentProcess();
                Console.WriteLine(string.Format("Process Id: {0}, Name: {1}", currentProcess.Id, currentProcess.ProcessName));

                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                }

                Debugger.Break();
            }

            // Avoid logging "Task returned false but did not log an error." on test failure, because we don't
            // write MSBuild error. https://github.com/dotnet/msbuild/blob/51a1071f8871e0c93afbaf1b2ac2c9e59c7b6491/src/Framework/IBuildEngine7.cs#L12
            var allowfailureWithoutError = BuildEngine.GetType().GetProperty("AllowFailureWithoutError");
            allowfailureWithoutError?.SetValue(BuildEngine, true);

            var processInfo = new ProcessStartInfo
                                  {
                                      FileName = toolExe,
                                      Arguments = this.CreateCommandLineArguments(),
                                      UseShellExecute = false,
                                  };

            if (!string.IsNullOrEmpty(this.VSTestFramework))
            {
                Console.WriteLine(Resources.TestRunningSummary, this.TestFileFullPath, this.VSTestFramework);
            }

            Tracing.Trace("VSTest: Starting vstest.console...");
            Tracing.Trace("VSTest: Arguments: " + processInfo.FileName + " " + processInfo.Arguments);

            using (var activeProcess = new Process { StartInfo = processInfo })
            {
                activeProcess.Start();
                this.activeProcessId = activeProcess.Id;

                activeProcess.WaitForExit();
                Tracing.Trace("VSTest: Exit code: " + activeProcess.ExitCode);
                return activeProcess.ExitCode == 0;
            }
        }
        public void Cancel()
        {
            Tracing.Trace("VSTest: Killing the process...");
            try
            {
                Process.GetProcessById(activeProcessId).Kill();
            }
            catch (ArgumentException ex)
            {
                Tracing.Trace(string.Format("VSTest: Killing process throws ArgumentException with the following message {0}. It may be that process is not running", ex));
            }
        }
    }
}
