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

    internal static class TcpClientExtensions
    {
        // Timeout for polling stream in micro seconds.
        private const int STREAMREADTIMEOUT = 1000 * 1000;

        internal static Task MessageLoopAsync(
                this TcpClient client,
                ICommunicationChannel channel,
                Action<Exception> errorHandler,
                CancellationToken cancellationToken)
        {
            Exception error = null;
            var remoteEndPoint = client.Client.RemoteEndPoint.ToString();
            var localEndPoint = client.Client.LocalEndPoint.ToString();

            // Set read timeout to avoid blocking receive raw message
            while (channel != null && !cancellationToken.IsCancellationRequested)
            {
                EqtTrace.Verbose("TcpClientExtensions.MessageLoopAsync: Polling on remoteEndPoint: {0} localEndPoint: {1}", remoteEndPoint, localEndPoint);

                try
                {
                    if (client.Client.Poll(STREAMREADTIMEOUT, SelectMode.SelectRead))
                    {
                        EqtTrace.Verbose("TcpClientExtensions.MessageLoopAsync: NotifyDataAvailable remoteEndPoint: {0} localEndPoint: {1}", remoteEndPoint, localEndPoint);
                        channel.NotifyDataAvailable();
                    }
                }
                catch (IOException ioException)
                {
                    var socketException = ioException.InnerException as SocketException;
                    if (socketException != null
                            && socketException.SocketErrorCode == SocketError.TimedOut)
                    {
                        EqtTrace.Info(
                                "Socket: Message loop: failed to receive message due to read timeout {0}, remoteEndPoint: {1} localEndPoint: {2}",
                                ioException,
                                remoteEndPoint,
                                localEndPoint);
                    }
                    else
                    {
                        EqtTrace.Error(
                                "Socket: Message loop: failed to receive message due to socket error {0}, remoteEndPoint: {1} localEndPoint: {2}",
                                ioException,
                                remoteEndPoint,
                                localEndPoint);
                        error = ioException;
                        break;
                    }
                }
                catch (Exception exception)
                {
                    EqtTrace.Error(
                            "Socket: Message loop: failed to receive message {0}, remoteEndPoint: {1} localEndPoint: {2}",
                            exception,
                        remoteEndPoint,
                        localEndPoint);
                    error = exception;
                    break;
                }
            }

            // Try clean up and raise client disconnected events
            errorHandler(error);

            EqtTrace.Verbose("TcpClientExtensions.MessageLoopAsync: exiting MessageLoopAsync remoteEndPoint: {0} localEndPoint: {1}", remoteEndPoint, localEndPoint);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Converts a given string endpoint address to valid Ipv4, Ipv6 IPEndpoint
        /// </summary>
        /// <param name="value">Input endpoint address</param>
        /// <returns>IPEndpoint from give string, if its not a valid string. It will create endpoint with loop back address with port 0</returns>
        internal static IPEndPoint GetIPEndPoint(this string value)
        {
            if (Uri.TryCreate(string.Concat("tcp://", value), UriKind.Absolute, out Uri uri))
            {
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
            }

            return new IPEndPoint(IPAddress.Loopback, 0);
        }
    }
}
