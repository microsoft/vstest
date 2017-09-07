// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// A communication channel using a length prefix packet frame for communication.
    /// </summary>
    public class LengthPrefixCommunicationChannel : ICommunicationChannel
    {
        private readonly BinaryReader reader;

        private readonly BinaryWriter writer;

        public LengthPrefixCommunicationChannel(Stream stream)
        {
            this.reader = new BinaryReader(stream, Encoding.UTF8, true);

            // Using the Buffered stream while writing, improves the write performance. By reducing the number of writes.
            this.writer = new BinaryWriter(new PlatformStream().CreateBufferedStream(stream, SocketConstants.BufferSize), Encoding.UTF8, true);
        }

        /// <inheritdoc />
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <inheritdoc />
        public Task Send(string data)
        {
            try
            {
                this.writer.Write(data);
                this.writer.Flush();
            }
            catch (Exception ex)
            {
                EqtTrace.Verbose("LengthPrefixCommunicationChannel: Error sending data: {0}.", ex);
                throw new CommunicationException("Unable to send data over channel.", ex);
            }

            return Task.FromResult(0);
        }

        /// <inheritdoc />
        public Task NotifyDataAvailable()
        {
            // Try read data even if no one is listening to the data stream. Some server
            // implementations (like Sockets) depend on the read operation to determine if a
            // connection is closed.
            var data = this.reader.ReadString();

            if (this.MessageReceived != null)
            {
                this.MessageReceived.SafeInvoke(this, new MessageReceivedEventArgs { Data = data }, "LengthPrefixCommunicationChannel: MessageReceived");
            }

            return Task.FromResult(0);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.reader.Dispose();
            this.writer.Dispose();
        }
    }
}
