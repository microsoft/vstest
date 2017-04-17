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

    /// <summary>
    /// Communication server implementation over sockets.
    /// </summary>
    public class SocketServer : ICommunicationServer
    {
        private const int STREAMREADTIMEOUT = 1000 * 1000;

        private readonly CancellationTokenSource cancellation;

        private readonly Func<Stream, ICommunicationChannel> channelFactory;

        private ICommunicationChannel channel;

        private TcpListener tcpListener;

        private TcpClient tcpClient;

        private bool stopped;

        ////private CancellationTokenSource cancellationToken;

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

            if (this.ClientConnected != null)
            {
                this.channel = this.channelFactory(client.GetStream());
                this.ClientConnected.Invoke(this, new ConnectedEventArgs(this.channel));

                // Start the message loop
                Task.Run(() => this.MessageLoopAsync(this.tcpClient)).ConfigureAwait(false);
            }
        }

        private Task MessageLoopAsync(TcpClient client)
        {
            Exception error = null;

            // Set read timeout to avoid blocking receive raw message
            while (this.channel != null && !this.cancellation.Token.IsCancellationRequested)
            {
                try
                {
                    if (client.Client.Poll(STREAMREADTIMEOUT, SelectMode.SelectRead))
                    {
                        this.channel.NotifyDataAvailable();
                    }
                }
                catch (IOException ioException)
                {
                    var socketException = ioException.InnerException as SocketException;
                    if (socketException != null
                            && socketException.SocketErrorCode == SocketError.TimedOut)
                    {
                        EqtTrace.Info(
                                "SocketServer: Message loop: failed to receive message due to read timeout {0}",
                                ioException);
                    }
                    else
                    {
                        EqtTrace.Error(
                                "SocketServer: Message loop: failed to receive message due to socket error {0}",
                                ioException);
                        error = ioException;
                        break;
                    }
                }
                catch (Exception exception)
                {
                    EqtTrace.Error(
                            "SocketServer: Message loop: failed to receive message {0}",
                            exception);
                    error = exception;
                    break;
                }
            }

            // Try clean up and raise client disconnected events
            this.Stop(error);

            return Task.FromResult(0);
        }

        private void Stop(Exception error)
        {
            if (!this.stopped)
            {
                // Do not allow stop to be called multiple times.
                this.stopped = true;

                // Stop accepting any other connections
                this.tcpListener.Stop();

                // Close the client and dispose the underlying stream
                this.tcpClient?.Dispose();
                this.channel.Dispose();

                this.cancellation.Dispose();

                if (this.ClientDisconnected != null)
                {
                    this.ClientDisconnected.Invoke(this, new DisconnectedEventArgs { Error = error });
                }
            }
        }
    }
}
