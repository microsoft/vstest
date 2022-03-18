// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;

/// <summary>
/// Orchestrates discovery operations for the engine communicating with the client.
/// </summary>
public class ProxyDiscoveryManager : IProxyDiscoveryManager, IBaseProxy, ITestDiscoveryEventsHandler2
{
    private readonly TestSessionInfo _testSessionInfo;
    readonly Func<string, ProxyDiscoveryManager, ProxyOperationManager> _proxyOperationManagerCreator;

    private ITestRuntimeProvider _testHostManager;
    private IRequestData _requestData;

    private readonly IFileHelper _fileHelper;
    private readonly IDataSerializer _dataSerializer;
    private bool _isCommunicationEstablished;

    private ProxyOperationManager _proxyOperationManager;
    private ITestDiscoveryEventsHandler2 _baseTestDiscoveryEventsHandler;
    private bool _skipDefaultAdapters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
    /// </summary>
    ///
    /// <param name="testSessionInfo">The test session info.</param>
    /// <param name="proxyOperationManagerCreator">The proxy operation manager creator.</param>
    public ProxyDiscoveryManager(
        TestSessionInfo testSessionInfo,
        Func<string, ProxyDiscoveryManager, ProxyOperationManager> proxyOperationManagerCreator)
    {
        // Filling in test session info and proxy information.
        _testSessionInfo = testSessionInfo;
        _proxyOperationManagerCreator = proxyOperationManagerCreator;

        _requestData = null;
        _testHostManager = null;
        _dataSerializer = JsonDataSerializer.Instance;
        _fileHelper = new FileHelper();
        _isCommunicationEstablished = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
    /// </summary>
    ///
    /// <param name="requestData">
    /// The request data for providing discovery services and data.
    /// </param>
    /// <param name="testRequestSender">Test request sender instance.</param>
    /// <param name="testHostManager">Test host manager instance.</param>
    public ProxyDiscoveryManager(
        IRequestData requestData,
        ITestRequestSender testRequestSender,
        ITestRuntimeProvider testHostManager)
        : this(
            requestData,
            testRequestSender,
            testHostManager,
            JsonDataSerializer.Instance,
            new FileHelper())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// Constructor with dependency injection. Used for unit testing.
    /// </remarks>
    ///
    /// <param name="requestData">
    /// The request data for providing discovery services and data.
    /// </param>
    /// <param name="requestSender">The request sender.</param>
    /// <param name="testHostManager">Test host manager instance.</param>
    /// <param name="dataSerializer">The data serializer.</param>
    /// <param name="fileHelper">The file helper.</param>
    internal ProxyDiscoveryManager(
        IRequestData requestData,
        ITestRequestSender requestSender,
        ITestRuntimeProvider testHostManager,
        IDataSerializer dataSerializer,
        IFileHelper fileHelper)
    {
        _requestData = requestData;
        _testHostManager = testHostManager;

        _dataSerializer = dataSerializer;
        _fileHelper = fileHelper;
        _isCommunicationEstablished = false;

        // Create a new proxy operation manager.
        _proxyOperationManager = new ProxyOperationManager(requestData, requestSender, testHostManager, this);
    }


    #region IProxyDiscoveryManager implementation.

    /// <inheritdoc/>
    public void Initialize(bool skipDefaultAdapters)
    {
        _skipDefaultAdapters = skipDefaultAdapters;
    }

    /// <inheritdoc/>
    public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
    {
        if (_proxyOperationManager == null)
        {
            _proxyOperationManager = _proxyOperationManagerCreator(
                discoveryCriteria.Sources.First(),
                this);

            _testHostManager = _proxyOperationManager.TestHostManager;
            _requestData = _proxyOperationManager.RequestData;
        }

        _baseTestDiscoveryEventsHandler = eventHandler;
        try
        {
            _isCommunicationEstablished = _proxyOperationManager.SetupChannel(discoveryCriteria.Sources, discoveryCriteria.RunSettings);

            if (_isCommunicationEstablished)
            {
                InitializeExtensions(discoveryCriteria.Sources);
                discoveryCriteria.UpdateDiscoveryCriteria(_testHostManager);

                _proxyOperationManager.RequestSender.DiscoverTests(discoveryCriteria, this);
            }
        }
        catch (Exception exception)
        {
            EqtTrace.Error("ProxyDiscoveryManager.DiscoverTests: Failed to discover tests: {0}", exception);

            // Log to vs ide test output
            var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = exception.ToString() };
            var rawMessage = _dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            HandleRawMessage(rawMessage);

            // Log to vstest.console
            // Send a discovery complete to caller. Similar logic is also used in ParallelProxyDiscoveryManager.DiscoverTestsOnConcurrentManager
            // Aborted is `true`: in case of parallel discovery (or non shared host), an aborted message ensures another discovery manager
            // created to replace the current one. This will help if the current discovery manager is aborted due to irreparable error
            // and the test host is lost as well.
            HandleLogMessage(TestMessageLevel.Error, exception.ToString());

            var discoveryCompletePayload = new DiscoveryCompletePayload()
            {
                IsAborted = true,
                LastDiscoveredTests = null,
                TotalTests = -1
            };
            HandleRawMessage(_dataSerializer.SerializePayload(MessageType.DiscoveryComplete, discoveryCompletePayload));
            var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(-1, true);

            HandleDiscoveryComplete(discoveryCompleteEventsArgs, new List<TestCase>());
        }
    }

