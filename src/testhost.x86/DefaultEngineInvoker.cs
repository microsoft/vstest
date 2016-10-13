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

        private const string PortLongname = "--port";

        private const string PortShortname = "-p";

        private const string ParentProcessIdLongname = "--parentprocessid";

        private const string ParentProcessIdShortname = "-i";

        public void Invoke(IDictionary<string, string> argsDictionary)
        {
            TestPlatformEventSource.Instance.TestHostStart();
            var portNumber = GetIntArgFromDict(argsDictionary, PortLongname, PortShortname);
            var requestHandler = new TestRequestHandler();
            requestHandler.InitializeCommunication(portNumber);
            var parentProcessId = GetIntArgFromDict(argsDictionary, ParentProcessIdLongname, ParentProcessIdShortname);
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

        /// <summary>
        /// Parse the value of an argument as an integer.
        /// </summary>
        /// <param name="argsDictionary">Dictionary of all arguments Ex: <c>{ "--port":"12312", "--parentprocessid":"2312" }</c></param>
        /// <param name="fullname">The full name for required argument. Ex: "--port"</param>
        /// <returns>Value of the argument.</returns>
        /// <exception cref="ArgumentException">Thrown if value of an argument is not an integer.</exception>
        private static int GetIntArgFromDict(IDictionary<string, string> argsDictionary, string fullname, string shortname)
        {
            var val = -1;
            if (argsDictionary.ContainsKey(fullname) && argsDictionary[fullname] != null)
            {
                int.TryParse(argsDictionary[fullname], out val);
            }
            else if (argsDictionary.ContainsKey(shortname) && argsDictionary[shortname] != null)
            {
                int.TryParse(argsDictionary[shortname], out val);
            }

            if (val < 0)
            {
                throw new ArgumentException($"Incorrect/No number for: {fullname}/{shortname}");
            }

            return val;
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
    }
}
