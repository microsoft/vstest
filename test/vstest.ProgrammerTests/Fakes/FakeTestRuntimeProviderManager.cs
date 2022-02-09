// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeTestRuntimeProviderManager : ITestRuntimeProviderManager
{
    public FakeTestRuntimeProviderManager(FakeProcessHelper fakeProcessHelper, FakeCommunicationEndpoint fakeCommunicationEndpoint, FakeErrorAggregator fakeErrorAggregator)
    {
        TestRuntimeProvider = new FakeTestRuntimeProvider(fakeProcessHelper, fakeCommunicationEndpoint);
        FakeErrorAggregator = fakeErrorAggregator;
    }

    public FakeTestRuntimeProvider TestRuntimeProvider { get; private set; }
    public FakeErrorAggregator FakeErrorAggregator { get; }

    public ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string runConfiguration)
    {
        return TestRuntimeProvider;
    }

    public ITestRuntimeProvider GetTestHostManagerByUri(string hostUri)
    {
        throw new NotImplementedException();
    }
}
