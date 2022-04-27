// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities;

using CrossPlatResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;

/// <summary>
/// Orchestrates test session operations for the engine communicating with the client.
/// </summary>
public class ProxyTestSessionManager : IProxyTestSessionManager
{
    private enum TestSessionState
    {
        Unknown,
        Error,
        Active,
        Terminated
    }

    private readonly object _lockObject = new();
    private readonly object _proxyOperationLockObject = new();
    private volatile bool _proxySetupFailed;
    private readonly StartTestSessionCriteria _testSessionCriteria;
    private readonly int _testhostCount;
    private TestSessionInfo _testSessionInfo;
    private readonly Func<ProxyOperationManager> _proxyCreator;
    private readonly IList<ProxyOperationManagerContainer> _proxyContainerList;
    private readonly IDictionary<string, int> _proxyMap;
    private readonly Stopwatch _testSessionStopwatch;
    private IDictionary<string, string> _runSettingsEnvironmentVariables;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyTestSessionManager"/> class.
    /// </summary>
    /// 
    /// <param name="criteria">The test session criteria.</param>
    /// <param name="testhostCount">The testhost count.</param>
    /// <param name="proxyCreator">The proxy creator.</param>
    public ProxyTestSessionManager(
        StartTestSessionCriteria criteria,
        int testhostCount,
        Func<ProxyOperationManager> proxyCreator)
    {
        _testSessionCriteria = criteria;
        _testhostCount = testhostCount;
        _proxyCreator = proxyCreator;

        _proxyContainerList = new List<ProxyOperationManagerContainer>();
        _proxyMap = new Dictionary<string, int>();
        _runSettingsEnvironmentVariables = new Dictionary<string, string>();
        _testSessionStopwatch = new Stopwatch();
    }

