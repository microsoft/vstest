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
            socketServer = new SocketServer();

            tcpClient = new TcpClient();
        }

        protected override TcpClient Client => tcpClient;

        public void Dispose()
        {
            socketServer.Stop();
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            tcpClient?.Close();
#else
            // tcpClient.Close() not available for netcoreapp1.0
            tcpClient?.Dispose();
#endif
            GC.SuppressFinalize(this);
        }

        [TestMethod]
        public async Task SocketServerStartShouldHostServer()
        {
            var connectionInfo = socketServer.Start(defaultConnection);

            Assert.IsFalse(string.IsNullOrEmpty(connectionInfo));
            await ConnectToServer(connectionInfo.GetIPEndPoint().Port);
            Assert.IsTrue(tcpClient.Connected);
        }

        [TestMethod]
        public void SocketServerStopShouldStopListening()
        {
            var connectionInfo = socketServer.Start(defaultConnection);

            socketServer.Stop();

            try
            {
                // This method throws ExtendedSocketException (which is private). It is not possible
                // to use Assert.ThrowsException in this case.
                ConnectToServer(connectionInfo.GetIPEndPoint().Port).GetAwaiter().GetResult();
            }
            catch (SocketException)
            {
            }
        }

        [TestMethod]
        public void SocketServerStopShouldCloseClient()
        {
            ManualResetEvent waitEvent = new(false);
            socketServer.Disconnected += (s, e) => waitEvent.Set();
            SetupChannel(out ConnectedEventArgs clientConnected);

            socketServer.Stop();

            waitEvent.WaitOne();
            Assert.ThrowsException<IOException>(() => WriteData(tcpClient));
        }

        [TestMethod]
        public void SocketServerStopShouldRaiseClientDisconnectedEventOnClientDisconnection()
        {
            DisconnectedEventArgs disconnected = null;
            ManualResetEvent waitEvent = new(false);
            socketServer.Disconnected += (s, e) =>
            {
                disconnected = e;
                waitEvent.Set();
            };
            SetupChannel(out ConnectedEventArgs clientConnected);

            socketServer.Stop();

            waitEvent.WaitOne();
            Assert.IsNotNull(disconnected);
            Assert.IsNull(disconnected.Error);
        }

        [TestMethod]
        public void SocketServerStopShouldCloseChannel()
        {
            var waitEvent = new ManualResetEventSlim(false);
            var channel = SetupChannel(out ConnectedEventArgs clientConnected);
            socketServer.Disconnected += (s, e) => waitEvent.Set();

            socketServer.Stop();

            waitEvent.Wait();
            Assert.ThrowsException<CommunicationException>(() => channel.Send(DUMMYDATA));
        }

        [TestMethod]
        public void SocketServerShouldRaiseClientDisconnectedEventIfConnectionIsBroken()
        {
            DisconnectedEventArgs clientDisconnected = null;
            ManualResetEvent waitEvent = new(false);
            socketServer.Disconnected += (sender, eventArgs) =>
            {
                clientDisconnected = eventArgs;
                waitEvent.Set();
            };
            var channel = SetupChannel(out ConnectedEventArgs clientConnected);

            channel.MessageReceived += (sender, args) =>
            {
            };

            // Close the client channel. Message loop should stop.
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            tcpClient?.Close();
#else
            // tcpClient.Close() not available for netcoreapp1.0
            tcpClient?.Dispose();
#endif
            Assert.IsTrue(waitEvent.WaitOne(1000));
            Assert.IsTrue(clientDisconnected.Error is IOException);
        }

        [TestMethod]
        public async Task SocketEndpointShouldInitializeChannelOnServerConnection()
        {
            var channel = SetupChannel(out ConnectedEventArgs _);

            await channel.Send(DUMMYDATA);

            Assert.AreEqual(DUMMYDATA, ReadData(Client));
        }

        protected override ICommunicationChannel SetupChannel(out ConnectedEventArgs connectedEvent)
        {
            ICommunicationChannel channel = null;
            ConnectedEventArgs clientConnectedEvent = null;
            ManualResetEvent waitEvent = new(false);
            socketServer.Connected += (sender, eventArgs) =>
            {
                clientConnectedEvent = eventArgs;
                channel = eventArgs.Channel;
                waitEvent.Set();
            };

            var connectionInfo = socketServer.Start(defaultConnection);
            var port = connectionInfo.GetIPEndPoint().Port;
            ConnectToServer(port).GetAwaiter().GetResult();
            waitEvent.WaitOne();

            connectedEvent = clientConnectedEvent;
            return channel;
        }

        private async Task ConnectToServer(int port)
        {
            await tcpClient.ConnectAsync(IPAddress.Loopback, port);
        }
    }
}
