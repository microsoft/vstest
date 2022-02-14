// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace TestPlatform.Playground;

internal class Program
{
    static void Main(string[] args)
    {
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

        var r = new VsTestConsoleWrapper(console, consoleOptions);

        var sourceSettings = @"
                <RunSettings>
                    <RunConfiguration>
                        <InIsolation>true</InIsolation>
                    </RunConfiguration>
                </RunSettings>
            ";
        var sources = new[] {
            Path.Combine(playground, "MSTest1", "bin", "Debug", "net472", "MSTest1.dll")
        };

        var options = new TestPlatformOptions();
        var discoveryHandler = new TestDiscoveryHandler();
        r.DiscoverTests(sources, sourceSettings, options, discoveryHandler);
        if (File.Exists(sources[0]))
        {
            throw new Exception($"File {sources[0]} exists, but it should not because we moved it during deployment!");
        }
        r.RunTestsWithCustomTestHost(discoveryHandler.DiscoveredTestCases, sourceSettings, options, new TestRunHandler(), new DebuggerTestHostLauncher());
    }

    public class TestRunHandler : ITestRunEventsHandler
    {

        public TestRunHandler()
        {
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console.WriteLine($"[{level.ToString().ToUpper()}]: {message}");
        }

        public void HandleRawMessage(string rawMessage)
        {
            Console.WriteLine($"[MESSAGE]: { rawMessage}");
        }

        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            Console.WriteLine($"[COMPLETE]: err: { testRunCompleteArgs.Error }, lastChunk: {WriteTests(lastChunkArgs?.NewTestResults)}");
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            Console.WriteLine($"[PROGRESS - NEW RESULTS]: {WriteTests(testRunChangedArgs.NewTestResults)}");
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            throw new NotImplementedException();
        }

        private string WriteTests(IEnumerable<TestResult> testResults)
        {
            return WriteTests(testResults?.Select(t => t.TestCase));
        }

        private string WriteTests(IEnumerable<TestCase> testCases)
        {
            return testCases == null ? null : "\t" + string.Join("\n\t", testCases.Select(r => r.DisplayName));
        }
    }

    public class TestDiscoveryHandler : ITestDiscoveryEventsHandler2
    {
        public List<TestCase> DiscoveredTestCases { get; } = new List<TestCase>();
        public List<string> Messages { get; } = new List<string>();

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            if (discoveredTestCases != null)
            {
                DiscoveredTestCases.AddRange(discoveredTestCases);
                Console.WriteLine($"[DISCOVERY UPDATE] {WriteTests(discoveredTestCases)}");
            }
        }

        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            if (lastChunk != null)
            {
                DiscoveredTestCases.AddRange(lastChunk);
                Console.WriteLine($"[DISCOVERY COMPLETE] {WriteTests(lastChunk)}");
            }
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Messages.Add($"[{level}]: {message}");
            Console.WriteLine(($"[{level}]: {message}"));
        }

        public void HandleRawMessage(string rawMessage)
        {
            Console.WriteLine(($"[RAWMESSAGE]: {rawMessage}"));
        }

        private string WriteTests(IEnumerable<TestResult> testResults)
        {
            return WriteTests(testResults?.Select(t => t.TestCase));
        }

        private string WriteTests(IEnumerable<TestCase> testCases)
        {
            return testCases == null ? null : "\t" + string.Join("\n\t", testCases.Select(r => r.DisplayName));
        }
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
