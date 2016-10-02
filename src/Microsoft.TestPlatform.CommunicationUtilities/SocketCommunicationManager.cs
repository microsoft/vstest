// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Facilitates communication using sockets
    /// </summary>    
    public class SocketCommunicationManager : ICommunicationManager
    {
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

        private NetworkStream serverStream;

        public SocketCommunicationManager(): this(JsonDataSerializer.Instance)
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
        /// <returns></returns>
        public int HostServer()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            this.tcpListener = new TcpListener(endpoint);
            this.tcpListener.Start();

            var portNumber = ((IPEndPoint)this.tcpListener.LocalEndpoint).Port;
            EqtTrace.Info("Listening on port : {0}", portNumber);

            return portNumber;
        }

        /// <summary>
        /// Accepts client async
        /// </summary>
        public async Task AcceptClientAsync()
        {

            if (this.tcpListener != null)
            {
                this.clientConnectedEvent.Reset();

                var client = await this.tcpListener.AcceptTcpClientAsync();

                this.serverStream = client.GetStream();

                this.binaryReader = new BinaryReader(this.serverStream);
                this.binaryWriter = new BinaryWriter(this.serverStream);

                this.clientConnectedEvent.Set();

                EqtTrace.Info("Accepted Client request and set the flag");
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
            this.binaryReader.Dispose();
            this.binaryWriter.Dispose();
        }

        #endregion

        #region ClientMethods

        /// <summary>
        /// Connects to server async
        /// </summary>
        public async Task SetupClientAsync(int portNumber)
        {
            this.clientConnectionAcceptedEvent.Reset();
            EqtTrace.Info("Trying to connect to server on port : {0}", portNumber);
            this.tcpClient = new TcpClient();
            await this.tcpClient.ConnectAsync(IPAddress.Loopback, portNumber);
            var networkStream = this.tcpClient.GetStream();
            this.binaryReader = new BinaryReader(networkStream);
            this.binaryWriter = new BinaryWriter(networkStream);
            this.clientConnectionAcceptedEvent.Set();
            EqtTrace.Info("Connected to the server successfully ");
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
            this.tcpClient?.Dispose();
            this.tcpClient = null;
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
        /// Reads message from the binary reader
        /// </summary>
        /// <returns>Returns message read from the binary reader</returns>
        public Message ReceiveMessage()
        {
            var rawMessage = this.ReceiveRawMessage();
            return this.dataSerializer.DeserializeMessage(rawMessage);
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
        /// The send hand shake message.
        /// </summary>
        public void SendHandShakeMessage()
        {
            this.SendMessage(MessageType.SessionStart);
        }

        /// <summary>
        /// Reads message from the binary reader
        /// </summary>
        /// <returns> Raw message string </returns>
        public string ReceiveRawMessage()
        {
            return this.binaryReader.ReadString();
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
        /// Deserializes the Message into actual TestPlatform objects
        /// </summary>
        /// <typeparam name="T"> The type of object to deserialize to. </typeparam>
        /// <param name="message"> Message object </param>
        /// <returns> TestPlatform object </returns>
        public T DeserializePayload<T>(Message message)
        {
            return this.dataSerializer.DeserializePayload<T>(message);
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
        public Message ReceiveMessage(CancellationToken cancellationToken)
        {
            var rawMessage = this.ReceiveRawMessage(cancellationToken);
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
        /// Raw message string 
        /// </returns>
        public string ReceiveRawMessage(CancellationToken cancellationToken)
        {
            string str = null;
            bool success = false;
            while (!cancellationToken.IsCancellationRequested && !success)
            {
                this.serverStream.ReadTimeout = 200;

                try
                {
                    str = this.ReceiveRawMessage();
                    success = true;
                }
                catch (Exception)
                {
                    // Ingore timeoutexception
                }
            }

            this.serverStream.ReadTimeout = -1;

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
                this.binaryWriter.Write(rawMessage);
                this.binaryWriter.Flush();
            }
        }
    }
}
