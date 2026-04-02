// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TestPlatform.Client.Async.Internal;

namespace Microsoft.TestPlatform.Client.Async;

/// <summary>
/// Represents an active connection to a vstest.console process.
/// Owns the process and socket connection for its lifetime.
/// </summary>
internal sealed class AsyncTestSession : IAsyncTestSession
{
    internal readonly AsyncRequestSender Sender;
    internal readonly ProcessManager Process;
    private volatile bool _isConnected;

    internal AsyncTestSession(AsyncRequestSender sender, ProcessManager process)
    {
        Sender = sender;
        Process = process;
        _isConnected = true;

        // Monitor process exit to update IsConnected.
        _ = MonitorProcessAsync();
    }

    /// <inheritdoc />
    public int ProcessId => Process.ProcessId;

    /// <inheritdoc />
    public bool IsConnected => _isConnected && !Process.HasExited;

    /// <inheritdoc />
    public async Task EndSessionAsync(CancellationToken cancellationToken)
    {
        if (!_isConnected) return;
        _isConnected = false;

        try
        {
            await Sender.SendMessageAsync(ProtocolConstants.SessionEnd, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Process may have already exited.
        }

        // Wait briefly for graceful exit.
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = linkedCts.Token.Register(() => cancelTcs.TrySetResult(true));
            await Task.WhenAny(Process.ExitedTask, cancelTcs.Task).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Timeout waiting for graceful exit — will be killed in Dispose.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await EndSessionAsync(CancellationToken.None).ConfigureAwait(false);
        Sender.Dispose();
        Process.Dispose();
    }

    private async Task MonitorProcessAsync()
    {
        await Process.ExitedTask.ConfigureAwait(false);
        _isConnected = false;
    }
}
