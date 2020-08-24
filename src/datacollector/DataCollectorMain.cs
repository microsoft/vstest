// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using PlatformAbstractions.Interfaces;
    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;

    public class DataCollectorMain
    {
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
        /// Trace level for logs.
        /// </summary>
        private const string TraceLevelArgument = "--tracelevel";

        private IProcessHelper processHelper;

        private IEnvironment environment;
        private IDataCollectionRequestHandler requestHandler;

        public DataCollectorMain():
            this(
                new ProcessHelper(),
                new PlatformEnvironment(),
                DataCollectionRequestHandler.Create(new SocketCommunicationManager(), new MessageSink())
                )
        {
        }

        internal DataCollectorMain(IProcessHelper processHelper, IEnvironment environment, IDataCollectionRequestHandler requestHandler)
        {
            this.processHelper = processHelper;
            this.environment = environment;
            this.requestHandler = requestHandler;
        }

        public void Run(string[] args)
        {
            WaitForDebuggerIfEnabled();
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args);

            // Setup logging if enabled
            string logFile;
            if (argsDictionary.TryGetValue(LogFileArgument, out logFile))
            {
                var traceLevelInt = CommandLineArgumentsHelper.GetIntArgFromDict(argsDictionary, TraceLevelArgument);
                var isTraceLevelArgValid = Enum.IsDefined(typeof(PlatformTraceLevel), traceLevelInt);

                // In case traceLevelInt is not defined in PlatfromTraceLevel, default it to verbose.
                var traceLevel = isTraceLevelArgValid ? (PlatformTraceLevel)traceLevelInt : PlatformTraceLevel.Verbose;

                // Initialize trace.
                EqtTrace.InitializeTrace(logFile, traceLevel);


                // Log warning in case tracelevel passed in arg is invalid
                if (!isTraceLevelArgValid)
                {
                    EqtTrace.Warning("DataCollectorMain.Run: Invalid trace level: {0}, defaulting to verbose tracelevel.", traceLevelInt);
                }
            }
            else
            {
                EqtTrace.DoNotInitailize = true;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                var version = typeof(DataCollectorMain)
                    .GetTypeInfo()
                    .Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                EqtTrace.Verbose($"Version: { version }");
            }

            SetCultureSpecifiedByUser();

            EqtTrace.Info("DataCollectorMain.Run: Starting data collector run with args: {0}", string.Join(",", args));

            // Attach to exit of parent process
            var parentProcessId = CommandLineArgumentsHelper.GetIntArgFromDict(argsDictionary, ParentProcessArgument);
            EqtTrace.Info("DataCollector: Monitoring parent process with id: '{0}'", parentProcessId);

            this.processHelper.SetExitCallback(
                parentProcessId,
                (obj) =>
                {
                    EqtTrace.Info("DataCollector: ParentProcess '{0}' Exited.", parentProcessId);
                    this.environment.Exit(1);
                });

            // Get server port and initialize communication.
            string portValue;
            int port = argsDictionary.TryGetValue(PortArgument, out portValue) ? int.Parse(portValue) : 0;

            if (port <= 0)
            {
                throw new ArgumentException("Incorrect/No Port number");
            }

            this.requestHandler.InitializeCommunication(port);

            // Can only do this after InitializeCommunication because datacollector cannot "Send Log" unless communications are initialized
            if (!string.IsNullOrEmpty(EqtTrace.LogFile))
            {
                (this.requestHandler as DataCollectionRequestHandler)?.SendDataCollectionMessage(new DataCollectionMessageEventArgs(TestMessageLevel.Informational, string.Format("Logging DataCollector Diagnostics in file: {0}", EqtTrace.LogFile)));
            }

            // Start processing async in a different task
            EqtTrace.Info("DataCollector: Start Request Processing.");
            StartProcessing();
        }

        private void WaitForDebuggerIfEnabled()
        {
            var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_DATACOLLECTOR_DEBUG");
            if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
            {
                while (!Debugger.IsAttached)
                {
                    System.Threading.Thread.Sleep(1000);
                }

                Debugger.Break();
            }
        }

        private static void SetCultureSpecifiedByUser()
        {
            var userCultureSpecified = Environment.GetEnvironmentVariable(CoreUtilities.Constants.DotNetUserSpecifiedCulture);
            if (!string.IsNullOrWhiteSpace(userCultureSpecified))
            {
                try
                {
                    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(userCultureSpecified);
                }
                catch (Exception)
                {
                    EqtTrace.Info(string.Format("Invalid Culture Info: {0}", userCultureSpecified));
                }
            }
        }

        private void StartProcessing()
        {
            var timeout = EnvironmentHelper.GetConnectionTimeout();

            // Wait for the connection to the sender and start processing requests from sender
            if (this.requestHandler.WaitForRequestSenderConnection(timeout * 1000))
            {
                this.requestHandler.ProcessRequests();
            }
            else
            {
                EqtTrace.Error(
                    "DataCollectorMain.StartProcessing: RequestHandler timed out while connecting to the Sender, timeout: {0} seconds.",
                    timeout);

                this.requestHandler.Close();

                throw new TestPlatformException(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CommunicationUtilitiesResources.ConnectionTimeoutErrorMessage,
                        CoreUtilitiesConstants.DatacollectorProcessName,
                        CoreUtilitiesConstants.VstestConsoleProcessName,
                        timeout,
                        EnvironmentHelper.VstestConnectionTimeout)
                    );
            }
        }
    }
}