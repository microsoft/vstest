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

namespace Microsoft.TestPlatform.CommunicationUtilities.PlatformTests;

[TestClass]
[Ignore("Flaky")]
public class SocketServerTests : SocketTestsBase, IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly string _defaultConnection = IPAddress.Loopback.ToString() + ":0";
    private readonly ICommunicationEndPoint _socketServer;

    public TestContext TestContext { get; set; }

    public SocketServerTests()
    {
        _socketServer = new SocketServer();

        _tcpClient = new TcpClient();
    }

    protected override TcpClient Client => _tcpClient;

    public void Dispose()
    {
        _socketServer.Stop();
        // tcpClient.Close() calls tcpClient.Dispose().
        _tcpClient?.Close();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task SocketServerStartShouldHostServer()
    {
        var connectionInfo = _socketServer.Start(_defaultConnection);

        Assert.IsFalse(string.IsNullOrEmpty(connectionInfo));
        await ConnectToServer(connectionInfo.GetIpEndPoint().Port);
        Assert.IsTrue(_tcpClient.Connected);
    }

    [TestMethod]
    public void SocketServerStopShouldStopListening()
    {
        var connectionInfo = _socketServer.Start(_defaultConnection);

        _socketServer.Stop();

        try
        {
            // This method throws ExtendedSocketException (which is private). It is not possible
            // to use Assert.ThrowsException in this case.
            ConnectToServer(connectionInfo.GetIpEndPoint().Port).GetAwaiter().GetResult();
        }
        catch (SocketException)
        {
        }
    }

    [TestMethod]
    public void SocketServerStopShouldCloseClient()
    {
        ManualResetEvent waitEvent = new(false);
        _socketServer.Disconnected += (s, e) => waitEvent.Set();
        SetupChannel(out ConnectedEventArgs? clientConnected);

        _socketServer.Stop();

        waitEvent.WaitOne();
        Assert.ThrowsExactly<IOException>(() => WriteData(_tcpClient));
    }

    [TestMethod]
    public void SocketServerStopShouldRaiseClientDisconnectedEventOnClientDisconnection()
    {
        DisconnectedEventArgs? disconnected = null;
        ManualResetEvent waitEvent = new(false);
        _socketServer.Disconnected += (s, e) =>
        {
            disconnected = e;
            waitEvent.Set();
        };
        SetupChannel(out ConnectedEventArgs? clientConnected);

        _socketServer.Stop();

        waitEvent.WaitOne();
        Assert.IsNotNull(disconnected);
        Assert.IsNull(disconnected.Error);
    }

    [TestMethod]
    public void SocketServerStopShouldCloseChannel()
    {
        var waitEvent = new ManualResetEventSlim(false);
        var channel = SetupChannel(out ConnectedEventArgs? clientConnected);
        _socketServer.Disconnected += (s, e) => waitEvent.Set();

        _socketServer.Stop();

        waitEvent.Wait(TestContext.CancellationToken);
        Assert.ThrowsExactly<CommunicationException>(() => channel!.Send(Dummydata));
    }

    [TestMethod]
    public void SocketServerShouldRaiseClientDisconnectedEventIfConnectionIsBroken()
    {
        DisconnectedEventArgs? clientDisconnected = null;
        ManualResetEvent waitEvent = new(false);
        _socketServer.Disconnected += (sender, eventArgs) =>
        {
            clientDisconnected = eventArgs;
            waitEvent.Set();
        };
        var channel = SetupChannel(out ConnectedEventArgs? clientConnected);

        channel!.MessageReceived.Subscribe((sender, args) =>
        {
        });

        // Close the client channel. Message loop should stop.
        // tcpClient.Close() calls tcpClient.Dispose().
        _tcpClient?.Close();

        Assert.IsTrue(waitEvent.WaitOne(1000));
        Assert.IsTrue(clientDisconnected!.Error is IOException);
    }

    [TestMethod]
    public async Task SocketEndpointShouldInitializeChannelOnServerConnection()
    {
        var channel = SetupChannel(out ConnectedEventArgs? _);

        await channel!.Send(Dummydata);

        Assert.AreEqual(Dummydata, ReadData(Client));
    }

    protected override ICommunicationChannel? SetupChannel(out ConnectedEventArgs? connectedEvent)
    {
        ICommunicationChannel? channel = null;
        ConnectedEventArgs? clientConnectedEvent = null;
        ManualResetEvent waitEvent = new(false);
        _socketServer.Connected += (sender, eventArgs) =>
        {
            clientConnectedEvent = eventArgs;
            channel = eventArgs.Channel;
            waitEvent.Set();
        };

        var connectionInfo = _socketServer.Start(_defaultConnection);
        var port = connectionInfo.GetIpEndPoint().Port;
        ConnectToServer(port).GetAwaiter().GetResult();
        waitEvent.WaitOne();

        connectedEvent = clientConnectedEvent;
        return channel;
    }

    private async Task ConnectToServer(int port)
    {
#pragma warning disable MSTEST0049 // Use 'TestContext.CancellationToken' - ConnectAsync CancellationToken overload unavailable on .NET Framework
        await _tcpClient.ConnectAsync(IPAddress.Loopback, port);
#pragma warning restore MSTEST0049
    }
}

