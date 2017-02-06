// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    /// <summary>
    /// Interface for sending test case events from test exectuion process to data collection process
    /// </summary>
    internal interface IDataCollectionTestCaseEventSender
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
        /// Closes the connection
        /// </summary>
        void Close();
    }
}
