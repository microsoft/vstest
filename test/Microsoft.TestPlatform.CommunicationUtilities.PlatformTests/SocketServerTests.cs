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
#if NET451
            // tcpClient.Close() calls tcpClient.Dispose().
            this.tcpClient?.Close();
#else
            // tcpClient.Close() not available for netcoreapp1.0
            this.tcpClient?.Dispose();
#endif
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

            await AssertListenerClosed(connectionInfo.GetIPEndPoint());
            Assert.IsFalse(connected);
        }

        [TestMethod]
        public void SocketServerStartShouldThrowAfterStop()
        {
            this.socketServer.Stop();
            Assert.ThrowsException<ObjectDisposedException>(() => this.socketServer.Start(this.defaultConnection));
        }

        [TestMethod]
        public async Task SocketServerShouldCloseAcceptedClientDeliveredAfterStop()
        {
            var clients = await CreateConnectedTcpClients();
            using (var peer = clients.Item1)
            using (var accepted = clients.Item2)
            {
                var completion = new TaskCompletionSource<TcpClient>();
                var channel = new Mock<ICommunicationChannel>();
                var factoryCount = 0;
                var connectedCount = 0;
                var server = new SocketServer(
                    stream =>
                    {
                        Interlocked.Increment(ref factoryCount);
                        return channel.Object;
                    },
                    listener => completion.Task);
                server.Connected += (sender, args) => Interlocked.Increment(ref connectedCount);
                try
                {
                    var endpoint = server.Start(this.defaultConnection).GetIPEndPoint();
                    server.Stop();
                    await Task.Run(() => completion.SetResult(accepted));

                    Assert.IsTrue(peer.Client.Poll(TIMEOUT * 1000, SelectMode.SelectRead));
                    Assert.AreEqual(0, peer.Client.Available);
                    Assert.AreEqual(0, factoryCount);
                    Assert.AreEqual(0, connectedCount);
                    channel.Verify(c => c.Dispose(), Times.Never);
                    await AssertListenerClosed(endpoint);
                }
                finally
                {
                    server.Stop();
                }
            }
        }

        // MSTest 1.3.1 discovers Task-returning tests only when they have an async state machine.
        [TestMethod]
        public async Task SocketServerStopDuringChannelInitializationMustNotPublishChannel()
        {
            await this.AssertChannelReturnedAfterStopIsNotPublished(true);
        }

        [TestMethod]
        public async Task SocketServerConcurrentStopDuringChannelInitializationMustNotPublishChannel()
        {
            await this.AssertChannelReturnedAfterStopIsNotPublished(false);
        }

        [TestMethod]
        public async Task SocketServerShouldCloseAcceptedClientWhenChannelFactoryThrowsDuringStop()
        {
            var clients = await CreateConnectedTcpClients();
            using (var peer = clients.Item1)
            using (var accepted = clients.Item2)
            {
                SocketServer server = null;
                server = new SocketServer(
                    stream =>
                    {
                        server.Stop();
                        throw new InvalidOperationException();
                    },
                    listener => Task.FromResult(accepted));
                server.Connected += (sender, args) => { };
                try
                {
                    var endpoint = server.Start(this.defaultConnection).GetIPEndPoint();
                    Assert.IsTrue(peer.Client.Poll(TIMEOUT * 1000, SelectMode.SelectRead));
                    Assert.AreEqual(0, peer.Client.Available);
                    await AssertListenerClosed(endpoint);
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
            var clients = await CreateConnectedTcpClients();
            using (var peer = clients.Item1)
            using (var accepted = clients.Item2)
            using (var entered = new ManualResetEventSlim(false))
            using (var exited = new ManualResetEventSlim(false))
            using (var release = new ManualResetEventSlim(false))
            {
                var channel = new Mock<ICommunicationChannel>();
                var server = new SocketServer(stream => channel.Object, listener => Task.FromResult(accepted));
                server.Connected += (sender, args) =>
                {
                    entered.Set();
                    release.Wait(TIMEOUT * 2);
                    exited.Set();
                };
                try
                {
                    Task<string> start;
                    try
                    {
                        start = Task.Run(() => server.Start(this.defaultConnection));
                        Assert.IsTrue(entered.Wait(TIMEOUT));
                        var stop = Task.Run(server.Stop);
                        Assert.IsTrue(stop.Wait(TIMEOUT), "Stop must not wait for a user callback.");
                    }
                    finally
                    {
                        release.Set();
                    }
                    Assert.IsTrue(exited.Wait(TIMEOUT));
                    Assert.IsTrue(start.Wait(TIMEOUT));
                    await AssertListenerClosed(start.Result.GetIPEndPoint());
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

            Assert.IsTrue(waitEvent.WaitOne(TIMEOUT));
            Assert.IsTrue(this.tcpClient.Client.Poll(TIMEOUT * 1000, SelectMode.SelectRead));
            Assert.AreEqual(0, this.tcpClient.Client.Available);
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

            Assert.IsTrue(waitEvent.WaitOne(TIMEOUT));
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

            Assert.IsTrue(waitEvent.Wait(TIMEOUT));
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
#if NET451
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
            Assert.IsTrue(waitEvent.WaitOne(TIMEOUT));

            connectedEvent = clientConnectedEvent;
            return channel;
        }

        private async Task ConnectToServer(int port)
        {
            await this.tcpClient.ConnectAsync(IPAddress.Loopback, port);
        }

        private async Task AssertChannelReturnedAfterStopIsNotPublished(bool stopInsideFactory)
        {
            var clients = await CreateConnectedTcpClients();
            using (var peer = clients.Item1)
            using (var accepted = clients.Item2)
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
                channel.Setup(c => c.Dispose()).Callback(() =>
                {
                    var stopTask = Task.Run(server.Stop);
                    stopCompletedDuringDisposal = stopTask.Wait(TIMEOUT);
                });
                server.Connected += (sender, args) => Interlocked.Increment(ref connectedCount);
                server.Disconnected += (sender, args) => Interlocked.Increment(ref disconnectedCount);
                // Preserve the synchronous reentrant reproducer as well as the concurrent case.
                var startTask = stopInsideFactory
                    ? Task.FromResult(server.Start(this.defaultConnection))
                    : Task.Run(() => server.Start(this.defaultConnection));
                try
                {
                    Assert.IsTrue(factoryEntered.Wait(TIMEOUT));
                    if (!stopInsideFactory)
                    {
                        server.Stop();
                    }
                    releaseFactory.Set();
                    Assert.IsTrue(startTask.Wait(TIMEOUT));
                    var endpoint = (await startTask).GetIPEndPoint();

                    Assert.AreEqual(0, connectedCount, "A channel returned after Stop must not be published.");
                    Assert.AreEqual(0, disconnectedCount, "An unpublished channel must not enter the message loop.");
                    channel.Verify(c => c.Dispose(), Times.Once);
                    Assert.IsTrue(stopCompletedDuringDisposal, "Channel disposal must run outside the state lock.");
                    channel.Verify(c => c.NotifyDataAvailable(), Times.Never);
                    Assert.IsTrue(peer.Client.Poll(TIMEOUT * 1000, SelectMode.SelectRead));
                    Assert.AreEqual(0, peer.Client.Available);
                    await AssertListenerClosed(endpoint);
                    server.Stop();
                    channel.Verify(c => c.Dispose(), Times.Once);
                }
                finally
                {
                    releaseFactory.Set();
                    server.Stop();
                    Assert.IsTrue(startTask.Wait(TIMEOUT));
                }
            }
        }

        private static async Task AssertListenerClosed(IPEndPoint endpoint)
        {
            using (var client = new TcpClient())
            {
                var refused = false;
                try
                {
                    await client.ConnectAsync(endpoint.Address, endpoint.Port);
                }
                catch (SocketException)
                {
                    refused = true;
                }
                Assert.IsTrue(refused, "The stopped listener still accepted a client.");
            }
        }

        private static async Task<Tuple<TcpClient, TcpClient>> CreateConnectedTcpClients()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var peer = new TcpClient();
                var accepted = listener.AcceptTcpClientAsync();
                await peer.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                return Tuple.Create(peer, await accepted);
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
