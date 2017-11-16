// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;

    public interface ICommunicationEndPoint
    {
        /// <summary>
        /// Event raised when an endPoint is connected.
        /// </summary>
        event EventHandler<ConnectedEventArgs> Connected;

        /// <summary>
        /// Event raised when an endPoint is disconnected.
        /// </summary>
        event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <summary>
        /// Starts the endPoint and channel.
        /// </summary>
        /// <param name="endPoint">Address to connect</param>
        /// <returns>Address of the connected endPoint</returns>
        string Start(string endPoint);

        /// <summary>
        /// Stops the endPoint and closes the underlying communication channel.
        /// </summary>
        void Stop();
    }
}