// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.PerformanceTests
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SocketTests
    {
        [TestMethod]
        public void SocketThroughput2()
        {
            // Measure the throughput with socket communication v2 (SocketServer, SocketClient)
            // implementation.
            var server = new SocketServer();
            var client = new SocketClient();
            ICommunicationChannel serverChannel = null;
            ICommunicationChannel clientChannel = null;
            ManualResetEventSlim dataTransferred = new ManualResetEventSlim(false);
            ManualResetEventSlim clientConnected = new ManualResetEventSlim(false);
            ManualResetEventSlim serverConnected = new ManualResetEventSlim(false);
            int dataReceived = 0;
            var watch = new Stopwatch();
            var thread = new Thread(() => SendData(clientChannel, watch));

            // Setup server
            server.ClientConnected += (sender, args) =>
            {
                serverChannel = args.Channel;
                serverChannel.MessageReceived += (channel, messageReceived) =>
                {
                    // Keep count of bytes
                    dataReceived += messageReceived.Data.Length;

                    if (dataReceived >= 65536 * 20000)
                    {
                        dataTransferred.Set();
                        watch.Stop();
                    }
                };

                clientConnected.Set();
            };

            client.ServerConnected += (sender, args) =>
            {
                clientChannel = args.Channel;

                thread.Start();

                serverConnected.Set();
            };

            var port = server.Start();
            client.Start(port);

            clientConnected.Wait();
            serverConnected.Wait();
            thread.Join();
            dataTransferred.Wait();

            Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(4), "Elapsed: " + watch.Elapsed);
        }

        [TestMethod]
        public void SocketThroughput1()
        {
            // Measure the throughput with socket communication v1 (SocketCommunicationManager)
            // implementation.
            var server = new SocketCommunicationManager();
            var client = new SocketCommunicationManager();
            var watch = new Stopwatch();

            int port = server.HostServer(new IPEndPoint(IPAddress.Loopback, 0)).Port;
            client.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, port)).Wait();
            server.AcceptClientAsync().Wait();

            server.WaitForClientConnection(1000);
            client.WaitForServerConnection(1000);

            var clientThread = new Thread(() => SendData2(client, watch));
            clientThread.Start();

            var dataReceived = 0;
            while (dataReceived < 65536 * 20000)
            {
                dataReceived += server.ReceiveRawMessage().Length;
            }

            watch.Stop();
            clientThread.Join();

            Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(4), "Elapsed: " + watch.Elapsed);
        }

        private static void SendData(ICommunicationChannel channel, Stopwatch watch)
        {
            var dataBytes = new byte[65536];
            for (int i = 0; i < dataBytes.Length; i++)
            {
                dataBytes[i] = 0x65;
            }

            var dataBytesStr = System.Text.Encoding.UTF8.GetString(dataBytes);

            watch.Start();
            for (int i = 0; i < 20000; i++)
            {
                channel.Send(dataBytesStr);
            }
        }

        private static void SendData2(ICommunicationManager communicationManager, Stopwatch watch)
        {
            var dataBytes = new byte[65536];
            for (int i = 0; i < dataBytes.Length; i++)
            {
                dataBytes[i] = 0x65;
            }

            var dataBytesStr = System.Text.Encoding.UTF8.GetString(dataBytes);

            watch.Start();
            for (int i = 0; i < 20000; i++)
            {
                communicationManager.SendRawMessage(dataBytesStr);
            }
        }
    }
}
