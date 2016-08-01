// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces
{
    /// <summary>
    /// The DataCollectionRequestHandler interface.
    /// </summary>
    internal interface IDataCollectionRequestHandler
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
        void ProcessRequests();

        /// <summary>
        /// Closes the connection
        /// </summary>
        void Close();
    }
}