[TestClass]
public class SocketServerLifecycleTests
{
    private const int Timeout = 10000;

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task SocketServerShouldAcceptTcpClient()
    {
        var socketServer = new SocketServer();
        using var client = new TcpClient();
        try
        {
            var connected = new TaskCompletionSource<ConnectedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            socketServer.Connected += (_, args) => connected.TrySetResult(args);
            var endpoint = socketServer.Start(IPAddress.Loopback + ":0").GetIpEndPoint();
#pragma warning disable MSTEST0049 // ConnectAsync CancellationToken overload is unavailable on .NET Framework.
            await client.ConnectAsync(endpoint.Address, endpoint.Port);
#pragma warning restore MSTEST0049
            Assert.IsTrue(connected.Task.Wait(Timeout, TestContext.CancellationToken));
            var result = await connected.Task;
            Assert.IsTrue(result.Connected);
            Assert.IsNotNull(result.Channel);
        }
        finally
        {
            socketServer.Stop();
        }
    }

    [TestMethod]
    public async Task SocketServerStopBeforeClientConnectsShouldCloseListener()
    {
        var socketServer = new SocketServer();
        var connected = false;
        socketServer.Connected += (_, _) => connected = true;
        var endpoint = socketServer.Start(IPAddress.Loopback + ":0").GetIpEndPoint();
        try
        {
            socketServer.Stop();
            socketServer.Stop();

            await AssertListenerClosed(endpoint);
            Assert.IsFalse(connected);
        }
        finally
        {
            socketServer.Stop();
        }
    }

    [TestMethod]
    public void SocketServerStartShouldThrowAfterStop()
    {
        var socketServer = new SocketServer();
        socketServer.Stop();

        Assert.ThrowsExactly<ObjectDisposedException>(() => socketServer.Start(IPAddress.Loopback + ":0"));
    }

    [TestMethod]
    public async Task SocketServerShouldCloseAcceptedClientDeliveredAfterStop()
    {
        var connectedClients = await CreateConnectedTcpClients();
        using var client = connectedClients.Client;
        using var acceptedClient = connectedClients.AcceptedClient;
        var accept = new TaskCompletionSource<TcpClient>(TaskCreationOptions.RunContinuationsAsynchronously);
        var channel = new Mock<ICommunicationChannel>();
        var channelFactory = new Mock<Func<Stream, ICommunicationChannel>>(MockBehavior.Strict);
        channelFactory.Setup(factory => factory(It.IsAny<Stream>())).Returns(channel.Object);
        var socketServer = new SocketServer(channelFactory.Object, _ => accept.Task);
        var connected = false;
        socketServer.Connected += (_, _) => connected = true;
        var endpoint = socketServer.Start(IPAddress.Loopback + ":0").GetIpEndPoint();
        try
        {
            socketServer.Stop();
            await AssertListenerClosed(endpoint);

            accept.SetResult(acceptedClient);

            Assert.IsTrue(client.Client.Poll(Timeout * 1000, SelectMode.SelectRead));
            Assert.AreEqual(0, client.Client.Available);
            channelFactory.Verify(factory => factory(It.IsAny<Stream>()), Times.Never);
            channel.Verify(value => value.Dispose(), Times.Never);
            Assert.IsFalse(connected);
        }
        finally
        {
            accept.TrySetResult(acceptedClient);
            socketServer.Stop();
        }
    }

