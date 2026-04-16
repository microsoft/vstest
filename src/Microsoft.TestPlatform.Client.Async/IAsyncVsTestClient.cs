// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Microsoft.TestPlatform.Client.Async;

/// <summary>
/// Async client for communicating with vstest.console processes.
/// Supports multiple concurrent sessions with no shared static state.
/// </summary>
public interface IAsyncVsTestClient : IAsyncDisposable
{
    /// <summary>
    /// Start a vstest.console session by launching the process and establishing a socket connection.
    /// </summary>
    /// <param name="vstestConsolePath">Path to vstest.console.exe or vstest.console.dll.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A connected test session.</returns>
    Task<IAsyncTestSession> StartSessionAsync(
        string vstestConsolePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discover tests in the given sources.
    /// </summary>
    Task<DiscoveryResult> DiscoverTestsAsync(
        IAsyncTestSession session,
        IEnumerable<string> sources,
        string? runSettings = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run tests in the given sources.
    /// </summary>
    Task<TestRunResult> RunTestsAsync(
        IAsyncTestSession session,
        IEnumerable<string> sources,
        string? runSettings = null,
        IProgress<TestRunChangedEventArgs>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run specific test cases.
    /// </summary>
    Task<TestRunResult> RunTestsAsync(
        IAsyncTestSession session,
        IEnumerable<TestCase> testCases,
        string? runSettings = null,
        IProgress<TestRunChangedEventArgs>? progress = null,
        CancellationToken cancellationToken = default);
}
