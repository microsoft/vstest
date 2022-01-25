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
        private readonly IDataSerializer dataSerializer;

        /// <summary>
        /// Event used to maintain client connection state
        /// </summary>
        private readonly ManualResetEvent clientConnectedEvent = new(false);

        /// <summary>
        /// Event used to maintain client connection state
        /// </summary>
        private readonly ManualResetEvent clientConnectionAcceptedEvent = new(false);

        /// <summary>
        /// Sync object for sending messages
        /// SendMessage over socket channel is NOT thread-safe
        /// </summary>
        private readonly object sendSyncObject = new();

        /// <summary>
        /// Sync object for receiving messages
        /// </summary>
        private readonly object receiveSyncObject = new();

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
            tcpListener = new TcpListener(endpoint);
            tcpListener.Start();
            EqtTrace.Info("Listening on Endpoint : {0}", (IPEndPoint)tcpListener.LocalEndpoint);

            return (IPEndPoint)tcpListener.LocalEndpoint;
        }

        /// <summary>
        /// Accepts client async
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AcceptClientAsync()
        {
            if (tcpListener != null)
            {
                clientConnectedEvent.Reset();

                var client = await tcpListener.AcceptTcpClientAsync();
                socket = client.Client;
                socket.NoDelay = true;

                // Using Buffered stream only in case of write, and Network stream in case of read.
                var bufferedStream = new PlatformStream().CreateBufferedStream(client.GetStream(), SocketConstants.BufferSize);
                var networkStream = client.GetStream();
                binaryReader = new BinaryReader(networkStream);
                binaryWriter = new BinaryWriter(bufferedStream);

                clientConnectedEvent.Set();
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
            return clientConnectedEvent.WaitOne(clientConnectionTimeout);
        }

        /// <summary>
        /// Stop Listener
        /// </summary>
        public void StopServer()
        {
            tcpListener?.Stop();
            tcpListener = null;
            binaryReader?.Dispose();
            binaryWriter?.Dispose();
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
            clientConnectionAcceptedEvent.Reset();
            EqtTrace.Info("Trying to connect to server on socket : {0} ", endpoint);
            tcpClient = new TcpClient { NoDelay = true };
            socket = tcpClient.Client;

            Stopwatch watch = new();
            watch.Start();
            var connectionTimeout = EnvironmentHelper.GetConnectionTimeout() * 1000;
            do
            {
                try
                {
                    EqtTrace.Verbose("SocketCommunicationManager : SetupClientAsync : Attempting to connect to the server.");
                    await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);

                    if (tcpClient.Connected)
                    {
                        // Using Buffered stream only in case of write, and Network stream in case of read.
                        var bufferedStream = new PlatformStream().CreateBufferedStream(tcpClient.GetStream(), SocketConstants.BufferSize);
                        var networkStream = tcpClient.GetStream();
                        binaryReader = new BinaryReader(networkStream);
                        binaryWriter = new BinaryWriter(bufferedStream);

                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("Connected to the server successfully ");
                            EqtTrace.Info("Using the buffer size of {0} bytes", SocketConstants.BufferSize);
                        }

                        clientConnectionAcceptedEvent.Set();
                    }
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("Connection Failed with error {0}, retrying", ex.ToString());
                }
            }
            while ((tcpClient != null) && !tcpClient.Connected && watch.ElapsedMilliseconds < connectionTimeout);
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
            return clientConnectionAcceptedEvent.WaitOne(connectionTimeout);
        }

        /// <summary>
        /// Stop Listener
        /// </summary>
        public void StopClient()
        {
#if NETFRAMEWORK
            // tcpClient.Close() calls tcpClient.Dispose().
            tcpClient?.Close();
#else
            // tcpClient.Close() not available for netstandard1.5.
            tcpClient?.Dispose();
#endif
            tcpClient = null;
            binaryReader?.Dispose();
            binaryWriter?.Dispose();
        }

        #endregion

        /// <summary>
        /// Writes message to the binary writer.
        /// </summary>
        /// <param name="messageType">Type of Message to be sent, for instance TestSessionStart</param>
        public void SendMessage(string messageType)
        {
            var serializedObject = dataSerializer.SerializeMessage(messageType);
            WriteAndFlushToChannel(serializedObject);
        }

        /// <summary>
        ///  Writes message to the binary writer with payload
        /// </summary>
        /// <param name="messageType">Type of Message to be sent, for instance TestSessionStart</param>
        /// <param name="payload">payload to be sent</param>
        public void SendMessage(string messageType, object payload)
        {
            var rawMessage = dataSerializer.SerializePayload(messageType, payload);
            WriteAndFlushToChannel(rawMessage);
        }

        /// <summary>
        ///  Writes message to the binary writer with payload
        /// </summary>
        /// <param name="messageType">Type of Message to be sent, for instance TestSessionStart</param>
        /// <param name="payload">payload to be sent</param>
        /// <param name="version">version to be sent</param>
        public void SendMessage(string messageType, object payload, int version)
        {
            var rawMessage = dataSerializer.SerializePayload(messageType, payload, version);
            WriteAndFlushToChannel(rawMessage);
        }

        /// <summary>
        /// Send serialized raw message
        /// </summary>
        /// <param name="rawMessage">serialized message</param>
        public void SendRawMessage(string rawMessage)
        {
            WriteAndFlushToChannel(rawMessage);
        }

        /// <summary>
        /// Reads message from the binary reader
        /// </summary>
        /// <returns>Returns message read from the binary reader</returns>
        public Message ReceiveMessage()
        {
            var rawMessage = ReceiveRawMessage();
            return !string.IsNullOrEmpty(rawMessage) ? dataSerializer.DeserializeMessage(rawMessage) : null;
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
            var rawMessage = await ReceiveRawMessageAsync(cancellationToken);
            return !string.IsNullOrEmpty(rawMessage) ? dataSerializer.DeserializeMessage(rawMessage) : null;
        }

        /// <summary>
        /// Reads message from the binary reader
        /// </summary>
        /// <returns> Raw message string </returns>
        public string ReceiveRawMessage()
        {
            lock (receiveSyncObject)
            {
                // Reading message on binaryreader is not thread-safe
                return binaryReader?.ReadString();
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
            var str = await Task.Run(() => TryReceiveRawMessage(cancellationToken));
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
            return dataSerializer.DeserializePayload<T>(message);
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
                    if (socket.Poll(STREAMREADTIMEOUT, SelectMode.SelectRead))
                    {
                        str = ReceiveRawMessage();
                        success = true;
                    }
                }
                catch (IOException ioException)
                {
                    if (ioException.InnerException is SocketException socketException
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
            lock (sendSyncObject)
            {
                binaryWriter?.Write(rawMessage);
                binaryWriter?.Flush();
            }
        }
    }
}
