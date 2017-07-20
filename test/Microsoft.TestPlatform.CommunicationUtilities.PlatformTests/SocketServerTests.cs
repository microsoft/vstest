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

        private readonly ICommunicationServer socketServer;

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
            var connectionInfo = this.socketServer.Start();

            Assert.IsFalse(string.IsNullOrEmpty(connectionInfo));
            await this.ConnectToServer(int.Parse(connectionInfo));
            Assert.IsTrue(this.tcpClient.Connected);
        }

        [TestMethod]
        public void SocketServerStopShouldStopListening()
        {
            var connectionInfo = this.socketServer.Start();

            this.socketServer.Stop();

            try
            {
                // This method throws ExtendedSocketException (which is private). It is not possible
                // to use Assert.ThrowsException in this case.
                this.ConnectToServer(int.Parse(connectionInfo)).GetAwaiter().GetResult();
            }
            catch (SocketException)
            {
            }
        }

        [TestMethod]
        public void SocketServerStopShouldCloseClient()
        {
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketServer.ClientDisconnected += (s, e) => { waitEvent.Set(); };
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
            this.socketServer.ClientDisconnected += (s, e) =>
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
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            var channel = this.SetupChannel(out ConnectedEventArgs clientConnected);
            this.socketServer.ClientDisconnected += (s, e) => { waitEvent.Set(); };

            this.socketServer.Stop();

            waitEvent.WaitOne();
            Assert.ThrowsException<CommunicationException>(() => channel.Send(DUMMYDATA));
        }

        [TestMethod]
        public void SocketServerShouldRaiseClientDisconnectedEventIfConnectionIsBroken()
        {
            DisconnectedEventArgs clientDisconnected = null;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketServer.ClientDisconnected += (sender, eventArgs) =>
            {
                clientDisconnected = eventArgs;
                waitEvent.Set();
            };
            var channel = this.SetupChannel(out ConnectedEventArgs clientConnected);

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

        protected override ICommunicationChannel SetupChannel(out ConnectedEventArgs connectedEvent)
        {
            ICommunicationChannel channel = null;
            ConnectedEventArgs clientConnectedEvent = null;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketServer.ClientConnected += (sender, eventArgs) =>
            {
                clientConnectedEvent = eventArgs;
                channel = eventArgs.Channel;
                waitEvent.Set();
            };

            var connectionInfo = this.socketServer.Start();
            this.ConnectToServer(int.Parse(connectionInfo)).GetAwaiter().GetResult();
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
