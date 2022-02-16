// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

using System.Collections.Concurrent;

using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

internal class FakeTestRuntimeProviderManager : ITestRuntimeProviderManager
{
    public FakeTestRuntimeProviderManager(List<FakeTestRuntimeProvider> testRuntimeProviders, FakeErrorAggregator fakeErrorAggregator)
    {
        testRuntimeProviders.ForEach(TestRuntimeProviders.Enqueue);
        FakeErrorAggregator = fakeErrorAggregator;
    }

    public ConcurrentQueue<FakeTestRuntimeProvider> TestRuntimeProviders { get; } = new();
    public List<FakeTestRuntimeProvider> ProvidedTestRuntimeProviders { get; } = new();

    public FakeErrorAggregator FakeErrorAggregator { get; }

    public ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string runConfiguration)
    {
        if (!TestRuntimeProviders.TryDequeue(out var next))
        {
            throw new InvalidOperationException("There are no more TestRuntimeProviders to be provided");
        }

        ProvidedTestRuntimeProviders.Add(next);
        return next;
    }

    public ITestRuntimeProvider GetTestHostManagerByUri(string hostUri)
    {
        throw new NotImplementedException();
    }
}
