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
        Assert.ThrowsException<IOException>(() => WriteData(_tcpClient));
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

        waitEvent.Wait();
        Assert.ThrowsException<CommunicationException>(() => channel!.Send(Dummydata));
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
        await _tcpClient.ConnectAsync(IPAddress.Loopback, port);
    }
}

[TestClass]
public class SocketServerLifecycleTests
{
    private const int Timeout = 10000;
    private const string Endpoint = "127.0.0.1:0";

    [TestMethod]
    public async Task StopBeforeClientConnectsShouldCloseListener()
    {
        var server = new SocketServer();
        var connected = 0;
        server.Connected += (_, _) => Interlocked.Increment(ref connected);
        try
        {
            var endpoint = server.Start(Endpoint).GetIpEndPoint();

            server.Stop();
            server.Stop();

            await AssertListenerClosed(endpoint);
            Assert.AreEqual(0, Volatile.Read(ref connected));
        }
        finally
        {
            server.Stop();
        }
    }

    [TestMethod]
    public void StartAfterStopShouldThrow()
    {
        var server = new SocketServer();
        server.Stop();

        Assert.ThrowsException<ObjectDisposedException>(() => server.Start(Endpoint));
    }

    [TestMethod]
    public async Task AcceptedClientDeliveredAfterStopShouldCloseWithoutCreatingChannel()
    {
        var connections = await CreateConnectedTcpClients();
        using var client = connections.Client;
        using var acceptedClient = connections.AcceptedClient;
        var accept = new TaskCompletionSource<TcpClient>();
        var channel = new Mock<ICommunicationChannel>();
        var channelCreated = 0;
        var connected = 0;
        var server = new SocketServer(
            _ =>
            {
                Interlocked.Increment(ref channelCreated);
                return channel.Object;
            },
            _ => accept.Task);
        server.Connected += (_, _) => Interlocked.Increment(ref connected);
        try
        {
            var endpoint = server.Start(Endpoint).GetIpEndPoint();
            server.Stop();
            await AssertListenerClosed(endpoint);

            accept.SetResult(acceptedClient);

            AssertPeerClosed(client);
            Assert.AreEqual(0, Volatile.Read(ref channelCreated));
            Assert.AreEqual(0, Volatile.Read(ref connected));
            channel.Verify(c => c.Dispose(), Times.Never);
        }
        finally
        {
            server.Stop();
            accept.TrySetResult(acceptedClient);
        }
    }

    [TestMethod]
    public async Task StopShouldNotWaitForConnectedHandlerWhenAcceptCompletesSynchronously()
    {
        var channel = new Mock<ICommunicationChannel>();
        var connections = await CreateConnectedTcpClients();
        using var client = connections.Client;
        using var acceptedClient = connections.AcceptedClient;
        IPEndPoint? endpoint = null;
        var server = new SocketServer(_ => channel.Object, listener =>
        {
            endpoint = (IPEndPoint)listener.LocalEndpoint;
            return Task.FromResult(acceptedClient);
        });
        using var handlerEntered = new ManualResetEventSlim();
        using var handlerExited = new ManualResetEventSlim();
        using var releaseHandler = new ManualResetEventSlim();
        using var disconnected = new ManualResetEventSlim();
        server.Connected += (_, _) =>
        {
            handlerEntered.Set();
            releaseHandler.Wait(Timeout * 2);
            handlerExited.Set();
        };
        server.Disconnected += (_, _) => disconnected.Set();
        try
        {
            var start = Task.Run(() => server.Start(Endpoint));
            try
            {
                Assert.IsTrue(handlerEntered.Wait(Timeout));
                var stop = Task.Run(server.Stop);
                Assert.IsTrue(stop.Wait(Timeout), "Stop waited for the Connected callback.");
                await stop;
                await AssertListenerClosed(endpoint!);
            }
            finally
            {
                releaseHandler.Set();
            }

            Assert.IsTrue(handlerExited.Wait(Timeout));
            Assert.IsTrue(start.Wait(Timeout));
            await start;
            Assert.IsTrue(disconnected.Wait(Timeout));
            AssertPeerClosed(client);
            channel.Verify(c => c.Dispose(), Times.Once);
        }
        finally
        {
            releaseHandler.Set();
            server.Stop();
        }
    }

