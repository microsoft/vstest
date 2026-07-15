// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// An <see cref="IProxyDiscoveryManager"/> that discovers tests by driving a
/// Microsoft.Testing.Platform (MTP) application over the MTP <c>dotnettestcli</c> named-pipe protocol
/// (<c>--list-tests</c>) instead of the vstest testhost protocol.
/// </summary>
internal sealed class MtpProxyDiscoveryManager : IProxyDiscoveryManager, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public void Initialize(bool skipDefaultAdapters)
    {
    }

    public void InitializeDiscovery(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler, bool skipDefaultAdapters)
        => Initialize(skipDefaultAdapters);

    public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
    {
        var sources = discoveryCriteria.Sources?.ToList() ?? new List<string>();
        long totalTests = 0;
        bool aborted = false;

        foreach (string source in sources)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                aborted = true;
                break;
            }

            try
            {
                totalTests += DiscoverSource(source, eventHandler);
            }
            catch (OperationCanceledException)
            {
                aborted = true;
                break;
            }
            catch (Exception ex)
            {
                EqtTrace.Error("MtpProxyDiscoveryManager.DiscoverTests: discovery failed for '{0}': {1}", source, ex);
                eventHandler.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, $"Microsoft.Testing.Platform discovery failed for '{source}': {ex.Message}");
                aborted = true;
            }
        }

        eventHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(totalTests, aborted), null);
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

    private int DiscoverSource(string source, ITestDiscoveryEventsHandler2 eventHandler)
    {
        var discovered = new List<TestCase>();

        (string fileName, string arguments, string workingDirectory) = MtpLaunch.Resolve(source);

        using var application = new TestApplication(fileName, $"{arguments} --list-tests".TrimStart(), workingDirectory)
        {
            OnDiscovered = message =>
            {
                foreach (DiscoveredTestMessage discoveredTest in message.DiscoveredMessages)
                {
                    lock (discovered)
                    {
                        discovered.Add(MtpMessageConverter.ToTestCase(discoveredTest, source));
                    }
                }

                return System.Threading.Tasks.Task.CompletedTask;
            },
        };

        application.RunAsync(afterProcessStartCallback: null, _cancellationTokenSource.Token).GetAwaiter().GetResult();

        List<TestCase> chunk;
        lock (discovered)
        {
            chunk = discovered.ToList();
        }

        if (chunk.Count > 0)
        {
            eventHandler.HandleDiscoveredTests(chunk);
        }

        return chunk.Count;
    }
}
