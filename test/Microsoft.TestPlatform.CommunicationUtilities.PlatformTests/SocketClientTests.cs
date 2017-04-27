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
    public class SocketClientTests : SocketTestsBase, IDisposable
    {
        private readonly TcpListener tcpListener;

        private readonly ICommunicationClient socketClient;

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
            this.tcpClient?.Dispose();
            this.tcpListener.Stop();
        }

        [TestMethod]
        public void SocketClientStartShouldConnectToLoopbackOnGivenPort()
        {
            var connectionInfo = this.StartLocalServer();

            this.socketClient.Start(connectionInfo);

            var acceptClientTask = this.tcpListener.AcceptTcpClientAsync();
            Assert.IsTrue(acceptClientTask.Wait(1000));
            Assert.IsTrue(acceptClientTask.Result.Connected);
        }

        [TestMethod]
        public void SocketClientStopShouldStopCommunication()
        {
            this.SetupChannel(out ConnectedEventArgs connectedEventArgs);

            this.socketClient.Stop();

            try
            {
                WriteData(this.Client);
            }
            catch (SocketException)
            {
            }
        }

        // public void SocketClientStopShouldCloseClient()
        // public void SocketClientStopShouldRaiseClientDisconnectedEventOnClientDisconnection()
        // public void SocketClientStopShouldCloseChannel()
        // public void SocketClientShouldRaiseClientDisconnectedEventIfConnectionIsBroken()
        // TODO
        // SocketClientStartShouldThrowIfServerIsNotListening
        protected override ICommunicationChannel SetupChannel(out ConnectedEventArgs connectedEvent)
        {
            ICommunicationChannel channel = null;
            ConnectedEventArgs serverConnectedEvent = null;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            this.socketClient.ServerConnected += (sender, eventArgs) =>
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

        private string StartLocalServer()
        {
            this.tcpListener.Start();

            return ((IPEndPoint)this.tcpListener.LocalEndpoint).Port.ToString();
        }
    }
}
