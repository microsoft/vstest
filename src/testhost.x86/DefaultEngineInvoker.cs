namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class DefaultEngineInvoker :
#if NET46
        MarshalByRefObject,
#endif
        IEngineInvoker
    {
        /// <summary>
        /// The timeout for the client to connect to the server.
        /// </summary>
        private const int ClientListenTimeOut = 5 * 1000;

        private const string PortArgument = "--port";

        private const string ParentProcessIdArgument = "--parentprocessid";

        private const string LogFileArgument = "--diag";

        public void Invoke(IDictionary<string, string> argsDictionary)
        {
            TestPlatformEventSource.Instance.TestHostStart();
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
            EqtTrace.Info("DefaultEngineInvoker: Exiting self because parent process exited");
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
    }
}
