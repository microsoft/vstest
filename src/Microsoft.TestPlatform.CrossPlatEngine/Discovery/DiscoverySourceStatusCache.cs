// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;

internal class DiscoverySourceStatusCache
{
    private readonly ConcurrentDictionary<string, DiscoveryStatus> _sourcesWithDiscoveryStatus = new();

    private string? _previousSource;

    public void MarkSourcesWithStatus(IEnumerable<string?> sources, DiscoveryStatus status)
    {
        if (sources is null)
        {
            return;
        }

        foreach (var source in sources)
        {
            if (source is null)
            {
                continue;
            }

            _sourcesWithDiscoveryStatus.AddOrUpdate(source,
                _ =>
                {
                    if (status != DiscoveryStatus.NotDiscovered)
                    {
                        EqtTrace.Warning($"DiscoverySourceStatusCache.MarkSourcesWithStatus: Undiscovered {source}.");
                    }

                    return status;
                },
                (_, _) =>
                {
                    EqtTrace.Info($"DiscoverySourceStatusCache.MarkSourcesWithStatus: Marking {source} with {status} status.");
                    return status;
                });
        }
    }

    public void MarkTheLastChunkSourcesAsFullyDiscovered(IEnumerable<TestCase> lastChunk)
    {
        // When all testcases count in project is dividable by chunk size (e.g. 100 tests and
        // chunk size of 10) then lastChunk is coming as empty. In this case, we need to take
        // the lastSource and mark it as FullyDiscovered.
        IEnumerable<string?> lastChunkSources = lastChunk.Any()
            ? lastChunk.Select<TestCase, string?>(testcase => testcase.Source)
            : new[] { _previousSource };

        MarkSourcesWithStatus(lastChunkSources, DiscoveryStatus.FullyDiscovered);
    }

    public void MarkSourcesBasedOnDiscoveredTestCases(IEnumerable<TestCase> testCases)
    {
        if (testCases is null)
        {
            return;
        }

        foreach (var testCase in testCases)
        {
            string currentSource = testCase.Source;

            // We rely on the fact that sources are processed in a sequential way, which means that
            // when we receive a different source than the previous, we can assume that the previous
            // source was fully discovered.
            if (_previousSource is null || _previousSource == currentSource)
            {
                MarkSourcesWithStatus(new[] { currentSource }, DiscoveryStatus.PartiallyDiscovered);
            }
            else if (currentSource != _previousSource)
            {
                MarkSourcesWithStatus(new[] { _previousSource }, DiscoveryStatus.FullyDiscovered);
                MarkSourcesWithStatus(new[] { currentSource }, DiscoveryStatus.PartiallyDiscovered);
            }

            _previousSource = currentSource;
        }
    }

    public List<string> GetSourcesWithStatus(DiscoveryStatus discoveryStatus)
        => GetSourcesWithStatus(discoveryStatus, _sourcesWithDiscoveryStatus);

    public static List<string> GetSourcesWithStatus(DiscoveryStatus discoveryStatus,
        ConcurrentDictionary<string, DiscoveryStatus> sourcesWithDiscoveryStatus)
    {
        // If by some accident SourcesWithDiscoveryStatus map is empty we will return empty list
        return sourcesWithDiscoveryStatus is null || sourcesWithDiscoveryStatus.IsEmpty
            ? new List<string>()
            : sourcesWithDiscoveryStatus
                .Where(source => source.Value == discoveryStatus)
                .Select(source => source.Key)
                .ToList();
    }
}
