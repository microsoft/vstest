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

    internal static class TcpClientExtensions
    {
        private const int STREAMREADTIMEOUT = 1000 * 1000;

        internal static Task MessageLoopAsync(
                this TcpClient client,
                ICommunicationChannel channel,
                Action<Exception> errorHandler,
                CancellationToken cancellationToken)
        {
            Exception error = null;

            // Set read timeout to avoid blocking receive raw message
            while (channel != null && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (client.Client.Poll(STREAMREADTIMEOUT, SelectMode.SelectRead))
                    {
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
                                "Socket: Message loop: failed to receive message due to read timeout {0}",
                                ioException);
                    }
                    else
                    {
                        EqtTrace.Error(
                                "Socket: Message loop: failed to receive message due to socket error {0}",
                                ioException);
                        error = ioException;
                        break;
                    }
                }
                catch (Exception exception)
                {
                    EqtTrace.Error(
                            "Socket: Message loop: failed to receive message {0}",
                            exception);
                    error = exception;
                    break;
                }
            }

            // Try clean up and raise client disconnected events
            errorHandler(error);

            return Task.FromResult(0);
        }
    }
}
