// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System;
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

        private static const string PortLongname = "--port";

        private static const string PortShortname = "-p";

        private static const string ParentProcessIdLongname = "--parentprocessid";

        private static const string ParentProcessIdShortname = "-i";

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

                Run(args);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("TestHost: Error occured during initialization of TestHost : {0}", ex);
            }
        }

        /// <summary>
        /// To parse int argument value.
        /// </summary>
        /// <param name="args">
        /// Array of all arguments Ex: { "--port", "12312", "--parentprocessid", "2312" }
        /// </param>
        /// <param name="fullname">
        /// The fullname for required argument. Ex: "--port"
        /// </param>
        /// <param name="shortname">
        /// The shortname for required argument. Ex: "-p"
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        private static int GetIntArg(string[] args, string fullname, string shortname)
        {
            var val = -1;

            for (var i = 0; i < args.Length; i++)
            {
                bool isFullname = fullname != null && string.Equals(fullname, args[i], StringComparison.OrdinalIgnoreCase);
                bool isShortname = shortname != null && string.Equals(shortname, args[i], StringComparison.OrdinalIgnoreCase);
                if (isFullname || isShortname)
                {
                    if (i < args.Length - 1)
                    {
                        int.TryParse(args[i + 1], out val);
                    }

                    break;
                }
            }

            if (val < 0)
            {
                throw new ArgumentException($"Incorrect/No number for: {fullname}/{shortname}");
            }

            return val;
        }

        private static void Run(string[] args)
        {
            TestPlatformEventSource.Instance.TestHostStart();
            var portNumber = GetIntArg(args, PortLongname, PortShortname);
            var requestHandler = new TestRequestHandler();
            requestHandler.InitializeCommunication(portNumber);
            var parentProcessId = GetIntArg(args, ParentProcessIdLongname, ParentProcessIdShortname);
            OnParentProcessExit(parentProcessId, requestHandler);

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

            TestPlatformEventSource.Instance.TestHostStop();
        }

        private static void OnParentProcessExit(int parentProcessId, ITestRequestHandler requestHandler)
        {
            EqtTrace.Info("TestHost: exits itself because parent process exited");
            Process process = Process.GetProcessById(parentProcessId);
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) =>
                {
                    requestHandler?.Close();
                    TestPlatformEventSource.Instance.TestHostStop();
                    process.Dispose();
                    Environment.Exit(0);
                };
        }
    }
}
