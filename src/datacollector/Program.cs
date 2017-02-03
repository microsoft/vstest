// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using System.Net.Sockets;

    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The timeout for the client to connect to the server.
        /// </summary>
        private const int ClientListenTimeOut = 5 * 1000;

        /// <summary>
        /// Port where vstest.console is listening 
        /// </summary>
        private static int port;

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Main(string[] args)
        {
            try
            {
                ParseArgs(args);
                Run();
            }
            catch (SocketException ex)
            {
                EqtTrace.Error("DataCollector: Socket exception is thrown : {0}", ex);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("DataCollector: Error occured during initialization of Datacollector : {0}", ex);
            }
        }

        /// <summary>
        /// Parse args.
        /// </summary>
        /// <param name="args">The args.</param>
        private static void ParseArgs(string[] args)
        {
            port = -1;

            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals("--port", args[i], StringComparison.OrdinalIgnoreCase) || string.Equals("-p", args[i], StringComparison.OrdinalIgnoreCase))
                {
                    if (i < args.Length - 1)
                    {
                        int.TryParse(args[i + 1], out port);
                    }

                    break;
                }
            }

            if (port < 0)
            {
                throw new ArgumentException("Incorrect/No Port number");
            }
        }

        private static void Run()
        {
            var requestHandler = DataCollectionRequestHandler.CreateInstance(new SocketCommunicationManager(),new MessageSink());

            requestHandler.InitializeCommunication(port);

            // Wait for the connection to the sender and start processing requests from sender
            if (requestHandler.WaitForRequestSenderConnection(ClientListenTimeOut))
            {
                requestHandler.ProcessRequests();
            }
            else
            {
                EqtTrace.Info("DataCollector: RequestHandler timed out while connecting to the Sender.");
                requestHandler.Close();
                throw new TimeoutException();
            }
        }
    }
}