// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TestPlatform.Client.Async;

/// <summary>
/// Represents an active connection to a vstest.console process.
/// </summary>
public interface IAsyncTestSession : IAsyncDisposable
{
    /// <summary>
    /// The process ID of the vstest.console process.
    /// </summary>
    int ProcessId { get; }

    /// <summary>
    /// Whether the session is still connected to the vstest.console process.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// End the session and terminate the vstest.console process.
    /// </summary>
    Task EndSessionAsync(CancellationToken cancellationToken = default);
}
