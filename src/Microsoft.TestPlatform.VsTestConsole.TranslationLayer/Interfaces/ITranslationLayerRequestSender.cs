// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Defines contract to send test platform requests to test host
    /// </summary>
    internal interface ITranslationLayerRequestSender : IDisposable
    {
        /// <summary>
        /// Initializes the communication for sending requests
        /// </summary>
        /// <returns>Port Number of the communication channel</returns>
        int InitializeCommunication();

        /// <summary>
        /// Waits for Request Handler to be connected 
        /// </summary>
        /// <param name="connectionTimeout">Time to wait for connection</param>
        /// <returns>True, if Handler is connected</returns>
        bool WaitForRequestHandlerConnection(int connectionTimeout);

        /// <summary>
        /// Asynchronous equivalent of <see cref="InitializeCommunication"/> and <see cref="WaitForRequestHandlerConnection(int)"/>.
        /// </summary>
        Task<int> InitializeCommunicationAsync(int clientConnectionTimeout);

        /// <summary>
        /// Close the Sender 
        /// </summary>
        void Close();

        /// <summary>
        /// Initializes the Extensions while probing additional extension paths 
        /// </summary>
        /// <param name="pathToAdditionalExtensions">Paths to check for additional extensions</param>
        void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions);

        /// <summary>
        /// Discovers the tests
        /// </summary>
        /// <param name="sources">Sources for discovering tests</param>
        /// <param name="runSettings">Run settings for discovering tests</param>
        /// <param name="discoveryEventsHandler">EventHandler for discovery events</param>
        void DiscoverTests(IEnumerable<string> sources, string runSettings, ITestDiscoveryEventsHandler discoveryEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="DiscoverTests(IEnumerable{string}, string, ITestDiscoveryEventsHandler)"/>.
        /// </summary>
        Task DiscoverTestsAsync(IEnumerable<string> sources, string runSettings, ITestDiscoveryEventsHandler discoveryEventsHandler);

        /// <summary>
        /// Starts the TestRun with given sources and criteria
        /// </summary>
        /// <param name="sources">Sources for test run</param>
        /// <param name="runSettings">RunSettings for test run</param>
        /// <param name="runEventsHandler">EventHandler for test run events</param>
        void StartTestRun(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler runEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="StartTestRun(IEnumerable{string}, string, ITestRunEventsHandler)"/>.
        /// </summary>
        Task StartTestRunAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler runEventsHandler);

        /// <summary>
        /// Starts the TestRun with given test cases and criteria
        /// </summary>
        /// <param name="testCases">TestCases to run</param>
        /// <param name="runSettings">RunSettings for test run</param>
        /// <param name="runEventsHandler">EventHandler for test run events</param>
        void StartTestRun(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler runEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="StartTestRun(IEnumerable{TestCase}, string, ITestRunEventsHandler)"/>.
        /// </summary>
        Task StartTestRunAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler runEventsHandler);

        /// <summary>
        /// Starts the TestRun with given sources and criteria with custom test host
        /// </summary>
        /// <param name="sources">Sources for test run</param>
        /// <param name="runSettings">RunSettings for test run</param>
        /// <param name="runEventsHandler">EventHandler for test run events</param>
        /// <param name="customTestHostLauncher">Custom TestHost launcher</param>
        void StartTestRunWithCustomHost(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Asynchronous equivalent of <see cref="StartTestRunWithCustomHost(IEnumerable{string}, string, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task StartTestRunWithCustomHostAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Starts the TestRun with given test cases and criteria with custom test host
        /// </summary>
        /// <param name="testCases">TestCases to run</param>
        /// <param name="runSettings">RunSettings for test run</param>
        /// <param name="runEventsHandler">EventHandler for test run events</param>
        /// <param name="customTestHostLauncher">Custom TestHost launcher</param>
        void StartTestRunWithCustomHost(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Asynchronous equivalent of <see cref="StartTestRunWithCustomHost(IEnumerable{TestCase}, string, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task StartTestRunWithCustomHostAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Ends the Session
        /// </summary>
        void EndSession();

        /// <summary>
        /// Cancel the test run
        /// </summary>
        void CancelTestRun();

        /// <summary>
        /// Abort the test run
        /// </summary>
        void AbortTestRun();

        /// <summary>
        /// On process exit unblocks communication waiting calls
        /// </summary>
        void OnProcessExited();
    }
}
