// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#nullable disable

namespace TestPlatform.Playground;

internal class Program
{
    static void Main(string[] args)
    {
        var mts = typeof(MessageType).GetFields().Select(f => (string)f.GetValue(null)).ToList().OrderByDescending(m => m.Length).ToList();
        var step = "run";
        var version = 7;
        var attempts = Enumerable.Range(1, 1);
        // For 6 disable the faster json path
        Environment.SetEnvironmentVariable("VSTEST_DISABLE_FASTER_JSON_SERIALIZATION", version == 6 ? "1" : "0");

        if (step == "d")
        {
            Console.WriteLine("Deserialization:");
            var dir = Directory.GetDirectories("C:\\temp\\tp-serialization\\").OrderBy(n => n).Last();
            // Skip first 3 beause that is version check and debugger attach
            // Skip last 1 because that is test run complete
            var completeMessage = File.ReadAllText(Directory.GetFiles(dir).Last());
            var rawMessages = Directory.GetFiles(dir)
                // There is no SkipLast on .NET Framework, do this inefficiently by ordering in descending order
                .OrderByDescending(n => n).Skip(1)
                .OrderBy(n => n).Skip(3)
                .Select(File.ReadAllText).ToList();
            Console.WriteLine($"There are {rawMessages.Count} raw messages.");
            var json = JsonDataSerializer.Instance;

            var count = Enumerable.Range(0, 10000).ToList();

            foreach (var attempt in attempts)
            {
                int testCount = 0;
                VersionedMessage lastMessage = null;
                Stopwatch sw = Stopwatch.StartNew();
                foreach (int _ in count)
                {
                    var rawMessage = completeMessage;


                    Message message = json.DeserializeMessage(rawMessage);
                    VersionedMessage versionedMessage = (VersionedMessage)message;
                    lastMessage = versionedMessage;
                    var v = json.DeserializePayload<int>(versionedMessage);
                    testCount++;
                }
                long duration = sw.ElapsedMilliseconds;
                Console.WriteLine($"Try {attempt}:");
                Console.WriteLine($" Tests: {testCount}");
                Console.WriteLine($" Duration {duration} ms");
                Console.WriteLine($" Message version {version}");
                Console.WriteLine();

            }

            foreach (var attempt in attempts)
            {
                int testCount = 0;
                VersionedMessage lastMessage = null;
                Stopwatch sw = Stopwatch.StartNew();
                foreach (string rawMessage in rawMessages)
                {
                    TestRunChangedEventArgs payload;

                    Message message = json.DeserializeMessage(rawMessage);
                    VersionedMessage versionedMessage = (VersionedMessage)message;
                    lastMessage = versionedMessage;
                    payload = json.DeserializePayload<TestRunChangedEventArgs>(versionedMessage);

                    testCount += payload.NewTestResults.Count();
                }
                long duration = sw.ElapsedMilliseconds;
                Console.WriteLine($"Try {attempt}:");
                Console.WriteLine($" Tests: {testCount}");
                Console.WriteLine($" Duration {duration} ms");
                Console.WriteLine($" Message version {version}");
                Console.WriteLine();

            }
        }
        else if (step == "f")
        {
            Console.WriteLine("Deserialization:");
            var dir = Directory.GetDirectories("C:\\temp\\tp-serialization\\").OrderBy(n => n).Last();
            // Skip first 3 beause that is version check and debugger attach
            // Skip last 1 because that is test run complete
            var rawMessages = Directory.GetFiles(dir)
                // There is no SkipLast on .NET Framework, do this inefficiently by ordering in descending order
                .OrderByDescending(n => n).Skip(1)
                .OrderBy(n => n).Skip(3)
                .Select(File.ReadAllText).ToList();
            Console.WriteLine($"There are {rawMessages.Count} raw messages.");
            var json = JsonDataSerializer.Instance;

            foreach (var attempt in attempts)
            {
                int messageCount = 0;
                Stopwatch sw = Stopwatch.StartNew();
                foreach (string rawMessage in rawMessages)
                {
                    messageCount++;

                    Message message = json.DeserializeMessage(rawMessage);
                    Message message2 = json.DeserializeMessage(rawMessage);
                    //    VersionedMessage versionedMessage = (VersionedMessage)message;

                }
                long duration = sw.ElapsedMilliseconds;
                Console.WriteLine($"Try {attempt}:");
                Console.WriteLine($" Message count: {messageCount}");
                Console.WriteLine($" Duration {duration} ms");
                Console.WriteLine($" Message version {version}");
                Console.WriteLine();
            }

        }
        // else if (step == "dd")
        //{
        //    Console.WriteLine("Deserialization 2:");
        //    var dir = Directory.GetDirectories("C:\\temp\\tp-serialization\\").OrderBy(n => n).Last();
        //    // Skip first 3 beause that is version check and debugger attach
        //    // Skip last 1 because that is test run complete
        //    var rawMessages = Directory.GetFiles(dir)
        //        // There is no SkipLast on .NET Framework, do this inefficiently by ordering in descending order
        //        .OrderByDescending(n => n).Skip(1)
        //        .OrderBy(n => n).Skip(3)
        //        .Select(File.ReadAllText).ToList();
        //    Console.WriteLine($"There are {rawMessages.Count} raw messages.");
        //    var json = JsonDataSerializer.Instance;
        //    TestRunChangedEventArgs payload6 = null;
        //    TestRunChangedEventArgs payload7 = null;
        //    foreach (var v in versions)
        //    {



        //        foreach (var attempt in Enumerable.Range(1, 2))
        //        {
        //            int testCount = 0;

        //            Stopwatch sw = Stopwatch.StartNew();
        //            Stopwatch getv = new Stopwatch();
        //            Stopwatch getp = new Stopwatch();
        //            foreach (string rawMessage in rawMessages)
        //            {
        //                TestRunChangedEventArgs payload;
        //                if (v == 6)
        //                {
        //                    getv.Start();
        //                    Message message = json.DeserializeMessage(rawMessage);
        //                    getv.Stop();
        //                    VersionedMessage versionedMessage = (VersionedMessage)message;
        //                    versionedMessage.Version = v;
        //                    getp.Start();
        //                    payload = json.DeserializePayload<TestRunChangedEventArgs>(versionedMessage);
        //                    payload6 = payload;
        //                    getp.Stop();
        //                }
        //                else
        //                {
        //                    getv.Start();
        //                    var m = JsonConvert.DeserializeObject<MessageHeader>(rawMessage, JsonDataSerializer.s_jsonSettings7);
        //                    getv.Stop();
        //                    getp.Start();
        //                    var mm = JsonConvert.DeserializeObject<PayloadedMessage<TestRunChangedEventArgs>>(rawMessage, JsonDataSerializer.s_jsonSettings7);
        //                    getp.Stop();
        //                    payload = mm.Payload;
        //                    payload7 = mm.Payload;
        //                }
        //                testCount += payload.NewTestResults.Count();
        //            }

        //            long duration = sw.ElapsedMilliseconds;
        //            Console.WriteLine($"Try {attempt}:");
        //            Console.WriteLine($" Tests: {testCount}");
        //            Console.WriteLine($" Duration {duration} ms");
        //            Console.WriteLine($"   Get header {getv.ElapsedMilliseconds} ms");
        //            Console.WriteLine($"   Get payload {getp.ElapsedMilliseconds} ms");
        //            Console.WriteLine($" Message version {v}");
        //            Console.WriteLine();

        //        }

        //    }

        //    // Make sure we kept the same data.
        //    //if (versions.Distinct().Count() > 1)
        //    //{
        //    json.SerializePayload("a", payload7).Should().BeEquivalentTo(json.SerializePayload("a", payload6));
        //    //}

        //}
        else if (step == "s")
        {
            Console.WriteLine("Serialization:");
            var dir = Directory.GetDirectories("C:\\temp\\tp-serialization\\").OrderBy(n => n).Last();
            // Skip first 3 beause that is version check and debugger attach
            // Skip last 1 because that is test run complete
            var rm = Directory.GetFiles(dir)
                // There is no SkipLast on .NET Framework, do this inefficiently by ordering in descending order
                .OrderByDescending(n => n).Skip(1)
                .OrderBy(n => n).Skip(3)
                .Select(File.ReadAllText).ToList().First();

            var json = JsonDataSerializer.Instance;
            Message m = json.DeserializeMessage(rm);
            VersionedMessage vm = (VersionedMessage)m;
            vm.Version = 6;
            var p = json.DeserializePayload<TestRunChangedEventArgs>(vm);
            var values = Enumerable.Range(0, 10_000).ToList();
            Dictionary<int, string> mmmm = new();

            foreach (var attempt in attempts)
            {
                int testCount = 0;
                Stopwatch sw = Stopwatch.StartNew();
                foreach (var _ in values)
                {
                    mmmm[version] = json.SerializePayload(MessageType.TestRunStatsChange, p, 6);
                    testCount++;
                }
                long duration = sw.ElapsedMilliseconds;
                Console.WriteLine($"Try {attempt}:");
                Console.WriteLine($" Tests: {testCount}");
                Console.WriteLine($" Duration {duration} ms");
                Console.WriteLine($" Message version {version}");
                Console.WriteLine();
            }

            //Console.WriteLine(mmmm[6]);
            //Console.WriteLine(mmmm[7]);
        }
        else
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


            foreach (var attempt in attempts)
            {

                var sw = Stopwatch.StartNew();
                var thisAssemblyPath = Assembly.GetEntryAssembly().Location;
                var here = Path.GetDirectoryName(thisAssemblyPath);
                var playground = Path.GetFullPath(Path.Combine(here, "..", "..", "..", ".."));

                var console = Path.Combine(here, "vstest.console", "vstest.console.exe");
                var consoleOptions = new ConsoleParameters
                {
                    LogFilePath = Path.Combine(here, "logs", "log.txt"),
                    TraceLevel = TraceLevel.Off,
                };

                var r = new VsTestConsoleWrapper(console, consoleOptions);

                var sourceSettings = @"
                <RunSettings>
                    <RunConfiguration>
                        <InIsolation>true</InIsolation>
                        <MaxCpuCount>0</MaxCpuCount>
                        <DisableAppDomain>true</DisableAppDomain>
                    </RunConfiguration>
                </RunSettings>
            ";

                Environment.SetEnvironmentVariable("TEST_COUNT", "10000");
                var sources = new[] {
                @"C:\p\vstest3\playground\MSTest1\bin\Debug\net472\MSTest1.dll"
            };

                r.StartSession();
                var sw2 = Stopwatch.StartNew();

                var options = new TestPlatformOptions();
                var handler = new TestRunHandler();
                r.DiscoverTests(sources, sourceSettings, options, handler);
                //var testCases = handler.TestCases;
                //r.RunTestsWithCustomTestHost(testCases, sourceSettings, options, handler, new DebuggerTestHostLauncher());

                Console.WriteLine($"Try {attempt}, version {version}.");
                Console.WriteLine($"Processed {handler.Count} tests in {sw.ElapsedMilliseconds} ms, {sw2.ElapsedMilliseconds} ms");

            }
        }
    }

    public class PlaygroundTestDiscoveryHandler : ITestDiscoveryEventsHandler, ITestDiscoveryEventsHandler2
    {
        private int _testCasesCount;


        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            Console.WriteLine($"[DISCOVERY.PROGRESS]");
            Console.WriteLine(WriteTests(discoveredTestCases));
            _testCasesCount += discoveredTestCases.Count();
        }

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            Console.WriteLine($"[DISCOVERY.COMPLETE] aborted? {isAborted}, tests count: {totalTests}");
            Console.WriteLine("Last chunk:");
            Console.WriteLine(WriteTests(lastChunk));
        }

        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            Console.WriteLine($"[DISCOVERY.COMPLETE] aborted? {discoveryCompleteEventArgs.IsAborted}, tests count: {discoveryCompleteEventArgs.TotalCount}, discovered count: {_testCasesCount}");
            Console.WriteLine("Last chunk:");
            Console.WriteLine(WriteTests(lastChunk));
            Console.WriteLine("Fully discovered:");
            Console.WriteLine(WriteSources(discoveryCompleteEventArgs.FullyDiscoveredSources));
            Console.WriteLine("Partially discovered:");
            Console.WriteLine(WriteSources(discoveryCompleteEventArgs.PartiallyDiscoveredSources));
            Console.WriteLine("Not discovered:");
            Console.WriteLine(WriteSources(discoveryCompleteEventArgs.NotDiscoveredSources));
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console.WriteLine($"[DISCOVERY.{level.ToString().ToUpper()}] {message}");
        }

        public void HandleRawMessage(string rawMessage)
        {
            Console.WriteLine($"[DISCOVERY.MESSAGE] {rawMessage}");
        }

        private static string WriteTests(IEnumerable<TestCase> testCases)
            => testCases?.Any() == true
                ? "\t" + string.Join("\n\t", testCases.Select(r => r.DisplayName))
                : "\t<empty>";

        private static string WriteSources(IEnumerable<string> sources)
            => sources?.Any() == true
                ? "\t" + string.Join("\n\t", sources)
                : "\t<empty>";
    }

    public class TestRunHandler : ITestRunEventsHandler, ITestDiscoveryEventsHandler2
    {
        public List<TestCase> TestCases = new();
        public int Count { get; private set; }

        public TestRunHandler()
        {
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console.WriteLine($"[{level.ToString().ToUpper()}]: {message}");
        }

        public void HandleRawMessage(string rawMessage)
        {
            Console.WriteLine($"[RUN.MESSAGE]: {rawMessage}");
        }

        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            Console.WriteLine($"[RUN.COMPLETE]: err: {testRunCompleteArgs.Error}, lastChunk:");
            Count += lastChunkArgs?.NewTestResults?.Count() ?? 0;
            //   Console.WriteLine(WriteTests(lastChunkArgs?.NewTestResults));
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {

            //var tpp = TestProperty.Register("TestObject.Traits", "Traits", typeof(KeyValuePair<string, string>[]), TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait, typeof(TestObject));
            //TestObject to = testRunChangedArgs.NewTestResults.First().TestCase;
            //var t = to.Properties.Contains(tpp);
            //var traits = to.GetPropertyValue(tpp, Enumerable.Empty<KeyValuePair<string, string>>().ToArray());
            //foreach (var ttt in traits)
            //{
            //    Console.WriteLine(ttt);
            //}
            Count += testRunChangedArgs?.NewTestResults?.Count() ?? 0;
            //   Console.WriteLine($"[RUN.PROGRESS]");
            //  Console.WriteLine(WriteTests(testRunChangedArgs.NewTestResults));
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            throw new NotImplementedException();
        }

        private static string WriteTests(IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult> testResults)
            => WriteTests(testResults?.Select(t => t.TestCase));

        private static string WriteTests(IEnumerable<TestCase> testCases)
            => testCases?.Any() == true
                ? "\t" + string.Join("\n\t", testCases.Select(r => r.DisplayName))
                : "\t<empty>";

        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            // if (lastChunk != null) { TestCases.AddRange(lastChunk); }

            Count += lastChunk?.Count() ?? 0;
        }

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            // if (discoveredTestCases != null) { TestCases.AddRange(discoveredTestCases); }

            Count += discoveredTestCases?.Count() ?? 0;
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
