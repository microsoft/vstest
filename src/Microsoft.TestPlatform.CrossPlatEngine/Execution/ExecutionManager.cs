// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;

using System;
using System.Collections.Generic;
using System.Linq;

using Common.ExtensionFramework;
using Common.Logging;
using Common.SettingsProvider;
using CommunicationUtilities;
using CoreUtilities.Tracing;
using CoreUtilities.Tracing.Interfaces;
using DataCollection;
using DataCollection.Interfaces;
using Utilities;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using ObjectModel.Engine;
using ObjectModel.Engine.ClientProtocol;
using ObjectModel.Engine.TesthostProtocol;
using ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

/// <summary>
/// Orchestrates test execution related functionality for the engine communicating with the test host process.
/// </summary>
public class ExecutionManager : IExecutionManager
{
    private readonly ITestPlatformEventSource _testPlatformEventSource;
    private BaseRunTests _activeTestRun;
    private readonly IRequestData _requestData;
    private readonly TestSessionMessageLogger _sessionMessageLogger;
    private ITestMessageEventHandler _testMessageEventsHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionManager"/> class.
    /// </summary>
    public ExecutionManager(IRequestData requestData) : this(TestPlatformEventSource.Instance, requestData)
    {
        _sessionMessageLogger = TestSessionMessageLogger.Instance;
        _sessionMessageLogger.TestRunMessage += TestSessionMessageHandler;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionManager"/> class.
    /// </summary>
    /// <param name="testPlatformEventSource">Test platform event source.</param>
    protected ExecutionManager(ITestPlatformEventSource testPlatformEventSource, IRequestData requestData)
    {
        _testPlatformEventSource = testPlatformEventSource;
        _requestData = requestData;
    }

    #region IExecutionManager Implementation

    /// <summary>
    /// Initializes the execution manager.
    /// </summary>
    /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
    public void Initialize(IEnumerable<string> pathToAdditionalExtensions, ITestMessageEventHandler testMessageEventsHandler)
    {
        _testMessageEventsHandler = testMessageEventsHandler;
        _testPlatformEventSource.AdapterSearchStart();

        if (pathToAdditionalExtensions != null && pathToAdditionalExtensions.Any())
        {
            // Start using these additional extensions
            TestPluginCache.Instance.DefaultExtensionPaths = pathToAdditionalExtensions;
        }

        LoadExtensions();

        //unsubscribe session logger
        _sessionMessageLogger.TestRunMessage -= TestSessionMessageHandler;

        _testPlatformEventSource.AdapterSearchStop();
    }

    /// <summary>
    /// Starts the test run
    /// </summary>
    /// <param name="adapterSourceMap"> The adapter Source Map.  </param>
    /// <param name="package">The user input test source(package) if it differ from actual test source otherwise null.</param>
    /// <param name="runSettings"> The run Settings.  </param>
    /// <param name="testExecutionContext"> The test Execution Context. </param>
    /// <param name="testCaseEventsHandler"> EventHandler for handling test cases level events from Engine. </param>
    /// <param name="runEventsHandler"> EventHandler for handling execution events from Engine.  </param>
    public void StartTestRun(
        Dictionary<string, IEnumerable<string>> adapterSourceMap,
        string package,
        string runSettings,
        TestExecutionContext testExecutionContext,
        ITestCaseEventsHandler testCaseEventsHandler,
        ITestRunEventsHandler runEventsHandler)
    {
        try
        {
            InitializeDataCollectors(runSettings, testCaseEventsHandler as ITestEventsPublisher, TestSourcesUtility.GetDefaultCodebasePath(adapterSourceMap));

            _activeTestRun = new RunTestsWithSources(_requestData, adapterSourceMap, package, runSettings, testExecutionContext, testCaseEventsHandler, runEventsHandler);

            _activeTestRun.RunTests();
        }
        catch (Exception e)
        {
            runEventsHandler.HandleLogMessage(TestMessageLevel.Error, e.ToString());
            Abort(runEventsHandler);
        }
        finally
        {
            _activeTestRun = null;
        }
    }

    /// <summary>
    /// Starts the test run with tests.
    /// </summary>
    /// <param name="tests"> The test list. </param>
    /// <param name="package">The user input test source(package) if it differ from actual test source otherwise null.</param>
    /// <param name="runSettings"> The run Settings.  </param>
    /// <param name="testExecutionContext"> The test Execution Context. </param>
    /// <param name="testCaseEventsHandler"> EventHandler for handling test cases level events from Engine. </param>
    /// <param name="runEventsHandler"> EventHandler for handling execution events from Engine. </param>
    public void StartTestRun(
        IEnumerable<TestCase> tests,
        string package,
        string runSettings,
        TestExecutionContext testExecutionContext,
        ITestCaseEventsHandler testCaseEventsHandler,
        ITestRunEventsHandler runEventsHandler)
    {
        try
        {
            InitializeDataCollectors(runSettings, testCaseEventsHandler as ITestEventsPublisher, TestSourcesUtility.GetDefaultCodebasePath(tests));

            _activeTestRun = new RunTestsWithTests(_requestData, tests, package, runSettings, testExecutionContext, testCaseEventsHandler, runEventsHandler);

            _activeTestRun.RunTests();
        }
        catch (Exception e)
        {
            runEventsHandler.HandleLogMessage(TestMessageLevel.Error, e.ToString());
            Abort(runEventsHandler);
        }
        finally
        {
            _activeTestRun = null;
        }
    }

    /// <summary>
    /// Cancel the test execution.
    /// </summary>
    public void Cancel(ITestRunEventsHandler testRunEventsHandler)
    {
        if (_activeTestRun == null)
        {
            var testRunCompleteEventArgs = new TestRunCompleteEventArgs(null, true, false, null, null, null, TimeSpan.Zero);
            testRunEventsHandler.HandleTestRunComplete(testRunCompleteEventArgs, null, null, null);
        }
        else
        {
            _activeTestRun.Cancel();
        }
    }

    /// <summary>
    /// Aborts the test execution.
    /// </summary>
    public void Abort(ITestRunEventsHandler testRunEventsHandler)
    {
        if (_activeTestRun == null)
        {
            var testRunCompleteEventArgs = new TestRunCompleteEventArgs(null, false, true, null, null, null, TimeSpan.Zero);
            testRunEventsHandler.HandleTestRunComplete(testRunCompleteEventArgs, null, null, null);
        }
        else
        {
            _activeTestRun.Abort();
        }
    }

    #endregion

    #region private methods

    private void LoadExtensions()
    {
        try
        {
            // Load the extensions on creation so that we dont have to spend time during first execution.
            EqtTrace.Verbose("TestExecutorService: Loading the extensions");

            TestExecutorExtensionManager.LoadAndInitializeAllExtensions(false);

            EqtTrace.Verbose("TestExecutorService: Loaded the executors");

            SettingsProviderExtensionManager.LoadAndInitializeAllExtensions(false);

            EqtTrace.Verbose("TestExecutorService: Loaded the settings providers");
            EqtTrace.Info("TestExecutorService: Loaded the extensions");
        }
        catch (Exception ex)
        {
            EqtTrace.Warning("TestExecutorWebService: Exception occurred while calling test connection. {0}", ex);
        }
    }

    /// <summary>
    /// Initializes out-proc and in-proc data collectors.
    /// </summary>
    private void InitializeDataCollectors(string runSettings, ITestEventsPublisher testEventsPublisher, string defaultCodeBase)
    {
        // Initialize out-proc data collectors if declared in run settings.
        if (DataCollectionTestCaseEventSender.Instance != null && XmlRunSettingsUtilities.IsDataCollectionEnabled(runSettings))
        {
            _ = new ProxyOutOfProcDataCollectionManager(DataCollectionTestCaseEventSender.Instance, testEventsPublisher);
        }

        // Initialize in-proc data collectors if declared in run settings.
        if (XmlRunSettingsUtilities.IsInProcDataCollectionEnabled(runSettings))
        {
            _ = new InProcDataCollectionExtensionManager(runSettings, testEventsPublisher, defaultCodeBase, TestPluginCache.Instance);
        }
    }

    private void TestSessionMessageHandler(object sender, TestRunMessageEventArgs e)
    {
        if (_testMessageEventsHandler != null)
        {
            _testMessageEventsHandler.HandleLogMessage(e.Level, e.Message);
        }
        else
        {
            EqtTrace.Warning(
                "ExecutionManager: Could not pass the log message  '{0}' as the callback is null.",
                e.Message);
        }
    }

    #endregion
}
