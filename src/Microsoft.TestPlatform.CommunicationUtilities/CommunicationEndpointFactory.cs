// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Implements ICommunicationEndpointFactory.
    /// </summary>
    public class CommunicationEndpointFactory : ICommunicationEndpointFactory
    {
        /// <inheritdoc />
        public ICommunicationEndPoint Create(ConnectionRole role)
        {
            ICommunicationEndPoint endPoint;
            if (role == ConnectionRole.Host)
            {
                endPoint = new SocketServer();
            }
            else
            {
                endPoint = new SocketClient();
            }

            return endPoint;
        }
    }
}
