// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TestPlatform.Client.Async.Internal;

/// <summary>
/// Sends and receives vstest protocol messages over a TCP socket.
/// Uses length-prefixed framing (BinaryWriter/BinaryReader) matching the
/// LengthPrefixCommunicationChannel in the existing vstest codebase.
/// All I/O is fully async with CancellationToken support.
/// </summary>
internal sealed class AsyncRequestSender : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null, // use exact property names
    };

    private readonly TcpListener _listener;
    private TcpClient? _client;
    private BinaryReader? _reader;
    private BinaryWriter? _writer;
    private int _negotiatedVersion;

    public AsyncRequestSender()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
    }

    /// <summary>
    /// The port the server is listening on.
    /// </summary>
    public int Port
    {
        get
        {
            var endpoint = (IPEndPoint)_listener.LocalEndpoint;
            return endpoint.Port;
        }
    }

    /// <summary>
    /// Start listening for the vstest.console process to connect.
    /// </summary>
    public void StartListening()
    {
        _listener.Start();
    }

    /// <summary>
    /// Wait for the vstest.console process to connect and perform the version handshake.
    /// </summary>
    public async Task WaitForConnectionAsync(ProcessManager processManager, CancellationToken cancellationToken)
    {
        // Wait for either the connection or process exit.
        // AcceptTcpClientAsync() does not accept CancellationToken on netstandard2.0;
        // we handle cancellation via WhenAny with a cancellation TCS instead.
#pragma warning disable CA2016
        var acceptTask = _listener.AcceptTcpClientAsync();
#pragma warning restore CA2016
        var exitTask = processManager.ExitedTask;
        var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var registration = cancellationToken.Register(() => cancelTcs.TrySetResult(true));

        var completed = await Task.WhenAny(acceptTask, exitTask, cancelTcs.Task).ConfigureAwait(false);

        if (completed == cancelTcs.Task)
        {
            _listener.Stop();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (completed == exitTask)
        {
            _listener.Stop();
            throw processManager.CreateExitException("connection");
        }

        _client = await acceptTask.ConfigureAwait(false);
        _listener.Stop();

        var stream = new BufferedStream(_client.GetStream(), 65536);
        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        await PerformHandshakeAsync(processManager, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a protocol message with a typed payload.
    /// </summary>
    public Task SendMessageAsync<T>(string messageType, T payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = new
        {
            MessageType = messageType,
            Version = _negotiatedVersion,
            Payload = payload,
        };

        string json = JsonSerializer.Serialize(message, JsonOptions);
        WriteString(json);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Send a protocol message with no payload.
    /// </summary>
    public Task SendMessageAsync(string messageType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = new
        {
            MessageType = messageType,
            Version = _negotiatedVersion,
        };

        string json = JsonSerializer.Serialize(message, JsonOptions);
        WriteString(json);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Receive the next protocol message. Returns null if the connection is closed.
    /// This monitors the process for unexpected exits and throws immediately if it dies.
    /// </summary>
    public async Task<ProtocolMessage?> ReceiveMessageAsync(ProcessManager processManager, CancellationToken cancellationToken)
    {
        // BinaryReader.ReadString() is synchronous. Run it on a thread pool thread
        // so we can race it against process exit and cancellation.
        var readTask = Task.Run(() =>
        {
            try
            {
                string json = ReadString();
                return json;
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }, CancellationToken.None);

        var exitTask = processManager.ExitedTask;
        var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => cancelTcs.TrySetResult(true));

        var completed = await Task.WhenAny(readTask, exitTask, cancelTcs.Task).ConfigureAwait(false);

        if (completed == cancelTcs.Task)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        // If the process exited, wait briefly for the read to finish (may have final message).
        if (completed == exitTask)
        {
            var delayTask = Task.Delay(500, CancellationToken.None);
            var readOrDelay = await Task.WhenAny(readTask, delayTask).ConfigureAwait(false);
            if (readOrDelay == readTask && readTask.Result != null)
            {
                return JsonSerializer.Deserialize<ProtocolMessage>(readTask.Result, JsonOptions);
            }

            throw processManager.CreateExitException("message receive");
        }

        string? result = await readTask.ConfigureAwait(false);
        if (result == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProtocolMessage>(result, JsonOptions);
    }

    public void Dispose()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _client?.Dispose();

        try
        {
            _listener.Stop();
        }
        catch
        {
            // Listener may already be stopped.
        }
    }

    private async Task PerformHandshakeAsync(ProcessManager processManager, CancellationToken cancellationToken)
    {
        // Step 1: Wait for SessionConnected message.
        var connectedMsg = await ReceiveMessageAsync(processManager, cancellationToken).ConfigureAwait(false);
        if (connectedMsg == null || connectedMsg.MessageType != ProtocolConstants.SessionConnected)
        {
            throw new InvalidOperationException(
                $"Expected '{ProtocolConstants.SessionConnected}' message but got '{connectedMsg?.MessageType ?? "null"}'.");
        }

        // Step 2: Send VersionCheck with our protocol version.
        await SendMessageAsync(ProtocolConstants.VersionCheck, ProtocolConstants.ProtocolVersion, cancellationToken).ConfigureAwait(false);

        // Step 3: Receive version response.
        var versionMsg = await ReceiveMessageAsync(processManager, cancellationToken).ConfigureAwait(false);
        if (versionMsg == null)
        {
            throw new InvalidOperationException("Connection closed during version negotiation.");
        }

        if (versionMsg.MessageType == ProtocolConstants.VersionCheck)
        {
            _negotiatedVersion = versionMsg.Payload?.GetInt32() ?? ProtocolConstants.ProtocolVersion;
        }
        else if (versionMsg.MessageType == ProtocolConstants.ProtocolError)
        {
            throw new InvalidOperationException(
                $"Protocol version negotiation failed: {versionMsg.Payload?.GetRawText() ?? "unknown error"}");
        }
        else
        {
            throw new InvalidOperationException(
                $"Unexpected message during version negotiation: '{versionMsg.MessageType}'.");
        }
    }

    private void WriteString(string value)
    {
        if (_writer == null) throw new ObjectDisposedException(nameof(AsyncRequestSender));
        _writer.Write(value);
        _writer.Flush();
    }

    private string ReadString()
    {
        if (_reader == null) throw new ObjectDisposedException(nameof(AsyncRequestSender));
        return _reader.ReadString();
    }
}