    /// <inheritdoc/>
    public void Abort()
    {
        // Do nothing if the proxy is not initialized yet.
        if (_proxyOperationManager == null)
        {
            return;
        }

        // Cancel fast, try to stop testhost deployment/launch
        _proxyOperationManager.CancellationTokenSource.Cancel();
        Close();
    }

    // <inheritdoc/>
    public void Abort(ITestDiscoveryEventsHandler2 eventHandler)
    {
        // Do nothing if the proxy is not initialized yet.
        if (_proxyOperationManager is null)
        {
            return;
        }

        if (_baseTestDiscoveryEventsHandler is null)
        {
            _baseTestDiscoveryEventsHandler = eventHandler;
        }

        if (_isCommunicationEstablished)
        {
            _proxyOperationManager.RequestSender.SendDiscoveryAbort();
        }
    }

    /// <inheritdoc/>
    public void Close()
    {
        // Do nothing if the proxy is not initialized yet.
        if (_proxyOperationManager == null)
        {
            return;
        }

        // When no test session is being used we don't share the testhost
        // between test discovery and test run. The testhost is closed upon
        // successfully completing the operation it was spawned for.
        //
        // In contrast, the new workflow (using test sessions) means we should keep
        // the testhost alive until explicitly closed by the test session owner.
        if (_testSessionInfo == null)
        {
            _proxyOperationManager.Close();
            return;
        }

        TestSessionPool.Instance.ReturnProxy(_testSessionInfo, _proxyOperationManager.Id);
    }

    /// <inheritdoc/>
    public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
    {
        _baseTestDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, lastChunk);
    }

    /// <inheritdoc/>
    public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
    {
        _baseTestDiscoveryEventsHandler.HandleDiscoveredTests(discoveredTestCases);
    }

    /// <inheritdoc/>
    public void HandleRawMessage(string rawMessage)
    {
        var message = _dataSerializer.DeserializeMessage(rawMessage);
        if (string.Equals(message.MessageType, MessageType.DiscoveryComplete))
        {
            Close();
        }

        _baseTestDiscoveryEventsHandler.HandleRawMessage(rawMessage);
    }

    /// <inheritdoc/>
    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        _baseTestDiscoveryEventsHandler.HandleLogMessage(level, message);
    }

    #endregion

    #region IBaseProxy implementation.
    /// <inheritdoc/>
    public virtual TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
    {
        // Update Telemetry Opt in status because by default in Test Host Telemetry is opted out
        var telemetryOptedIn = _proxyOperationManager.RequestData.IsTelemetryOptedIn ? "true" : "false";
        testProcessStartInfo.Arguments += " --telemetryoptedin " + telemetryOptedIn;
        return testProcessStartInfo;
    }
    #endregion

    private void InitializeExtensions(IEnumerable<string> sources)
    {
        var extensions = TestPluginCache.Instance.GetExtensionPaths(TestPlatformConstants.TestAdapterEndsWithPattern, _skipDefaultAdapters);

        // Filter out non existing extensions
        var nonExistingExtensions = extensions.Where(extension => !_fileHelper.Exists(extension));
        if (nonExistingExtensions.Any())
        {
            LogMessage(TestMessageLevel.Warning, string.Format(Resources.Resources.NonExistingExtensions, string.Join(",", nonExistingExtensions)));
        }

        var sourceList = sources.ToList();
        var platformExtensions = _testHostManager.GetTestPlatformExtensions(sourceList, extensions.Except(nonExistingExtensions));

        // Only send this if needed.
        if (platformExtensions.Any())
        {
            _proxyOperationManager.RequestSender.InitializeDiscovery(platformExtensions);
        }
    }

    private void LogMessage(TestMessageLevel testMessageLevel, string message)
    {
        // Log to translation layer.
        var testMessagePayload = new TestMessagePayload { MessageLevel = testMessageLevel, Message = message };
        var rawMessage = _dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
        HandleRawMessage(rawMessage);

        // Log to vstest.console layer.
        HandleLogMessage(testMessageLevel, message);
    }
}
