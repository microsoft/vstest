// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;

    public interface ICommunicationEndPoint
    {
        /// <summary>
        /// Event raised when an endpoint is connected.
        /// </summary>
        event EventHandler<ConnectedEventArgs> Connected;

        /// <summary>
        /// Event raised when an endpoint is disconnected.
        /// </summary>
        event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <summary>
        /// Starts the endpoint and channel.
        /// </summary>
        /// <param name="endpoint">Address to connect</param>
        /// <returns>Address of the connected endpoint</returns>
        string Start(string endpoint);

        /// <summary>
        /// Stops the endpoint and closes the underlying communication channel.
        /// </summary>
        void Stop();
    }
}