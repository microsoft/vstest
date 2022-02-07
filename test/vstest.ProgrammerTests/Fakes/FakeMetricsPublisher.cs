// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeMetricsPublisher : IMetricsPublisher
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void PublishMetrics(string eventName, IDictionary<string, object> metrics)
    {
        throw new NotImplementedException();
    }
}
