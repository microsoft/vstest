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

namespace TestPlatform.Playground
{
    internal class Program
    {
        static void Main(string[] args)
        {

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
                Path.Combine(playground, "MSTest1", "bin", "Debug", "net48", "MSTest1.dll")
            };

            var options = new TestPlatformOptions();
            r.RunTestsWithCustomTestHost(sources, sourceSettings, options, new TestRunHandler(), new DebuggerTestHostLauncher());
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
                if (testCases == null)
                {
                    return null;
                }

                return "\t" + string.Join("\n\t", testCases.Select(r => r.DisplayName));

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
