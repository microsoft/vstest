// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.Testing.Platform.ServerMode.Client;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.UnitTests.Client.MTP;

/// <summary>
/// Regression tests for the MTP client shutdown path.
///
/// Before the retarget, exit was a fire-and-forget notification with no cancellation token. It is
/// now an awaited request/response call, which introduced two failure modes these tests pin:
/// passing the run's own (possibly already-cancelled) token would skip the handshake exactly when a
/// run is aborted, and an unbounded await would let a wedged test application hang the run.
/// </summary>
[TestClass]
public class MtpServerClientFactoryTests
{
    [TestMethod]
    public void TryExitAsksTheServerToExit()
    {
        var client = new FakeMtpServerClient();

        MtpServerClientFactory.TryExit(client);

        Assert.IsTrue(client.ExitCalled);
    }

    /// <summary>
    /// Cancelling or aborting a run is precisely when the run's token is already cancelled. Exit
    /// must not be tied to it, or the graceful shutdown handshake would be skipped in the one case
    /// it matters most.
    /// </summary>
    [TestMethod]
    public void TryExitDoesNotUseAnAlreadyCancelledRunToken()
    {
        var client = new FakeMtpServerClient();

        using var cancelledRun = new CancellationTokenSource();
        cancelledRun.Cancel();

        MtpServerClientFactory.TryExit(client);

        Assert.IsTrue(client.ExitCalled, "Exit must still be attempted after a cancelled run.");
        Assert.IsFalse(
            client.ExitToken.IsCancellationRequested,
            "Exit must not be driven by the run's cancellation token.");
    }

    /// <summary>
    /// A test application that never acknowledges exit must not hang the run. Disposal (which the
    /// caller performs afterwards) terminates the process regardless, so abandoning the handshake is
    /// safe.
    /// </summary>
    [TestMethod]
    public void TryExitGivesUpOnAnUnresponsiveServer()
    {
        var client = new FakeMtpServerClient { ExitDelay = TimeSpan.FromMinutes(5) };

        var stopwatch = Stopwatch.StartNew();
        MtpServerClientFactory.TryExit(client);
        stopwatch.Stop();

        Assert.IsLessThan(TimeSpan.FromMinutes(1), stopwatch.Elapsed, "TryExit must be bounded by its own timeout.");
    }

    [TestMethod]
    public void TryExitSwallowsServerFailures()
    {
        var client = new FakeMtpServerClient { ThrowFromExit = new InvalidOperationException("server refused to exit") };

        MtpServerClientFactory.TryExit(client);

        Assert.IsTrue(client.ExitCalled, "A failing exit must not propagate: the caller disposes the client next.");
    }

    [TestMethod]
    public void LaunchDefaultsToTheRealClientLauncher()
        => Assert.IsNotNull(MtpServerClientFactory.Launch);
}
