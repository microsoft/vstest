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
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public abstract class SocketTestsBase
    {
        protected const string DUMMYDATA = "Dummy Data";
        protected const int TIMEOUT = 10 * 1000;

        protected abstract TcpClient Client { get; }

        [TestMethod]
        public void SocketEndpointStartShouldRaiseServerConnectedEventOnServerConnection()
        {
            this.SetupChannel(out ConnectedEventArgs connectedEventArgs);

            Assert.IsNotNull(connectedEventArgs);
        }

        [TestMethod]
        public void SocketEndpointShouldNotifyChannelOnDataAvailable()
        {
            var message = string.Empty;
            ManualResetEvent waitForMessage = new ManualResetEvent(false);
            this.SetupChannel(out ConnectedEventArgs _).MessageReceived += (s, e) =>
            {
                message = e.Data;
                waitForMessage.Set();
            };

            WriteData(this.Client);

            waitForMessage.WaitOne();
            Assert.AreEqual(DUMMYDATA, message);
        }

        protected static string ReadData(TcpClient client)
        {
            using (BinaryReader reader = new BinaryReader(client.GetStream()))
            {
                return reader.ReadString();
            }
        }

        protected static void WriteData(TcpClient client)
        {
            using (BinaryWriter writer = new BinaryWriter(client.GetStream()))
            {
                writer.Write(DUMMYDATA);
            }
        }

        protected abstract ICommunicationChannel SetupChannel(out ConnectedEventArgs connectedEventArgs);

        protected IPEndPoint GetIpEndPoint(string value)
        {
            if (Uri.TryCreate(string.Concat("tcp://", value), UriKind.Absolute, out Uri uri))
            {
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
            }

            return new IPEndPoint(IPAddress.Loopback, 0);
        }
    }
}
