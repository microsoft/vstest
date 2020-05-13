// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.TestPlatform.Build.Resources;

    public class VSTestTask : ToolTask
    {

        [Required]
        //TODO: rename, as relative paths are allowed?
        public ITaskItem TestFileFullPath
        {
            get;
            set;
        }

        public string VSTestSetting
        {
            get;
            set;
        }

        public ITaskItem[] VSTestTestAdapterPath
        {
            get;
            set;
        }

        public string VSTestFramework
        {
            get;
            set;
        }

        public string VSTestPlatform
        {
            get;
            set;
        }

        public string VSTestTestCaseFilter
        {
            get;
            set;
        }
        public string[] VSTestLogger
        {
            get;
            set;
        }

        public bool VSTestListTests
        {
            get;
            set;
        }

        public string VSTestDiag
        {
            get;
            set;
        }

        public string[] VSTestCLIRunSettings
        {
            get;
            set;
        }

        [Required]
        public ITaskItem VSTestConsolePath
        {
            get;
            set;
        }

        public ITaskItem VSTestResultsDirectory
        {
            get;
            set;
        }

        public string VSTestVerbosity
        {
            get;
            set;
        }

        public string[] VSTestCollect
        {
            get;
            set;
        }

        public bool VSTestBlame
        {
            get;
            set;
        }

        public bool VSTestBlameCrash
        {
            get;
            set;
        }

        public string VSTestBlameCrashDumpType
        {
            get;
            set;
        }

        public bool VSTestBlameCrashCollectAlways
        {
            get;
            set;
        }

        public bool VSTestBlameHang
        {
            get;
            set;
        }

        public string VSTestBlameHangDumpType
        {
            get;
            set;
        }

        public string VSTestBlameHangTimeout
        {
            get;
            set;
        }

        public ITaskItem VSTestTraceDataCollectorDirectoryPath
        {
            get;
            set;
        }

        public bool VSTestNoLogo
        {
            get;
            set;
        }

        protected override string ToolName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "dotnet.exe";
                else
                    return "dotnet";
            }
        }

        public VSTestTask()
        {
            this.LogStandardErrorAsError = true;
            this.StandardOutputImportance = "Normal";
        }

        protected override string GenerateCommandLineCommands()
        {
            var isConsoleLoggerSpecifiedByUser = false;
            var isCollectCodeCoverageEnabled = false;
            var isRunSettingsEnabled = false;

            var builder = new CommandLineBuilder();
            builder.AppendSwitch("exec");
            builder.AppendSwitchIfNotNull("", this.VSTestConsolePath);

            if (this.VSTestSetting != null)
            {
                isRunSettingsEnabled = true;
                builder.AppendSwitchIfNotNull("--settings:", this.VSTestSetting);
            }

            if (this.VSTestTestAdapterPath != null && this.VSTestTestAdapterPath.Any())
            {
                foreach (var arg in this.VSTestTestAdapterPath)
                {
                    builder.AppendSwitchIfNotNull("--testAdapterPath:", arg);
                }
            }

            if (!string.IsNullOrEmpty(this.VSTestFramework))
            {
                builder.AppendSwitchIfNotNull("--framework:", this.VSTestFramework);
            }

            // vstest.console only support x86 and x64 for argument platform
            if (!string.IsNullOrEmpty(this.VSTestPlatform) && !this.VSTestPlatform.Contains("AnyCPU"))
            {
                builder.AppendSwitchIfNotNull("--platform:", this.VSTestPlatform);
            }

            if (!string.IsNullOrEmpty(this.VSTestTestCaseFilter))
            {
                builder.AppendSwitchIfNotNull("--testCaseFilter:", this.VSTestTestCaseFilter);
            }

            if (this.VSTestLogger != null && this.VSTestLogger.Length > 0)
            {
                foreach (var arg in this.VSTestLogger)
                {
                    builder.AppendSwitchIfNotNull("--logger:", arg);

                    if (arg.StartsWith("console", StringComparison.OrdinalIgnoreCase))
                    {
                        isConsoleLoggerSpecifiedByUser = true;
                    }
                }
            }

            if (this.VSTestResultsDirectory != null && !string.IsNullOrEmpty(this.VSTestResultsDirectory.ItemSpec))
            {
                builder.AppendSwitchIfNotNull("--resultsDirectory:", this.VSTestResultsDirectory);
            }

            if (this.VSTestListTests)
            {
                builder.AppendSwitch("--listTests");
            }

            if (!string.IsNullOrEmpty(this.VSTestDiag))
            {
                builder.AppendSwitchIfNotNull("--Diag:", this.VSTestDiag);
            }

            if (this.TestFileFullPath != null)
            {
                builder.AppendFileNameIfNotNull(this.TestFileFullPath);
            }

            // Console logger was not specified by user, but verbosity was, hence add default console logger with verbosity as specified
            if (!string.IsNullOrWhiteSpace(this.VSTestVerbosity) && !isConsoleLoggerSpecifiedByUser)
            {
                var normalTestLogging = new List<string>() { "n", "normal", "d", "detailed", "diag", "diagnostic" };
                var quietTestLogging = new List<string>() { "q", "quiet" };

                string vsTestVerbosity = "minimal";
                if (normalTestLogging.Contains(this.VSTestVerbosity, StringComparer.InvariantCultureIgnoreCase))
                {
                    vsTestVerbosity = "normal";
                }
                else if (quietTestLogging.Contains(this.VSTestVerbosity, StringComparer.InvariantCultureIgnoreCase))
                {
                    vsTestVerbosity = "quiet";
                }

                builder.AppendSwitchUnquotedIfNotNull("--logger:", $"Console;Verbosity={vsTestVerbosity}");
            }

            if (this.VSTestBlame)
            {
                var dumpArgs = new List<string>();
                if (this.VSTestBlameCrash || this.VSTestBlameHang)
                {
                    if (this.VSTestBlameCrash)
                    {
                        dumpArgs.Add("CollectDump");

                        if (this.VSTestBlameCrashCollectAlways)
                        {
                            dumpArgs.Add($"CollectAlways={this.VSTestBlameCrashCollectAlways}");
                        }

                        if (!string.IsNullOrEmpty(this.VSTestBlameCrashDumpType))
                        {
                            dumpArgs.Add($"DumpType={this.VSTestBlameCrashDumpType}");
                        }
                    }

                    if (this.VSTestBlameHang)
                    {
                        dumpArgs.Add("CollectHangDump");

                        if (!string.IsNullOrEmpty(this.VSTestBlameHangDumpType))
                        {
                            dumpArgs.Add($"HangDumpType={this.VSTestBlameHangDumpType}");
                        }

                        if (!string.IsNullOrEmpty(this.VSTestBlameHangTimeout))
                        {
                            dumpArgs.Add($"TestTimeout={this.VSTestBlameHangTimeout}");
                        }
                    }
                }

                if (dumpArgs.Any())
                {
                    builder.AppendSwitchIfNotNull("--Blame:", string.Join(";", dumpArgs));
                } else
                {
                    builder.AppendSwitch("--Blame");
                }
            }

            if (this.VSTestCollect != null && this.VSTestCollect.Length > 0)
            {
                foreach (var arg in this.VSTestCollect)
                {
                    if (arg.Equals("Code Coverage", StringComparison.OrdinalIgnoreCase))
                    {
                        isCollectCodeCoverageEnabled = true;
                    }

                    builder.AppendSwitchIfNotNull("--collect:", arg);
                }
            }

            if (isCollectCodeCoverageEnabled || isRunSettingsEnabled)
            {
                // Pass TraceDataCollector path to vstest.console as TestAdapterPath if --collect "Code Coverage"
                // or --settings (User can enable code coverage from runsettings) option given.
                // Not parsing the runsettings for two reason:
                //    1. To keep no knowledge of runsettings structure in VSTestTask.
                //    2. Impact of adding adapter path always is minimal. (worst case: loads additional data collector assembly in datacollector process.)
                // This is required due to currently trace datacollector not ships with dotnet sdk, can be remove once we have
                // go code coverage x-plat.
                if (this.VSTestTraceDataCollectorDirectoryPath != null && !string.IsNullOrEmpty(this.VSTestTraceDataCollectorDirectoryPath.ItemSpec))
                {
                    builder.AppendSwitchIfNotNull("--testAdapterPath:", this.VSTestTraceDataCollectorDirectoryPath);
                } else
                {
                    if (isCollectCodeCoverageEnabled)
                    {
                        // Not showing message in runsettings scenario, because we are not sure that code coverage is enabled.
                        // User might be using older Microsoft.NET.Test.Sdk which don't have CodeCoverage infra.
                        this.Log.LogWarning(Resources.UpdateTestSdkForCollectingCodeCoverage);
                    }
                }
            }

            if (this.VSTestNoLogo)
            {
                builder.AppendSwitch("--nologo");
            }

            // VSTestCLIRunSettings should be last argument as vstest.console ignore options after "--"(CLIRunSettings option).
            if (this.VSTestCLIRunSettings != null && this.VSTestCLIRunSettings.Any())
            {
                builder.AppendSwitch("--");

                foreach (var arg in this.VSTestCLIRunSettings)
                {
                    builder.AppendSwitchIfNotNull(string.Empty, arg);
                }
            }

            return builder.ToString();
        }

        protected override string GenerateFullPathToTool()
        {
            string path = null;

            if (!string.IsNullOrEmpty(ToolPath))
            {
                path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(ToolPath)), ToolExe);
            } else
            {
                //TODO: https://github.com/dotnet/sdk/issues/20 Need to get the dotnet path from MSBuild
                if (File.Exists(ToolExe))
                {
                    path = Path.GetFullPath(ToolExe);
                } else
                {
                    var values = Environment.GetEnvironmentVariable("PATH");
                    foreach (var p in values.Split(Path.PathSeparator))
                    {
                        var fullPath = Path.Combine(p, ToolExe);
                        if (File.Exists(fullPath))
                            path = fullPath;
                    }
                }
            }

            return path;
        }

        /// To be used by unit tests only
        internal protected string CreateCommandLineArguments()
        {
            return this.GenerateCommandLineCommands();
        }
    }
}
