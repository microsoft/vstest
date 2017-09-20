// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

    internal class DefaultEngineInvoker :
#if NET451
        MarshalByRefObject,
#endif
        IEngineInvoker
    {
        /// <summary>
        /// The timeout for the client to connect to the server.
        /// Increasing Timeout to allow client to connect, not always the client can connect within 5 seconds
        /// </summary>
        private const int ClientListenTimeOut = 30 * 1000;

        private const string EndpointArgument = "--endpoint";

        private const string RoleArgument = "--role";

        private const string ParentProcessIdArgument = "--parentprocessid";

        private const string LogFileArgument = "--diag";

        private const string DataCollectionPortArgument = "--datacollectionport";

        public void Invoke(IDictionary<string, string> argsDictionary)
        {
            // Setup logging if enabled
            string logFile;
            if (argsDictionary.TryGetValue(LogFileArgument, out logFile))
            {
                EqtTrace.InitializeVerboseTrace(logFile);
            }

#if NET451
            if (EqtTrace.IsInfoEnabled)
            {
                var appConfigText = System.IO.File.ReadAllText(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                EqtTrace.Info("DefaultEngineInvoker: Using Application Configuration: '{0}'", appConfigText);
            }
#endif

            // vstest.console < 15.5 won't send endpoint and role arguments.
            // So derive endpoint from port argument and Make connectionRole as Client.
            string endpoint = CommandLineArgumentsHelper.GetStringArgFromDict(argsDictionary, EndpointArgument);
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                var port = CommandLineArgumentsHelper.GetIntArgFromDict(argsDictionary, "--port");
                endpoint = IPAddress.Loopback + ":" + port;
            }

            var connectionRole = ConnectionRole.Client;
            string role = CommandLineArgumentsHelper.GetStringArgFromDict(argsDictionary, RoleArgument);
            if (!string.IsNullOrWhiteSpace(role) && string.Equals(role, "host", StringComparison.OrdinalIgnoreCase))
            {
                connectionRole = ConnectionRole.Host;
            }

            // Start Processing of requests
            using (var requestHandler = new TestRequestHandler(new TestHostConnectionInfo { Endpoint = endpoint, Role = connectionRole, Transport = Transport.Sockets }))
            {
                // Attach to exit of parent process
                var parentProcessId = CommandLineArgumentsHelper.GetIntArgFromDict(argsDictionary, ParentProcessIdArgument);
                EqtTrace.Info("DefaultEngineInvoker: Monitoring parent process with id: '{0}'", parentProcessId);

                // In remote scenario we cannot monitor parent process, so we expect user to pass parentProcessId as -1
                if (parentProcessId != -1)
                {
                    var processHelper = new ProcessHelper();
                    processHelper.SetExitCallback(
                        parentProcessId,
                        () =>
                            {
                                EqtTrace.Info("DefaultEngineInvoker: ParentProcess '{0}' Exited.", parentProcessId);
                                new PlatformEnvironment().Exit(1);
                            });
                }

                // Initialize Communication
                EqtTrace.Info("DefaultEngineInvoker: Initialize communication on endpoint address: '{0}'", endpoint);
                requestHandler.InitializeCommunication();

                // Initialize DataCollection Communication if data collection port is provided.
                var dcPort = CommandLineArgumentsHelper.GetIntArgFromDict(argsDictionary, DataCollectionPortArgument);
                if (dcPort > 0)
                {
                    var dataCollectionTestCaseEventSender = DataCollectionTestCaseEventSender.Create();
                    dataCollectionTestCaseEventSender.InitializeCommunication(dcPort);
                    dataCollectionTestCaseEventSender.WaitForRequestSenderConnection(ClientListenTimeOut);
                }

                // Start processing async in a different task
                EqtTrace.Info("DefaultEngineInvoker: Start Request Processing.");
                var requestData = new RequestData { MetricsCollection = new MetricsCollection() };
                var processingTask = this.StartProcessingAsync(requestHandler, new TestHostManagerFactory(requestData));

                // Wait for processing to complete.
                Task.WaitAny(processingTask);

                if (dcPort > 0)
                {
                    // Close socket communication connection.
                    DataCollectionTestCaseEventSender.Instance.Close();
                }
            }
        }

        private Task StartProcessingAsync(ITestRequestHandler requestHandler, ITestHostManagerFactory managerFactory)
        {
            var task = new Task(
                () =>
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
                    },
                TaskCreationOptions.LongRunning);

            task.Start();
            return task;
        }
    }
}
