// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Communication server implementation over sockets.
    /// </summary>
    public class SocketServer : ICommunicationEndPoint
    {
        private readonly CancellationTokenSource cancellation;

        private readonly Func<Stream, ICommunicationChannel> channelFactory;
        private readonly Func<TcpListener, Task<TcpClient>> acceptClientAsync;
        private readonly object stateSyncObject = new object();

        private ICommunicationChannel channel;

        private TcpListener tcpListener;

        private TcpClient tcpClient;

        private bool stopped;
        private int stopRequested;

        private string endPoint;

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
            : this(channelFactory, listener => listener.AcceptTcpClientAsync())
        {
        }

        internal SocketServer(
            Func<Stream, ICommunicationChannel> channelFactory,
            Func<TcpListener, Task<TcpClient>> acceptClientAsync)
        {
            // Used to cancel the message loop
            this.cancellation = new CancellationTokenSource();

            this.channelFactory = channelFactory;
            this.acceptClientAsync = acceptClientAsync;
        }

        /// <inheritdoc />
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <inheritdoc />
        public string Start(string endPoint)
        {
            TcpListener listener;
            lock (this.stateSyncObject)
            {
                if (this.stopRequested != 0)
                {
                    throw new ObjectDisposedException(nameof(SocketServer));
                }
                this.tcpListener = new TcpListener(endPoint.GetIPEndPoint());
                this.tcpListener.Start();
                listener = this.tcpListener;
                this.endPoint = listener.LocalEndpoint.ToString();
                EqtTrace.Info("SocketServer.Start: Listening on endpoint : {0}", this.endPoint);
            }

            _ = this.AcceptClientAsync(listener);
            return this.endPoint;
        }

        /// <inheritdoc />
        public void Stop()
        {
            EqtTrace.Info("SocketServer.Stop: Stop server endPoint: {0}", this.endPoint);
            lock (this.stateSyncObject)
            {
                if (this.stopRequested != 0)
                {
                    return;
                }
                this.stopRequested = 1;
                EqtTrace.Info("SocketServer.Stop: Cancellation requested. Stopping message loop.");
                try
                {
                    this.cancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // The message loop may have completed cleanup concurrently.
                }
                this.tcpListener?.Stop();
            }
        }

        private async Task AcceptClientAsync(TcpListener listener)
        {
            TcpClient client = null;
            try
            {
                try
                {
                    client = await this.acceptClientAsync(listener).ConfigureAwait(false);
                }
                catch (Exception ex) when (Volatile.Read(ref this.stopRequested) != 0 &&
                                           (ex is InvalidOperationException || ex is SocketException))
                {
                    EqtTrace.Verbose("SocketServer.AcceptClientAsync: Listener stopped before a client connected: {0}", ex);
                    return;
                }

                client.Client.NoDelay = true;
                lock (this.stateSyncObject)
                {
                    if (this.stopRequested != 0)
                    {
                        ((IDisposable)client).Dispose();
                        return;
                    }
                    this.tcpClient = client;
                }
                this.OnClientConnected(client);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("SocketServer.AcceptClientAsync: Failed to accept a client: {0}", ex);
                ((IDisposable)client)?.Dispose();
                this.Stop();
            }
        }

        private void OnClientConnected(TcpClient client)
        {
            if (this.Connected == null)
            {
                return;
            }

            var connectedChannel = this.channelFactory(client.GetStream());
            bool stopRequested;
            lock (this.stateSyncObject)
            {
                // Stop can complete while the channel factory is running.
                stopRequested = this.stopRequested != 0;
                if (!stopRequested)
                {
                    this.channel = connectedChannel;
                }
            }

            if (stopRequested)
            {
                try
                {
                    connectedChannel.Dispose();
                }
                finally
                {
                    ((IDisposable)client).Dispose();
                }

                return;
            }

            this.Connected.SafeInvoke(this, new ConnectedEventArgs(connectedChannel), "SocketServer: ClientConnected");
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("SocketServer.OnClientConnected: Client connected for endPoint: {0}, starting MessageLoopAsync:", this.endPoint);
            }
            _ = Task.Run(() => client.MessageLoopAsync(connectedChannel, error => this.Stop(error), this.cancellation.Token));
        }

        private void Stop(Exception error)
        {
            EqtTrace.Info("SocketServer.PrivateStop: Stopp server endPoint: {0} error: {1}", this.endPoint, error);

            lock (this.stateSyncObject)
            {
                if (this.stopped)
                {
                    return;
                }
                this.stopped = true;
                this.stopRequested = 1;
                this.tcpListener?.Stop();
            }

            // Close the client and dispose the underlying stream.
#if NET451
            // tcpClient.Close() calls tcpClient.Dispose().
            this.tcpClient?.Close();
#else
            // tcpClient.Close() is not available for netstandard1.4.
            this.tcpClient?.Dispose();
#endif
            this.channel?.Dispose();
            this.cancellation.Dispose();

            EqtTrace.Info("SocketServer.Stop: Raise disconnected event endPoint: {0} error: {1}", this.endPoint, error);
            this.Disconnected?.SafeInvoke(this, new DisconnectedEventArgs { Error = error }, "SocketServer: ClientDisconnected");
        }
    }
}
