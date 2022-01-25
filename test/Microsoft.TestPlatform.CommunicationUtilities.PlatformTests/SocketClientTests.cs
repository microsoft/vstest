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
            socketClient = new SocketClient();

            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            tcpListener = new TcpListener(endpoint);
        }

        protected override TcpClient Client => tcpClient;

        public void Dispose()
        {
            socketClient.Stop();
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
        public void SocketClientStartShouldConnectToLoopbackOnGivenPort()
        {
            var connectionInfo = StartLocalServer();

            socketClient.Start(connectionInfo);

            var acceptClientTask = tcpListener.AcceptTcpClientAsync();
            Assert.IsTrue(acceptClientTask.Wait(TIMEOUT));
            Assert.IsTrue(acceptClientTask.Result.Connected);
        }

        [TestMethod]
        [Ignore]
        public void SocketClientStartShouldThrowIfServerIsNotListening()
        {
            var dummyConnectionInfo = "5345";

            socketClient.Start(dummyConnectionInfo);

            var exceptionThrown = false;
            try
            {
                socketClient.Start(dummyConnectionInfo);
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
            var waitEvent = SetupClientDisconnect(out ICommunicationChannel _);

            // Close the communication from client side
            socketClient.Stop();

            Assert.IsTrue(waitEvent.WaitOne(TIMEOUT));
        }

        [TestMethod]
        public void SocketClientShouldRaiseClientDisconnectedEventIfConnectionIsBroken()
        {
            var waitEvent = SetupClientDisconnect(out ICommunicationChannel _);

            // Close the communication from server side
            tcpClient.GetStream().Dispose();
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            tcpClient?.Close();
#else
            // tcpClient.Close() not available for netcoreapp1.0
            tcpClient?.Dispose();
#endif
            Assert.IsTrue(waitEvent.WaitOne(TIMEOUT));
        }

        [TestMethod]
        public void SocketClientStopShouldStopCommunication()
        {
            var waitEvent = SetupClientDisconnect(out ICommunicationChannel _);

            // Close the communication from socket client side
            socketClient.Stop();

            // Validate that write on server side fails
            waitEvent.WaitOne(TIMEOUT);
            Assert.ThrowsException<IOException>(() => WriteData(Client));
        }

        [TestMethod]
        public void SocketClientStopShouldCloseChannel()
        {
            var waitEvent = SetupClientDisconnect(out ICommunicationChannel channel);

            socketClient.Stop();

            waitEvent.WaitOne(TIMEOUT);
            Assert.ThrowsException<CommunicationException>(() => channel.Send(DUMMYDATA));
        }

        protected override ICommunicationChannel SetupChannel(out ConnectedEventArgs connectedEvent)
        {
            ICommunicationChannel channel = null;
            ConnectedEventArgs serverConnectedEvent = null;
            ManualResetEvent waitEvent = new(false);
            socketClient.Connected += (sender, eventArgs) =>
            {
                serverConnectedEvent = eventArgs;
                channel = eventArgs.Channel;
                waitEvent.Set();
            };

            var connectionInfo = StartLocalServer();
            socketClient.Start(connectionInfo);

            var acceptClientTask = tcpListener.AcceptTcpClientAsync();
            if (acceptClientTask.Wait(TimeSpan.FromMilliseconds(1000)))
            {
                tcpClient = acceptClientTask.Result;
                waitEvent.WaitOne(1000);
            }

            connectedEvent = serverConnectedEvent;
            return channel;
        }

        private ManualResetEvent SetupClientDisconnect(out ICommunicationChannel channel)
        {
            var waitEvent = new ManualResetEvent(false);
            socketClient.Disconnected += (s, e) => waitEvent.Set();
            channel = SetupChannel(out ConnectedEventArgs _);
            channel.MessageReceived += (sender, args) =>
            {
            };
            return waitEvent;
        }

        private string StartLocalServer()
        {
            tcpListener.Start();

            return ((IPEndPoint)tcpListener.LocalEndpoint).ToString();
        }
    }
}