    [TestMethod]
    public async Task ConnectedHandlerShouldBeAbleToStopServer()
    {
        var channel = new Mock<ICommunicationChannel>();
        var connections = await CreateConnectedTcpClients();
        using var client = connections.Client;
        using var acceptedClient = connections.AcceptedClient;
        var server = new SocketServer(_ => channel.Object, _ => Task.FromResult(acceptedClient));
        using var disconnected = new ManualResetEventSlim();
        var connected = 0;
        server.Connected += (_, _) =>
        {
            Interlocked.Increment(ref connected);
            server.Stop();
        };
        server.Disconnected += (_, _) => disconnected.Set();
        try
        {
            var start = Task.Run(() => server.Start(Endpoint));
            Assert.IsTrue(start.Wait(Timeout));
            var endpoint = (await start).GetIpEndPoint();

            Assert.AreEqual(1, Volatile.Read(ref connected));
            Assert.IsTrue(disconnected.Wait(Timeout));
            AssertPeerClosed(client);
            await AssertListenerClosed(endpoint);
            server.Stop();
            channel.Verify(c => c.Dispose(), Times.Once);
        }
        finally
        {
            server.Stop();
        }
    }

    [TestMethod]
    public async Task ChannelFactoryThrowingDuringStopShouldCloseClientAndListener()
    {
        var connections = await CreateConnectedTcpClients();
        using var client = connections.Client;
        using var acceptedClient = connections.AcceptedClient;
        IPEndPoint? endpoint = null;
        SocketServer? server = null;
        server = new SocketServer(
            _ =>
            {
                server!.Stop();
                throw new InvalidOperationException();
            },
            listener =>
            {
                endpoint = (IPEndPoint)listener.LocalEndpoint;
                return Task.FromResult(acceptedClient);
            });
        var connected = 0;
        server.Connected += (_, _) => Interlocked.Increment(ref connected);
        try
        {
            server.Start(Endpoint);

            AssertPeerClosed(client);
            await AssertListenerClosed(endpoint!);
            Assert.AreEqual(0, Volatile.Read(ref connected));
        }
        finally
        {
            server.Stop();
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ChannelFactoryReturningAfterStopShouldNotPublishConnection(bool stopInsideFactory)
    {
        var channel = new Mock<ICommunicationChannel>();
        var connections = await CreateConnectedTcpClients();
        using var client = connections.Client;
        using var acceptedClient = connections.AcceptedClient;
        using var factoryEntered = new ManualResetEventSlim();
        using var releaseFactory = new ManualResetEventSlim();
        var stopCompletedDuringDisposal = false;
        SocketServer? server = null;
        server = new SocketServer(
            _ =>
            {
                factoryEntered.Set();
                if (stopInsideFactory)
                {
                    server!.Stop();
                }
                else
                {
                    releaseFactory.Wait(Timeout * 2);
                }

                return channel.Object;
            },
            _ => Task.FromResult(acceptedClient));
        channel.Setup(c => c.Dispose()).Callback(() =>
        {
            var stop = Task.Run(server.Stop);
            stopCompletedDuringDisposal = stop.Wait(Timeout);
        });
        var connected = 0;
        var disconnected = 0;
        server.Connected += (_, _) => Interlocked.Increment(ref connected);
        server.Disconnected += (_, _) => Interlocked.Increment(ref disconnected);
        var start = Task.Run(() => server.Start(Endpoint));
        try
        {
            Assert.IsTrue(factoryEntered.Wait(Timeout));
            if (!stopInsideFactory)
            {
                server.Stop();
            }

            releaseFactory.Set();
            Assert.IsTrue(start.Wait(Timeout));
            var endpoint = (await start).GetIpEndPoint();

            AssertPeerClosed(client);
            await AssertListenerClosed(endpoint);
            Assert.AreEqual(0, Volatile.Read(ref connected), "A channel returned after Stop must not be published.");
            Assert.AreEqual(0, Volatile.Read(ref disconnected), "An unpublished channel must not enter the message loop.");
            Assert.IsTrue(stopCompletedDuringDisposal, "Channel disposal must run outside the state lock.");
            channel.Verify(c => c.NotifyDataAvailable(It.IsAny<CancellationToken>()), Times.Never);
            channel.Verify(c => c.Dispose(), Times.Once);
            server.Stop();
            channel.Verify(c => c.Dispose(), Times.Once);
        }
        finally
        {
            releaseFactory.Set();
            server.Stop();
            Assert.IsTrue(start.Wait(Timeout));
        }
    }

    private static void AssertPeerClosed(TcpClient client)
    {
        Assert.IsTrue(client.Client.Poll(Timeout * 1000, SelectMode.SelectRead));
        Assert.AreEqual(0, client.Client.Available);
    }

    private static async Task AssertListenerClosed(IPEndPoint endpoint)
    {
        using var client = new TcpClient();
        var refused = false;
        try
        {
            await client.ConnectAsync(endpoint.Address, endpoint.Port);
        }
        catch (SocketException)
        {
            refused = true;
        }

        Assert.IsTrue(refused, "The stopped listener accepted another connection.");
    }

    private static async Task<(TcpClient Client, TcpClient AcceptedClient)> CreateConnectedTcpClients()
    {
        var client = new TcpClient();
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var accept = listener.AcceptTcpClientAsync();
            await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
            return (client, await accept);
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
