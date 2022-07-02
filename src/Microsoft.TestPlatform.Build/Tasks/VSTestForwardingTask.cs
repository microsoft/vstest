// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.TestPlatform.Build.Trace;

namespace Microsoft.TestPlatform.Build.Tasks;

public class VSTestForwardingTask : Task, ICancelableTask
{
    private int _activeProcessId;

    private const string DotnetExe = "dotnet";
    private const string VsTestAppName = "vstest.console.dll";
    private const string CodeCoverageString = "Code Coverage";

    public ITaskItem? TestFileFullPath { get; set; }
    public string? VSTestSetting { get; set; }
    public ITaskItem[]? VSTestTestAdapterPath { get; set; }
    public string? VSTestFramework { get; set; }
    public string? VSTestPlatform { get; set; }
    public string? VSTestTestCaseFilter { get; set; }
    public string[]? VSTestLogger { get; set; }
    public bool VSTestListTests { get; set; }
    public string? VSTestDiag { get; set; }
    public string[]? VSTestCLIRunSettings { get; set; }
    [Required]
    public ITaskItem? VSTestConsolePath { get; set; }
    public ITaskItem? VSTestResultsDirectory { get; set; }
    public string? VSTestVerbosity { get; set; }
    public string[]? VSTestCollect { get; set; }
    public bool VSTestBlame { get; set; }
    public bool VSTestBlameCrash { get; set; }
    public string? VSTestBlameCrashDumpType { get; set; }
    public bool VSTestBlameCrashCollectAlways { get; set; }
    public bool VSTestBlameHang { get; set; }
    public string? VSTestBlameHangDumpType { get; set; }
    public string? VSTestBlameHangTimeout { get; set; }
    public ITaskItem? VSTestTraceDataCollectorDirectoryPath { get; set; }
    public bool VSTestNoLogo { get; set; }
    public string? VSTestArtifactsProcessingMode { get; set; }
    public string? VSTestSessionCorrelationId { get; set; }

    public override bool Execute()
    {
        var traceEnabledValue = Environment.GetEnvironmentVariable("VSTEST_BUILD_TRACE");
        Tracing.traceEnabled = !traceEnabledValue.IsNullOrEmpty() && traceEnabledValue.Equals("1", StringComparison.OrdinalIgnoreCase);

        var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_BUILD_DEBUG");
        if (!debugEnabled.IsNullOrEmpty() && debugEnabled.Equals("1", StringComparison.Ordinal))
        {
            Console.WriteLine("Waiting for debugger attach...");

            var currentProcess = Process.GetCurrentProcess();
            Console.WriteLine($"Process Id: {currentProcess.Id}, Name: {currentProcess.ProcessName}");

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
            FileName = DotnetExe,
            Arguments = CreateArguments(),
            UseShellExecute = false,
        };

        if (!VSTestFramework.IsNullOrEmpty())
        {
            Console.WriteLine(Resources.Resources.TestRunningSummary, TestFileFullPath, VSTestFramework);
        }

        Tracing.Trace("VSTest: Starting vstest.console...");
        Tracing.Trace("VSTest: Arguments: " + processInfo.FileName + " " + processInfo.Arguments);

        using var activeProcess = new Process { StartInfo = processInfo };
        activeProcess.Start();
        _activeProcessId = activeProcess.Id;

        activeProcess.WaitForExit();
        Tracing.Trace("VSTest: Exit code: " + activeProcess.ExitCode);
        return activeProcess.ExitCode == 0;
    }

    public void Cancel()
    {
        Tracing.Trace("VSTest: Killing the process...");
        try
        {
            Process.GetProcessById(_activeProcessId).Kill();
        }
        catch (ArgumentException ex)
        {
            Tracing.Trace(string.Format("VSTest: Killing process throws ArgumentException with the following message {0}. It may be that process is not running", ex));
        }
    }

    internal string CreateArguments()
    {
        var builder = new CommandLineBuilder();
        builder.AppendSwitch("exec");
        if (VSTestConsolePath != null && !VSTestConsolePath.ItemSpec.IsNullOrEmpty())
        {
            builder.AppendSwitchIfNotNull("", VSTestConsolePath);
        }
        else
        {
            builder.AppendSwitch(VsTestAppName);
        }

        CreateCommandLineArguments(builder);

        // VSTestCLIRunSettings should be last argument in allArgs as vstest.console ignore options after "--"(CLIRunSettings option).
        AddCliRunSettingsArgs(builder);

        return builder.ToString();
    }

    private void AddCliRunSettingsArgs(CommandLineBuilder builder)
    {
        if (VSTestCLIRunSettings != null && VSTestCLIRunSettings.Any())
        {
            builder.AppendSwitch("--");
            foreach (var arg in VSTestCLIRunSettings)
            {
                builder.AppendSwitchIfNotNull(string.Empty, arg);
            }
        }
    }

