// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

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
            // Setup logging if enabled
            string logFile;
            if (argsDictionary.TryGetValue(LogFileArgument, out logFile))
            {
                EqtTrace.InitializeVerboseTrace(logFile);
            }

#if NET46
            if (EqtTrace.IsInfoEnabled)
            {
                var appConfigText = System.IO.File.ReadAllText(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                EqtTrace.Info("DefaultEngineInvoker: Using Application Configuration: '{0}'", appConfigText);
            }
#endif

            // Get port number and initialize communication
            var portNumber = GetIntArgFromDict(argsDictionary, PortArgument);

            // Start Processing of requests
            using (var requestHandler = new TestRequestHandler())
            {
                // Attach to exit of parent process
                var parentProcessId = GetIntArgFromDict(argsDictionary, ParentProcessIdArgument);
                EqtTrace.Info("DefaultEngineInvoker: Monitoring parent process with id: '{0}'", parentProcessId);
                var parentProcessMonitoringTask = WaitForParentProcessExitAsync(parentProcessId);

                // Initialize Communication
                EqtTrace.Info("DefaultEngineInvoker: Initialize communication on port number: '{0}'", portNumber);
                requestHandler.InitializeCommunication(portNumber);

                // Start processing async in a different task
                EqtTrace.Info("DefaultEngineInvoker: Start Request Processing.");
                var processingTask = StartProcessingAsync(requestHandler, new TestHostManagerFactory());

                // Wait for either processing to complete or parent process exit
                Task.WaitAny(processingTask, parentProcessMonitoringTask);
            }
        }

        private Task StartProcessingAsync(ITestRequestHandler requestHandler, ITestHostManagerFactory managerFactory)
        {
            return Task.Run(() =>
            {
                // Wait for the connection to the sender and start processing requests from sender
                if (requestHandler.WaitForRequestSenderConnection(ClientListenTimeOut))
                {
                    requestHandler.ProcessRequests(managerFactory);
                }
                else
                {
                    EqtTrace.Info("DefaultEngineInvoker: RequestHandler timed out while connecting to the Sender.");
                    throw new TimeoutException();
                }
            });
        }

        private static Task WaitForParentProcessExitAsync(int parentProcessId)
        {
            var parentProcessExitedHandle = new AutoResetEvent(false);
            var process = Process.GetProcessById(parentProcessId);

            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) =>
            {
                EqtTrace.Info("DefaultEngineInvoker: ParentProcess '{0}' Exited.", parentProcessId);
                parentProcessExitedHandle.Set();
            };

            return Task.Run(() => parentProcessExitedHandle.WaitOne());
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
    }
}
