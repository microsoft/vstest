// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LengthPrefixCommunicationChannelTests : IDisposable
    {
        private const string DUMMYDATA = "Dummy Data";

        private readonly ICommunicationChannel channel;

        private readonly MemoryStream stream;

        private readonly BinaryReader reader;

        private readonly BinaryWriter writer;

        public LengthPrefixCommunicationChannelTests()
        {
            this.stream = new MemoryStream();
            this.channel = new LengthPrefixCommunicationChannel(this.stream);

            this.reader = new BinaryReader(this.stream);
            this.writer = new BinaryWriter(this.stream);
        }

        public void Dispose()
        {
            this.stream.Dispose();

            this.reader.Dispose();
            this.writer.Dispose();
        }

        [TestMethod]
        public async Task SendShouldWriteTheDataOnStream()
        {
            await this.channel.Send(DUMMYDATA);

            SeekToBeginning(this.stream);
            Assert.AreEqual(DUMMYDATA, this.reader.ReadString());
        }

        [TestMethod]
        public async Task SendShouldWriteInLengthPrefixedFormat()
        {
            await this.channel.Send(DUMMYDATA);

            SeekToBeginning(this.stream);
            Assert.AreEqual(DUMMYDATA.Length, Read7BitEncodedInt(this.reader));
        }

        [TestMethod]
        public async Task SendShouldBeAbleToWriteUnicodeData()
        {
            // Every day is a good day
            var utf8Data = "日日是好日";
            await this.channel.Send(utf8Data);

            SeekToBeginning(this.stream);
            Assert.AreEqual(utf8Data, this.reader.ReadString());
        }

        [TestMethod]
        public async Task SendShouldFlushTheStream()
        {
            // A buffered stream doesn't immediately flush, it waits until buffer is filled in
            using (var bufferedStream = new BufferedStream(this.stream, 2048))
            {
                var communicationChannel = new LengthPrefixCommunicationChannel(bufferedStream);

                await communicationChannel.Send("a");

                SeekToBeginning(this.stream);
                Assert.AreEqual("a", this.reader.ReadString());
            }
        }

        [TestMethod]
        public void SendShouldThrowIfChannelIsDisconnected()
        {
            this.stream.Dispose();

            Assert.ThrowsException<CommunicationException>(() => this.channel.Send(DUMMYDATA).Wait());
        }

        [TestMethod]
        public async Task MessageReceivedShouldProvideDataOverStream()
        {
            var data = string.Empty;
            this.channel.MessageReceived += (sender, messageEventArgs) =>
            {
                data = messageEventArgs.Data;
            };
            this.writer.Write(DUMMYDATA);
            SeekToBeginning(this.stream);

            await this.channel.NotifyDataAvailable();

            Assert.AreEqual(DUMMYDATA, data);
        }

        [TestMethod]
        public async Task NotifyDataAvailableShouldReadStreamIfNoListenersAreRegistered()
        {
            this.writer.Write(DUMMYDATA);
            SeekToBeginning(this.stream);

            await this.channel.NotifyDataAvailable();

            // Data is read irrespective of listeners. See note in NotifyDataAvailable
            // implementation.
            Assert.AreEqual(11, this.stream.Position);
        }

        [TestMethod]
        public void DisposeShouldNotCloseTheStream()
        {
            this.channel.Dispose();

            // Should throw if stream is disposed.
            Assert.IsTrue(this.stream.CanWrite);
        }

        // TODO
        // WriteFromMultilpleThreadShouldBeInSequence
        private static void SeekToBeginning(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        private static int Read7BitEncodedInt(BinaryReader reader)
        {
            // Copied from BinaryReader.Read7BitEncodedInt
            // https://referencesource.microsoft.com/#mscorlib/system/io/binaryreader.cs,f30b8b6e8ca06e0f
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                // 5 bytes max per Int32, shift += 7
                if (shift == 5 * 7)
                {
                    throw new FormatException("Format_Bad7BitInt32");
                }

                // ReadByte handles end of stream cases for us.
                b = reader.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);

            return count;
        }
    }
}
