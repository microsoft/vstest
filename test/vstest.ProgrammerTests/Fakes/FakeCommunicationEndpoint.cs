// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;

using vstest.ProgrammerTests.Fakes;

namespace vstest.ProgrammerTests.CommandLine;

internal class FakeCommunicationEndpoint : ICommunicationEndPoint
{
    public FakeCommunicationEndpoint()
    {
    }

    public event EventHandler<ConnectedEventArgs>? Connected;
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    public string Start(string endPoint)
    {
        // TODO: insert this from the outside so some channel manager can give us overview of the open channels?
        Connected?.SafeInvoke(this, new ConnectedEventArgs(new FakeCommunicationChannel()), "FakeCommunicationEndpoint.Start");
        return endPoint;
    }

    public void Stop()
    {
        Disconnected?.Invoke(this, new DisconnectedEventArgs());
    }
}
