// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Controller for various test operations on the test runner.
    /// </summary>
    public interface IVsTestConsoleWrapper
    {
        /// <summary>
        /// Starts the test runner process and readies for requests.
        /// </summary>
        void StartSession();

        /// <summary>
        /// Initialize the TestPlatform with Paths to extensions like adapters, loggers and any other extensions
        /// </summary>
        /// <param name="pathToAdditionalExtensions">Folder Paths to where extension DLLs are present</param>
        void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions);

        /// <summary>
        /// Start Discover Tests for the given sources and discovery settings.
        /// </summary>
        /// <param name="sources">List of source assemblies, files to discover tests</param>
        /// <param name="discoverySettings">Settings XML for test discovery</param>
        /// <param name="discoveryEventsHandler">EventHandler to receive discovery events</param>
        void DiscoverTests(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler);

        /// <summary>
        /// Cancels the last discovery request.
        /// </summary>
        void CancelDiscovery();

        /// <summary>
        /// Starts a test run given a list of sources.
        /// </summary>
        /// <param name="sources">Sources to Run tests on</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        void RunTests(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Starts a test run given a list of test cases
        /// </summary>
        /// <param name="testCases">TestCases to run</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        void RunTests(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Starts a test run given a list of sources by giving caller an option to start their own test host.
        /// </summary>
        /// <param name="sources">Sources to Run tests on</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        /// <param name="customTestHostLauncher">Custom test host launcher for the run.</param>
        void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Starts a test run given a list of test cases by giving caller an option to start their own test host
        /// </summary>
        /// <param name="testCases">TestCases to run.</param>
        /// <param name="runSettings">RunSettings XML to run the tests.</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events.</param>
        /// <param name="customTestHostLauncher">Custom test host launcher for the run.</param>
        void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Cancel the last test run.
        /// </summary>
        void CancelTestRun();

        /// <summary>
        /// Abort the last test run.
        /// </summary>
        void AbortTestRun();
        
        /// <summary>
        /// Ends the test session and stops processing requests.
        /// </summary>
        void EndSession();
    }
}
