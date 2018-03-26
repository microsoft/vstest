// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
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
        private const int ClientListenTimeOut = Timeout.Infinite;

        private const int DataConnectionClientListenTimeOut = 60 * 1000;

        private const string EndpointArgument = "--endpoint";

        private const string RoleArgument = "--role";

        private const string ParentProcessIdArgument = "--parentprocessid";

        private const string LogFileArgument = "--diag";

        private const string DataCollectionPortArgument = "--datacollectionport";

        private const string TelemetryOptedIn = "--telemetryoptedin";

        public void Invoke(IDictionary<string, string> argsDictionary)
        {
            // Setup logging if enabled
            if (argsDictionary.TryGetValue(LogFileArgument, out string logFile))
            {
                EqtTrace.InitializeVerboseTrace(logFile);
            }
            else
            {
                EqtTrace.DoNotInitailize = true;
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
                        (obj) =>
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

                    // It's possible that connection to vstest.console happens, but to datacollector fails, why?
                    // DataCollector keeps the server alive for testhost only for 15secs(increased to 60 now), 
                    // if somehow(on slower machines, with Profiler Enabled) testhost can take considerable time to launch,
                    // in such scenario dc.exe would have killed the server, but testhost will wait infinitely to connect to it,
                    // hence do not wait to connect to datacollector process infinitely, as it will cause process hang.
                    if(!dataCollectionTestCaseEventSender.WaitForRequestSenderConnection(DataConnectionClientListenTimeOut))
                    {
                        EqtTrace.Info("DefaultEngineInvoker: Connection to DataCollector failed: '{0}', DataCollection will not happen in this session", dcPort);
                    }
                }

                // Checks for Telemetry Opted in or not from Command line Arguments.
                // By Default opting out in Test Host to handle scenario when user running old version of vstest.console
                var telemetryStatus = CommandLineArgumentsHelper.GetStringArgFromDict(argsDictionary, TelemetryOptedIn);
                var telemetryOptedIn = false;
                if (!string.IsNullOrWhiteSpace(telemetryStatus))
                {
                    if (telemetryStatus.Equals("true", StringComparison.Ordinal))
                    {
                        telemetryOptedIn = true;
                    }
                }

                var requestData = new RequestData
                                      {
                                          MetricsCollection =
                                              telemetryOptedIn
                                                  ? (IMetricsCollection)new MetricsCollection()
                                                  : new NoOpMetricsCollection(),
                                          IsTelemetryOptedIn = telemetryOptedIn
                };

                // Start processing async in a different task
                EqtTrace.Info("DefaultEngineInvoker: Start Request Processing.");
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
                        // Note that we are waiting here infinitely to connect to vstest.console, but at the same time vstest.console doesn't wait infinitely.
                        // It has a default timeout of 60secs(which is configurable), & then it kills testhost.exe
                        // The reason to wait infinitely, was remote debugging scenarios of UWP app,
                        // in such cases after the app gets launched, VS debugger takes control of it, & causes a lot of delay, which frequently causes timeout with vstest.console.
                        // One fix would be just double this timeout, but there is no telling how much time it can actually take.
                        // Hence we are waiting here indefinelty, to avoid such guessed timeouts, & letting user kill the debugging if they feel it is taking too much time.
                        // In other cases if vstest.console's timeout exceeds it will definitelty such down the app.
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
