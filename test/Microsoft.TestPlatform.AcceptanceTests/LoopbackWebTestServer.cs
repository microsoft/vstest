// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class LoopbackWebTestServer : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly Task requests;
        private int requestCount;

        internal LoopbackWebTestServer()
        {
            var responseData = new byte[200000];
            new Random(123).NextBytes(responseData);
            var body = Encoding.UTF8.GetBytes("<html><body><input type=\"hidden\" name=\"token\" value=\"test\" />" + Convert.ToBase64String(responseData) + "</body></html>");
            this.listener.Start();
            this.Url = "http://127.0.0.1:" + ((IPEndPoint)this.listener.LocalEndpoint).Port + "/";
            this.requests = Task.Run(async () =>
            {
                try
                {
                    while (!this.cancellation.IsCancellationRequested)
                    {
                        using (var client = await this.listener.AcceptTcpClientAsync())
                        using (var stream = client.GetStream())
                        using (var reader = new StreamReader(stream))
                        {
                            client.ReceiveTimeout = 5000;
                            string line;
                            do
                            {
                                line = reader.ReadLine();
                                if (line == null)
                                {
                                    throw new IOException("The web test disconnected before completing its HTTP request.");
                                }
                            }
                            while (line.Length > 0);

                            var header = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n");
                            stream.Write(header, 0, header.Length);
                            stream.Write(body, 0, body.Length);
                            stream.Flush();
                            Interlocked.Increment(ref this.requestCount);
                        }
                    }
                }
                catch (SocketException) when (this.cancellation.IsCancellationRequested)
                {
                }
                catch (ObjectDisposedException) when (this.cancellation.IsCancellationRequested)
                {
                }
            });
        }

        internal string Url { get; }
        internal int RequestCount => this.requestCount;

        public void Dispose()
        {
            this.cancellation.Cancel();
            this.listener.Stop();
            if (!this.requests.Wait(10000))
            {
                throw new TimeoutException("The loopback web test server did not stop.");
            }
            this.cancellation.Dispose();
        }
    }
}
