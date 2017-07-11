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
    public class SocketServer : ICommunicationServer
    {
        private readonly CancellationTokenSource cancellation;

        private readonly Func<Stream, ICommunicationChannel> channelFactory;

        private ICommunicationChannel channel;

        private TcpListener tcpListener;

        private TcpClient tcpClient;

        private Stream stream;

        private bool stopped;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketServer"/> class.
        /// </summary>
        public SocketServer()
            : this((stream) => new LengthPrefixCommunicationChannel(stream))
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
        public event EventHandler<ConnectedEventArgs> ClientConnected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs> ClientDisconnected;

        /// <inheritdoc />
        public string Start()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            this.tcpListener = new TcpListener(endpoint);

            this.tcpListener.Start();

            var connectionInfo = ((IPEndPoint)this.tcpListener.LocalEndpoint).Port.ToString();
            EqtTrace.Info("SocketServer: Listening on port : {0}", connectionInfo);

            // Serves a single client at the moment. An error in connection, or message loop just
            // terminates the entire server.
            this.tcpListener.AcceptTcpClientAsync().ContinueWith(t => this.OnClientConnected(t.Result));
            return connectionInfo;
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (!this.stopped)
            {
                EqtTrace.Info("SocketServer: Stop: Cancellation requested. Stopping message loop.");
                this.cancellation.Cancel();
            }
        }

        private void OnClientConnected(TcpClient client)
        {
            this.tcpClient = client;
            this.tcpClient.Client.NoDelay = true;

            if (this.ClientConnected != null)
            {
                this.stream = new BufferedStream(client.GetStream(), 8 * 1024);
                this.channel = this.channelFactory(this.stream);
                this.ClientConnected.SafeInvoke(this, new ConnectedEventArgs(this.channel), "SocketServer: ClientConnected");

                // Start the message loop
                Task.Run(() => this.tcpClient.MessageLoopAsync(this.channel, error => this.Stop(error), this.cancellation.Token)).ConfigureAwait(false);
            }
        }

        private void Stop(Exception error)
        {
            if (!this.stopped)
            {
                // Do not allow stop to be called multiple times.
                this.stopped = true;

                // Stop accepting any other connections
                this.tcpListener.Stop();

                // Close the client and dispose the underlying stream.
                // Depending on implementation order of dispose may be important.
                // 1. Channel dispose -> disposes reader/writer which call a Flush on the Stream. Stream shouldn't
                // be disposed at this time.
                // 2. Stream's dispose may Flush the underlying base stream (if it's a BufferedStream). We should try
                // dispose it next.
                // 3. TcpClient's dispose will clean up the network stream and close any sockets. NetworkStream's dispose
                // is a no-op if called a second time.
                this.channel?.Dispose();
                this.stream?.Dispose();
#if !NET451
                this.tcpClient?.Dispose();
#endif

                this.cancellation.Dispose();

                this.ClientDisconnected?.SafeInvoke(this, new DisconnectedEventArgs { Error = error }, "SocketServer: ClientDisconnected");
            }
        }
    }
}
