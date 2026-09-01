// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Testing.Platform.ServerMode.Client;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// An <see cref="IProxyDiscoveryManager"/> that discovers tests by driving a
/// Microsoft.Testing.Platform (MTP) application over the MTP JSON-RPC protocol instead of the
/// vstest testhost protocol.
/// </summary>
internal sealed class MtpProxyDiscoveryManager : IProxyDiscoveryManager, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly int _protocolVersion;

    public MtpProxyDiscoveryManager(int protocolVersion)
    {
        _protocolVersion = protocolVersion;
    }

    public void Initialize(bool skipDefaultAdapters)
    {
    }

    public void InitializeDiscovery(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler, bool skipDefaultAdapters)
        => Initialize(skipDefaultAdapters);

    public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
    {
        var sources = discoveryCriteria.Sources?.ToList() ?? new List<string>();
        var fullyDiscoveredSources = new List<string>();
        var partiallyDiscoveredSources = new List<string>();
        long totalTests = 0;
        bool aborted = false;

        foreach (string source in sources)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                aborted = true;
                break;
            }

            bool discoveryStarted = false;
            try
            {
                totalTests += DiscoverSource(source, eventHandler, out discoveryStarted);
                fullyDiscoveredSources.Add(source);
            }
            catch (OperationCanceledException)
            {
                if (discoveryStarted)
                {
                    partiallyDiscoveredSources.Add(source);
                }

                aborted = true;
                break;
            }
            catch (Exception ex)
            {
                EqtTrace.Error("MtpProxyDiscoveryManager.DiscoverTests: discovery failed for '{0}': {1}", source, ex);
                ReportLogMessage(eventHandler, TestMessageLevel.Error, $"Microsoft.Testing.Platform discovery failed for '{source}': {ex.Message}");
                if (discoveryStarted)
                {
                    partiallyDiscoveredSources.Add(source);
                }

                aborted = true;
            }
        }

        List<string> notDiscoveredSources = sources
            .Except(fullyDiscoveredSources)
            .Except(partiallyDiscoveredSources)
            .ToList();
        long reportedTotalTests = aborted ? -1 : totalTests;
        var completeArgs = new DiscoveryCompleteEventArgs(reportedTotalTests, aborted)
        {
            FullyDiscoveredSources = fullyDiscoveredSources,
            PartiallyDiscoveredSources = partiallyDiscoveredSources,
            NotDiscoveredSources = notDiscoveredSources,
        };
        var completePayload = new DiscoveryCompletePayload
        {
            TotalTests = reportedTotalTests,
            IsAborted = aborted,
            FullyDiscoveredSources = fullyDiscoveredSources,
            PartiallyDiscoveredSources = partiallyDiscoveredSources,
            NotDiscoveredSources = notDiscoveredSources,
        };

        eventHandler.HandleRawMessage(JsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, completePayload, _protocolVersion));
        eventHandler.HandleDiscoveryComplete(completeArgs, null);
    }

    public void Abort() => _cancellationTokenSource.Cancel();

    public void Abort(ITestDiscoveryEventsHandler2 eventHandler) => Abort();

    public void Close() => _cancellationTokenSource.Cancel();

    public void Dispose()
    {
        try
        {
            _cancellationTokenSource.Dispose();
        }
        catch
        {
            // ignore
        }
    }

    private int DiscoverSource(string source, ITestDiscoveryEventsHandler2 eventHandler, out bool discoveryStarted)
    {
        discoveryStarted = false;
        var discovered = new List<TestCase>();

        MtpServerClientOptions options = MtpClientOptionsFactory.CreateOptions();
        using IMtpServerClient client = MtpServerClientFactory.Launch(source, options);
        client.LogReceived += (_, e) => ReportLogMessage(eventHandler, MtpClientOptionsFactory.MapServerLogLevel(e.Level), e.Message);
        client.TestNodesUpdated += (_, e) =>
        {
            foreach (MtpTestNodeUpdate change in e.Changes)
            {
                if (MtpTestNodeConverter.IsActionNode(change))
                {
                    lock (discovered)
                    {
                        discovered.Add(MtpTestNodeConverter.ToTestCase(change, source));
                    }
                }
            }
        };

        try
        {
            client.InitializeAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();

            // Awaiting the discover request is sufficient: server-to-client messages arrive on a single
            // ordered stream that the client reads sequentially and dispatches synchronously, so every
            // node notification has already been delivered by the time the request completes.
            discoveryStarted = true;
            client.DiscoverTestsAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();
        }
        finally
        {
            MtpServerClientFactory.TryExit(client);
        }

        List<TestCase> chunk;
        lock (discovered)
        {
            chunk = discovered.ToList();
        }

        if (chunk.Count > 0)
        {
            eventHandler.HandleRawMessage(JsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, chunk, _protocolVersion));
            eventHandler.HandleDiscoveredTests(chunk);
        }

        return chunk.Count;
    }

    private void ReportLogMessage(ITestDiscoveryEventsHandler2 eventHandler, TestMessageLevel level, string? message)
    {
        var payload = new TestMessagePayload { MessageLevel = level, Message = message };
        eventHandler.HandleRawMessage(JsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, payload, _protocolVersion));
        eventHandler.HandleLogMessage(level, message);
    }
}
