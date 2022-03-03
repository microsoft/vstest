// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

namespace vstest.ProgrammerTests.Fakes;

internal class FakeTestRuntimeProviderManager : ITestRuntimeProviderManager
{
    public FakeTestRuntimeProviderManager(FakeErrorAggregator fakeErrorAggregator)
    {
        FakeErrorAggregator = fakeErrorAggregator;
    }

    public ConcurrentQueue<FakeTestRuntimeProvider> TestRuntimeProviders { get; } = new();
    public List<FakeTestRuntimeProvider> UsedTestRuntimeProviders { get; } = new();

    public FakeErrorAggregator FakeErrorAggregator { get; }

    public void AddTestRuntimeProviders(params FakeTestRuntimeProvider[] runtimeProviders)
    {
        // This is not a bug, I am registering each provider twice because TestPlatform resolves
        // them twice for every request that does not run in-process.
        foreach (var runtimeProvider in runtimeProviders)
        {
            TestRuntimeProviders.Enqueue(runtimeProvider);
            TestRuntimeProviders.Enqueue(runtimeProvider);
        }
    }

    public ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string runConfiguration)
    {


        if (!TestRuntimeProviders.TryDequeue(out var next))
        {
            throw new InvalidOperationException("There are no more TestRuntimeProviders to be provided");
        }

        UsedTestRuntimeProviders.Add(next);
        return next;
    }

    public ITestRuntimeProvider GetTestHostManagerByUri(string hostUri)
    {
        throw new NotImplementedException();
    }
}
