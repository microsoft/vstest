// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

using vstest.ProgrammerTests.CommandLine.Fakes;
using vstest.ProgrammerTests.Fakes;

internal class FakeTestFixtureHost
{
    private readonly Fixture _fixture;

    public int Id { get; }
    public List<FakeTestDllFile> Dlls { get; }
    public FakeTestRuntimeProvider FakeTestRuntimeProvider { get; }
    public FakeCommunicationEndpoint FakeCommunicationEndpoint { get; }
    public FakeCommunicationChannel FakeCommunicationChannel { get; }
    public List<RequestResponsePair<string, FakeMessage>> Responses { get; }
    public FakeProcess Process { get; internal set; }

    public FakeTestFixtureHost(
        Fixture fixture,
        int id, List<FakeTestDllFile> dlls,
        FakeTestRuntimeProvider fakeTestRuntimeProvider,
        FakeCommunicationEndpoint fakeCommunicationEndpoint,
        FakeCommunicationChannel fakeCommunicationChannel,
        FakeProcess process,
        List<RequestResponsePair<string, FakeMessage>> responses)
    {
        _fixture = fixture;
        Id = id;
        Dlls = dlls;
        FakeTestRuntimeProvider = fakeTestRuntimeProvider;
        FakeCommunicationEndpoint = fakeCommunicationEndpoint;
        FakeCommunicationChannel = fakeCommunicationChannel;
        Process = process;
        Responses = responses;
    }
}
