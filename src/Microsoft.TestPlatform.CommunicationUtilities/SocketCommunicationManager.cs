// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

    /// <summary>
    /// Facilitates communication using sockets
    /// </summary>
    public class SocketCommunicationManager : ICommunicationManager
    {
        /// <summary>
        /// The server stream read timeout constant (in microseconds).
        /// </summary>
        private const int STREAMREADTIMEOUT = 1000 * 1000;

        /// <summary>
        /// TCP Listener to host TCP channel and listen
        /// </summary>
        private TcpListener tcpListener;

        /// <summary>
        /// TCP Client that can connect to a TCP listener
        /// </summary>
        private TcpClient tcpClient;

        /// <summary>
        /// Binary Writer to write to channel stream
        /// </summary>
        private BinaryWriter binaryWriter;

        /// <summary>
        /// Binary reader to read from channel stream
        /// </summary>
        private BinaryReader binaryReader;

        /// <summary>
        /// Serializer for the data objects
        /// </summary>
        private IDataSerializer dataSerializer;

        /// <summary>
        /// Event used to maintain client connection state
        /// </summary>
        private ManualResetEvent clientConnectedEvent = new ManualResetEvent(false);

        /// <summary>
        /// Event used to maintain client connection state
        /// </summary>
        private ManualResetEvent clientConnectionAcceptedEvent = new ManualResetEvent(false);

        /// <summary>
        /// Sync object for sending messages
        /// SendMessage over socket channel is NOT thread-safe
        /// </summary>
        private object sendSyncObject = new object();

        /// <summary>
        /// Sync object for receiving messages
        /// </summary>
        private object receiveSyncObject = new object();

        private Socket socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketCommunicationManager"/> class.
        /// </summary>
        public SocketCommunicationManager()
            : this(JsonDataSerializer.Instance)
        {
        }

        internal SocketCommunicationManager(IDataSerializer dataSerializer)
        {
            this.dataSerializer = dataSerializer;
        }

        #region ServerMethods

        /// <summary>
        /// Host TCP Socket Server and start listening
        /// </summary>
        /// <param name="endpoint">End point where server is hosted</param>
        /// <returns>Port of the listener</returns>
        public IPEndPoint HostServer(IPEndPoint endpoint)
        {
            this.tcpListener = new TcpListener(endpoint);
            this.tcpListener.Start();
            EqtTrace.Info("Listening on Endpoint : {0}", (IPEndPoint)this.tcpListener.LocalEndpoint);

            return (IPEndPoint)this.tcpListener.LocalEndpoint;
        }

        /// <summary>
        /// Accepts client async
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AcceptClientAsync()
        {
            if (this.tcpListener != null)
            {
                this.clientConnectedEvent.Reset();

                var client = await this.tcpListener.AcceptTcpClientAsync();
                this.socket = client.Client;
                this.socket.NoDelay = true;

                // Using Buffered stream only in case of write, and Network stream in case of read.
                var bufferedStream = new PlatformStream().CreateBufferedStream(client.GetStream(), SocketConstants.BufferSize);
                var networkStream = client.GetStream();
                this.binaryReader = new BinaryReader(networkStream);
                this.binaryWriter = new BinaryWriter(bufferedStream);

                this.clientConnectedEvent.Set();
                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("Using the buffer size of {0} bytes", SocketConstants.BufferSize);
                    EqtTrace.Info("Accepted Client request and set the flag");
                }
            }
        }

        /// <summary>
        /// Waits for Client Connection
        /// </summary>
        /// <param name="clientConnectionTimeout">Time to Wait for the connection</param>
        /// <returns>True if Client is connected, false otherwise</returns>
        public bool WaitForClientConnection(int clientConnectionTimeout)
        {
            return this.clientConnectedEvent.WaitOne(clientConnectionTimeout);
        }

        /// <summary>
        /// Stop Listener
        /// </summary>
        public void StopServer()
        {
            this.tcpListener?.Stop();
            this.tcpListener = null;
            this.binaryReader?.Dispose();
            this.binaryWriter?.Dispose();
        }

        #endregion

        #region ClientMethods

        /// <summary>
        /// Connects to server async
        /// </summary>
        /// <param name="endpoint">EndPointAddress for client to connect</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SetupClientAsync(IPEndPoint endpoint)
        {
            // TODO: pass cancellation token, if user cancels the operation, so we don't wait 50 secs to connect
            // for now added a check for validation of this.tcpclient
            this.clientConnectionAcceptedEvent.Reset();
            EqtTrace.Info("Trying to connect to server on socket : {0} ", endpoint);
            this.tcpClient = new TcpClient { NoDelay = true };
            this.socket = this.tcpClient.Client;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            var connectionTimeout = EnvironmentHelper.GetConnectionTimeout() * 1000;
            do
            {
                try
                {
                    EqtTrace.Verbose("SocketCommunicationManager : SetupClientAsync : Attempting to connect to the server.");
                    await this.tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);

                    if (this.tcpClient.Connected)
                    {
                        // Using Buffered stream only in case of write, and Network stream in case of read.
                        var bufferedStream = new PlatformStream().CreateBufferedStream(this.tcpClient.GetStream(), SocketConstants.BufferSize);
                        var networkStream = this.tcpClient.GetStream();
                        this.binaryReader = new BinaryReader(networkStream);
                        this.binaryWriter = new BinaryWriter(bufferedStream);

                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("Connected to the server successfully ");
                            EqtTrace.Info("Using the buffer size of {0} bytes", SocketConstants.BufferSize);
                        }

                        this.clientConnectionAcceptedEvent.Set();
                    }
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("Connection Failed with error {0}, retrying", ex.ToString());
                }
            }
            while ((this.tcpClient != null) && !this.tcpClient.Connected && watch.ElapsedMilliseconds < connectionTimeout);
        }

        /// <summary>
        /// Waits for server to be connected
        /// Whoever creating the client and trying to connect to a server
        /// should use this method to wait for connection to be established with server
        /// </summary>
        /// <param name="connectionTimeout">Time to wait for the connection</param>
        /// <returns>True, if Server got a connection from client</returns>
        public bool WaitForServerConnection(int connectionTimeout)
        {
            return this.clientConnectionAcceptedEvent.WaitOne(connectionTimeout);
        }

        /// <summary>
        /// Stop Listener
        /// </summary>
        public void StopClient()
        {
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            this.tcpClient?.Close();
#else
            // tcpClient.Close() not available for netstandard1.5.
            this.tcpClient?.Dispose();
#endif
            this.tcpClient = null;
            this.binaryReader?.Dispose();
            this.binaryWriter?.Dispose();
        }

        #endregion

        /// <summary>
        /// Writes message to the binary writer.
        /// </summary>
        /// <param name="messageType">Type of Message to be sent, for instance TestSessionStart</param>
        public void SendMessage(string messageType)
        {
            var serializedObject = this.dataSerializer.SerializeMessage(messageType);
            this.WriteAndFlushToChannel(serializedObject);
        }

        /// <summary>
        ///  Writes message to the binary writer with payload
        /// </summary>
        /// <param name="messageType">Type of Message to be sent, for instance TestSessionStart</param>
        /// <param name="payload">payload to be sent</param>
        public void SendMessage(string messageType, object payload)
        {
            var rawMessage = this.dataSerializer.SerializePayload(messageType, payload);
            this.WriteAndFlushToChannel(rawMessage);
        }

        /// <summary>
        ///  Writes message to the binary writer with payload
        /// </summary>
        /// <param name="messageType">Type of Message to be sent, for instance TestSessionStart</param>
        /// <param name="payload">payload to be sent</param>
        /// <param name="version">version to be sent</param>
        public void SendMessage(string messageType, object payload, int version)
        {
            var rawMessage = this.dataSerializer.SerializePayload(messageType, payload, version);
            this.WriteAndFlushToChannel(rawMessage);
        }

        /// <summary>
        /// Send serialized raw message
        /// </summary>
        /// <param name="rawMessage">serialized message</param>
        public void SendRawMessage(string rawMessage)
        {
            this.WriteAndFlushToChannel(rawMessage);
        }

        /// <summary>
        /// Reads message from the binary reader
        /// </summary>
        /// <returns>Returns message read from the binary reader</returns>
        public Message ReceiveMessage()
        {
            var rawMessage = this.ReceiveRawMessage();
            if (!string.IsNullOrEmpty(rawMessage))
            {
                return this.dataSerializer.DeserializeMessage(rawMessage);
            }

            return null;
        }

        /// <summary>
        /// Reads message from the binary reader using read timeout
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation Token.
        /// </param>
        /// <returns>
        /// Returns message read from the binary reader
        /// </returns>
        public async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            var rawMessage = await this.ReceiveRawMessageAsync(cancellationToken);
            if (!string.IsNullOrEmpty(rawMessage))
            {
                return this.dataSerializer.DeserializeMessage(rawMessage);
            }

            return null;
        }

        /// <summary>
        /// Reads message from the binary reader
        /// </summary>
        /// <returns> Raw message string </returns>
        public string ReceiveRawMessage()
        {
            lock (this.receiveSyncObject)
            {
                // Reading message on binaryreader is not thread-safe
                return this.binaryReader?.ReadString();
            }
        }

        /// <summary>
        /// Reads message from the binary reader using read timeout
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation Token.
        /// </param>
        /// <returns>
        /// Raw message string
        /// </returns>
        public async Task<string> ReceiveRawMessageAsync(CancellationToken cancellationToken)
        {
            var str = await Task.Run(() => this.TryReceiveRawMessage(cancellationToken));
            return str;
        }

        /// <summary>
        /// Deserializes the Message into actual TestPlatform objects
        /// </summary>
        /// <typeparam name="T"> The type of object to deserialize to. </typeparam>
        /// <param name="message"> Message object </param>
        /// <returns> TestPlatform object </returns>
        public T DeserializePayload<T>(Message message)
        {
            return this.dataSerializer.DeserializePayload<T>(message);
        }

        private string TryReceiveRawMessage(CancellationToken cancellationToken)
        {
            string str = null;
            bool success = false;

            // Set read timeout to avoid blocking receive raw message
            while (!cancellationToken.IsCancellationRequested && !success)
            {
                try
                {
                    if (this.socket.Poll(STREAMREADTIMEOUT, SelectMode.SelectRead))
                    {
                        str = this.ReceiveRawMessage();
                        success = true;
                    }
                }
                catch (IOException ioException)
                {
                    var socketException = ioException.InnerException as SocketException;
                    if (socketException != null
                        && socketException.SocketErrorCode == SocketError.TimedOut)
                    {
                        EqtTrace.Info(
                            "SocketCommunicationManager ReceiveMessage: failed to receive message because read timeout {0}",
                            ioException);
                    }
                    else
                    {
                        EqtTrace.Error(
                            "SocketCommunicationManager ReceiveMessage: failed to receive message {0}",
                            ioException);
                        break;
                    }
                }
                catch (Exception exception)
                {
                    EqtTrace.Error(
                        "SocketCommunicationManager ReceiveMessage: failed to receive message {0}",
                        exception);
                    break;
                }
            }

            return str;
        }

        /// <summary>
        /// Writes the data on socket and flushes the buffer
        /// </summary>
        /// <param name="rawMessage">message to write</param>
        private void WriteAndFlushToChannel(string rawMessage)
        {
            // Writing Message on binarywriter is not Thread-Safe
            // Need to sync one by one to avoid buffer corruption
            lock (this.sendSyncObject)
            {
                this.binaryWriter?.Write(rawMessage);
                this.binaryWriter?.Flush();
            }
        }
    }
}
