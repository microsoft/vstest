// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.IO;
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

            // Serves a single client at the moment. An error in connection, or message loop just
            // terminates the entire server.
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
                    // Message-loop cleanup can dispose the source concurrently with Stop.
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
                catch (Exception ex) when (Volatile.Read(ref this.stopRequested) != 0
                    && (ex is SocketException || ex is InvalidOperationException))
                {
                    EqtTrace.Verbose("SocketServer.AcceptClientAsync: Listener stopped before a client connected: {0}", ex);
                    return;
                }

                client.Client.NoDelay = true;

                lock (this.stateSyncObject)
                {
                    if (this.stopRequested != 0)
                    {
                        CloseClient(client);
                        return;
                    }

                    this.tcpClient = client;
                }

                this.OnClientConnected(client);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("SocketServer.AcceptClientAsync: Failed to accept a client: {0}", ex);
                CloseClient(client);
                this.Stop();
            }
        }

        private void OnClientConnected(TcpClient client)
        {
            if (this.Connected != null)
            {
                // The factory and Connected handlers may call Stop. Never invoke them under the state lock.
                var createdChannel = this.channelFactory(client.GetStream());
                bool publishChannel;
                lock (this.stateSyncObject)
                {
                    publishChannel = this.stopRequested == 0;
                    if (publishChannel)
                    {
                        this.channel = createdChannel;
                    }
                }

                if (!publishChannel)
                {
                    try
                    {
                        createdChannel.Dispose();
                    }
                    finally
                    {
                        CloseClient(client);
                    }
                    return;
                }

                this.Connected.SafeInvoke(this, new ConnectedEventArgs(createdChannel), "SocketServer: ClientConnected");

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("SocketServer.OnClientConnected: Client connected for endPoint: {0}, starting MessageLoopAsync:", this.endPoint);
                }

                // Start the message loop
                _ = Task.Run(() => client.MessageLoopAsync(createdChannel, this.StopOnError, this.cancellation.Token));
            }
        }

        private void StopOnError(Exception error)
        {
            EqtTrace.Info("SocketServer.PrivateStop: Stop server endPoint: {0} error: {1}", this.endPoint, error);

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

            CloseClient(this.tcpClient);
            this.channel.Dispose();
            this.cancellation.Dispose();

            EqtTrace.Info("SocketServer.Stop: Raise disconnected event endPoint: {0} error: {1}", this.endPoint, error);
            this.Disconnected?.SafeInvoke(this, new DisconnectedEventArgs { Error = error }, "SocketServer: ClientDisconnected");
        }

        private static void CloseClient(TcpClient client)
        {
#if NETFRAMEWORK
            client?.Close();
#else
            client?.Dispose();
#endif
        }
    }
}
