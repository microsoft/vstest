// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.PlatformTests
{
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

    [TestClass]
    public class SocketServerTests : SocketTestsBase, IDisposable
    {
        private readonly TcpClient tcpClient;
        private readonly string defaultConnection = IPAddress.Loopback.ToString() + ":0";
        private readonly ICommunicationEndPoint socketServer;

        public SocketServerTests()
        {
            this.socketServer = new SocketServer();

            this.tcpClient = new TcpClient();
        }

        protected override TcpClient Client => this.tcpClient;

        public void Dispose()
        {
            this.socketServer.Stop();
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            this.tcpClient?.Close();
#else
            // tcpClient.Close() not available for netcoreapp1.0
            this.tcpClient?.Dispose();
#endif
            GC.SuppressFinalize(this);
        }

        [TestMethod]
        public async Task SocketServerStartShouldHostServer()
        {
            var connectionInfo = this.socketServer.Start(this.defaultConnection);

            Assert.IsFalse(string.IsNullOrEmpty(connectionInfo));
            await this.ConnectToServer(connectionInfo.GetIPEndPoint().Port);
            Assert.IsTrue(this.tcpClient.Connected);
        }

        [TestMethod]
        public async Task SocketServerStopShouldStopListening()
        {
            var connected = false;
            this.socketServer.Connected += (sender, args) => connected = true;
            var connectionInfo = this.socketServer.Start(this.defaultConnection);

            this.socketServer.Stop();

            await AssertListenerStopped(connectionInfo.GetIPEndPoint());
            Assert.IsFalse(connected);
        }

        [TestMethod]
        public void SocketServerStartShouldThrowAfterStop()
        {
            this.socketServer.Stop();

            Assert.ThrowsException<ObjectDisposedException>(() => this.socketServer.Start(this.defaultConnection));
        }

        [TestMethod]
        public async Task SocketServerShouldCloseClientAcceptedAfterStop()
        {
            var connections = await CreateConnectedTcpClients();
            using (var peer = connections.Item1)
            using (var accepted = connections.Item2)
            {
                var pendingAccept = new TaskCompletionSource<TcpClient>();
                var channel = new Mock<ICommunicationChannel>();
                var factoryCalled = false;
                var connected = false;
                var server = new SocketServer(
                    stream => { factoryCalled = true; return channel.Object; },
                    listener => pendingAccept.Task);
                server.Connected += (sender, args) => connected = true;
                try
                {
                    var endpoint = server.Start(this.defaultConnection).GetIPEndPoint();
                    server.Stop();
                    pendingAccept.SetResult(accepted);

                    Assert.IsTrue(peer.Client.Poll(TIMEOUT * 1000, SelectMode.SelectRead));
                    Assert.AreEqual(0, peer.Client.Available);
                    Assert.IsFalse(factoryCalled);
                    Assert.IsFalse(connected);
                    channel.Verify(value => value.Dispose(), Times.Never);
                    await AssertListenerStopped(endpoint);
                }
                finally
                {
                    server.Stop();
                }
            }
        }

        [TestMethod]
        public async Task SocketServerStopShouldNotWaitForConnectedHandlerWhenAcceptCompletesSynchronously()
        {
            var connections = await CreateConnectedTcpClients();
            using (var peer = connections.Item1)
            using (var accepted = connections.Item2)
            using (var handlerEntered = new ManualResetEventSlim(false))
            using (var handlerExited = new ManualResetEventSlim(false))
            using (var releaseHandler = new ManualResetEventSlim(false))
            using (var disconnected = new ManualResetEventSlim(false))
            {
                var channel = new Mock<ICommunicationChannel>();
                var server = new SocketServer(stream => channel.Object, listener => Task.FromResult(accepted));
                server.Connected += (sender, args) =>
                {
                    handlerEntered.Set();
                    releaseHandler.Wait(TIMEOUT * 2);
                    handlerExited.Set();
                };
                server.Disconnected += (sender, args) => disconnected.Set();
                try
                {
                    Task<string> startTask;
                    try
                    {
                        startTask = Task.Run(() => server.Start(this.defaultConnection));
                        Assert.IsTrue(handlerEntered.Wait(TIMEOUT));
                        Assert.IsTrue(Task.Run(server.Stop).Wait(TIMEOUT));
                    }
                    finally
                    {
                        releaseHandler.Set();
                    }

                    Assert.IsTrue(handlerExited.Wait(TIMEOUT));
                    Assert.IsTrue(startTask.Wait(TIMEOUT));
                    Assert.IsTrue(disconnected.Wait(TIMEOUT));
                    server.Stop();
                    channel.Verify(value => value.Dispose(), Times.Once);
                    await AssertListenerStopped(startTask.Result.GetIPEndPoint());
                }
                finally
                {
                    server.Stop();
                }
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SocketServerShouldNotPublishChannelWhenFactoryReturnsAfterStop(bool stopInsideFactory)
        {
            var connections = await CreateConnectedTcpClients();
            using (var peer = connections.Item1)
            using (var accepted = connections.Item2)
            using (var factoryEntered = new ManualResetEventSlim(false))
            using (var releaseFactory = new ManualResetEventSlim(false))
            {
                var channel = new Mock<ICommunicationChannel>();
                var connectedCount = 0;
                var disconnectedCount = 0;
                var stopCompletedDuringDisposal = false;
                SocketServer server = null;
                server = new SocketServer(
                    stream =>
                    {
                        factoryEntered.Set();
                        if (stopInsideFactory)
                        {
                            server.Stop();
                        }
                        else
                        {
                            releaseFactory.Wait(TIMEOUT * 2);
                        }
                        return channel.Object;
                    },
                    listener => Task.FromResult(accepted));
                channel.Setup(value => value.Dispose()).Callback(() =>
                {
                    stopCompletedDuringDisposal = Task.Run(server.Stop).Wait(TIMEOUT);
                });
                server.Connected += (sender, args) => Interlocked.Increment(ref connectedCount);
                server.Disconnected += (sender, args) => Interlocked.Increment(ref disconnectedCount);
                try
                {
                    Task<string> startTask;
                    try
                    {
                        startTask = Task.Run(() => server.Start(this.defaultConnection));
                        Assert.IsTrue(factoryEntered.Wait(TIMEOUT));
                        if (!stopInsideFactory)
                        {
                            Assert.IsTrue(Task.Run(server.Stop).Wait(TIMEOUT));
                        }
                    }
                    finally
                    {
                        releaseFactory.Set();
                    }

                    Assert.IsTrue(startTask.Wait(TIMEOUT));
                    Assert.AreEqual(0, connectedCount, "A channel returned after Stop must not be published.");
                    Assert.AreEqual(0, disconnectedCount, "An unpublished channel must not enter the message loop.");
                    channel.Verify(value => value.Dispose(), Times.Once);
                    Assert.IsTrue(stopCompletedDuringDisposal, "Channel disposal must run outside the state lock.");
                    channel.Verify(value => value.NotifyDataAvailable(), Times.Never);
                    Assert.IsTrue(peer.Client.Poll(TIMEOUT * 1000, SelectMode.SelectRead));
                    Assert.AreEqual(0, peer.Client.Available);
                    server.Stop();
                    channel.Verify(value => value.Dispose(), Times.Once);
                    await AssertListenerStopped(startTask.Result.GetIPEndPoint());
                }
                finally
                {
                    server.Stop();
                }
            }
        }

        [TestMethod]
        public async Task SocketServerShouldCloseAcceptedClientWhenChannelFactoryThrowsDuringStop()
        {
            var connections = await CreateConnectedTcpClients();
            using (var peer = connections.Item1)
            using (var accepted = connections.Item2)
            using (var factoryCalled = new ManualResetEventSlim(false))
            {
                SocketServer server = null;
                server = new SocketServer(
                    stream =>
                    {
                        server.Stop();
                        factoryCalled.Set();
                        throw new InvalidOperationException();
                    },
                    listener => Task.FromResult(accepted));
                var connected = false;
                server.Connected += (sender, args) => connected = true;
                try
                {
                    var endpoint = server.Start(this.defaultConnection).GetIPEndPoint();

                    Assert.IsTrue(factoryCalled.Wait(TIMEOUT));
                    Assert.IsTrue(peer.Client.Poll(TIMEOUT * 1000, SelectMode.SelectRead));
                    Assert.AreEqual(0, peer.Client.Available);
                    Assert.IsFalse(connected);
                    await AssertListenerStopped(endpoint);
                }
                finally
                {
                    server.Stop();
                }
            }
        }

        [TestMethod]
        public void SocketServerStopShouldCloseClient()
        {
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketServer.Disconnected += (s, e) => { waitEvent.Set(); };
            this.SetupChannel(out ConnectedEventArgs clientConnected);

            this.socketServer.Stop();

            waitEvent.WaitOne();
            Assert.ThrowsException<IOException>(() => WriteData(this.tcpClient));
        }

        [TestMethod]
        public void SocketServerStopShouldRaiseClientDisconnectedEventOnClientDisconnection()
        {
            DisconnectedEventArgs disconnected = null;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketServer.Disconnected += (s, e) =>
            {
                disconnected = e;
                waitEvent.Set();
            };
            this.SetupChannel(out ConnectedEventArgs clientConnected);

            this.socketServer.Stop();

            waitEvent.WaitOne();
            Assert.IsNotNull(disconnected);
            Assert.IsNull(disconnected.Error);
        }

        [TestMethod]
        public void SocketServerStopShouldCloseChannel()
        {
            var waitEvent = new ManualResetEventSlim(false);
            var channel = this.SetupChannel(out ConnectedEventArgs clientConnected);
            this.socketServer.Disconnected += (s, e) => { waitEvent.Set(); };

            this.socketServer.Stop();

            waitEvent.Wait();
            Assert.ThrowsException<CommunicationException>(() => channel.Send(DUMMYDATA));
        }

        [TestMethod]
        public void SocketServerShouldRaiseClientDisconnectedEventIfConnectionIsBroken()
        {
            DisconnectedEventArgs clientDisconnected = null;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketServer.Disconnected += (sender, eventArgs) =>
            {
                clientDisconnected = eventArgs;
                waitEvent.Set();
            };
            var channel = this.SetupChannel(out ConnectedEventArgs clientConnected);

            channel.MessageReceived += (sender, args) =>
            {
            };

            // Close the client channel. Message loop should stop.
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            this.tcpClient?.Close();
#else
            // tcpClient.Close() not available for netcoreapp1.0
            this.tcpClient?.Dispose();
#endif
            Assert.IsTrue(waitEvent.WaitOne(1000));
            Assert.IsTrue(clientDisconnected.Error is IOException);
        }

        [TestMethod]
        public async Task SocketEndpointShouldInitializeChannelOnServerConnection()
        {
            var channel = this.SetupChannel(out ConnectedEventArgs _);

            await channel.Send(DUMMYDATA);

            Assert.AreEqual(DUMMYDATA, ReadData(this.Client));
        }

        protected override ICommunicationChannel SetupChannel(out ConnectedEventArgs connectedEvent)
        {
            ICommunicationChannel channel = null;
            ConnectedEventArgs clientConnectedEvent = null;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketServer.Connected += (sender, eventArgs) =>
            {
                clientConnectedEvent = eventArgs;
                channel = eventArgs.Channel;
                waitEvent.Set();
            };

            var connectionInfo = this.socketServer.Start(this.defaultConnection);
            var port = connectionInfo.GetIPEndPoint().Port;
            this.ConnectToServer(port).GetAwaiter().GetResult();
            waitEvent.WaitOne();

            connectedEvent = clientConnectedEvent;
            return channel;
        }

        private async Task ConnectToServer(int port)
        {
            await this.tcpClient.ConnectAsync(IPAddress.Loopback, port);
        }

        private static async Task AssertListenerStopped(IPEndPoint endpoint)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(endpoint.Address, endpoint.Port);
                    Assert.Fail("The stopped server accepted another client.");
                }
                catch (SocketException ex)
                {
                    Assert.AreEqual(SocketError.ConnectionRefused, ex.SocketErrorCode);
                }
            }
        }

        private static async Task<Tuple<TcpClient, TcpClient>> CreateConnectedTcpClients()
        {
            var client = new TcpClient();
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var acceptTask = listener.AcceptTcpClientAsync();
                await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                return Tuple.Create(client, await acceptTask);
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
}
