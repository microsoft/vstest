// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.Fakes;

using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

internal class FakeTestRuntimeProviderManager : ITestRuntimeProviderManager
{
    public FakeTestRuntimeProviderManager(FakeErrorAggregator fakeErrorAggregator)
    {
        FakeErrorAggregator = fakeErrorAggregator;
    }

    public List<FakeTestRuntimeProvider> TestRuntimeProviders { get; } = new();
    public List<ActionRecord<FakeTestRuntimeProvider>> UsedTestRuntimeProviders { get; } = new();

    public FakeErrorAggregator FakeErrorAggregator { get; }

    public void AddTestRuntimeProviders(params FakeTestRuntimeProvider[] runtimeProviders)
    {
        TestRuntimeProviders.AddRange(runtimeProviders);
    }

    public ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string _, List<string> sources)
    {
        var allMatchingProviders = TestRuntimeProviders
            .Where(r => r.TestDlls.Select(dll => dll.Path)
            .Any(path => sources.Contains(path)))
            .ToList();

        if (allMatchingProviders.Count > 1)
        {

        }
        var match = allMatchingProviders.FirstOrDefault();
        if (match == null)
        {
            throw new InvalidOperationException("There is no FakeTestRuntimeProvider that would match the filter.");
        }

        UsedTestRuntimeProviders.Add(new ActionRecord<FakeTestRuntimeProvider>(match));
        return match;
    }

    public ITestRuntimeProvider GetTestHostManagerByUri(string hostUri)
    {
        throw new NotImplementedException();
    }
}
