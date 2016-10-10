// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using CrossPlatEngine;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;


    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The timeout for the client to connect to the server.
        /// </summary>
        private const int ClientListenTimeOut = 5 * 1000;

        private const string PortArgument = "--port";

        private const string ParentProcessIdArgument = "--parentprocessid";

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
            catch (Exception ex)
            {
                EqtTrace.Error("TestHost: Error occured during initialization of TestHost : {0}", ex);
            }
        }

        private static void Run(string[] args)
        {
            TestPlatformEventSource.Instance.TestHostStart();
            var argsDictionary = GetArguments(args);
            var requestHandler = new TestRequestHandler();

            // Attach to exit of parent process
            var parentProcessId = GetIntArgFromDict(argsDictionary, ParentProcessIdArgument);
            OnParentProcessExit(parentProcessId, requestHandler);

            // Setup logging if enabled
            string logFile;
            if (argsDictionary.TryGetValue(LogFileArgument, out logFile))
            {
                EqtTrace.InitializeVerboseTrace(logFile);
            }

            // Get port number and initialize communication
            var portNumber = GetIntArgFromDict(argsDictionary, PortArgument);
            requestHandler.InitializeCommunication(portNumber);

            // Setup the factory
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

            TestPlatformEventSource.Instance.TestHostStop();
        }

        /// <summary>
        /// Parse command line arguments to a dictionary.
        /// </summary>
        /// <param name="args">Command line arguments. Ex: <c>{ "--port", "12312", "--parentprocessid", "2312" }</c></param>
        /// <returns>Dictionary of arguments keys and values.</returns>
        private static IDictionary<string, string> GetArguments(string[] args)
        {
            IDictionary<string, string> argsDictionary = new Dictionary<string, string>();
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

        /// <summary>
        /// Parse the value of an argument as an integer.
        /// </summary>
        /// <param name="argsDictionary">Dictionary of all arguments Ex: <c>{ "--port":"12312", "--parentprocessid":"2312" }</c></param>
        /// <param name="fullname">The full name for required argument. Ex: "--port"</param>
        /// <returns>Value of the argument.</returns>
        /// <exception cref="ArgumentException">Thrown if value of an argument is not an integer.</exception>
        private static int GetIntArgFromDict(IDictionary<string, string> argsDictionary, string fullname)
        {
            string optionValue;
            return argsDictionary.TryGetValue(fullname, out optionValue) ? int.Parse(optionValue) : 0;
        }


        private static void OnParentProcessExit(int parentProcessId, ITestRequestHandler requestHandler)
        {
            EqtTrace.Info("TestHost: exits itself because parent process exited");
            var process = Process.GetProcessById(parentProcessId);
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) =>
                {
                    requestHandler?.Close();
                    TestPlatformEventSource.Instance.TestHostStop();
                    process.Dispose();
                    Environment.Exit(0);
                };
        }

        private static void WaitForDebuggerIfEnabled()
        {
            var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
            if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
            {
                ConsoleOutput.Instance.WriteLine("Waiting for debugger attach...", OutputLevel.Information);

                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                ConsoleOutput.Instance.WriteLine(
                    string.Format("Process Id: {0}, Name: {1}", currentProcess.Id, currentProcess.ProcessName),
                    OutputLevel.Information);

                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Threading.Thread.Sleep(1000);
                }

                System.Diagnostics.Debugger.Break();
            }
        }
    }
}
