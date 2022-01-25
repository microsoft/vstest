// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Resources;
using Utils;

using Trace;

public class VsTestTask : Task, ICancelableTask
{
    // The process which is invoking vstest.console
    private VsTestForwardingApp _vsTestForwardingApp;

    private const string VsTestAppName = "vstest.console.dll";
    private const string CodeCovergaeString = "Code Coverage";

    public string TestFileFullPath
    {
        get;
        set;
    }

    public string VsTestSetting
    {
        get;
        set;
    }

    public string[] VsTestTestAdapterPath
    {
        get;
        set;
    }

    public string VsTestFramework
    {
        get;
        set;
    }

    public string VsTestPlatform
    {
        get;
        set;
    }

    public string VsTestTestCaseFilter
    {
        get;
        set;
    }
    public string[] VsTestLogger
    {
        get;
        set;
    }

    public string VsTestListTests
    {
        get;
        set;
    }

    public string VsTestDiag
    {
        get;
        set;
    }

    public string[] VsTestCliRunSettings
    {
        get;
        set;
    }

    [Required]
    public string VsTestConsolePath
    {
        get;
        set;
    }

    public string VsTestResultsDirectory
    {
        get;
        set;
    }

    public string VsTestVerbosity
    {
        get;
        set;
    }

    public string[] VsTestCollect
    {
        get;
        set;
    }

    public string VsTestBlame
    {
        get;
        set;
    }

    public string VsTestBlameCrash
    {
        get;
        set;
    }

    public string VsTestBlameCrashDumpType
    {
        get;
        set;
    }

    public string VsTestBlameCrashCollectAlways
    {
        get;
        set;
    }

    public string VsTestBlameHang
    {
        get;
        set;
    }

    public string VsTestBlameHangDumpType
    {
        get;
        set;
    }
    public string VsTestBlameHangTimeout
    {
        get;
        set;
    }

    public string VsTestTraceDataCollectorDirectoryPath
    {
        get;
        set;
    }

    public string VsTestNoLogo
    {
        get;
        set;
    }

    public override bool Execute()
    {
        var traceEnabledValue = Environment.GetEnvironmentVariable("VSTEST_BUILD_TRACE");
        Tracing.TraceEnabled = !string.IsNullOrEmpty(traceEnabledValue) && traceEnabledValue.Equals("1", StringComparison.OrdinalIgnoreCase);

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

        _vsTestForwardingApp = new VsTestForwardingApp(VsTestConsolePath, CreateArgument());
        if (!string.IsNullOrEmpty(VsTestFramework))
        {
            Console.WriteLine(Resources.TestRunningSummary, TestFileFullPath, VsTestFramework);
        }

        return _vsTestForwardingApp.Execute() == 0;
    }

    public void Cancel()
    {
        Tracing.Trace("VSTest: Killing the process...");
        _vsTestForwardingApp.Cancel();
    }

    internal IEnumerable<string> CreateArgument()
    {
        var allArgs = AddArgs();

        // VSTestCLIRunSettings should be last argument in allArgs as vstest.console ignore options after "--"(CLIRunSettings option).
        AddCliRunSettingsArgs(allArgs);

        return allArgs;
    }

    private void AddCliRunSettingsArgs(List<string> allArgs)
    {
        if (VsTestCliRunSettings != null && VsTestCliRunSettings.Length > 0)
        {
            allArgs.Add("--");
            foreach (var arg in VsTestCliRunSettings)
            {
                allArgs.Add(ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(arg));
            }
        }
    }

