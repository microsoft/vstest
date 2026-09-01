// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Testing.Platform.ServerMode.Client;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.UnitTests.Client.MTP;

/// <summary>
/// Tests that drive <see cref="MtpProxyDiscoveryManager"/> end to end against a fake MTP server,
/// so the launch/initialize/discover/exit sequence is asserted without starting a real test
/// application.
/// </summary>
/// <remarks>
/// Not parallelized: these tests swap the process-wide <see cref="MtpServerClientFactory.Launch"/>
/// seam, so running them alongside another class that does the same would let one class's fake leak
/// into the other's run.
/// </remarks>
[TestClass]
[DoNotParallelize]
public class MtpProxyDiscoveryManagerTests
{
    private const int ProtocolVersion = 7;
    private const string Source = @"C:\tests\MtpApp.dll";

    private Func<string, MtpServerClientOptions, IMtpServerClient>? _originalLaunch;
    private FakeMtpServerClient _client = null!;
    private Mock<ITestDiscoveryEventsHandler2> _eventHandler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _originalLaunch = MtpServerClientFactory.Launch;
        _client = new FakeMtpServerClient();
        MtpServerClientFactory.Launch = (_, _) => _client;
        _eventHandler = new Mock<ITestDiscoveryEventsHandler2>();
    }

    [TestCleanup]
    public void Cleanup()
        => MtpServerClientFactory.Launch = _originalLaunch!;

    private static DiscoveryCriteria Criteria()
        => new([Source], 1, "<RunSettings></RunSettings>");

    private static MtpTestNodeUpdate ActionNode(string uid, string displayName)
        => new(
            new Dictionary<string, object?>
            {
                ["uid"] = uid,
                ["display-name"] = displayName,
                ["node-type"] = "action",
            },
            parentUid: null);

    private static MtpTestNodeUpdate ActionNodeWithoutUid(string displayName)
        => new(
            new Dictionary<string, object?>
            {
                ["display-name"] = displayName,
                ["node-type"] = "action",
            },
            parentUid: null);

    [TestMethod]
    public void DiscoverTestsReportsDiscoveredActionNodes()
    {
        _client.NodesToPush = [ActionNode("uid-1", "TestOne"), ActionNode("uid-2", "TestTwo")];

        List<TestCase>? discovered = null;
        DiscoveryCompleteEventArgs? discoveryComplete = null;
        var rawMessages = new List<string>();
        _eventHandler
            .Setup(h => h.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()))
            .Callback<IEnumerable<TestCase>>(tests => discovered = [.. tests]);
        _eventHandler
            .Setup(h => h.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
            .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>?>((args, _) => discoveryComplete = args);
        _eventHandler
            .Setup(h => h.HandleRawMessage(It.IsAny<string>()))
            .Callback<string>(rawMessages.Add);

        using var manager = new MtpProxyDiscoveryManager(ProtocolVersion);
        manager.DiscoverTests(Criteria(), _eventHandler.Object);

        Assert.IsNotNull(discovered);
        Assert.HasCount(2, discovered);
        Assert.IsNotNull(discoveryComplete);
        Assert.AreEqual(2, discoveryComplete.TotalCount);
        Assert.IsFalse(discoveryComplete.IsAborted);
        Assert.HasCount(1, discoveryComplete.FullyDiscoveredSources!);
        Assert.AreEqual(Source, discoveryComplete.FullyDiscoveredSources![0]);
        Assert.IsEmpty(discoveryComplete.PartiallyDiscoveredSources!);
        Assert.IsEmpty(discoveryComplete.NotDiscoveredSources!);

        Assert.HasCount(2, rawMessages);
        var discoveredMessage = JsonDataSerializer.Instance.DeserializeMessage(rawMessages[0]);
        Assert.AreEqual(ProtocolVersion, discoveredMessage.Version);
        Assert.AreEqual(MessageType.TestCasesFound, discoveredMessage.MessageType);
        var rawDiscovered = JsonDataSerializer.Instance.DeserializePayload<IEnumerable<TestCase>>(discoveredMessage);
        Assert.HasCount(2, rawDiscovered!.ToList());

        var completeMessage = JsonDataSerializer.Instance.DeserializeMessage(rawMessages[1]);
        Assert.AreEqual(ProtocolVersion, completeMessage.Version);
        Assert.AreEqual(MessageType.DiscoveryComplete, completeMessage.MessageType);
        var rawComplete = JsonDataSerializer.Instance.DeserializePayload<DiscoveryCompletePayload>(completeMessage);
        Assert.IsNotNull(rawComplete);
        Assert.AreEqual(2, rawComplete.TotalTests);
        Assert.HasCount(1, rawComplete.FullyDiscoveredSources!);
        Assert.AreEqual(Source, rawComplete.FullyDiscoveredSources![0]);
    }

    [TestMethod]
    public void DiscoverTestsAsksTheServerToExit()
    {
        using var manager = new MtpProxyDiscoveryManager(ProtocolVersion);
        manager.DiscoverTests(Criteria(), _eventHandler.Object);

        Assert.IsTrue(_client.ExitCalled);
        Assert.IsTrue(_client.Disposed);
    }

    [TestMethod]
    public void SelectedRunFailsWhenDiscoveredActionNodeHasNoUid()
    {
        _client.NodesToPush = [ActionNodeWithoutUid("MissingUid")];

        List<TestCase>? discovered = null;
        _eventHandler
            .Setup(h => h.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()))
            .Callback<IEnumerable<TestCase>>(tests => discovered = [.. tests]);

        using (var discoveryManager = new MtpProxyDiscoveryManager(ProtocolVersion))
        {
            discoveryManager.DiscoverTests(Criteria(), _eventHandler.Object);
        }

        Assert.IsNotNull(discovered);
        Assert.HasCount(1, discovered);

        var runClient = new FakeMtpServerClient();
        MtpServerClientFactory.Launch = (_, _) => runClient;
        var runEventHandler = new Mock<IInternalTestRunEventsHandler>();

        using var executionManager = new MtpProxyExecutionManager(ProtocolVersion);
        executionManager.StartTestRun(new TestRunCriteria(discovered, 1), runEventHandler.Object);

        Assert.IsNull(runClient.RunFilterUids, "No run may be requested for a node the server did not identify.");
        runEventHandler.Verify(
            h => h.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Cancelling a run cancels the token the in-flight request is riding on. Before this fix the
    /// manager awaited exit on that same token, so the graceful shutdown was skipped in exactly the
    /// case it matters most. Exit now runs on its own bounded token from a finally block.
    /// </summary>
    [TestMethod]
    public void DiscoverTestsStillExitsWhenDiscoveryIsCancelled()
    {
        _client.ThrowFromRequest = new OperationCanceledException();

        using var manager = new MtpProxyDiscoveryManager(ProtocolVersion);
        manager.DiscoverTests(Criteria(), _eventHandler.Object);

        Assert.IsTrue(_client.ExitCalled, "A cancelled discovery must still shut the test application down.");
        Assert.IsFalse(
            _client.ExitToken.IsCancellationRequested,
            "Exit must not be driven by the cancelled run token, or it would be skipped.");
        Assert.IsTrue(_client.Disposed);
    }

    /// <summary>
    /// A failure part-way through discovery must not leak the launched test application.
    /// </summary>
    [TestMethod]
    public void DiscoverTestsExitsWhenDiscoveryFails()
    {
        _client.ThrowFromRequest = new InvalidOperationException("server blew up");

        using var manager = new MtpProxyDiscoveryManager(ProtocolVersion);
        manager.DiscoverTests(Criteria(), _eventHandler.Object);

        Assert.IsTrue(_client.ExitCalled, "Exit runs in a finally block, so a failed discovery still shuts down.");
        Assert.IsTrue(_client.Disposed);
        _eventHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public void DiscoverTestsReportsSourceAsNotDiscoveredWhenLaunchFails()
    {
        MtpServerClientFactory.Launch = (_, _) => throw new InvalidOperationException("launch failed");
        DiscoveryCompleteEventArgs? discoveryComplete = null;
        _eventHandler
            .Setup(h => h.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
            .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>?>((args, _) => discoveryComplete = args);

        using var manager = new MtpProxyDiscoveryManager(ProtocolVersion);
        manager.DiscoverTests(Criteria(), _eventHandler.Object);

        Assert.IsNotNull(discoveryComplete);
        Assert.IsEmpty(discoveryComplete.FullyDiscoveredSources!);
        Assert.IsEmpty(discoveryComplete.PartiallyDiscoveredSources!);
        Assert.HasCount(1, discoveryComplete.NotDiscoveredSources!);
        Assert.AreEqual(Source, discoveryComplete.NotDiscoveredSources![0]);
    }

    [TestMethod]
    public void DiscoverTestsReportsSourceAsNotDiscoveredWhenInitializationFails()
    {
        _client.ThrowFromInitialize = new InvalidOperationException("initialization failed");
        DiscoveryCompleteEventArgs? discoveryComplete = null;
        _eventHandler
            .Setup(h => h.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
            .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>?>((args, _) => discoveryComplete = args);

        using var manager = new MtpProxyDiscoveryManager(ProtocolVersion);
        manager.DiscoverTests(Criteria(), _eventHandler.Object);

        Assert.IsNotNull(discoveryComplete);
        Assert.IsEmpty(discoveryComplete.FullyDiscoveredSources!);
        Assert.IsEmpty(discoveryComplete.PartiallyDiscoveredSources!);
        Assert.HasCount(1, discoveryComplete.NotDiscoveredSources!);
        Assert.AreEqual(Source, discoveryComplete.NotDiscoveredSources![0]);
    }
}
