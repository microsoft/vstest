// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

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

    // Writes header as 7bitEncodedInteger (see https://msdn.microsoft.com/en-us/library/dd946975(v=office.12).aspx)
    // Followed by data as bytes
}