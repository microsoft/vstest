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

        private ICommunicationChannel channel;

        private TcpListener tcpListener;

        private TcpClient tcpClient;

        private bool stopped;

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
        {
            // Used to cancel the message loop
            this.cancellation = new CancellationTokenSource();

            this.channelFactory = channelFactory;
        }

        /// <inheritdoc />
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        public string Start(string endPoint)
        {
            this.tcpListener = new TcpListener(endPoint.GetIPEndPoint());

            this.tcpListener.Start();

            this.endPoint = this.tcpListener.LocalEndpoint.ToString();
            EqtTrace.Info("SocketServer.Start: Listening on endpoint : {0}", this.endPoint);

            // Serves a single client at the moment. An error in connection, or message loop just
            // terminates the entire server.
            this.tcpListener.AcceptTcpClientAsync().ContinueWith(t => this.OnClientConnected(t.Result));
            return this.endPoint;
        }

        /// <inheritdoc />
        public void Stop()
        {
            EqtTrace.Info("SocketServer.Stop: Stop server endPoint: {0}", this.endPoint);
            if (!this.stopped)
            {
                EqtTrace.Info("SocketServer.Stop: Cancellation requested. Stopping message loop.");
                this.cancellation.Cancel();
            }
        }

        private void OnClientConnected(TcpClient client)
        {
            this.tcpClient = client;
            this.tcpClient.Client.NoDelay = true;

            if (this.Connected != null)
            {
                this.channel = this.channelFactory(this.tcpClient.GetStream());
                this.Connected.SafeInvoke(this, new ConnectedEventArgs(this.channel), "SocketServer: ClientConnected");

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("SocketServer.OnClientConnected: Client connected for endPoint: {0}, starting MessageLoopAsync:", this.endPoint);
                }

                // Start the message loop
                Task.Run(() => this.tcpClient.MessageLoopAsync(this.channel, error => this.Stop(error), this.cancellation.Token)).ConfigureAwait(false);
            }
        }

        private void Stop(Exception error)
        {
            EqtTrace.Info("SocketServer.PrivateStop: Stopping server endPoint: {0} error: {1}", this.endPoint, error);

            if (!this.stopped)
            {
                // Do not allow stop to be called multiple times.
                this.stopped = true;

                // Stop accepting any other connections
                this.tcpListener.Stop();

                // Close the client and dispose the underlying stream
#if NETFRAMEWORK
                // tcpClient.Close() calls tcpClient.Dispose().
                this.tcpClient?.Close();
#else
                // tcpClient.Close() not available for netstandard1.5.
                this.tcpClient?.Dispose();
#endif
                this.channel.Dispose();
                this.cancellation.Dispose();

                EqtTrace.Info("SocketServer.Stop: Raise disconnected event endPoint: {0} error: {1}", this.endPoint, error);
                this.Disconnected?.SafeInvoke(this, new DisconnectedEventArgs { Error = error }, "SocketServer: ClientDisconnected");
            }
        }
    }
}