    // NOTE: The method is virtual for mocking purposes.
    /// <inheritdoc/>
    public virtual bool StartSession(ITestSessionEventsHandler eventsHandler, IRequestData requestData)
    {
        lock (_lockObject)
        {
            if (_testSessionInfo != null)
            {
                return false;
            }
            _testSessionInfo = new TestSessionInfo();
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Create all the proxies in parallel, one task per proxy.
        var taskList = new Task[_testhostCount];
        for (int i = 0; i < taskList.Length; ++i)
        {
            // The testhost count is equal to 1 because one of the following conditions
            // holds true:
            //     1. we're dealing with a shared testhost (e.g.: .NET Framework testhost)
            //        that must process multiple sources within the same testhost process;
            //     2. we're dealing with a single testhost (shared or not, it doesn't matter)
            //        that must process a single source;
            // Either way, no further processing of the original test source list is needed
            // in either of those cases.
            //
            // Consequentely, if the testhost count is greater than one it means that the
            // testhost is not shared (e.g.: .NET Core testhost), in which case each test
            // source must be processed by a dedicated testhost, which is the reason we
            // create a list with a single element, i.e. the current source to be processed.
            var sources = (_testhostCount == 1)
                ? _testSessionCriteria.Sources
                : new List<string>() { _testSessionCriteria.Sources[i] };

            taskList[i] = Task.Factory.StartNew(() =>
            {
                if (!SetupRawProxy(
                        sources,
                        _testSessionCriteria.RunSettings))
                {
                    _proxySetupFailed = true;
                }
            });
        }

        // Wait for proxy creation to be over.
        Task.WaitAll(taskList);
        stopwatch.Stop();

        // Collecting session metrics.
        requestData?.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionId,
            _testSessionInfo.Id);
        requestData?.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionSpawnedTesthostCount,
            _proxyContainerList.Count);
        requestData?.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionTesthostSpawnTimeInSec,
            stopwatch.Elapsed.TotalSeconds);

        // Dispose of all proxies if even one of them failed during setup.
        if (_proxySetupFailed)
        {
            requestData?.MetricsCollection.Add(
                TelemetryDataConstants.TestSessionState,
                TestSessionState.Error.ToString());
            DisposeProxies();
            return false;
        }

        // Make the session available.
        if (!TestSessionPool.Instance.AddSession(_testSessionInfo, this))
        {
            requestData?.MetricsCollection.Add(
                TelemetryDataConstants.TestSessionState,
                TestSessionState.Error.ToString());
            DisposeProxies();
            return false;
        }

        requestData?.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionState,
            TestSessionState.Active.ToString());

        // This counts as the session start time.
        _testSessionStopwatch.Start();

        // Let the caller know the session has been created.
        eventsHandler.HandleStartTestSessionComplete(
            new()
            {
                TestSessionInfo = _testSessionInfo,
                Metrics = requestData?.MetricsCollection.Metrics
            });
        return true;
    }

    // NOTE: The method is virtual for mocking purposes.
    /// <inheritdoc/>
    public virtual bool StopSession(IRequestData requestData)
    {
        var testSessionId = string.Empty;
        lock (_lockObject)
        {
            if (_testSessionInfo == null)
            {
                return false;
            }

            testSessionId = _testSessionInfo.Id.ToString();
            _testSessionInfo = null;
        }

        // Dispose of the pooled testhosts.
        DisposeProxies();

        // Compute session time.
        _testSessionStopwatch.Stop();

        // Collecting session metrics.
        requestData?.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionId,
            testSessionId);
        requestData?.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionTotalSessionTimeInSec,
            _testSessionStopwatch.Elapsed.TotalSeconds);
        requestData?.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionState,
            TestSessionState.Terminated.ToString());

        return true;
    }

    /// <summary>
    /// Dequeues a proxy to be used either by discovery or execution.
    /// </summary>
    /// 
    /// <param name="source">The source to be associated to this proxy.</param>
    /// <param name="runSettings">The run settings.</param>
    /// 
    /// <returns>The dequeued proxy.</returns>
    public virtual ProxyOperationManager DequeueProxy(string source, string runSettings)
    {
        ProxyOperationManagerContainer proxyContainer = null;

        lock (_proxyOperationLockObject)
        {
            // No proxy available means the caller will have to create its own proxy.
            if (!_proxyMap.ContainsKey(source)
                || !_proxyContainerList[_proxyMap[source]].IsAvailable)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatResources.NoAvailableProxyForDeque));
            }

            // We must ensure the current run settings match the run settings from when the
            // testhost was started. If not, throw an exception to force the caller to create
            // its own proxy instead.
            if (!CheckRunSettingsAreCompatible(runSettings))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatResources.NoProxyMatchesDescription));
            }

            // Get the actual proxy.
            proxyContainer = _proxyContainerList[_proxyMap[source]];

            // Mark the proxy as unavailable.
            proxyContainer.IsAvailable = false;
        }

        return proxyContainer.Proxy;
    }

    /// <summary>
    /// Enqueues a proxy back once discovery or executions is done with it.
    /// </summary>
    /// 
    /// <param name="proxyId">The id of the proxy to be re-enqueued.</param>
    /// 
    /// <returns>True if the operation succeeded, false otherwise.</returns>
    public virtual bool EnqueueProxy(int proxyId)
    {
        lock (_proxyOperationLockObject)
        {
            // Check if the proxy exists.
            if (proxyId < 0 || proxyId >= _proxyContainerList.Count)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatResources.NoSuchProxyId,
                        proxyId));
            }

            // Get the actual proxy.
            var proxyContainer = _proxyContainerList[proxyId];
            if (proxyContainer.IsAvailable)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatResources.ProxyIsAlreadyAvailable,
                        proxyId));
            }

            // Mark the proxy as available.
            proxyContainer.IsAvailable = true;
        }

        return true;
    }

    private int EnqueueNewProxy(
        IList<string> sources,
        ProxyOperationManagerContainer operationManagerContainer)
    {
        lock (_proxyOperationLockObject)
        {
            var index = _proxyContainerList.Count;

            // Add the proxy container to the proxy container list.
            _proxyContainerList.Add(operationManagerContainer);

            foreach (var source in sources)
            {
                // Add the proxy index to the map.
                _proxyMap.Add(
                    source,
                    index);
            }

            return index;
        }
    }

    private bool SetupRawProxy(
        IList<string> sources,
        string runSettings)
    {
        try
        {
            // Create and cache the proxy.
            var operationManagerProxy = _proxyCreator();
            if (operationManagerProxy == null)
            {
                return false;
            }

            // Initialize the proxy.
            operationManagerProxy.Initialize(skipDefaultAdapters: false);

            // Start the test host associated to the proxy.
            if (!operationManagerProxy.SetupChannel(sources, runSettings))
            {
                return false;
            }

            // Associate each source in the source list with this new proxy operation
            // container.
            var operationManagerContainer = new ProxyOperationManagerContainer(
                operationManagerProxy,
                available: true);

            operationManagerContainer.Proxy.Id = EnqueueNewProxy(sources, operationManagerContainer);
            return true;
        }
        catch (Exception ex)
        {
            // Log & silently eat up the exception. It's a valid course of action to
            // just forfeit proxy creation. This means that anyone wishing to get a
            // proxy operation manager would have to create their own, on the spot,
            // instead of getting one already created, and this case is handled
            // gracefully already.
            EqtTrace.Error(
                "ProxyTestSessionManager.StartSession: Cannot create proxy. Error: {0}",
                ex.ToString());
        }

        return false;
    }

    private void DisposeProxies()
    {
        lock (_proxyOperationLockObject)
        {
            if (_proxyContainerList.Count == 0)
            {
                return;
            }

            // Dispose of all the proxies in parallel, one task per proxy.
            int i = 0;
            var taskList = new Task[_proxyContainerList.Count];
            foreach (var proxyContainer in _proxyContainerList)
            {
                taskList[i++] = Task.Factory.StartNew(() =>
                    // Initiate the end session handshake with the underlying testhost.
                    proxyContainer.Proxy.Close());
            }

            // Wait for proxy disposal to be over.
            Task.WaitAll(taskList);

            _proxyContainerList.Clear();
            _proxyMap.Clear();
        }
    }

    private bool CheckRunSettingsAreCompatible(string requestRunSettings)
    {
        if (_runSettingsEnvironmentVariables.Count == 0)
        {
            _runSettingsEnvironmentVariables = InferRunSettingsHelper.GetEnvironmentVariables(
                    _testSessionCriteria.RunSettings)
                ?? _runSettingsEnvironmentVariables;
        }

        // Environment variable sets should be identical, otherwise it's not safe to reuse the
        // already running testhosts.
        var requestEnvironmentVariables = InferRunSettingsHelper.GetEnvironmentVariables(requestRunSettings);
        if (requestEnvironmentVariables != null
            && _runSettingsEnvironmentVariables != null
            && (requestEnvironmentVariables.Count != _runSettingsEnvironmentVariables.Count
                || requestEnvironmentVariables.Except(_runSettingsEnvironmentVariables).Any()))
        {
            return false;
        }

        // Data collection is not supported for test sessions yet.
        if (XmlRunSettingsUtilities.IsDataCollectionEnabled(requestRunSettings))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Defines a container encapsulating the proxy and its corresponding state info.
/// </summary>
internal class ProxyOperationManagerContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyOperationManagerContainer"/> class.
    /// </summary>
    /// 
    /// <param name="proxy">The proxy.</param>
    /// <param name="available">A flag indicating if the proxy is available to do work.</param>
    public ProxyOperationManagerContainer(ProxyOperationManager proxy, bool available)
    {
        Proxy = proxy;
        IsAvailable = available;
    }

    /// <summary>
    /// Gets or sets the proxy.
    /// </summary>
    public ProxyOperationManager Proxy { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if the proxy is available to do work.
    /// </summary>
    public bool IsAvailable { get; set; }
}
