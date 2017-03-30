// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Defines contract to send test platform requests to test host
    /// </summary>
    public interface ITestRequestSender : IDisposable
    {
        /// <summary>
        /// Initializes the communication for sending requests
        /// </summary>
        /// <returns>Port Number of the communication channel</returns>
        int InitializeCommunication();

        /// <summary>
        /// Used for protocol version check with TestHost
        /// </summary>
        /// <returns></returns>
        bool CheckVersionWithTestHost();

        /// <summary>
        /// Waits for Request Handler to be connected 
        /// </summary>
        /// <param name="connectionTimeout">Time to wait for connection</param>
        /// <returns>True, if Handler is connected</returns>
        bool WaitForRequestHandlerConnection(int connectionTimeout);

        /// <summary>
        /// Close the Sender
        /// </summary>
        void Close();

        /// <summary>
        /// Initializes the Discovery
        /// </summary>
        /// <param name="pathToAdditionalExtensions">Paths to check for additional extensions</param>
        /// <param name="loadOnlyWellKnownExtensions">Load only well only extensions</param>
        void InitializeDiscovery(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions);

        /// <summary>
        /// Initializes the Execution
        /// </summary>
        /// <param name="pathToAdditionalExtensions">Paths to check for additional extensions</param>
        /// <param name="loadOnlyWellKnownExtensions">Load only well only extensions</param>
        void InitializeExecution(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions);

        /// <summary>
        /// Discovers the tests
        /// </summary>
        /// <param name="discoveryCriteria">DiscoveryCriteria for discovery</param>
        /// <param name="eventHandler">EventHandler for discovery events</param>
        void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler);

        /// <summary>
        /// Starts the TestRun with given sources and criteria
        /// </summary>
        /// <param name="runCriteria">RunCriteria for test run</param>
        /// <param name="eventHandler">EventHandler for test run events</param>
        void StartTestRun(TestRunCriteriaWithSources runCriteria, ITestRunEventsHandler eventHandler);

        /// <summary>
        /// Starts the TestRun with given test cases and criteria
        /// </summary>
        /// <param name="runCriteria">RunCriteria for test run</param>
        /// <param name="eventHandler">EventHandler for test run events</param>
        void StartTestRun(TestRunCriteriaWithTests runCriteria, ITestRunEventsHandler eventHandler);

        /// <summary>
        /// Ends the Session
        /// </summary>
        void EndSession();

        /// <summary>
        /// Send the request to cancel the test run
        /// </summary>
        void SendTestRunCancel();

        /// <summary>
        /// Send the request to abort the test run
        /// </summary>
        void SendTestRunAbort();

        /// <summary>
        /// Handle client process exit
        /// </summary>
        /// <param name="stdError">Standard error output</param>
        void OnClientProcessExit(string stdError);
    }
}
