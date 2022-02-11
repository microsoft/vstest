// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Interfaces;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using Utilities;

/// <summary>
/// Communication server implementation over sockets.
/// </summary>
public class SocketServer : ICommunicationEndPoint
{
    private readonly CancellationTokenSource _cancellation;

    private readonly Func<Stream, ICommunicationChannel> _channelFactory;

    private ICommunicationChannel _channel;

    private TcpListener _tcpListener;

    private TcpClient _tcpClient;

    private bool _stopped;

    private string _endPoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketServer"/> class.
    /// </summary>
    public SocketServer()
        : this(stream => new LengthPrefixCommunicationChannel(stream))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketServer"/> class with given channel
    /// factory implementation.
    /// </summary>
    /// <param name="channelFactory">Factory to create communication channel.</param>
    protected SocketServer(Func<Stream, ICommunicationChannel> channelFactory)
    {
        // Used to cancel the message loop
        _cancellation = new CancellationTokenSource();

        _channelFactory = channelFactory;
    }

    /// <inheritdoc />
    public event EventHandler<ConnectedEventArgs> Connected;

    /// <inheritdoc />
    public event EventHandler<DisconnectedEventArgs> Disconnected;

    public string Start(string endPoint)
    {
        _tcpListener = new TcpListener(endPoint.GetIpEndPoint());

        _tcpListener.Start();

        _endPoint = _tcpListener.LocalEndpoint.ToString();
        EqtTrace.Info("SocketServer.Start: Listening on endpoint : {0}", _endPoint);

        // Serves a single client at the moment. An error in connection, or message loop just
        // terminates the entire server.
        _tcpListener.AcceptTcpClientAsync().ContinueWith(t => OnClientConnected(t.Result));
        return _endPoint;
    }

    /// <inheritdoc />
    public void Stop()
    {
        EqtTrace.Info("SocketServer.Stop: Stop server endPoint: {0}", _endPoint);
        if (!_stopped)
        {
            EqtTrace.Info("SocketServer.Stop: Cancellation requested. Stopping message loop.");
            _cancellation.Cancel();
        }
    }

    private void OnClientConnected(TcpClient client)
    {
        _tcpClient = client;
        _tcpClient.Client.NoDelay = true;

        if (Connected != null)
        {
            _channel = _channelFactory(_tcpClient.GetStream());
            Connected.SafeInvoke(this, new ConnectedEventArgs(_channel), "SocketServer: ClientConnected");

            EqtTrace.Verbose("SocketServer.OnClientConnected: Client connected for endPoint: {0}, starting MessageLoopAsync:", _endPoint);

            // Start the message loop
            Task.Run(() => _tcpClient.MessageLoopAsync(_channel, error => Stop(error), _cancellation.Token)).ConfigureAwait(false);
        }
    }

    private void Stop(Exception error)
    {
        EqtTrace.Info("SocketServer.PrivateStop: Stopping server endPoint: {0} error: {1}", _endPoint, error);

        if (!_stopped)
        {
            // Do not allow stop to be called multiple times.
            _stopped = true;

            // Stop accepting any other connections
            _tcpListener.Stop();

            // Close the client and dispose the underlying stream
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            _tcpClient?.Close();
#else
            // tcpClient.Close() not available for netstandard1.5.
            _tcpClient?.Dispose();
#endif
            _channel.Dispose();
            _cancellation.Dispose();

            EqtTrace.Info("SocketServer.Stop: Raise disconnected event endPoint: {0} error: {1}", _endPoint, error);
            Disconnected?.SafeInvoke(this, new DisconnectedEventArgs { Error = error }, "SocketServer: ClientDisconnected");
        }
    }
}