    [TestMethod]
    public async Task SocketServerStopShouldNotWaitForConnectedHandlerWhenAcceptCompletesSynchronously()
    {
        var channel = new Mock<ICommunicationChannel>();
        var connectedClients = await CreateConnectedTcpClients();
        using var client = connectedClients.Client;
        using var acceptedClient = connectedClients.AcceptedClient;
        using var handlerEntered = new ManualResetEventSlim(false);
        using var releaseHandler = new ManualResetEventSlim(false);
        using var disconnected = new ManualResetEventSlim(false);
        var socketServer = new SocketServer(_ => channel.Object, _ => Task.FromResult(acceptedClient));
        socketServer.Connected += (_, _) =>
        {
            handlerEntered.Set();
            releaseHandler.Wait(Timeout * 2, TestContext.CancellationToken);
        };
        socketServer.Disconnected += (_, _) => disconnected.Set();
        try
        {
            var startTask = Task.Run(() => socketServer.Start(IPAddress.Loopback + ":0"), TestContext.CancellationToken);
            try
            {
                Assert.IsTrue(handlerEntered.Wait(Timeout, TestContext.CancellationToken));
                var stopTask = Task.Run(socketServer.Stop, TestContext.CancellationToken);
                Assert.IsTrue(stopTask.Wait(Timeout, TestContext.CancellationToken));
                await stopTask;
            }
            finally
            {
                releaseHandler.Set();
            }

            Assert.IsTrue(startTask.Wait(Timeout, TestContext.CancellationToken));
            var endpoint = (await startTask).GetIpEndPoint();
            await AssertListenerClosed(endpoint);
            Assert.IsTrue(disconnected.Wait(Timeout, TestContext.CancellationToken));
            Assert.IsTrue(client.Client.Poll(Timeout * 1000, SelectMode.SelectRead));
            Assert.AreEqual(0, client.Client.Available);
            channel.Verify(value => value.Dispose(), Times.Once);
        }
        finally
        {
            releaseHandler.Set();
            socketServer.Stop();
        }
    }

    [TestMethod]
    public async Task SocketServerShouldCloseAcceptedClientWhenChannelFactoryThrowsDuringStop()
    {
        var connectedClients = await CreateConnectedTcpClients();
        using var client = connectedClients.Client;
        using var acceptedClient = connectedClients.AcceptedClient;
        var connected = false;
        SocketServer? socketServer = null;
        socketServer = new SocketServer(
            _ =>
            {
                socketServer!.Stop();
                throw new InvalidOperationException();
            },
            _ => Task.FromResult(acceptedClient));
        socketServer.Connected += (_, _) => connected = true;
        try
        {
            var endpoint = socketServer.Start(IPAddress.Loopback + ":0").GetIpEndPoint();

            await AssertListenerClosed(endpoint);
            Assert.IsTrue(client.Client.Poll(Timeout * 1000, SelectMode.SelectRead));
            Assert.AreEqual(0, client.Client.Available);
            Assert.IsFalse(connected);
        }
        finally
        {
            socketServer.Stop();
        }
    }

    [TestMethod]
    public async Task SocketServerShouldNotPublishChannelWhenStoppedDuringChannelInitialization()
    {
        await AssertChannelReturnedAfterStopIsNotPublished(stopInsideFactory: true);
    }

    [TestMethod]
    public async Task SocketServerShouldNotPublishChannelWhenStopCompletesBeforeChannelFactoryReturns()
    {
        await AssertChannelReturnedAfterStopIsNotPublished(stopInsideFactory: false);
    }

