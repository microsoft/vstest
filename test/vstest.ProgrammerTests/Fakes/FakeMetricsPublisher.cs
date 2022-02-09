// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeMetricsPublisher : IMetricsPublisher
{
    public FakeMetricsPublisher(FakeErrorAggregator fakeErrorAggregator)
    {
        FakeErrorAggregator = fakeErrorAggregator;
    }

    public FakeErrorAggregator FakeErrorAggregator { get; }

    public void Dispose()
    {
        // do nothing
    }

    public void PublishMetrics(string eventName, IDictionary<string, object> metrics)
    {
        // TODO: does nothing but probably should
    }
}
