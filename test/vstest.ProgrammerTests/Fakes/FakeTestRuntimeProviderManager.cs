// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.Fakes;

using System.Collections.Concurrent;

using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

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
        foreach (var runtimeProvider in runtimeProviders)
        {
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
