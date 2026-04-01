// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests;

/// <summary>
/// Regression tests for TcpClientExtensions.MessageLoopAsync.
/// Issue #4461 / PR #4493: Socket IOException should NOT be propagated to the error handler
/// when child test process exits unexpectedly.
/// </summary>
[TestClass]
public class ICSRegressionTests
{
    public TestContext TestContext { get; set; }

    private static (TcpClient client, TcpClient serverSide, TcpListener listener) CreateConnectedTcpPair()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var client = new TcpClient();
        client.Connect(IPAddress.Loopback, port);
        var serverSide = listener.AcceptTcpClient();

        return (client, serverSide, listener);
    }

    [TestMethod]
    public async Task MessageLoopAsync_SocketIOExceptionWithTimedOut_ShouldNotSetError()
    {
        // Arrange
        var (client, serverSide, listener) = CreateConnectedTcpPair();

        var channel = new Mock<ICommunicationChannel>();
        channel.Setup(c => c.NotifyDataAvailable(It.IsAny<CancellationToken>()))
            .Throws(new IOException("Timed out", new SocketException((int)SocketError.TimedOut)));

        Exception? capturedError = null;

        // Act: use a CTS that cancels after a short delay to break the while loop.
        // TimedOut IOException is caught and only logged; the loop continues until cancelled.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        await client.MessageLoopAsync(channel.Object, error => capturedError = error, cts.Token);

        // Assert: error should be null because TimedOut IOException is caught and only logged.
        Assert.IsNull(capturedError, "TimedOut IOException should not be propagated to the error handler.");

        // Cleanup
        client.Dispose();
        serverSide.Dispose();
        listener.Stop();
    }

    [TestMethod]
    public async Task MessageLoopAsync_SocketIOExceptionWithConnectionReset_ShouldNotSetError()
    {
        // Arrange
        var (client, serverSide, listener) = CreateConnectedTcpPair();

        var channel = new Mock<ICommunicationChannel>();
        channel.Setup(c => c.NotifyDataAvailable(It.IsAny<CancellationToken>()))
            .Throws(new IOException("Connection reset", new SocketException((int)SocketError.ConnectionReset)));

        Exception? capturedError = null;

        // Act: non-TimedOut IOException with SocketException breaks the loop immediately.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await client.MessageLoopAsync(channel.Object, error => capturedError = error, cts.Token);

        // Assert: IOException with SocketException is NOT propagated (fix for #4461).
        Assert.IsNull(capturedError, "IOException with SocketException should not be propagated to the error handler.");

        // Cleanup
        client.Dispose();
        serverSide.Dispose();
        listener.Stop();
    }

    [TestMethod]
    public async Task MessageLoopAsync_NonSocketException_ShouldSetError()
    {
        // Arrange
        var (client, serverSide, listener) = CreateConnectedTcpPair();

        var channel = new Mock<ICommunicationChannel>();
        var expectedException = new InvalidOperationException("Unexpected error");
        channel.Setup(c => c.NotifyDataAvailable(It.IsAny<CancellationToken>()))
            .Throws(expectedException);

        Exception? capturedError = null;

        // Send data from the server side so Poll returns true and NotifyDataAvailable is called.
        serverSide.GetStream().Write(new byte[] { 1, 2, 3 }, 0, 3);

        // Act
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await client.MessageLoopAsync(channel.Object, error => capturedError = error, cts.Token);

        // Assert: non-IOException exceptions ARE propagated.
        Assert.AreSame(expectedException, capturedError, "Non-IOException should be propagated to the error handler.");

        // Cleanup
        client.Dispose();
        serverSide.Dispose();
        listener.Stop();
    }
}
