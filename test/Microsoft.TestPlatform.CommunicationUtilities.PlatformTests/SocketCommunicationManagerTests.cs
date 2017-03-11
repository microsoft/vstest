// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.PlatformTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SocketCommunicationManagerTests : IDisposable
    {
        private readonly SocketCommunicationManager communicationManager;

        private readonly TcpClient tcpClient;

        private readonly TcpListener tcpListener;

        public SocketCommunicationManagerTests()
        {
            this.communicationManager = new SocketCommunicationManager();
            this.tcpClient = new TcpClient();
            this.tcpListener = new TcpListener(IPAddress.Loopback, 0);
        }

        public void Dispose()
        {
            this.tcpListener.Stop();
            this.tcpClient.Dispose();

            this.communicationManager.StopServer();
            this.communicationManager.StopClient();
        }

        #region Server tests

        [TestMethod]
        public async Task HostServerShouldStartServerAndReturnPortNumber()
        {
            var port = this.communicationManager.HostServer();

            Assert.IsTrue(port > 0);
            await this.tcpClient.ConnectAsync(IPAddress.Loopback, port);
            Assert.IsTrue(this.tcpClient.Connected);
        }

        [TestMethod]
        public async Task AcceptClientAsyncShouldWaitForClientConnection()
        {
            var clientConnected = false;
            var waitEvent = new ManualResetEvent(false);
            var port = this.communicationManager.HostServer();

            var acceptClientTask = this.communicationManager.AcceptClientAsync().ContinueWith(
                (continuationTask, state) =>
                    {
                        clientConnected = true;
                        waitEvent.Set();
                    },
                null);

            await this.tcpClient.ConnectAsync(IPAddress.Loopback, port);
            Assert.IsTrue(this.tcpClient.Connected);
            Assert.IsTrue(waitEvent.WaitOne(1000) && clientConnected);
        }

        [TestMethod]
        public async Task WaitForClientConnectionShouldWaitUntilClientIsConnected()
        {
            var port = this.communicationManager.HostServer();
            var acceptClientTask = this.communicationManager.AcceptClientAsync();
            await this.tcpClient.ConnectAsync(IPAddress.Loopback, port);

            var clientConnected = this.communicationManager.WaitForClientConnection(1000);

            Assert.IsTrue(this.tcpClient.Connected);
            Assert.IsTrue(clientConnected);
        }

        [TestMethod]
        public void WaitForClientConnectionShouldReturnFalseIfClientIsNotConnected()
        {
            this.communicationManager.HostServer();
            var acceptClientTask = this.communicationManager.AcceptClientAsync();

            // Do not attempt the client to connect to server. Directly wait until timeout.
            var clientConnected = this.communicationManager.WaitForClientConnection(100);

            Assert.IsFalse(clientConnected);
        }

        [TestMethod]
        public void StopServerShouldCloseServer()
        {
            var port = this.communicationManager.HostServer();
            var acceptClientTask = this.communicationManager.AcceptClientAsync();
            
            this.communicationManager.StopServer();

            Assert.ThrowsException<AggregateException>(() => this.tcpClient.ConnectAsync(IPAddress.Loopback, port).Wait());
        }

        #endregion

        #region Client tests

        [TestMethod]
        public async Task SetupClientAsyncShouldConnectToServer()
        {
            var port = this.StartServer();

            var setupClientTask = this.communicationManager.SetupClientAsync(port);

            var client = await this.tcpListener.AcceptTcpClientAsync();
            Assert.IsTrue(client.Connected);
        }

        [TestMethod]
        public async Task WaitForServerConnectionShouldWaitUntilClientIsConnected()
        {
            var port = this.StartServer();
            var setupClientTask = this.communicationManager.SetupClientAsync(port);
            await this.tcpListener.AcceptTcpClientAsync();

            var serverConnected = this.communicationManager.WaitForServerConnection(1000);

            Assert.IsTrue(serverConnected);
        }

        [TestMethod]
        public void WaitForServerConnectionShouldReturnFalseIfClientIsNotConnected()
        {
            // There is no server listening on port 20000.
            var setupClientTask = this.communicationManager.SetupClientAsync(20000);

            var serverConnected = this.communicationManager.WaitForServerConnection(100);

            Assert.IsFalse(serverConnected);
        }

        [TestMethod]
        public async Task StopClientShouldDisconnectClient()
        {
            var port = this.StartServer();
            var setupClientTask = this.communicationManager.SetupClientAsync(port);
            var client = await this.tcpListener.AcceptTcpClientAsync();
            this.communicationManager.WaitForServerConnection(1000);

            this.communicationManager.StopClient();

            // Attempt to write on client socket should throw since it should have disconnected.
            Assert.ThrowsException<SocketException>(() => this.WriteOnSocket(client.Client));
        }

        #endregion

        #region Message sender tests

        [TestMethod]
        public async Task SendMessageShouldSendMessageWithoutAnyPayload()
        {
            var client = await this.StartServerAndWaitForConnection();

            this.communicationManager.SendMessage(MessageType.StartDiscovery);

            var message = "{\"MessageType\":\"TestDiscovery.Start\",\"Payload\":null}";
            Assert.AreEqual(message, this.ReadFromStream(client.GetStream()));
        }

        [TestMethod]
        public async Task SendMessageWithPayloadShouldSerializeAndSendThePayload()
        {
            var client = await this.StartServerAndWaitForConnection();

            this.communicationManager.SendMessage(MessageType.StartDiscovery, "Dummy Payload");

            var message = "{\"MessageType\":\"TestDiscovery.Start\",\"Payload\":\"Dummy Payload\"}";
            Assert.AreEqual(message, this.ReadFromStream(client.GetStream()));
        }

        [TestMethod]
        public void SendMessageWithRawMessageShouldNotSerializeThePayload()
        {
        }

        #endregion

        #region Message receiver tests

        #endregion

        private int StartServer()
        {
            this.tcpListener.Start();

            return ((IPEndPoint)this.tcpListener.LocalEndpoint).Port;
        }

        private async Task<TcpClient> StartServerAndWaitForConnection()
        {
            var port = this.StartServer();
            var setupClientTask = this.communicationManager.SetupClientAsync(port);
            var client = await this.tcpListener.AcceptTcpClientAsync();
            this.communicationManager.WaitForServerConnection(1000);

            return client;
        }

        private void WriteOnSocket(Socket socket)
        {
            for (int i = 0; i < 10; i++)
            {
                socket.Send(new byte[2] { 0x1, 0x0 });
            }
        }

        private string ReadFromStream(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadString();
            }
        }
    }
}
