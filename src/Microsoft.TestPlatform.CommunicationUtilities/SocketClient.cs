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

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

    /// <summary>
    /// Communication client implementation over sockets.
    /// </summary>
    public class SocketClient : ICommunicationClient
    {
        private readonly CancellationTokenSource cancellation;
        private readonly TcpClient tcpClient;
        private readonly Func<Stream, ICommunicationChannel> channelFactory;
        private ICommunicationChannel channel;

        public SocketClient()
            : this((stream) => new LengthPrefixCommunicationChannel(stream))
        {
        }

        protected SocketClient(Func<Stream, ICommunicationChannel> channelFactory)
        {
            // Used to cancel the message loop
            this.cancellation = new CancellationTokenSource();

            this.tcpClient = new TcpClient();
            this.channelFactory = channelFactory;
        }

        /// <inheritdoc />
        public event EventHandler<ConnectedEventArgs> ServerConnected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs> ServerDisconnected;

        /// <inheritdoc />
        public void Start(string connectionInfo)
        {
            this.tcpClient.ConnectAsync(IPAddress.Loopback, int.Parse(connectionInfo)).ContinueWith(t => this.OnServerConnected(t));
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (this.ServerDisconnected != null)
            {
                this.ServerDisconnected.Invoke(this, new DisconnectedEventArgs());
            }
        }

        private void OnServerConnected(Task connectAsyncTask)
        {
            if (connectAsyncTask.IsFaulted)
            {
                // Throw an exception
            }

            this.channel = this.channelFactory(this.tcpClient.GetStream());
            if (this.ServerConnected != null)
            {
                this.ServerConnected.Invoke(this, new ConnectedEventArgs(this.channel));

                // Start the message loop
                Task.Run(() => this.tcpClient.MessageLoopAsync(this.channel, error => this.Stop(error), this.cancellation.Token)).ConfigureAwait(false);
            }
        }

        private void Stop(Exception error)
        {
            ////if (!this.stopped)
            ////{
                ////// Do not allow stop to be called multiple times.
                ////this.stopped = true;

                ////// Stop accepting any other connections
                ////this.tcpListener.Stop();

                ////// Close the client and dispose the underlying stream
                ////this.tcpClient?.Dispose();
                ////this.channel.Dispose();

                ////this.cancellation.Dispose();

                ////if (this.ClientDisconnected != null)
                ////{
                    ////this.ClientDisconnected.Invoke(this, new DisconnectedEventArgs { Error = error });
                ////}
            ////}
        }
    }
}
