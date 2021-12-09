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
using System.Threading.Tasks;

namespace TestPlatform.Playground
{
    internal class Program
    {
        static async Task Main(string[] args)
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

            EqtTrace.InitializeTrace(Path.Combine(here, "logs", "wrapper.log.txt"), PlatformTraceLevel.Verbose);

            var r = new VsTestConsoleWrapper(console, consoleOptions);

            var sourceSettings = @"
                <RunSettings>
                    <RunConfiguration>
                        <InIsolation>true</InIsolation>
                    </RunConfiguration>
                </RunSettings>
            ";
            var sources = new[] {
                (Path.Combine(playground, "MSTest1", "bin", "Debug", "net472", "MSTest1.dll"), new TestRunHandler("net472")),
                // (Path.Combine(playground, "MSTest1", "bin", "Debug", "net48", "MSTest1.dll"), new TestRunHandler("net48")),
            };

            var tasks = new List<Task>();
            foreach (var (source, handler) in sources)
            {
                var options = new TestPlatformOptions();
                tasks.Add(Task.Run(() => r.RunTestsWithCustomTestHost(new[] { source }, sourceSettings, options, handler, new DebuggerTestHostLauncher())));
            }

            await Task.WhenAll(tasks);

            var tasks2 = new List<Task>();
            foreach (var (source, handler) in sources)
            {
                var options = new TestPlatformOptions();
                tasks2.Add(Task.Run(() => r.DiscoverTests(new[] { source }, sourceSettings, options, handler)));
            }

            await Task.WhenAll(tasks2);
            await Task.Delay(1000);
        }

        public class TestRunHandler : ITestRunEventsHandler, ITestDiscoveryEventsHandler2
        {
            private string _name;

            public TestRunHandler(string name)
            {
                _name = name;
            }

            public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
            {
                Console.WriteLine($"{_name} [DISCOVERED]: {WriteTests(discoveredTestCases)}");
            }

            public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
            {
                Console.WriteLine($"{_name} [DISCOVERED]: id: { discoveryCompleteEventArgs.TestRunId } {WriteTests(lastChunk)}");
            }

            public void HandleLogMessage(TestMessageLevel level, string message)
            {
                Console.WriteLine($"{_name} [{level.ToString().ToUpper()}]: {message}");
                if (level == TestMessageLevel.Error)
                {

                }
            }

            public void HandleRawMessage(string rawMessage)
            {
                Console.WriteLine($"{_name} [MESSAGE]: { rawMessage}");
            }

            public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
            {
                Console.WriteLine($"{_name} [COMPLETE]: id: { testRunCompleteArgs.TestRunId } err: { testRunCompleteArgs.Error }, lastChunk: {WriteTests(lastChunkArgs?.NewTestResults)}");
            }

            public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
            {
                Console.WriteLine($"{_name} [PROGRESS - NEW RESULTS]: id: { testRunChangedArgs.TestRunId } {WriteTests(testRunChangedArgs.NewTestResults)}");
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
                if (testCases == null)
                {
                    return null;
                }

                return "\t" + string.Join("\n\t", testCases.Select(r => $"{r.Source} - {r.DisplayName}"));

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
}
