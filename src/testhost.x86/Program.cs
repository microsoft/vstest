// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;

    using CrossPlatEngine;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

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
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Main(string[] args)
        {
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("TestHost: Error occured during initialization of TestHost : {0}", ex);
            }
        }

        /// <summary>
        /// Get port number from command line arguments
        /// </summary>
        /// <param name="args">command line arguments</param>
        /// <returns>port number</returns>
        private static int GetPortNumber(string[] args)
        {
            var port = -1;

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

            return port;
        }

        private static void Run(string[] args)
        {
            var portNumber = GetPortNumber(args);

            var requestHandler = new TestRequestHandler();
            requestHandler.InitializeCommunication(portNumber);

            // setup the factory.
            var managerFactory = new TestHostManagerFactory();

            // Wait for the connection to the sender and start processing requests from sender
            if (requestHandler.WaitForRequestSenderConnection(ClientListenTimeOut))
            {
                requestHandler.ProcessRequests(managerFactory);
            }
            else
            {
                EqtTrace.Info("TestHost: RequestHandler timed out while connecting to the Sender.");
                requestHandler.Close();
                throw new TimeoutException();
            }
        }
    }
}
