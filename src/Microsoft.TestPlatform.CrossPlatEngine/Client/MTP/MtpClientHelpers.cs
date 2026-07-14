// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Jsonite;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// Shared helpers for the MTP proxies.
/// </summary>
internal static class MtpClientHelpers
{
    public static Dictionary<string, object?> InitializeParameters()
        => new()
        {
            ["processId"] = GetCurrentProcessId(),
            ["clientInfo"] = new Dictionary<string, object?>
            {
                ["name"] = "vstest",
                ["version"] = "1.0.0",
            },
            ["capabilities"] = new Dictionary<string, object?>
            {
                ["testing"] = new Dictionary<string, object?>
                {
                    ["debuggerProvider"] = false,
                },
            },
        };

    public static TestMessageLevel MapLevel(string level)
        => level switch
        {
            "Error" or "Critical" => TestMessageLevel.Error,
            "Warning" => TestMessageLevel.Warning,
            _ => TestMessageLevel.Informational,
        };

    public static TimeSpan GetConnectionTimeout()
    {
        // Reuse vstest's connection timeout knob so users can extend it in slow environments.
        string? value = Environment.GetEnvironmentVariable("VSTEST_CONNECTION_TIMEOUT");
        if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int seconds) && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.FromSeconds(90);
    }

    /// <summary>
    /// Runs a single MTP discovery pass against <paramref name="source"/> and returns the discovered tests.
    /// Shared by the discovery proxy (which forwards the tests to the discovery handler) and the execution
    /// proxy (which resolves a <c>/TestCaseFilter</c> against the returned set). Discovery is started with no
    /// environment variables so execution-only data-collector profiler variables are never injected.
    /// </summary>
    /// <param name="source">The MTP application to discover.</param>
    /// <param name="logHandler">Receives log messages produced by the MTP application.</param>
    /// <param name="cancellationToken">Cancels the discovery pass.</param>
    public static List<TestCase> DiscoverSourceTests(
        string source,
        Action<TestMessageLevel, string?> logHandler,
        CancellationToken cancellationToken)
    {
        var discovered = new List<TestCase>();
        using var completed = new ManualResetEventSlim(false);

        using var connection = new MtpServerConnection();
        connection.LogReceived += (level, message) => logHandler(MapLevel(level), message);
        connection.TestNodesUpdated += parameters =>
        {
            if (IsCompletionSentinel(parameters))
            {
                completed.Set();
                return;
            }

            foreach (JsonObject node in EnumerateNodes(parameters))
            {
                if (MtpTestNodeConverter.IsActionNode(node))
                {
                    lock (discovered)
                    {
                        discovered.Add(MtpTestNodeConverter.ToTestCase(node, source));
                    }
                }
            }
        };

        connection.Start(source, environmentVariables: null, GetConnectionTimeout());
        connection.InvokeAsync(MtpConstants.InitializeMethod, InitializeParameters(), cancellationToken).GetAwaiter().GetResult();

        var runId = Guid.NewGuid();
        var discoverTask = connection.InvokeAsync(
            MtpConstants.DiscoverTestsMethod,
            new Dictionary<string, object?> { [MtpConstants.RunIdParameter] = runId.ToString() },
            cancellationToken);

        // The DiscoverTests response indicates the server finished discovery. Because messages arrive on a
        // single ordered stream that we read sequentially, every node notification sent before the response
        // has already been dispatched, so 'discovered' is complete once the response returns. Wait briefly
        // for the trailing completion sentinel (honoring cancellation) purely to drain it; not observing it
        // does not invalidate the discovered set.
        discoverTask.GetAwaiter().GetResult();
        if (!completed.Wait(TimeSpan.FromSeconds(3), cancellationToken))
        {
            EqtTrace.Warning(
                "MtpClientHelpers.DiscoverSourceTests: discovery for '{0}' did not signal the completion sentinel within the drain window; results reflect the nodes received so far.",
                source);
        }

        connection.SendNotification(MtpConstants.ExitMethod, null);

        lock (discovered)
        {
            return discovered.ToList();
        }
    }

    private static int GetCurrentProcessId()
    {
        using var process = Process.GetCurrentProcess();
        return process.Id;
    }

    /// <summary>
    /// Returns true when a <c>testing/testUpdates/tests</c> notification is the completion sentinel
    /// (its <c>changes</c> array is <c>null</c> or absent).
    /// </summary>
    public static bool IsCompletionSentinel(object? parameters)
    {
        JsonObject? node = MtpJson.AsObject(parameters);
        return node is null
            || !node.TryGetValue(MtpConstants.ChangesProperty, out object? changes)
            || changes is null;
    }

    /// <summary>
    /// Enumerates the <c>node</c> objects carried by a <c>testing/testUpdates/tests</c> notification.
    /// </summary>
    public static IEnumerable<JsonObject> EnumerateNodes(object? parameters)
    {
        if (MtpJson.AsObject(parameters) is not JsonObject node
            || !node.TryGetValue(MtpConstants.ChangesProperty, out object? changesValue)
            || changesValue is not JsonArray changes)
        {
            yield break;
        }

        foreach (object? changeObject in changes)
        {
            if (changeObject is JsonObject change
                && change.TryGetValue(MtpConstants.NodeProperty, out object? nodeValue)
                && nodeValue is JsonObject testNode)
            {
                yield return testNode;
            }
        }
    }
}
