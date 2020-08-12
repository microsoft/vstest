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
        public void SocketServerStopShouldStopListening()
        {
            var connectionInfo = this.socketServer.Start(this.defaultConnection);

            this.socketServer.Stop();

            try
            {
                // This method throws ExtendedSocketException (which is private). It is not possible
                // to use Assert.ThrowsException in this case.
                this.ConnectToServer(connectionInfo.GetIPEndPoint().Port).GetAwaiter().GetResult();
            }
            catch (SocketException)
            {
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
    }
}