    private void CreateCommandLineArguments(CommandLineBuilder builder)
    {
        var isConsoleLoggerSpecifiedByUser = false;
        var isCollectCodeCoverageEnabled = false;
        var isRunSettingsEnabled = false;

        // TODO log arguments in task
        if (!VSTestSetting.IsNullOrEmpty())
        {
            isRunSettingsEnabled = true;
            builder.AppendSwitchIfNotNull("--settings:", VSTestSetting);
        }

        if (VSTestTestAdapterPath != null && VSTestTestAdapterPath.Any())
        {
            foreach (var arg in VSTestTestAdapterPath)
            {
                builder.AppendSwitchIfNotNull("--testAdapterPath:", arg);
            }
        }

        if (!VSTestFramework.IsNullOrEmpty())
        {
            builder.AppendSwitchIfNotNull("--framework:", VSTestFramework);
        }

        // vstest.console only support x86 and x64 for argument platform
        if (!VSTestPlatform.IsNullOrEmpty() && !VSTestPlatform.Contains("AnyCPU"))
        {
            builder.AppendSwitchIfNotNull("--platform:", VSTestPlatform);
        }

        if (!VSTestTestCaseFilter.IsNullOrEmpty())
        {
            builder.AppendSwitchIfNotNull("--testCaseFilter:", VSTestTestCaseFilter);
        }

        if (VSTestLogger != null && VSTestLogger.Length > 0)
        {
            foreach (var arg in VSTestLogger)
            {
                builder.AppendSwitchIfNotNull("--logger:", arg);

                if (arg.StartsWith("console", StringComparison.OrdinalIgnoreCase))
                {
                    isConsoleLoggerSpecifiedByUser = true;
                }
            }
        }

        if (VSTestResultsDirectory != null && !VSTestResultsDirectory.ItemSpec.IsNullOrEmpty())
        {
            builder.AppendSwitchIfNotNull("--resultsDirectory:", VSTestResultsDirectory);
        }

        if (VSTestListTests)
        {
            builder.AppendSwitch("--listTests");
        }

        if (!VSTestDiag.IsNullOrEmpty())
        {
            builder.AppendSwitchIfNotNull("--Diag:", VSTestDiag);
        }

        if (TestFileFullPath == null)
        {
            Log.LogError("Test file path cannot be empty or null.");
        }
        else
        {
            builder.AppendFileNameIfNotNull(TestFileFullPath);
        }

        // Console logger was not specified by user, but verbosity was, hence add default console logger with verbosity as specified
        if (!VSTestVerbosity.IsNullOrWhiteSpace() && !isConsoleLoggerSpecifiedByUser)
        {
            var normalTestLogging = new List<string>() { "n", "normal", "d", "detailed", "diag", "diagnostic" };
            var quietTestLogging = new List<string>() { "q", "quiet" };

            string vsTestVerbosity = "minimal";
            if (normalTestLogging.Contains(VSTestVerbosity.ToLowerInvariant()))
            {
                vsTestVerbosity = "normal";
            }
            else if (quietTestLogging.Contains(VSTestVerbosity.ToLowerInvariant()))
            {
                vsTestVerbosity = "quiet";
            }

            builder.AppendSwitchUnquotedIfNotNull("--logger:", $"Console;Verbosity={vsTestVerbosity}");
        }

        if (VSTestBlame || VSTestBlameCrash || VSTestBlameHang)
        {
            var dumpArgs = new List<string>();
            if (VSTestBlameCrash || VSTestBlameHang)
            {
                if (VSTestBlameCrash)
                {
                    dumpArgs.Add("CollectDump");
                    if (VSTestBlameCrashCollectAlways)
                    {
                        dumpArgs.Add($"CollectAlways={VSTestBlameCrashCollectAlways}");
                    }

                    if (!VSTestBlameCrashDumpType.IsNullOrEmpty())
                    {
                        dumpArgs.Add($"DumpType={VSTestBlameCrashDumpType}");
                    }
                }

                if (VSTestBlameHang)
                {
                    dumpArgs.Add("CollectHangDump");

                    if (!VSTestBlameHangDumpType.IsNullOrEmpty())
                    {
                        dumpArgs.Add($"HangDumpType={VSTestBlameHangDumpType}");
                    }

                    if (!VSTestBlameHangTimeout.IsNullOrEmpty())
                    {
                        dumpArgs.Add($"TestTimeout={VSTestBlameHangTimeout}");
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

        if (VSTestCollect != null && VSTestCollect.Any())
        {
            foreach (var arg in VSTestCollect)
            {
                // For collecting code coverage, argument value can be either "Code Coverage" or "Code Coverage;a=b;c=d".
                // Split the argument with ';' and compare first token value.
                var tokens = arg.Split(';');

                if (arg.Equals(CodeCoverageString, StringComparison.OrdinalIgnoreCase) ||
                    tokens[0].Equals(CodeCoverageString, StringComparison.OrdinalIgnoreCase))
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
            if (VSTestTraceDataCollectorDirectoryPath != null && !VSTestTraceDataCollectorDirectoryPath.ItemSpec.IsNullOrEmpty())
            {
                builder.AppendSwitchIfNotNull("--testAdapterPath:", VSTestTraceDataCollectorDirectoryPath);
            }
            else
            {
                if (isCollectCodeCoverageEnabled)
                {
                    // Not showing message in runsettings scenario, because we are not sure that code coverage is enabled.
                    // User might be using older Microsoft.NET.Test.Sdk which don't have CodeCoverage infra.
                    Console.WriteLine(Resources.Resources.UpdateTestSdkForCollectingCodeCoverage);
                }
            }
        }

        if (VSTestNoLogo)
        {
            builder.AppendSwitch("--nologo");
        }

        if (!VSTestArtifactsProcessingMode.IsNullOrEmpty() && VSTestArtifactsProcessingMode.Equals("collect", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendSwitch("--artifactsProcessingMode-collect");
        }

        if (!VSTestSessionCorrelationId.IsNullOrEmpty())
        {
            builder.AppendSwitchIfNotNull("--testSessionCorrelationId:", VSTestSessionCorrelationId);
        }
    }
}
