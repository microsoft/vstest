// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public sealed class DiagnosticPathTests : AcceptanceTestBase
{
    [TestMethod]
    [TestMatrix]
    public void CommandLinePreservesDiagnosticPaths(RunnerInfo runnerInfo)
        => VerifyDiagnosticPaths(runnerInfo, "cli");

    [TestMethod]
    [TestMatrix]
    public void EnvironmentPreservesDiagnosticPaths(RunnerInfo runnerInfo)
        => VerifyDiagnosticPaths(runnerInfo, "environment");

    [TestMethod]
    [TestMatrix]
    public void TranslationLayerPreservesDiagnosticPaths(RunnerInfo runnerInfo)
        => VerifyDiagnosticPaths(runnerInfo, "translation");

    [TestMethod]
    [TestMatrix(testHost: Net)]
    public void PrequotedMultipleSourcesStillRun(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var sources = BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll");
        var arguments = PrepareArguments(sources, null, null, FrameworkArgValue);

        InvokeVsTest(arguments);

        ValidateSummaryStatus(2, 2, 2);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    [TestMatrix(testHost: Net)]
    public void InvalidWindowsQuotesReportArgumentErrors(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        foreach (var path in new[] { "log\"one.txt", "\"lead.txt", "trail.txt\"", "\"\"log.txt\"\"", "\"\"" })
        {
            foreach (var entryPoint in new[] { "cli", "environment" })
            {
                var environment = new Dictionary<string, string?> { ["VSTEST_DIAG"] = entryPoint == "environment" ? path : null };
                var arguments = entryPoint == "cli" ? new[] { "/help", "--diag:" + path } : new[] { "/help" };
                var result = RunConsole(arguments, environment, TempDirectory.Path);

                Assert.AreEqual(1, result.ExitCode, result.Output + result.Error);
                Assert.Contains("Diag argument", result.Error);
                Assert.DoesNotContain("Unhandled exception", result.Error);
            }
        }
    }

    [TestMethod]
    [TestMatrix(testHost: Net)]
    public void ExistingDirectoryWithoutSeparatorIsNotInferred(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        foreach (var entryPoint in new[] { "cli", "environment", "translation" })
        {
            var directory = TempDirectory.CreateDirectory(entryPoint).FullName;
            var environment = new Dictionary<string, string?> { ["VSTEST_DIAG"] = entryPoint == "environment" ? directory : null };
            if (entryPoint == "translation")
            {
                var wrapper = CreateVsTestConsoleWrapper(new ConsoleParameters { LogFilePath = directory, EnvironmentVariables = environment });
                try
                {
                    wrapper.StartSession();
                }
                finally
                {
                    wrapper.EndSession();
                }
            }
            else
            {
                var arguments = entryPoint == "cli" ? new[] { "/help", "--diag:" + directory } : new[] { "/help" };
                var result = RunConsole(arguments, environment, TempDirectory.Path);
                Assert.Contains(directory, result.Output + result.Error);
                Assert.Contains("UnauthorizedAccessException", result.Output + result.Error);
            }

            Assert.IsEmpty(Directory.GetFiles(directory));
        }
    }

    private void VerifyDiagnosticPaths(RunnerInfo runnerInfo, string entryPoint)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var collectorDirectory = Path.GetDirectoryName(GetTestDllForFramework("OutOfProcDataCollector.dll", "netstandard2.0"))!;
        var source = GetAssetFullPath("SimpleTestProject2.dll");
        var names = new List<string> { "log with spaces.txt", "\"quoted\"", "directory with spaces/", @"terminal\", @"terminal\\" };
        if (OSUtils.IsWindows)
        {
            names.Add("\"quoted directory\\\"");
        }
        if (!OSUtils.IsWindows)
        {
            names.AddRange(["log\"one\".txt", "dir\"one/log.txt", "log\\\"one\".txt", "\"leading", "trailing\"", "\"\"", "dir\\\"one/"]);
        }

        foreach (var name in names)
        {
            var caseDirectory = TempDirectory.CreateDirectory(Guid.NewGuid().ToString("N")).FullName;
            var logsDirectory = Directory.CreateDirectory(Path.Combine(caseDirectory, "logs")).FullName;
            var legacyQuotes = OSUtils.IsWindows && name.StartsWith("\"", StringComparison.Ordinal);
            var path = Path.Combine(logsDirectory, legacyQuotes ? name.Trim('"') : name);
            var input = legacyQuotes ? "\"" + path + "\"" : path;
            var environment = new Dictionary<string, string?>
            {
                ["TEST_ASSET_SAMPLE_COLLECTOR_PATH"] = caseDirectory,
                ["VSTEST_DIAG"] = entryPoint == "environment" ? input : null,
            };
            if (entryPoint == "translation")
            {
                var parameters = new ConsoleParameters { LogFilePath = input, EnvironmentVariables = environment };
                var wrapper = CreateVsTestConsoleWrapper(parameters);
                var handler = new RunHandler();
                try
                {
                    wrapper.StartSession();
                    wrapper.RunTests([source], $"""
                        <RunSettings>
                          <RunConfiguration>
                            <InIsolation>true</InIsolation>
                            <TestAdaptersPaths>{SecurityElement.Escape(collectorDirectory)}</TestAdaptersPaths>
                            <ResultsDirectory>{SecurityElement.Escape(caseDirectory)}</ResultsDirectory>
                          </RunConfiguration>
                          <DataCollectionRunSettings>
                            <DataCollectors><DataCollector friendlyName="SampleDataCollector" /></DataCollectors>
                          </DataCollectionRunSettings>
                        </RunSettings>
                        """, handler);
                }
                finally
                {
                    wrapper.EndSession();
                }

                Assert.IsNotNull(handler.Complete);
                Assert.IsFalse(handler.Complete.IsAborted);
                Assert.IsNull(handler.Complete.Error);
                Assert.HasCount(3, handler.Results);
            }
            else
            {
                var arguments = new List<string>
                {
                    source,
                    "/Collect:SampleDataCollector",
                    "/TestAdapterPath:" + collectorDirectory,
                    "/ResultsDirectory:" + caseDirectory,
                    "/logger:console;verbosity=normal",
                };
                if (runnerInfo.InIsolationValue is not null)
                {
                    arguments.Add(runnerInfo.InIsolationValue);
                }
                if (entryPoint == "cli")
                {
                    // Use a relative name to exercise matching outer quotes on Unix too.
                    arguments.Add("--diag:" + name);
                }

                var result = RunConsole(arguments, environment, logsDirectory);
                Assert.AreEqual(1, result.ExitCode, result.Output + result.Error);
                Assert.Contains("Passed: 1", result.Output, result.Error);
            }

            AssertLogs(path);
            Assert.HasCount(3, Directory.GetFiles(logsDirectory, "*", SearchOption.AllDirectories));
            Console.WriteLine($"{entryPoint}: {path}");
            foreach (var file in Directory.GetFiles(logsDirectory, "*", SearchOption.AllDirectories))
            {
                Console.WriteLine($"{file} ({new FileInfo(file).Length} bytes)");
                foreach (var line in File.ReadLines(file).Where(line => line.Contains("Runtime location:", StringComparison.Ordinal)))
                {
                    Console.WriteLine(line);
                }
            }
        }
    }

    private (int ExitCode, string Output, string Error) RunConsole(IEnumerable<string> arguments, Dictionary<string, string?> environment, string workingDirectory)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(GetConsoleRunnerPath())
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory,
        };
        if (IsNetCoreRunner())
        {
            process.StartInfo.ArgumentList.Add(GetDotnetRunnerPath());
        }

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        foreach (var variable in environment)
        {
            process.StartInfo.Environment[variable.Key] = variable.Value;
        }

        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(120_000))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"Diagnostic run timed out in {workingDirectory}");
        }

        return (process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    private static void AssertLogs(string path)
    {
        var isDirectory = path.EndsWith("/", StringComparison.Ordinal)
            || (OSUtils.IsWindows && path.EndsWith("\\", StringComparison.Ordinal));
        var directory = isDirectory ? path : Path.GetDirectoryName(path)!;
        Assert.IsTrue(Directory.Exists(directory), directory);
        var files = Directory.GetFiles(directory);
        string prefix;
        string extension;
        string runnerFile;
        const string timestamp = @"\d{2}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}_\d{5}";
        if (isDirectory)
        {
            runnerFile = Assert.ContainsSingle(files.Where(file => Regex.IsMatch(Path.GetFileName(file), @"^.+_\d+\." + timestamp + @"\.diag$")));
            var match = Regex.Match(Path.GetFileName(runnerFile), @"^(?<prefix>.+_\d+)\." + timestamp + @"\.diag$");
            prefix = match.Groups["prefix"].Value;
            extension = ".diag";
        }
        else
        {
            runnerFile = path;
            prefix = Path.GetFileNameWithoutExtension(path);
            extension = Path.GetExtension(path);
            Assert.IsTrue(File.Exists(path), $"Requested log missing: {path}; found: {string.Join(", ", files)}");
            Assert.IsFalse(Directory.Exists(path), path);
        }

        Assert.Contains("Version:", File.ReadAllText(runnerFile));
        foreach (var child in new[] { "host", "datacollector" })
        {
            var pattern = "^" + Regex.Escape(prefix + "." + child + ".") + timestamp + @"_\d+" + Regex.Escape(extension) + "$";
            var log = Assert.ContainsSingle(files.Where(file => Regex.IsMatch(Path.GetFileName(file), pattern)), path);
            Assert.Contains(child == "host" ? "Testhost process started" : "DataCollectionRequestHandler", File.ReadAllText(log));
        }

        Assert.HasCount(3, files, $"Unexpected diagnostic filenames in {directory}: {string.Join(", ", files)}");
    }

    private sealed class RunHandler : ITestRunEventsHandler
    {
        public List<TestResult> Results { get; } = [];
        public TestRunCompleteEventArgs? Complete { get; private set; }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs? args)
        {
            if (args?.NewTestResults is not null)
            {
                Results.AddRange(args.NewTestResults);
            }
        }

        public void HandleTestRunComplete(TestRunCompleteEventArgs args, TestRunChangedEventArgs? lastChunk, ICollection<AttachmentSet>? attachments, ICollection<string>? executors)
        {
            Complete = args;
            HandleTestRunStatsChange(lastChunk);
        }

        public void HandleLogMessage(TestMessageLevel level, string? message) { }
        public void HandleRawMessage(string message) { }
        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo info) => throw new NotSupportedException();
    }
}
