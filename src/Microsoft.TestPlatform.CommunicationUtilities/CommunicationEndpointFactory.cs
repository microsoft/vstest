// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

using Interfaces;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Implements ICommunicationEndpointFactory.
/// </summary>
public class CommunicationEndpointFactory : ICommunicationEndpointFactory
{
    /// <inheritdoc />
    public ICommunicationEndPoint Create(ConnectionRole role)
    {
        ICommunicationEndPoint endPoint = role == ConnectionRole.Host ? new SocketServer() : new SocketClient();
        return endPoint;
    }
}
