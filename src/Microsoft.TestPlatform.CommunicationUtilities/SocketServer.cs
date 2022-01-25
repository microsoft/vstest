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
            cancellation = new CancellationTokenSource();

            this.channelFactory = channelFactory;
        }

        /// <inheritdoc />
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        public string Start(string endPoint)
        {
            tcpListener = new TcpListener(endPoint.GetIPEndPoint());

            tcpListener.Start();

            this.endPoint = tcpListener.LocalEndpoint.ToString();
            EqtTrace.Info("SocketServer.Start: Listening on endpoint : {0}", this.endPoint);

            // Serves a single client at the moment. An error in connection, or message loop just
            // terminates the entire server.
            tcpListener.AcceptTcpClientAsync().ContinueWith(t => OnClientConnected(t.Result));
            return this.endPoint;
        }

        /// <inheritdoc />
        public void Stop()
        {
            EqtTrace.Info("SocketServer.Stop: Stop server endPoint: {0}", endPoint);
            if (!stopped)
            {
                EqtTrace.Info("SocketServer.Stop: Cancellation requested. Stopping message loop.");
                cancellation.Cancel();
            }
        }

        private void OnClientConnected(TcpClient client)
        {
            tcpClient = client;
            tcpClient.Client.NoDelay = true;

            if (Connected != null)
            {
                channel = channelFactory(tcpClient.GetStream());
                Connected.SafeInvoke(this, new ConnectedEventArgs(channel), "SocketServer: ClientConnected");

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("SocketServer.OnClientConnected: Client connected for endPoint: {0}, starting MessageLoopAsync:", endPoint);
                }

                // Start the message loop
                Task.Run(() => tcpClient.MessageLoopAsync(channel, error => Stop(error), cancellation.Token)).ConfigureAwait(false);
            }
        }

        private void Stop(Exception error)
        {
            EqtTrace.Info("SocketServer.PrivateStop: Stopping server endPoint: {0} error: {1}", endPoint, error);

            if (!stopped)
            {
                // Do not allow stop to be called multiple times.
                stopped = true;

                // Stop accepting any other connections
                tcpListener.Stop();

                // Close the client and dispose the underlying stream
#if NETFRAMEWORK
                // tcpClient.Close() calls tcpClient.Dispose().
                tcpClient?.Close();
#else
                // tcpClient.Close() not available for netstandard1.5.
                tcpClient?.Dispose();
#endif
                channel.Dispose();
                cancellation.Dispose();

                EqtTrace.Info("SocketServer.Stop: Raise disconnected event endPoint: {0} error: {1}", endPoint, error);
                Disconnected?.SafeInvoke(this, new DisconnectedEventArgs { Error = error }, "SocketServer: ClientDisconnected");
            }
        }
    }
}
