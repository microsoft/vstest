// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Utilities;
    using Microsoft.TestPlatform.Build.Resources;

    public static class TestTaskExtensions
    {

        public static string CreateCommandLineArguments(this ITestTask task)
        {
            var isConsoleLoggerSpecifiedByUser = false;
            var isCollectCodeCoverageEnabled = false;
            var isRunSettingsEnabled = false;

            var builder = new CommandLineBuilder();
            builder.AppendSwitch("exec");
            if (task.VSTestConsolePath != null && !string.IsNullOrEmpty(task.VSTestConsolePath.ItemSpec))
            {
                builder.AppendSwitchIfNotNull("", task.VSTestConsolePath);
            }
            else
            {
                builder.AppendSwitch("vstest.console.dll");
            }

            // TODO log arguments in task
            if (!string.IsNullOrEmpty(task.VSTestSetting))
            {
                isRunSettingsEnabled = true;
                builder.AppendSwitchIfNotNull("--settings:", task.VSTestSetting);
            }

            if (task.VSTestTestAdapterPath != null && task.VSTestTestAdapterPath.Any())
            {
                foreach (var arg in task.VSTestTestAdapterPath)
                {
                    builder.AppendSwitchIfNotNull("--testAdapterPath:", arg);
                }
            }

            if (!string.IsNullOrEmpty(task.VSTestFramework))
            {
                builder.AppendSwitchIfNotNull("--framework:", task.VSTestFramework);
            }

            // vstest.console only support x86 and x64 for argument platform
            if (!string.IsNullOrEmpty(task.VSTestPlatform) && !task.VSTestPlatform.Contains("AnyCPU"))
            {
                builder.AppendSwitchIfNotNull("--platform:", task.VSTestPlatform);
            }

            if (!string.IsNullOrEmpty(task.VSTestTestCaseFilter))
            {
                builder.AppendSwitchIfNotNull("--testCaseFilter:", task.VSTestTestCaseFilter);
            }

            if (task.VSTestLogger != null && task.VSTestLogger.Any())
            {
                foreach (var arg in task.VSTestLogger)
                {
                    builder.AppendSwitchIfNotNull("--logger:", arg);

                    if (arg.StartsWith("console", StringComparison.OrdinalIgnoreCase))
                    {
                        isConsoleLoggerSpecifiedByUser = true;
                    }
                }
            }

            if (task.VSTestResultsDirectory != null && !string.IsNullOrEmpty(task.VSTestResultsDirectory.ItemSpec))
            {
                builder.AppendSwitchIfNotNull("--resultsDirectory:", task.VSTestResultsDirectory);
            }

            if (task.VSTestListTests)
            {
                builder.AppendSwitch("--listTests");
            }

            if (!string.IsNullOrEmpty(task.VSTestDiag))
            {
                builder.AppendSwitchIfNotNull("--Diag:", task.VSTestDiag);
            }

            if (task.TestFileFullPath != null)
            {
                builder.AppendFileNameIfNotNull(task.TestFileFullPath);
            }

            // Console logger was not specified by user, but verbosity was, hence add default console logger with verbosity as specified
            if (!string.IsNullOrWhiteSpace(task.VSTestVerbosity) && !isConsoleLoggerSpecifiedByUser)
            {
                var normalTestLogging = new List<string>() { "n", "normal", "d", "detailed", "diag", "diagnostic" };
                var quietTestLogging = new List<string>() { "q", "quiet" };

                string vsTestVerbosity = "minimal";
                if (normalTestLogging.Contains(task.VSTestVerbosity, StringComparer.InvariantCultureIgnoreCase))
                {
                    vsTestVerbosity = "normal";
                }
                else if (quietTestLogging.Contains(task.VSTestVerbosity, StringComparer.InvariantCultureIgnoreCase))
                {
                    vsTestVerbosity = "quiet";
                }

                builder.AppendSwitchUnquotedIfNotNull("--logger:", $"Console;Verbosity={vsTestVerbosity}");
            }

            if (task.VSTestBlame)
            {
                var dumpArgs = new List<string>();
                if (task.VSTestBlameCrash || task.VSTestBlameHang)
                {
                    if (task.VSTestBlameCrash)
                    {
                        dumpArgs.Add("CollectDump");

                        if (task.VSTestBlameCrashCollectAlways)
                        {
                            dumpArgs.Add($"CollectAlways={task.VSTestBlameCrashCollectAlways}");
                        }

                        if (!string.IsNullOrEmpty(task.VSTestBlameCrashDumpType))
                        {
                            dumpArgs.Add($"DumpType={task.VSTestBlameCrashDumpType}");
                        }
                    }

                    if (task.VSTestBlameHang)
                    {
                        dumpArgs.Add("CollectHangDump");

                        if (!string.IsNullOrEmpty(task.VSTestBlameHangDumpType))
                        {
                            dumpArgs.Add($"HangDumpType={task.VSTestBlameHangDumpType}");
                        }

                        if (!string.IsNullOrEmpty(task.VSTestBlameHangTimeout))
                        {
                            dumpArgs.Add($"TestTimeout={task.VSTestBlameHangTimeout}");
                        }
                    }
                }

                if (dumpArgs.Any())
                {
                    builder.AppendSwitchIfNotNull("--Blame:", string.Join(";", dumpArgs));
                }
                else
                {
                    builder.AppendSwitch("--Blame");
                }
            }

            if (task.VSTestCollect != null && task.VSTestCollect.Any())
            {
                foreach (var arg in task.VSTestCollect)
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
                if (task.VSTestTraceDataCollectorDirectoryPath != null && !string.IsNullOrEmpty(task.VSTestTraceDataCollectorDirectoryPath.ItemSpec))
                {
                    builder.AppendSwitchIfNotNull("--testAdapterPath:", task.VSTestTraceDataCollectorDirectoryPath);
                }
                else
                {
                    if (isCollectCodeCoverageEnabled)
                    {
                        // Not showing message in runsettings scenario, because we are not sure that code coverage is enabled.
                        // User might be using older Microsoft.NET.Test.Sdk which don't have CodeCoverage infra.
                        task.Log.LogWarning(Resources.UpdateTestSdkForCollectingCodeCoverage);
                    }
                }
            }

            if (task.VSTestNoLogo)
            {
                builder.AppendSwitch("--nologo");
            }

            // VSTestCLIRunSettings should be last argument as vstest.console ignore options after "--"(CLIRunSettings option).
            if (task.VSTestCLIRunSettings != null && task.VSTestCLIRunSettings.Any())
            {
                builder.AppendSwitch("--");

                foreach (var arg in task.VSTestCLIRunSettings)
                {
                    builder.AppendSwitchIfNotNull(string.Empty, arg);
                }
            }

            return builder.ToString();
        }
    }
}
