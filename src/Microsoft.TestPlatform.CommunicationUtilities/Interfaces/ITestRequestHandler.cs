// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Defines the contract for handling test platform requests
    /// </summary>
    public interface ITestRequestHandler
    {
        /// <summary>
        /// Setups client based on port
        /// </summary>
        /// <param name="port">port number to connect</param>
        void InitializeCommunication(int port);

        /// <summary>
        /// Waits for Request Handler to connect to Request Sender
        /// </summary>
        /// <param name="connectionTimeout">Timeout for establishing connection</param>
        /// <returns>True if connected, false if timed-out</returns>
        bool WaitForRequestSenderConnection(int connectionTimeout);

        /// <summary>
        /// Listens to the commands from server
        /// </summary>
        /// <param name="testHostManagerFactory">the test host manager.</param>
        void ProcessRequests(ITestHostManagerFactory testHostManagerFactory);

        /// <summary>
        /// Closes the connection
        /// </summary>
        void Close();

        /// <summary>
        /// The send test cases.
        /// </summary>
        /// <param name="discoveredTestCases"> The discovered test cases. </param>
        void SendTestCases(IEnumerable<TestCase> discoveredTestCases);

        /// <summary>
        /// The send test run statistics.
        /// </summary>
        /// <param name="testRunChangedArgs"> The test run changed args. </param>
        void SendTestRunStatistics(TestRunChangedEventArgs testRunChangedArgs);

        /// <summary>
        /// Sends the logs back to the server.
        /// </summary>
        /// <param name="messageLevel"> The message level. </param>
        /// <param name="message"> The message. </param>
        void SendLog(TestMessageLevel messageLevel, string message);

        /// <summary>
        /// The send execution complete.
        /// </summary>
        /// <param name="testRunCompleteArgs"> The test run complete args. </param>
        /// <param name="lastChunkArgs"> The last chunk args. </param>
        /// <param name="runContextAttachments"> The run context attachments. </param>
        /// <param name="executorUris"> The executor uris. </param>
        void SendExecutionComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris);

        /// <summary>
        /// The discovery complete handler
        /// </summary>
        /// <param name="totalTests"> The total Tests. </param>
        /// <param name="lastChunk"> The last Chunk. </param>
        /// <param name="isAborted"> The is Aborted. </param>
        void DiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted);

        /// <summary>
        /// Launches a process with a given process info under debugger
        /// Adapter get to call into this to launch any additional processes under debugger
        /// </summary>
        /// <param name="testProcessStartInfo">Process start info</param>
        /// <returns>ProcessId of the launched process</returns>
        int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo);
    }
}
