// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

// using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestPlatform.Playground;

internal class Program
{
    static void Main()
    {
        // This project references TranslationLayer, vstest.console, TestHostProvider, testhost and MSTest1 projects, to make sure
        // we build all the dependencies of that are used to run tests via VSTestConsoleWrapper. It then copies the components from
        // their original build locations, to $(TargetDir)\vstest.console directory, and its subfolders to create an executable
        // copy of TestPlatform that is similar to what we ship.
        //
        // The copying might trigger only on re-build, if you see outdated dependencies, Rebuild this project instead of just Build.
        //
        // Use this as playground for your debugging of end-to-end scenarios, it will automatically attach vstest.console and teshost
        // sub-processes. It won't stop at entry-point automatically, don't forget to set your breakpoints, or remove VSTEST_DEBUG_NOBP
        // from the environment variables of this project.

        var thisAssemblyPath = Assembly.GetEntryAssembly()!.Location;
        var here = Path.GetDirectoryName(thisAssemblyPath)!;

        var console = Path.Combine(here, "vstest.console", "vstest.console.exe");

        var sourceSettings = $$$"""
            <RunSettings>
                <RunConfiguration>

                    <MaxCpuCount>0</MaxCpuCount>
            </RunConfiguration>

            </RunSettings>
            """;

        var sources = new[] {
           @"S:\t\dlls99\mstest1\bin\Debug\net8.0\mstest1.dll",
@"S:\t\dlls99\mstest10\bin\Debug\net8.0\mstest10.dll",
@"S:\t\dlls99\mstest11\bin\Debug\net8.0\mstest11.dll",
@"S:\t\dlls99\mstest12\bin\Debug\net8.0\mstest12.dll",
@"S:\t\dlls99\mstest13\bin\Debug\net8.0\mstest13.dll",
@"S:\t\dlls99\mstest14\bin\Debug\net8.0\mstest14.dll",
@"S:\t\dlls99\mstest15\bin\Debug\net8.0\mstest15.dll",
@"S:\t\dlls99\mstest16\bin\Debug\net8.0\mstest16.dll",
@"S:\t\dlls99\mstest17\bin\Debug\net8.0\mstest17.dll",
@"S:\t\dlls99\mstest18\bin\Debug\net8.0\mstest18.dll",
@"S:\t\dlls99\mstest19\bin\Debug\net8.0\mstest19.dll",
@"S:\t\dlls99\mstest2\bin\Debug\net8.0\mstest2.dll",
@"S:\t\dlls99\mstest20\bin\Debug\net8.0\mstest20.dll",
@"S:\t\dlls99\mstest21\bin\Debug\net8.0\mstest21.dll",
@"S:\t\dlls99\mstest22\bin\Debug\net8.0\mstest22.dll",
@"S:\t\dlls99\mstest23\bin\Debug\net8.0\mstest23.dll",
@"S:\t\dlls99\mstest24\bin\Debug\net8.0\mstest24.dll",
@"S:\t\dlls99\mstest25\bin\Debug\net8.0\mstest25.dll",
@"S:\t\dlls99\mstest26\bin\Debug\net8.0\mstest26.dll",
@"S:\t\dlls99\mstest27\bin\Debug\net8.0\mstest27.dll",
@"S:\t\dlls99\mstest28\bin\Debug\net8.0\mstest28.dll",
@"S:\t\dlls99\mstest29\bin\Debug\net8.0\mstest29.dll",
@"S:\t\dlls99\mstest3\bin\Debug\net8.0\mstest3.dll",
@"S:\t\dlls99\mstest30\bin\Debug\net8.0\mstest30.dll",
@"S:\t\dlls99\mstest31\bin\Debug\net8.0\mstest31.dll",
@"S:\t\dlls99\mstest32\bin\Debug\net8.0\mstest32.dll",
@"S:\t\dlls99\mstest33\bin\Debug\net8.0\mstest33.dll",
@"S:\t\dlls99\mstest34\bin\Debug\net8.0\mstest34.dll",
@"S:\t\dlls99\mstest35\bin\Debug\net8.0\mstest35.dll",
@"S:\t\dlls99\mstest36\bin\Debug\net8.0\mstest36.dll",
@"S:\t\dlls99\mstest37\bin\Debug\net8.0\mstest37.dll",
@"S:\t\dlls99\mstest38\bin\Debug\net8.0\mstest38.dll",
@"S:\t\dlls99\mstest39\bin\Debug\net8.0\mstest39.dll",
@"S:\t\dlls99\mstest4\bin\Debug\net8.0\mstest4.dll",
@"S:\t\dlls99\mstest40\bin\Debug\net8.0\mstest40.dll",
@"S:\t\dlls99\mstest41\bin\Debug\net8.0\mstest41.dll",
@"S:\t\dlls99\mstest42\bin\Debug\net8.0\mstest42.dll",
@"S:\t\dlls99\mstest43\bin\Debug\net8.0\mstest43.dll",
@"S:\t\dlls99\mstest44\bin\Debug\net8.0\mstest44.dll",
@"S:\t\dlls99\mstest45\bin\Debug\net8.0\mstest45.dll",
@"S:\t\dlls99\mstest46\bin\Debug\net8.0\mstest46.dll",
@"S:\t\dlls99\mstest47\bin\Debug\net8.0\mstest47.dll",
@"S:\t\dlls99\mstest48\bin\Debug\net8.0\mstest48.dll",
@"S:\t\dlls99\mstest49\bin\Debug\net8.0\mstest49.dll",
@"S:\t\dlls99\mstest5\bin\Debug\net8.0\mstest5.dll",
@"S:\t\dlls99\mstest50\bin\Debug\net8.0\mstest50.dll",
@"S:\t\dlls99\mstest51\bin\Debug\net8.0\mstest51.dll",
@"S:\t\dlls99\mstest52\bin\Debug\net8.0\mstest52.dll",
@"S:\t\dlls99\mstest53\bin\Debug\net8.0\mstest53.dll",
@"S:\t\dlls99\mstest54\bin\Debug\net8.0\mstest54.dll",
@"S:\t\dlls99\mstest55\bin\Debug\net8.0\mstest55.dll",
@"S:\t\dlls99\mstest56\bin\Debug\net8.0\mstest56.dll",
@"S:\t\dlls99\mstest57\bin\Debug\net8.0\mstest57.dll",
@"S:\t\dlls99\mstest58\bin\Debug\net8.0\mstest58.dll",
@"S:\t\dlls99\mstest59\bin\Debug\net8.0\mstest59.dll",
@"S:\t\dlls99\mstest6\bin\Debug\net8.0\mstest6.dll",
@"S:\t\dlls99\mstest60\bin\Debug\net8.0\mstest60.dll",
@"S:\t\dlls99\mstest61\bin\Debug\net8.0\mstest61.dll",
@"S:\t\dlls99\mstest62\bin\Debug\net8.0\mstest62.dll",
@"S:\t\dlls99\mstest63\bin\Debug\net8.0\mstest63.dll",
@"S:\t\dlls99\mstest64\bin\Debug\net8.0\mstest64.dll",
@"S:\t\dlls99\mstest65\bin\Debug\net8.0\mstest65.dll",
@"S:\t\dlls99\mstest66\bin\Debug\net8.0\mstest66.dll",
@"S:\t\dlls99\mstest67\bin\Debug\net8.0\mstest67.dll",
@"S:\t\dlls99\mstest68\bin\Debug\net8.0\mstest68.dll",
@"S:\t\dlls99\mstest69\bin\Debug\net8.0\mstest69.dll",
@"S:\t\dlls99\mstest7\bin\Debug\net8.0\mstest7.dll",
@"S:\t\dlls99\mstest70\bin\Debug\net8.0\mstest70.dll",
@"S:\t\dlls99\mstest71\bin\Debug\net8.0\mstest71.dll",
@"S:\t\dlls99\mstest72\bin\Debug\net8.0\mstest72.dll",
@"S:\t\dlls99\mstest73\bin\Debug\net8.0\mstest73.dll",
@"S:\t\dlls99\mstest74\bin\Debug\net8.0\mstest74.dll",
@"S:\t\dlls99\mstest75\bin\Debug\net8.0\mstest75.dll",
@"S:\t\dlls99\mstest76\bin\Debug\net8.0\mstest76.dll",
@"S:\t\dlls99\mstest77\bin\Debug\net8.0\mstest77.dll",
@"S:\t\dlls99\mstest78\bin\Debug\net8.0\mstest78.dll",
@"S:\t\dlls99\mstest79\bin\Debug\net8.0\mstest79.dll",
@"S:\t\dlls99\mstest8\bin\Debug\net8.0\mstest8.dll",
@"S:\t\dlls99\mstest80\bin\Debug\net8.0\mstest80.dll",
@"S:\t\dlls99\mstest81\bin\Debug\net8.0\mstest81.dll",
@"S:\t\dlls99\mstest82\bin\Debug\net8.0\mstest82.dll",
@"S:\t\dlls99\mstest83\bin\Debug\net8.0\mstest83.dll",
@"S:\t\dlls99\mstest84\bin\Debug\net8.0\mstest84.dll",
@"S:\t\dlls99\mstest85\bin\Debug\net8.0\mstest85.dll",
@"S:\t\dlls99\mstest86\bin\Debug\net8.0\mstest86.dll",
@"S:\t\dlls99\mstest87\bin\Debug\net8.0\mstest87.dll",
@"S:\t\dlls99\mstest88\bin\Debug\net8.0\mstest88.dll",
@"S:\t\dlls99\mstest89\bin\Debug\net8.0\mstest89.dll",
@"S:\t\dlls99\mstest9\bin\Debug\net8.0\mstest9.dll",
@"S:\t\dlls99\mstest90\bin\Debug\net8.0\mstest90.dll",
@"S:\t\dlls99\mstest91\bin\Debug\net8.0\mstest91.dll",
@"S:\t\dlls99\mstest92\bin\Debug\net8.0\mstest92.dll",
@"S:\t\dlls99\mstest93\bin\Debug\net8.0\mstest93.dll",
@"S:\t\dlls99\mstest94\bin\Debug\net8.0\mstest94.dll",
@"S:\t\dlls99\mstest95\bin\Debug\net8.0\mstest95.dll",
@"S:\t\dlls99\mstest96\bin\Debug\net8.0\mstest96.dll",
@"S:\t\dlls99\mstest97\bin\Debug\net8.0\mstest97.dll",
@"S:\t\dlls99\mstest98\bin\Debug\net8.0\mstest98.dll",
@"S:\t\dlls99\mstest99\bin\Debug\net8.0\mstest99.dll"
        };

        // Console mode
        // Uncomment if providing command line parameters is easier for you
        // than converting them to settings, or when you debug command line scenario specifically.
        var settingsFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(settingsFile, sourceSettings);
            var processStartInfo = new ProcessStartInfo
            {
                FileName = console,
                Arguments = $"{string.Join(" ", sources)} --settings:{settingsFile}",
                UseShellExecute = false,
            };
            EnvironmentVariables.Variables.ToList().ForEach(processStartInfo.Environment.Add);
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception($"Process failed with {process.ExitCode}");
            }
        }
        finally
        {
            try { File.Delete(settingsFile); } catch { }
        }
    }

    public class PlaygroundTestDiscoveryHandler : ITestDiscoveryEventsHandler, ITestDiscoveryEventsHandler2
    {
        private int _testCasesCount;
        private readonly bool _detailedOutput;

        public PlaygroundTestDiscoveryHandler(bool detailedOutput)
        {
            _detailedOutput = detailedOutput;
        }

        public List<TestCase> TestCases { get; internal set; } = new List<TestCase>();

        public void HandleDiscoveredTests(IEnumerable<TestCase>? discoveredTestCases)
        {
            if (_detailedOutput)
            {
                Console.WriteLine($"[DISCOVERY.PROGRESS]");
                Console.WriteLine(WriteTests(discoveredTestCases));
            }
            _testCasesCount += discoveredTestCases!.Count();
            if (discoveredTestCases != null) { TestCases.AddRange(discoveredTestCases); }
        }

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase>? lastChunk, bool isAborted)
        {
            Console.WriteLine($"[DISCOVERY.COMPLETE] aborted? {isAborted}, tests count: {totalTests}");
            if (_detailedOutput)
            {
                Console.WriteLine("Last chunk:");
                Console.WriteLine(WriteTests(lastChunk));
            }
            if (lastChunk != null) { TestCases.AddRange(lastChunk); }
        }

        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase>? lastChunk)
        {
            Console.WriteLine($"[DISCOVERY.COMPLETE] aborted? {discoveryCompleteEventArgs.IsAborted}, tests count: {discoveryCompleteEventArgs.TotalCount}, discovered count: {_testCasesCount}");
            if (_detailedOutput)
            {
                Console.WriteLine("Last chunk:");
                Console.WriteLine(WriteTests(lastChunk));
            }
            Console.WriteLine("Fully discovered:");
            Console.WriteLine(WriteSources(discoveryCompleteEventArgs.FullyDiscoveredSources));
            Console.WriteLine("Partially discovered:");
            Console.WriteLine(WriteSources(discoveryCompleteEventArgs.PartiallyDiscoveredSources));
            Console.WriteLine("Skipped discovery:");
            Console.WriteLine(WriteSources(discoveryCompleteEventArgs.SkippedDiscoveredSources));
            Console.WriteLine("Not discovered:");
            Console.WriteLine(WriteSources(discoveryCompleteEventArgs.NotDiscoveredSources));
            if (lastChunk != null) { TestCases.AddRange(lastChunk); }
        }

        public void HandleLogMessage(TestMessageLevel level, string? message)
        {
            Console.WriteLine($"[DISCOVERY.{level.ToString().ToUpper(CultureInfo.InvariantCulture)}] {message}");
        }

        public void HandleRawMessage(string rawMessage)
        {
            Console.WriteLine($"[DISCOVERY.MESSAGE] {rawMessage}");
        }

        private static string WriteTests(IEnumerable<TestCase>? testCases)
            => testCases?.Any() == true
                ? "\t" + string.Join("\n\t", testCases!.Select(r => r.Source + " " + r.DisplayName))
                : "\t<empty>";

        private static string WriteSources(IEnumerable<string>? sources)
            => sources?.Any() == true
                ? "\t" + string.Join("\n\t", sources)
                : "\t<empty>";
    }

    public class TestRunHandler : ITestRunEventsHandler
    {
        private readonly bool _detailedOutput;

        public TestRunHandler(bool detailedOutput)
        {
            _detailedOutput = detailedOutput;
        }

        public void HandleLogMessage(TestMessageLevel level, string? message)
        {
            Console.WriteLine($"[{level.ToString().ToUpper(CultureInfo.InvariantCulture)}]: {message}");
        }

        public void HandleRawMessage(string rawMessage)
        {
            if (_detailedOutput)
            {
                Console.WriteLine($"[RUN.MESSAGE]: {rawMessage}");
            }
        }

        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs? lastChunkArgs, ICollection<AttachmentSet>? runContextAttachments, ICollection<string>? executorUris)
        {
            Console.WriteLine($"[RUN.COMPLETE]: err: {testRunCompleteArgs.Error}, lastChunk:");
            if (_detailedOutput)
            {
                Console.WriteLine(WriteTests(lastChunkArgs?.NewTestResults));
            }
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
        {
            if (_detailedOutput)
            {
                Console.WriteLine($"[RUN.PROGRESS]");
                Console.WriteLine(WriteTests(testRunChangedArgs?.NewTestResults));
            }
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            throw new NotImplementedException();
        }

        private static string WriteTests(IEnumerable<TestResult>? testResults)
            => WriteTests(testResults?.Select(t => t.TestCase));

        private static string WriteTests(IEnumerable<TestCase>? testCases)
            => testCases?.Any() == true
                ? "\t" + string.Join("\n\t", testCases.Select(r => r.DisplayName))
                : "\t<empty>";
    }

    internal class DebuggerTestHostLauncher : ITestHostLauncher2
    {
        public bool IsDebug => true;

        public bool AttachDebuggerToProcess(int pid)
        {
            return true;
        }

        public bool AttachDebuggerToProcess(int pid, CancellationToken cancellationToken)
        {
            return true;
        }

        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
        {
            return 1;
        }

        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken)
        {
            return 1;
        }
    }
}

internal class TestSessionHandler : ITestSessionEventsHandler
{
    public TestSessionHandler() { }
    public TestSessionInfo? TestSessionInfo { get; private set; }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {

    }

    public void HandleRawMessage(string rawMessage)
    {

    }

    public void HandleStartTestSessionComplete(StartTestSessionCompleteEventArgs? eventArgs)
    {
        TestSessionInfo = eventArgs?.TestSessionInfo;
    }

    public void HandleStopTestSessionComplete(StopTestSessionCompleteEventArgs? eventArgs)
    {

    }
}
