// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var mstestTestDll = FileUtility.FindDll("MSTestTestProject/bin/Debug/netcoreapp3.1/MSTestTestProject.dll");
            var xunitTestDll = FileUtility.FindDll("XUnitTestProject/bin/Debug/netcoreapp3.1/XUnitTestProject.dll");
            var vstestConsoleDll = FileUtility.FindVstestConsole();

            new MyTestClient().RunTests(vstestConsoleDll, new[] { mstestTestDll, xunitTestDll });
        }
    }

    internal class MyTestClient
    {
        public void RunTests(string vstestConsolePath, string[] testDlls)
        {
            // VsTestConsoleWrapper is the client for automating test runs.
            //
            // You need to provide a path to vstest.console.exe
            // or path to vstest.console.dll. These can be obtained in multiple ways. Most commonly from:
            // C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe
            // C:\Program Files\dotnet\sdk\<version>\vstest.console.dll
            // But also from nuget cache after installing these packages:
            // - Microsoft.TestPlatform (Visual Studio insertion)
            // - Microsoft.TestPlatform.CLI (dotnet/sdk insertion)
            // - Microsoft.TestPlatform.Portable
            // Or from 'VsTestToolsInstallerInstalledToolLocation' after using VSTestInstaller task in Azure DevOps.
            //
            // Additional console parameters allow you to write verbose logs, which is very useful for debugging.
            // These logs can be also observed live by using tools like DebugView++.
            var vstestConsoleWrapper = new VsTestConsoleWrapper(vstestConsolePath, ConsoleParameters);

            // The wrapper provides three main methods:
            // - RunTests(dlls, filter, ...) - Runs all tests, or all tests that match a filter, from the given dlls.
            // - DiscoverTests(dlls, ...) - Discover tests in all given dlls, and return them as TestCases.
            // - RunTests(testCases, ...) - Runs all given test cases, that were discovered previously.
            //
            // Each method also takes an instance of a handler, that is used to communicate back updates and results.
            // For example discovered tests, executed tests, or request to attach a debugger.

            // EXAMPLE 1: RunTests(dlls, filter, ...) - Runs all tests from the given dlls.
            // We use a "special" implementation of ITestRunEventsHandler that allows us to only consume parts of the
            // functionality that we are intersted in by providing an Action. More often the ITestRunEventsHandler would
            // be implemented as a separate class, but here we prefer to have the code closer together.
            //
            // The handler below handles 2 callbacks:
            // - TestRunStatsChange = A test or multiple tests were executed, and they are sent back as an update,
            //                      to allow reporting test run progress, without waiting till the whole run is done.
            //                      This also allows the user to get results even when the test run ends up crashing,
            //                      for example because of StackOverFlowException that happens in a test.
            // - TestRunComplete = All the tests were run, or a crash occured. The first parameter (testRunComplete) holds
            //                     summarized information about the whole run. The second parameter testRunChanged, contains the
            //                     last batch of tests that were not reported via TestRunStatsChange yet.
            ITestRunEventsHandler runHandler1 = new MyTestRunEventsHandler
            {
                OnTestRunStatsChange = testRunChanged => WriteTestResults(testRunChanged.NewTestResults),
                OnTestRunComplete = (testRunComplete, testRunChanged, _, _2) =>
                {
                    if (testRunChanged != null && testRunChanged.NewTestResults != null)
                    {
                        WriteTestResults(testRunChanged.NewTestResults);
                    }
                    WriteSummary(testRunComplete);
                },
            };
            vstestConsoleWrapper.RunTests(testDlls, Settings, runHandler1);


            //ITestDiscoveryEventsHandler discoveryHandler = GetDiscoveryHandler();
            //vstestConsoleWrapper.DiscoverTests(testDlls, GetSettings(), discoveryHandler);
            //List<TestCase> discoveredTests = discoveryHandler.DiscoveredTests;
            //vstestConsoleWrapper.RunTests(discoveredTests, GetSettings(), GetRunHandler());
        }

        private string Settings =>
            @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";

        internal ConsoleParameters ConsoleParameters =>
            new ConsoleParameters
            {
                // Write verbose logs to a directory placed next to our Client executable. These logs have all the details
                // of the execution. Including the execution of vstest.console, testhost, and datacollector.
                LogFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "log.txt"),
                TraceLevel = TraceLevel.Verbose,

                // Avoid setting EnvironmentVariables until version 17.3. Setting any environment variable will remove all
                // other environment variables. Which will most likely prevent your application from starting. After 17.3
                // the variables are added / replaced and all untouched variables are preserved.
                // EnvironmentVariables = new Dictionary<string, string>()
            };

        void WriteTestResults(IEnumerable<TestResult> testResults)
        {
            foreach (var testResult in testResults)
            {
                Console.WriteLine($"{testResult.Outcome} {testResult.TestCase.FullyQualifiedName}");
            }
        }

        private void WriteSummary(TestRunCompleteEventArgs testRunComplete)
        {
            testRunComplete.TestRunStatistics.Stats.TryGetValue(TestOutcome.Passed, out long passed);
            testRunComplete.TestRunStatistics.Stats.TryGetValue(TestOutcome.Failed, out long failed);
            testRunComplete.TestRunStatistics.Stats.TryGetValue(TestOutcome.Skipped, out long skipped);

            Console.WriteLine($"Summary: Passed: {passed}, Failed: {failed}, Skipped {skipped}");
            if (testRunComplete.Error != null)
            {
                Console.WriteLine($"ERROR: {testRunComplete.Error}");
            }
        }
    }
}
