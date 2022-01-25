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
    /// Communication client implementation over sockets.
    /// </summary>
    public class SocketClient : ICommunicationEndPoint
    {
        private readonly CancellationTokenSource cancellation;
        private readonly TcpClient tcpClient;
        private readonly Func<Stream, ICommunicationChannel> channelFactory;
        private ICommunicationChannel channel;
        private bool stopped;
        private string endPoint;

        public SocketClient()
            : this(stream => new LengthPrefixCommunicationChannel(stream))
        {
        }

        protected SocketClient(Func<Stream, ICommunicationChannel> channelFactory)
        {
            // Used to cancel the message loop
            cancellation = new CancellationTokenSource();
            stopped = false;

            tcpClient = new TcpClient { NoDelay = true };
            this.channelFactory = channelFactory;
        }

        /// <inheritdoc />
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <inheritdoc />
        public string Start(string endPoint)
        {
            this.endPoint = endPoint;
            var ipEndPoint = endPoint.GetIPEndPoint();

            EqtTrace.Info("SocketClient.Start: connecting to server endpoint: {0}", endPoint);

            // Don't start if the endPoint port is zero
            tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port).ContinueWith(OnServerConnected);
            return ipEndPoint.ToString();
        }

        /// <inheritdoc />
        public void Stop()
        {
            EqtTrace.Info("SocketClient.Stop: Stop communication from server endpoint: {0}", endPoint);

            if (!stopped)
            {
                EqtTrace.Info("SocketClient: Stop: Cancellation requested. Stopping message loop.");
                cancellation.Cancel();
            }
        }

        private void OnServerConnected(Task connectAsyncTask)
        {
            EqtTrace.Info("SocketClient.OnServerConnected: connected to server endpoint: {0}", endPoint);

            if (Connected != null)
            {
                if (connectAsyncTask.IsFaulted)
                {
                    Connected.SafeInvoke(this, new ConnectedEventArgs(connectAsyncTask.Exception), "SocketClient: Server Failed to Connect");
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("Unable to connect to server, Exception occurred : {0}", connectAsyncTask.Exception);
                    }
                }
                else
                {
                    channel = channelFactory(tcpClient.GetStream());
                    Connected.SafeInvoke(this, new ConnectedEventArgs(channel), "SocketClient: ServerConnected");

                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("Connected to server, and starting MessageLoopAsync");
                    }

                    // Start the message loop
                    Task.Run(() => tcpClient.MessageLoopAsync(
                            channel,
                            Stop,
                            cancellation.Token))
                        .ConfigureAwait(false);
                }
            }
        }

        private void Stop(Exception error)
        {
            EqtTrace.Info("SocketClient.PrivateStop: Stop communication from server endpoint: {0}, error:{1}", endPoint, error);

            if (!stopped)
            {
                // Do not allow stop to be called multiple times.
                stopped = true;

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

                Disconnected?.SafeInvoke(this, new DisconnectedEventArgs(), "SocketClient: ServerDisconnected");
            }
        }
    }
}