    private List<string> AddArgs()
    {
        var isConsoleLoggerSpecifiedByUser = false;
        var isCollectCodeCoverageEnabled = false;
        var isRunSettingsEnabled = false;
        var allArgs = new List<string>();

        // TODO log arguments in task
        if (!string.IsNullOrEmpty(VsTestSetting))
        {
            isRunSettingsEnabled = true;
            allArgs.Add("--settings:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(VsTestSetting));
        }

        if (VsTestTestAdapterPath != null && VsTestTestAdapterPath.Length > 0)
        {
            foreach (var arg in VsTestTestAdapterPath)
            {
                allArgs.Add("--testAdapterPath:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(arg));
            }
        }

        if (!string.IsNullOrEmpty(VsTestFramework))
        {
            allArgs.Add("--framework:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(VsTestFramework));
        }

        // vstest.console only support x86 and x64 for argument platform
        if (!string.IsNullOrEmpty(VsTestPlatform) && !VsTestPlatform.Contains("AnyCPU"))
        {
            allArgs.Add("--platform:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(VsTestPlatform));
        }

        if (!string.IsNullOrEmpty(VsTestTestCaseFilter))
        {
            allArgs.Add("--testCaseFilter:" +
                        ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(VsTestTestCaseFilter));
        }

        if (VsTestLogger != null && VsTestLogger.Length > 0)
        {
            foreach (var arg in VsTestLogger)
            {
                allArgs.Add("--logger:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(arg));

                if (arg.StartsWith("console", StringComparison.OrdinalIgnoreCase))
                {
                    isConsoleLoggerSpecifiedByUser = true;
                }
            }
        }

        if (!string.IsNullOrEmpty(VsTestResultsDirectory))
        {
            allArgs.Add("--resultsDirectory:" +
                        ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(VsTestResultsDirectory));
        }

        if (!string.IsNullOrEmpty(VsTestListTests))
        {
            allArgs.Add("--listTests");
        }

        if (!string.IsNullOrEmpty(VsTestDiag))
        {
            allArgs.Add("--Diag:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(VsTestDiag));
        }

        if (string.IsNullOrEmpty(TestFileFullPath))
        {
            Log.LogError("Test file path cannot be empty or null.");
        }
        else
        {
            allArgs.Add(ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(TestFileFullPath));
        }

        // Console logger was not specified by user, but verbosity was, hence add default console logger with verbosity as specified
        if (!string.IsNullOrWhiteSpace(VsTestVerbosity) && !isConsoleLoggerSpecifiedByUser)
        {
            var normalTestLogging = new List<string>() { "n", "normal", "d", "detailed", "diag", "diagnostic" };
            var quietTestLogging = new List<string>() { "q", "quiet" };

            string vsTestVerbosity = "minimal";
            if (normalTestLogging.Contains(VsTestVerbosity.ToLowerInvariant()))
            {
                vsTestVerbosity = "normal";
            }
            else if (quietTestLogging.Contains(VsTestVerbosity.ToLowerInvariant()))
            {
                vsTestVerbosity = "quiet";
            }

            allArgs.Add("--logger:Console;Verbosity=" + vsTestVerbosity);
        }

        var blameCrash = !string.IsNullOrEmpty(VsTestBlameCrash);
        var blameHang = !string.IsNullOrEmpty(VsTestBlameHang);
        if (!string.IsNullOrEmpty(VsTestBlame) || blameCrash || blameHang)
        {
            var blameArgs = "--Blame";

            var dumpArgs = new List<string>();
            if (blameCrash || blameHang)
            {
                if (blameCrash)
                {
                    dumpArgs.Add("CollectDump");
                    if (!string.IsNullOrEmpty(VsTestBlameCrashCollectAlways))
                    {
                        dumpArgs.Add($"CollectAlways={VsTestBlameCrashCollectAlways}");
                    }

                    if (!string.IsNullOrEmpty(VsTestBlameCrashDumpType))
                    {
                        dumpArgs.Add($"DumpType={VsTestBlameCrashDumpType}");
                    }
                }

                if (blameHang)
                {
                    dumpArgs.Add("CollectHangDump");

                    if (!string.IsNullOrEmpty(VsTestBlameHangDumpType))
                    {
                        dumpArgs.Add($"HangDumpType={VsTestBlameHangDumpType}");
                    }

                    if (!string.IsNullOrEmpty(VsTestBlameHangTimeout))
                    {
                        dumpArgs.Add($"TestTimeout={VsTestBlameHangTimeout}");
                    }
                }

                if (dumpArgs.Any())
                {
                    blameArgs += $":\"{string.Join(";", dumpArgs)}\"";
                }
            }

            allArgs.Add(blameArgs);
        }

        if (VsTestCollect != null && VsTestCollect.Length > 0)
        {
            foreach (var arg in VsTestCollect)
            {
                // For collecting code coverage, argument value can be either "Code Coverage" or "Code Coverage;a=b;c=d".
                // Split the argument with ';' and compare first token value.
                var tokens = arg.Split(';');

                if (arg.Equals(CodeCovergaeString, StringComparison.OrdinalIgnoreCase) ||
                    tokens[0].Equals(CodeCovergaeString, StringComparison.OrdinalIgnoreCase))
                {
                    isCollectCodeCoverageEnabled = true;
                }

                allArgs.Add("--collect:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(arg));
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
            if (!string.IsNullOrEmpty(VsTestTraceDataCollectorDirectoryPath))
            {
                allArgs.Add("--testAdapterPath:" +
                            ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(
                                VsTestTraceDataCollectorDirectoryPath));
            }
            else
            {
                if (isCollectCodeCoverageEnabled)
                {
                    // Not showing message in runsettings scenario, because we are not sure that code coverage is enabled.
                    // User might be using older Microsoft.NET.Test.Sdk which don't have CodeCoverage infra.
                    Console.WriteLine(Resources.UpdateTestSdkForCollectingCodeCoverage);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(VsTestNoLogo))
        {
            allArgs.Add("--nologo");
        }

        return allArgs;
    }
}