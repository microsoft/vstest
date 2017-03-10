// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Sockets;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

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
        /// Port number used to communicate with test runner process.
        /// </summary>
        private const string PortArgument = "--port";

        /// <summary>
        /// Log file for writing eqt trace logs.
        /// </summary>
        private const string LogFileArgument = "--diag";

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
                WaitForDebuggerIfEnabled();
                Run(args);
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
        /// Parse command line arguments to a dictionary.
        /// </summary>
        /// <param name="args">Command line arguments. Ex: <c>{ "--port", "12312", "--parentprocessid", "2312", "--testsourcepath", "C:\temp\1.dll" }</c></param>
        /// <returns>Dictionary of arguments keys and values.</returns>
        private static IDictionary<string, string> GetArguments(string[] args)
        {
            var argsDictionary = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    if (i < args.Length - 1 && !args[i + 1].StartsWith("-"))
                    {
                        argsDictionary.Add(args[i], args[i + 1]);
                        i++;
                    }
                    else
                    {
                        argsDictionary.Add(args[i], null);
                    }
                }
            }

            return argsDictionary;
        }

        private static void Run(string[] args)
        {
            var argsDictionary = GetArguments(args);
            var requestHandler = DataCollectionRequestHandler.Create(new SocketCommunicationManager(), new MessageSink());

            // Setup logging if enabled
            string logFile;
            if (argsDictionary.TryGetValue(LogFileArgument, out logFile))
            {
                EqtTrace.InitializeVerboseTrace(logFile);
            }

            // Get server port and initialize communication.
            string portValue;
            int port = argsDictionary.TryGetValue(PortArgument, out portValue) ? int.Parse(portValue) : 0;

            if (port <= 0)
            {
                throw new ArgumentException("Incorrect/No Port number");
            }

            requestHandler.InitializeCommunication(port);

            // Can only do this after InitializeCommunication because datacollector cannot "Send Log" unless communications are initialized
            if (!string.IsNullOrEmpty(EqtTrace.LogFile))
            {
                requestHandler.SendDataCollectionMessage(new DataCollectionMessageEventArgs(TestMessageLevel.Informational, string.Format("Logging DataCollector Diagnostics in file: {0}", EqtTrace.LogFile)));
            }

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

        private static void WaitForDebuggerIfEnabled()
        {
            var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_DATACOLLECTOR_DEBUG");
            if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
            {
                ConsoleOutput.Instance.WriteLine("Waiting for debugger attach...", OutputLevel.Information);

                var currentProcess = Process.GetCurrentProcess();
                ConsoleOutput.Instance.WriteLine(
                    string.Format("Process Id: {0}, Name: {1}", currentProcess.Id, currentProcess.ProcessName),
                    OutputLevel.Information);

                while (!Debugger.IsAttached)
                {
                    System.Threading.Thread.Sleep(1000);
                }

                Debugger.Break();
            }
        }
    }
}