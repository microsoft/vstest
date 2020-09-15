// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.PlatformTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SocketClientTests : SocketTestsBase, IDisposable
    {
        private readonly TcpListener tcpListener;

        private readonly ICommunicationEndPoint socketClient;

        private TcpClient tcpClient;

        public SocketClientTests()
        {
            this.socketClient = new SocketClient();

            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            this.tcpListener = new TcpListener(endpoint);
        }

        protected override TcpClient Client => this.tcpClient;

        public void Dispose()
        {
            this.socketClient.Stop();
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            this.tcpClient?.Close();
#else
            // tcpClient.Close() not available for netcoreapp1.0
            this.tcpClient?.Dispose();
#endif
        }

        [TestMethod]
        public void SocketClientStartShouldConnectToLoopbackOnGivenPort()
        {
            var connectionInfo = this.StartLocalServer();

            this.socketClient.Start(connectionInfo);

            var acceptClientTask = this.tcpListener.AcceptTcpClientAsync();
            Assert.IsTrue(acceptClientTask.Wait(TIMEOUT));
            Assert.IsTrue(acceptClientTask.Result.Connected);
        }

        [TestMethod]
        [Ignore]
        public void SocketClientStartShouldThrowIfServerIsNotListening()
        {
            var dummyConnectionInfo = "5345";

            this.socketClient.Start(dummyConnectionInfo);

            var exceptionThrown = false;
            try
            {
                this.socketClient.Start(dummyConnectionInfo);
            }
            catch (PlatformNotSupportedException)
            {
                // Thrown on unix
                exceptionThrown = true;
            }
            catch (SocketException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        public void SocketClientStopShouldRaiseClientDisconnectedEventOnClientDisconnection()
        {
            var waitEvent = this.SetupClientDisconnect(out ICommunicationChannel _);

            // Close the communication from client side
            this.socketClient.Stop();

            Assert.IsTrue(waitEvent.WaitOne(TIMEOUT));
        }

        [TestMethod]
        public void SocketClientShouldRaiseClientDisconnectedEventIfConnectionIsBroken()
        {
            var waitEvent = this.SetupClientDisconnect(out ICommunicationChannel _);

            // Close the communication from server side
            this.tcpClient.GetStream().Dispose();
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            this.tcpClient?.Close();
#else
            // tcpClient.Close() not available for netcoreapp1.0
            this.tcpClient?.Dispose();
#endif
            Assert.IsTrue(waitEvent.WaitOne(TIMEOUT));
        }

        [TestMethod]
        public void SocketClientStopShouldStopCommunication()
        {
            var waitEvent = this.SetupClientDisconnect(out ICommunicationChannel _);

            // Close the communication from socket client side
            this.socketClient.Stop();

            // Validate that write on server side fails
            waitEvent.WaitOne(TIMEOUT);
            Assert.ThrowsException<IOException>(() => WriteData(this.Client));
        }

        [TestMethod]
        public void SocketClientStopShouldCloseChannel()
        {
            var waitEvent = this.SetupClientDisconnect(out ICommunicationChannel channel);

            this.socketClient.Stop();

            waitEvent.WaitOne(TIMEOUT);
            Assert.ThrowsException<CommunicationException>(() => channel.Send(DUMMYDATA));
        }

        protected override ICommunicationChannel SetupChannel(out ConnectedEventArgs connectedEvent)
        {
            ICommunicationChannel channel = null;
            ConnectedEventArgs serverConnectedEvent = null;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketClient.Connected += (sender, eventArgs) =>
            {
                serverConnectedEvent = eventArgs;
                channel = eventArgs.Channel;
                waitEvent.Set();
            };

            var connectionInfo = this.StartLocalServer();
            this.socketClient.Start(connectionInfo);

            var acceptClientTask = this.tcpListener.AcceptTcpClientAsync();
            if (acceptClientTask.Wait(TimeSpan.FromMilliseconds(1000)))
            {
                this.tcpClient = acceptClientTask.Result;
                waitEvent.WaitOne(1000);
            }

            connectedEvent = serverConnectedEvent;
            return channel;
        }

        private ManualResetEvent SetupClientDisconnect(out ICommunicationChannel channel)
        {
            var waitEvent = new ManualResetEvent(false);
            this.socketClient.Disconnected += (s, e) => { waitEvent.Set(); };
            channel = this.SetupChannel(out ConnectedEventArgs _);
            channel.MessageReceived += (sender, args) =>
            {
            };
            return waitEvent;
        }

        private string StartLocalServer()
        {
            this.tcpListener.Start();

            return ((IPEndPoint)this.tcpListener.LocalEndpoint).ToString();
        }
    }
}
