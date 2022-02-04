// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TestPlatform.Playground;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console2.WriteLine("Start");
        // This project references TranslationLayer, vstest.console, TestHostProvider, testhost and MSTest1 projects, to make sure
        // we build all the dependencies of that are used to run tests via VSTestConsoleWrapper. It then copies the components from
        // their original build locations, to $(TargetDir)\vstest.console directory, and it's subfolders to create an executable
        // copy of TestPlatform that is similar to what we ship.
        //
        // The copying might trigger only on re-build, if you see outdated dependencies, Rebuild this project instead of just Build.
        // 
        // Use this as playground for your debugging of end-to-end scenarios, it will automatically attach vstest.console and teshost
        // sub-processes. It won't stop at entry-point automatically, don't forget to set your breakpoints, or remove VSTEST_DEBUG_NOBP
        // from the environment variables of this project.

        var thisAssemblyPath = Assembly.GetEntryAssembly().Location;
        var here = Path.GetDirectoryName(thisAssemblyPath);
        var playground = Path.GetFullPath(Path.Combine(here, "..", "..", "..", ".."));

        var console = Path.Combine(here, "vstest.console", "vstest.console.exe");
        var consoleOptions = new ConsoleParameters
        {
            LogFilePath = Path.Combine(here, "logs", "log.txt"),
            TraceLevel = TraceLevel.Verbose,
        };

        EqtTrace.InitializeTrace(Path.Combine(here, "logs", "wrapper.log.txt"), PlatformTraceLevel.Verbose);

        var r = new VsTestConsoleWrapper(console, consoleOptions);

        var sourceSettings = @"
            <RunSettings>
                <RunConfiguration>
                    <InIsolation>true</InIsolation>
                </RunConfiguration>
            </RunSettings>
        ";

        var tasks = new List<Task>();
        Action<string, TestRunHandler, DebuggerTestHostLauncher> execute = (source, handler, launcher) =>
        {
            var options = new TestPlatformOptions();
            Console2.WriteLine("Run tests");
            r.RunTestsWithCustomTestHost(new[] { source }, sourceSettings, options, handler, launcher);
        };

        Action<string, TestRunHandler, DebuggerTestHostLauncher> discoverThenExecute = (source, handler, launcher) =>
        {
            Console2.WriteLine("Discover");
            Thread.Sleep(5000);
            var options = new TestPlatformOptions();
            Console2.WriteLine("Discover tests");
            r.DiscoverTests(new[] { source }, sourceSettings, options, handler);
            var tests = handler.DiscoveredTests;
            if (!tests.Any())
            {
                // REview: the second discovery can get cancelled too early. This is task that will probably be solved by fixing the cancellation in the translation layer sender and how it receives messages.
                // or maybe it calls cancel when it is done, and everything is just cancelled.
            }
            Console2.WriteLine("Run discovered tests");
            r.RunTestsWithCustomTestHost(tests, sourceSettings, options, handler, launcher);
        };
        var sources = new[] {
            (Path.Combine(playground, "MSTest1", "bin", "Debug", "net472", "MSTest1.dll"), new TestRunHandler("net472-execute"), new DebuggerTestHostLauncher("net472-execute"), execute),
            (Path.Combine(playground, "MSTest1", "bin", "Debug", "net48", "MSTest1.dll"), new TestRunHandler("net48-execute"), new DebuggerTestHostLauncher("net48-execute"), execute),
            (Path.Combine(playground, "MSTest1", "bin", "Debug", "net472", "MSTest1.dll"), new TestRunHandler("net472-discoverThenExecute"), new DebuggerTestHostLauncher("net472-discoverThenExecute"), discoverThenExecute),
            (Path.Combine(playground, "MSTest1", "bin", "Debug", "net48", "MSTest1.dll"), new TestRunHandler("net48-discoverThenExecute"), new DebuggerTestHostLauncher("net48-discoverThenExecute"), discoverThenExecute),
        };

        foreach (var (source, handler, launcher, action) in sources)
        {
            tasks.Add(Task.Run(() => action(source, handler, launcher)));
        }

        await Task.WhenAll(tasks);
        await Task.Delay(1000);
    }

    public class TestRunHandler : ITestRunEventsHandler2, ITestDiscoveryEventsHandler2
    {
        private readonly string _name;
        private readonly ConcurrentBag<TestCase> _discoveredTests = new();
        public List<TestCase> DiscoveredTests => _discoveredTests.ToList();

        public TestRunHandler(string name)
        {
            _name = name;
        }

        public bool AttachDebuggerToProcess(int pid)
        {
            Console2.WriteLine($"{_name} [ATTACH DEBUGGER]");
            return false;
        }

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            Console2.WriteLine($"{_name} [DISCOVERED]: {WriteTests(discoveredTestCases)}");
            DiscoveredTests.AddRange(discoveredTestCases);
        }

        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            Console2.WriteLine($"{_name} [DISCOVERED]: {WriteTests(lastChunk)}");
            DiscoveredTests.AddRange(lastChunk ?? new List<TestCase>());
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console2.WriteLine($"{_name} [{level.ToString().ToUpper()}]: {message}");
            if (level == TestMessageLevel.Error)
            {

            }
        }

        public void HandleRawMessage(string rawMessage)
        {
            Console2.WriteLine($"{_name} [MESSAGE]: { rawMessage}");
        }

        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            Console2.WriteLine($"{_name} [COMPLETE]: err: { testRunCompleteArgs.Error }, lastChunk: {WriteTests(lastChunkArgs?.NewTestResults)}");
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            Console2.WriteLine($"{_name} [PROGRESS - NEW RESULTS]: {WriteTests(testRunChangedArgs.NewTestResults)}");
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            Console2.WriteLine($"{_name} [LAUNCH WITH DEBUGGER]");
            throw new NotImplementedException();
        }

        private string WriteTests(IEnumerable<TestResult> testResults)
        {
            return WriteTests(testResults?.Select(t => t.TestCase));
        }

        private string WriteTests(IEnumerable<TestCase> testCases)
        {
            if (testCases == null)
            {
                return null;
            }

            return "\t" + string.Join("\n\t", testCases.Select(r => $"{r.Source} - {r.DisplayName}"));

        }
    }

    internal class DebuggerTestHostLauncher : ITestHostLauncher2
    {
        private readonly string _name;

        public DebuggerTestHostLauncher(string name)
        {
            _name = name;
        }

        public bool IsDebug => true;

        public bool AttachDebuggerToProcess(int pid)
        {
            Console2.WriteLine($"{_name} [ATTACH DEBUGGER] to { Process.GetProcessById(pid).ProcessName }");
            return true;
        }

        public bool AttachDebuggerToProcess(int pid, CancellationToken cancellationToken)
        {
            Console2.WriteLine($"{_name} [ATTACH DEBUGGER] to { Process.GetProcessById(pid).ProcessName }");
            return true;
        }

        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
        {
            Console2.WriteLine($"{_name} [LAUNCH TESTHOST] for {defaultTestHostStartInfo.FileName } { defaultTestHostStartInfo.Arguments }");
            return 1;
        }

        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken)
        {
            Console2.WriteLine($"{_name} [LAUNCH TESTHOST] for {defaultTestHostStartInfo.FileName } { defaultTestHostStartInfo.Arguments }");
            return 1;
        }
    }

    static class Console2
    {
        public static void WriteLine(string text)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}] {text}");
        }
    }
}
