// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.TestPlatform.Client.Async;

/// <summary>
/// Result of a test discovery operation.
/// </summary>
public class DiscoveryResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryResult"/> class.
    /// </summary>
    public DiscoveryResult(IReadOnlyList<TestCase> testCases, long totalCount, bool isAborted)
    {
        TestCases = testCases;
        TotalCount = totalCount;
        IsAborted = isAborted;
    }

    /// <summary>
    /// The discovered test cases.
    /// </summary>
    public IReadOnlyList<TestCase> TestCases { get; }

    /// <summary>
    /// The total number of tests reported by the server.
    /// </summary>
    public long TotalCount { get; }

    /// <summary>
    /// Whether discovery was aborted.
    /// </summary>
    public bool IsAborted { get; }
}
