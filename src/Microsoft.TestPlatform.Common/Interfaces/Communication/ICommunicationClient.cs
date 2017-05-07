// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;

    /// <summary>
    /// Client interface for inter-process communications in test platform.
    /// </summary>
    public interface ICommunicationClient
    {
        /// <summary>
        /// Event raised when the client is connected to server.
        /// </summary>
        event EventHandler<ConnectedEventArgs> ServerConnected;

        /// <summary>
        /// Event raise when connection with server is broken.
        /// </summary>
        event EventHandler<DisconnectedEventArgs> ServerDisconnected;

        /// <summary>
        /// Connect to the server specified in <see cref="connectionInfo"/>.
        /// </summary>
        /// <param name="connectionInfo">Parameters to connect to server.</param>
        void Start(string connectionInfo);

        /// <summary>
        /// Close the communication channel and stop the client.
        /// </summary>
        void Stop();
    }
}
