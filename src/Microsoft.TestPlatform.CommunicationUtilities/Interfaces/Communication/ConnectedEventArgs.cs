// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;

    /// <summary>
    /// Provides properties for the connected communication channel.
    /// </summary>
    public class ConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedEventArgs"/> class.
        /// </summary>
        public ConnectedEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedEventArgs"/> class.
        /// </summary>
        /// <param name="channel">Communication channel for this connection.</param>
        public ConnectedEventArgs(ICommunicationChannel channel)
        {
            this.Channel = channel;
            this.Connected = true;
        }

        public ConnectedEventArgs(Exception faultException)
        {
            this.Connected = false;
            this.Fault = faultException;
        }

        /// <summary>
        /// Gets the communication channel based on this connection.
        /// </summary>
        public ICommunicationChannel Channel { get; private set; }

        /// <summary>
        /// Gets true if it's connected.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Gets the exception if it's not connected.
        /// </summary>
        public Exception Fault { get; private set; }
    }
}
