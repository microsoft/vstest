using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TestPlatform.TranslationLayer.E2ETest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Spawn of vstest.console with a run tests from the current execting folder.
            var executingLocation = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            var runnerLocation = Path.Combine(executingLocation, "vstest.console.exe");
            var testadapterPath = Path.Combine(executingLocation, "Adapter\\Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.dll");
            var testAssembly = Path.Combine(executingLocation, "UnitTestProject.dll");

            IVsTestConsoleWrapper consoleWrapper = new VsTestConsoleWrapper(runnerLocation);

            consoleWrapper.StartSession();
            consoleWrapper.InitializeExtensions(new List<string>() { testadapterPath });

            var testCases = DiscoverTests(new List<string>() { testAssembly }, consoleWrapper);
            Console.WriteLine("Discovered Tests Count: " + testCases?.Count());
            Console.WriteLine("Discovered Test: " + testCases?.FirstOrDefault()?.DisplayName);

            Console.WriteLine("-------------------------------------------------------");

            var testresults = RunSelectedTests(consoleWrapper, testCases);
            Console.WriteLine("Run Selected Tests Count: " + testresults?.Count());
            Console.WriteLine("Run Selected Test Result: " + testresults?.FirstOrDefault()?.TestCase?.DisplayName + " :" + testresults?.FirstOrDefault()?.Outcome);

            Console.WriteLine("-------------------------------------------------------");

            var testResults = RunAllTests(consoleWrapper, new List<string>() { testAssembly });

            Console.WriteLine("Run All Test Count: " + testresults?.Count());
            Console.WriteLine("Run All Test Result: " + testresults?.FirstOrDefault()?.TestCase?.DisplayName + " :" + testresults?.FirstOrDefault()?.Outcome);

            Console.WriteLine("-------------------------------------------------------");
        }

        static IEnumerable<TestCase> DiscoverTests(IEnumerable<string> sources, IVsTestConsoleWrapper consoleWrapper)
        {
            var waitHandle = new AutoResetEvent(false);
            var handler = new DiscoveryEventHandler(waitHandle);
            consoleWrapper.DiscoverTests(sources, null, handler);

            waitHandle.WaitOne();

            return handler.DiscoveredTestCases;
        }

        static IEnumerable<TestResult> RunSelectedTests(IVsTestConsoleWrapper consoleWrapper, IEnumerable<TestCase> testCases)
        {
            var waitHandle = new AutoResetEvent(false);
            var handler = new RunEventHandler(waitHandle);
            consoleWrapper.RunTests(testCases, null, handler);

            waitHandle.WaitOne();
            return handler.TestResults;
        }

        static IEnumerable<TestResult> RunAllTests(IVsTestConsoleWrapper consoleWrapper, IEnumerable<string> sources)
        {
            var waitHandle = new AutoResetEvent(false);
            var handler = new RunEventHandler(waitHandle);
            consoleWrapper.RunTests(sources, null, handler);

            waitHandle.WaitOne();
            return handler.TestResults;
        }
    }

    public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
    {
        private AutoResetEvent waitHandle;

        public List<TestCase> DiscoveredTestCases { get; private set; }

        public DiscoveryEventHandler(AutoResetEvent waitHandle)
        {
            this.waitHandle = waitHandle;
            this.DiscoveredTestCases = new List<TestCase>();
        }

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            Console.WriteLine("Discovery: " + discoveredTestCases.FirstOrDefault()?.DisplayName);

            if (discoveredTestCases != null)
            {
                this.DiscoveredTestCases.AddRange(discoveredTestCases);
            }
        }

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            if (lastChunk != null)
            {
                this.DiscoveredTestCases.AddRange(lastChunk);
            }

            Console.WriteLine("DiscoveryComplete");
            waitHandle.Set();
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console.WriteLine("Discovery Message: " + message);
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No op
        }
    }

    public class RunEventHandler : ITestRunEventsHandler
    {
        private AutoResetEvent waitHandle;

        public List<TestResult> TestResults { get; private set; }

        public RunEventHandler(AutoResetEvent waitHandle)
        {
            this.waitHandle = waitHandle;
            this.TestResults = new List<TestResult>();
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console.WriteLine("Run Message: " + message);
        }

        public void HandleTestRunComplete(
            TestRunCompleteEventArgs testRunCompleteArgs,
            TestRunChangedEventArgs lastChunkArgs,
            ICollection<AttachmentSet> runContextAttachments,
            ICollection<string> executorUris)
        {
            if (lastChunkArgs != null && lastChunkArgs.NewTestResults != null)
            {
                this.TestResults.AddRange(lastChunkArgs.NewTestResults);
            }

            Console.WriteLine("TestRunComplete");
            waitHandle.Set();
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            if (testRunChangedArgs != null && testRunChangedArgs.NewTestResults != null)
            {
                this.TestResults.AddRange(testRunChangedArgs.NewTestResults);
            }
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No op
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            // No op
            return -1;
        }
    }
}