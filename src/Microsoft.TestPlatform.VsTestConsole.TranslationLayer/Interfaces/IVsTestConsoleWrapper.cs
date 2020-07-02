// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Controller for various test operations on the test runner.
    /// </summary>
    public interface IVsTestConsoleWrapper : IVsTestConsoleWrapperAsync
    {
        /// <summary>
        /// Starts the test runner process and readies for requests.
        /// </summary>
        void StartSession();

        /// <summary>
        /// Initialize the TestPlatform with Paths to extensions like adapters, loggers and any other extensions
        /// </summary>
        /// <param name="pathToAdditionalExtensions">Full Paths to extension DLLs</param>
        void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions);

        /// <summary>
        /// Start Discover Tests for the given sources and discovery settings.
        /// </summary>
        /// <param name="sources">List of source assemblies, files to discover tests</param>
        /// <param name="discoverySettings">Settings XML for test discovery</param>
        /// <param name="discoveryEventsHandler">EventHandler to receive discovery events</param>
        void DiscoverTests(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler);

        /// <summary>
        /// Start Discover Tests for the given sources and discovery settings.
        /// </summary>
        /// <param name="sources">List of source assemblies, files to discover tests</param>
        /// <param name="discoverySettings">Settings XML for test discovery</param>
        /// <param name="options">Options to be passed into the platform.</param>
        /// <param name="discoveryEventsHandler">EventHandler to receive discovery events</param>
        void DiscoverTests(IEnumerable<string> sources, string discoverySettings, TestPlatformOptions options, ITestDiscoveryEventsHandler2 discoveryEventsHandler);        

        /// <summary>
        /// Cancels the last discovery request.
        /// </summary>
        new void CancelDiscovery();

        /// <summary>
        /// Starts a test run given a list of sources.
        /// </summary>
        /// <param name="sources">Sources to Run tests on</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        void RunTests(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Starts a test run given a list of sources.
        /// </summary>
        /// <param name="sources">Sources to Run tests on</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="options">Options to be passed into the platform.</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        void RunTests(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Starts a test run given a list of test cases
        /// </summary>
        /// <param name="testCases">TestCases to run</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        void RunTests(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Starts a test run given a list of test cases
        /// </summary>
        /// <param name="testCases">TestCases to run</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="options">Options to be passed into the platform.</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        void RunTests(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Starts a test run given a list of sources by giving caller an option to start their own test host.
        /// </summary>
        /// <param name="sources">Sources to Run tests on</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        /// <param name="customTestHostLauncher">Custom test host launcher for the run.</param>
        void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Starts a test run given a list of sources by giving caller an option to start their own test host.
        /// </summary>
        /// <param name="sources">Sources to Run tests on</param>
        /// <param name="runSettings">RunSettings XML to run the tests</param>
        /// <param name="options">Options to be passed into the platform.</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events</param>
        /// <param name="customTestHostLauncher">Custom test host launcher for the run.</param>
        void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Starts a test run given a list of test cases by giving caller an option to start their own test host
        /// </summary>
        /// <param name="testCases">TestCases to run.</param>
        /// <param name="runSettings">RunSettings XML to run the tests.</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events.</param>
        /// <param name="customTestHostLauncher">Custom test host launcher for the run.</param>
        void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Starts a test run given a list of test cases by giving caller an option to start their own test host
        /// </summary>
        /// <param name="testCases">TestCases to run.</param>
        /// <param name="runSettings">RunSettings XML to run the tests.</param>
        /// <param name="options">Options to be passed into the platform.</param>
        /// <param name="testRunEventsHandler">EventHandler to receive test run events.</param>
        /// <param name="customTestHostLauncher">Custom test host launcher for the run.</param>
        void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Cancel the last test run.
        /// </summary>
        new void CancelTestRun();

        /// <summary>
        /// Abort the last test run.
        /// </summary>
        new void AbortTestRun();

        /// <summary>
        /// Ends the test session and stops processing requests.
        /// </summary>
        new void EndSession();
    }
}
