// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector;

using System;
using System.Globalization;
using System.Reflection;

using CommunicationUtilities;
using CommunicationUtilities.DataCollection;
using CommunicationUtilities.DataCollection.Interfaces;

using CoreUtilities.Helpers;

using Execution;

using ObjectModel;
using ObjectModel.Logging;

using PlatformAbstractions;

using PlatformAbstractions.Interfaces;

using CommunicationUtilitiesResources = CommunicationUtilities.Resources.Resources;
using CoreUtilitiesConstants = CoreUtilities.Constants;

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

    private readonly IProcessHelper _processHelper;

    private readonly IEnvironment _environment;
    private readonly IDataCollectionRequestHandler _requestHandler;

    public DataCollectorMain() :
        this(
            new ProcessHelper(),
            new PlatformEnvironment(),
            DataCollectionRequestHandler.Create(new SocketCommunicationManager(), new MessageSink())
        )
    {
    }

    internal DataCollectorMain(IProcessHelper processHelper, IEnvironment environment, IDataCollectionRequestHandler requestHandler)
    {
        _processHelper = processHelper;
        _environment = environment;
        _requestHandler = requestHandler;
    }

    public void Run(string[] args)
    {
        DebuggerBreakpoint.AttachVisualStudioDebugger("VSTEST_DATACOLLECTOR_DEBUG_ATTACHVS");
        DebuggerBreakpoint.WaitForDebugger("VSTEST_DATACOLLECTOR_DEBUG");

        var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args);

        // Setup logging if enabled
        if (argsDictionary.TryGetValue(LogFileArgument, out var logFile))
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

        UiLanguageOverride.SetCultureSpecifiedByUser();

        EqtTrace.Info("DataCollectorMain.Run: Starting data collector run with args: {0}", string.Join(",", args));

        // Attach to exit of parent process
        var parentProcessId = CommandLineArgumentsHelper.GetIntArgFromDict(argsDictionary, ParentProcessArgument);
        EqtTrace.Info("DataCollector: Monitoring parent process with id: '{0}'", parentProcessId);

        _processHelper.SetExitCallback(
            parentProcessId,
            (obj) =>
            {
                EqtTrace.Info("DataCollector: ParentProcess '{0}' Exited.", parentProcessId);
                _environment.Exit(1);
            });

        // Get server port and initialize communication.
        int port = argsDictionary.TryGetValue(PortArgument, out var portValue) ? int.Parse(portValue) : 0;

        if (port <= 0)
        {
            throw new ArgumentException("Incorrect/No Port number");
        }

        _requestHandler.InitializeCommunication(port);

        // Can only do this after InitializeCommunication because datacollector cannot "Send Log" unless communications are initialized
        if (!string.IsNullOrEmpty(EqtTrace.LogFile))
        {
            (_requestHandler as DataCollectionRequestHandler)?.SendDataCollectionMessage(new DataCollectionMessageEventArgs(TestMessageLevel.Informational, string.Format("Logging DataCollector Diagnostics in file: {0}", EqtTrace.LogFile)));
        }

        // Start processing async in a different task
        EqtTrace.Info("DataCollector: Start Request Processing.");
        StartProcessing();
    }


    private void StartProcessing()
    {
        var timeout = EnvironmentHelper.GetConnectionTimeout();

        // Wait for the connection to the sender and start processing requests from sender
        if (_requestHandler.WaitForRequestSenderConnection(timeout * 1000))
        {
            _requestHandler.ProcessRequests();
        }
        else
        {
            EqtTrace.Error(
                "DataCollectorMain.StartProcessing: RequestHandler timed out while connecting to the Sender, timeout: {0} seconds.",
                timeout);

            _requestHandler.Close();

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