    private async Task AssertChannelReturnedAfterStopIsNotPublished(bool stopInsideFactory)
    {
        var channel = new Mock<ICommunicationChannel>();
        var connectedClients = await CreateConnectedTcpClients();
        using var client = connectedClients.Client;
        using var acceptedClient = connectedClients.AcceptedClient;
        using var factoryEntered = new ManualResetEventSlim(false);
        using var releaseFactory = new ManualResetEventSlim(false);
        var connectedCount = 0;
        var disconnectedCount = 0;
        var stopCompletedDuringDisposal = false;
        SocketServer? socketServer = null;
        socketServer = new SocketServer(
            _ =>
            {
                factoryEntered.Set();
                if (stopInsideFactory)
                {
                    socketServer!.Stop();
                }
                else
                {
                    Assert.IsTrue(releaseFactory.Wait(Timeout * 2, TestContext.CancellationToken));
                }

                return channel.Object;
            },
            _ => Task.FromResult(acceptedClient));
        channel.Setup(value => value.Dispose()).Callback(() =>
        {
            var stopTask = Task.Run(socketServer.Stop, TestContext.CancellationToken);
            stopCompletedDuringDisposal = stopTask.Wait(Timeout, TestContext.CancellationToken);
        });
        socketServer.Connected += (_, _) => Interlocked.Increment(ref connectedCount);
        socketServer.Disconnected += (_, _) => Interlocked.Increment(ref disconnectedCount);
        var startTask = Task.Run(() => socketServer.Start(IPAddress.Loopback + ":0"), TestContext.CancellationToken);
        try
        {
            try
            {
                Assert.IsTrue(factoryEntered.Wait(Timeout, TestContext.CancellationToken));
                if (!stopInsideFactory)
                {
                    var stopTask = Task.Run(socketServer.Stop, TestContext.CancellationToken);
                    Assert.IsTrue(stopTask.Wait(Timeout, TestContext.CancellationToken));
                    await stopTask;
                }
            }
            finally
            {
                releaseFactory.Set();
            }

            Assert.IsTrue(startTask.Wait(Timeout, TestContext.CancellationToken));
            var endpoint = (await startTask).GetIpEndPoint();
            await AssertListenerClosed(endpoint);
            Assert.AreEqual(0, connectedCount, "A channel returned after Stop must not be published.");
            Assert.AreEqual(0, disconnectedCount, "An unpublished channel must not enter the message loop.");
            channel.Verify(value => value.Dispose(), Times.Once);
            Assert.IsTrue(stopCompletedDuringDisposal, "Channel disposal must run outside the state lock.");
            channel.Verify(value => value.NotifyDataAvailable(It.IsAny<CancellationToken>()), Times.Never);
            Assert.IsTrue(client.Client.Poll(Timeout * 1000, SelectMode.SelectRead));
            Assert.AreEqual(0, client.Client.Available);
            socketServer.Stop();
            channel.Verify(value => value.Dispose(), Times.Once);
        }
        finally
        {
            releaseFactory.Set();
            socketServer.Stop();
            Assert.IsTrue(startTask.Wait(Timeout, TestContext.CancellationToken));
        }
    }

    private static async Task AssertListenerClosed(IPEndPoint endpoint)
    {
        using var client = new TcpClient();
#pragma warning disable MSTEST0049 // ConnectAsync CancellationToken overload is unavailable on .NET Framework.
        await Assert.ThrowsExactlyAsync<SocketException>(() => client.ConnectAsync(endpoint.Address, endpoint.Port));
#pragma warning restore MSTEST0049
    }

    private static async Task<(TcpClient Client, TcpClient AcceptedClient)> CreateConnectedTcpClients()
    {
        var client = new TcpClient();
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
#pragma warning disable MSTEST0049 // ConnectAsync and AcceptTcpClientAsync CancellationToken overloads are unavailable on .NET Framework.
            var acceptTask = listener.AcceptTcpClientAsync();
            await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
            var acceptedClient = await acceptTask;
#pragma warning restore MSTEST0049
            return (client, acceptedClient);
        }
        catch
        {
            client.Close();
            throw;
        }
        finally
        {
            listener.Stop();
        }
    }
}
