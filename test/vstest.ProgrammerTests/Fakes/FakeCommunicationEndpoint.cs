// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

namespace vstest.ProgrammerTests.CommandLine;

internal class FakeCommunicationEndpoint : ICommunicationEndPoint
{
    public FakeCommunicationEndpoint()
    {
    }

    public event EventHandler<ConnectedEventArgs> Connected;
    public event EventHandler<DisconnectedEventArgs> Disconnected;

    public string Start(string endPoint)
    {
        Connected?.Invoke(this, new ConnectedEventArgs());
        return endPoint;
    }

    public void Stop()
    {
        Disconnected?.Invoke(this, new DisconnectedEventArgs());
    }
}
