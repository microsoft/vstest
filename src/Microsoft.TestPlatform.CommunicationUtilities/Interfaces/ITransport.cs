// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;

    /// <summary>
    /// The transport Layer Interface
    /// </summary>
    public interface ITransport : IDisposable
    {
        /// <summary>
        /// Initializes Transport Layer depending upon TransportProtocol
        /// </summary>
        /// <returns>Commnunication Manager</returns>
        int InitializeTransportLayer();

        /// <summary>
        /// Waits for the connection over transport layer to established
        /// </summary>
        /// <param name="connectionTimeout">Time to wait for connection</param>
        /// <returns>True if connection is established</returns>
        bool WaitForConnectionToEstablish(int connectionTimeout);
    }
}
