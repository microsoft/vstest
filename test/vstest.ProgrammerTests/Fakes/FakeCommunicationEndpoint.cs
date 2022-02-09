// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;

using vstest.ProgrammerTests.Fakes;

namespace vstest.ProgrammerTests.CommandLine;

internal class FakeCommunicationEndpoint : ICommunicationEndPoint
{
    private bool _stopped;

    public FakeCommunicationEndpoint(FakeErrorAggregator fakeErrorAggregator)
    {
        FakeErrorAggregator = fakeErrorAggregator;
    }

    public FakeErrorAggregator FakeErrorAggregator { get; }

    public event EventHandler<ConnectedEventArgs>? Connected;
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    public string Start(string endPoint)
    {
        // TODO: insert this from the outside so some channel manager can give us overview of the open channels?
        Connected?.SafeInvoke(this, new ConnectedEventArgs(new FakeCommunicationChannel(FakeErrorAggregator)), "FakeCommunicationEndpoint.Start");
        return endPoint;
    }

    public void Stop()
    {
        if (!_stopped)
        {
            // Do not allow stop to be called multiple times, because it will end up calling us back and stack overflows.
            _stopped = true;

            Disconnected?.Invoke(this, new DisconnectedEventArgs());
        }
    }
}
