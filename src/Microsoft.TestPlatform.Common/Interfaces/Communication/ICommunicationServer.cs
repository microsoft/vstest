// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;

    /// <summary>
    /// Server interface for inter-process communications in test platform. A server can serve only a single
    /// client.
    /// </summary>
    public interface ICommunicationServer
    {
        /// <summary>
        /// Event raised when a client is connected to the server.
        /// </summary>
        event EventHandler<ConnectedEventArgs> ClientConnected;

        /// <summary>
        /// Event raised when a client is disconnected from the server.
        /// </summary>
        event EventHandler<DisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// Starts a communication server.
        /// </summary>
        /// <returns>Connection parameters for a client to connect.</returns>
        string Start();

        /// <summary>
        /// Stops the server and closes the underlying communication channel.
        /// </summary>
        void Stop();
    }
}
