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
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Communication client implementation over sockets.
    /// </summary>
    public class SocketClient : ICommunicationEndPoint
    {
        private readonly CancellationTokenSource cancellation;
        private readonly TcpClient tcpClient;
        private readonly Func<Stream, ICommunicationChannel> channelFactory;
        private ICommunicationChannel channel;
        private bool stopped;

        public SocketClient()
            : this(stream => new LengthPrefixCommunicationChannel(stream))
        {
        }

        protected SocketClient(Func<Stream, ICommunicationChannel> channelFactory)
        {
            // Used to cancel the message loop
            this.cancellation = new CancellationTokenSource();
            this.stopped = false;

            this.tcpClient = new TcpClient { NoDelay = true };
            this.channelFactory = channelFactory;
        }

        /// <inheritdoc />
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <inheritdoc />
        public string Start(string endpoint)
        {
            var ipEndPoint = endpoint.GetIPEndPoint();

            // Don't start if the endpoint port is zero
            this.tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port).ContinueWith(this.OnServerConnected);
            return endpoint;
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (!this.stopped)
            {
                EqtTrace.Info("SocketClient: Stop: Cancellation requested. Stopping message loop.");
                this.cancellation.Cancel();
            }
        }

        private void OnServerConnected(Task connectAsyncTask)
        {
            if (this.Connected != null)
            {
                if (connectAsyncTask.IsFaulted)
                {
                    this.Connected.SafeInvoke(this, new ConnectedEventArgs(connectAsyncTask.Exception), "SocketClient: ServerConnected");
                }
                else
                {
                    this.channel = this.channelFactory(this.tcpClient.GetStream());
                    this.Connected.SafeInvoke(this, new ConnectedEventArgs(this.channel), "SocketClient: ServerConnected");

                    // Start the message loop
                    Task.Run(() => this.tcpClient.MessageLoopAsync(
                            this.channel,
                            this.Stop,
                            this.cancellation.Token))
                        .ConfigureAwait(false);
                }
            }
        }

        private void Stop(Exception error)
        {
            if (!this.stopped)
            {
                // Do not allow stop to be called multiple times.
                this.stopped = true;

                // Close the client and dispose the underlying stream
#if NET451
                // tcpClient.Close() calls tcpClient.Dispose().
                this.tcpClient?.Close();
#else
                // tcpClient.Close() not available for netstandard1.5.
                this.tcpClient?.Dispose();
#endif
                this.channel.Dispose();
                this.cancellation.Dispose();

                this.Disconnected?.SafeInvoke(this, new DisconnectedEventArgs(), "SocketClient: ServerDisconnected");
            }
        }
    }
}
