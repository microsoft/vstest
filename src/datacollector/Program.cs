// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
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
        /// Parent process Id argument to monitor parent process.
        /// </summary>
        private const string ParentProcessArgument = "--parentprocessid";

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

        private static void Run(string[] args)
        {
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args);
            var requestHandler = DataCollectionRequestHandler.Create(new SocketCommunicationManager(), new MessageSink());

            // Attach to exit of parent process
            var parentProcessId = CommandLineArgumentsHelper.GetIntArgFromDict(argsDictionary, ParentProcessArgument);
            EqtTrace.Info("DataCollector: Monitoring parent process with id: '{0}'", parentProcessId);

            var processHelper = new ProcessHelper();
            processHelper.SetExitCallback(parentProcessId,
                () =>
                    {
                        EqtTrace.Info("DataCollector: ParentProcess '{0}' Exited.", parentProcessId);
                        Environment.Exit(1);
                    });

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

            // Start processing async in a different task
            EqtTrace.Info("DataCollector: Start Request Processing.");
            var processingTask = StartProcessingAsync(requestHandler);

            // Wait for processing to complete.
            Task.WaitAny(processingTask);
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

        private static Task StartProcessingAsync(IDataCollectionRequestHandler requestHandler)
        {
            return Task.Run(() =>
            {
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
            });
        }
    }
}