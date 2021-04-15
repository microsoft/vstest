// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AdapterUtilities
{
    using System;
    using System.Text;

    public class TestIdProvider
    {
        internal const int BlockBits = 512;
        internal const int DigestBits = 160;
        internal const int BlockBytes = BlockBits / 8;
        internal const int DigestBytes = DigestBits / 8;

        private Guid id = Guid.Empty;
        private byte[] hash = null;
        private byte[] lastBlock = new byte[BlockBytes];
        private int position = 0;

        private readonly Sha1Implementation hasher;

        public TestIdProvider()
        {
            hasher = new Sha1Implementation();
        }

        public void AppendString(string str)
        {
            if (hash != null)
            {
                throw new InvalidOperationException();
            }

            var bytes = Encoding.Unicode.GetBytes(str);
            var end = Math.Min(BlockBytes - position, bytes.Length);

            Buffer.BlockCopy(bytes, 0, lastBlock, position, end);

            // Block length is not reached yet.
            if (end + position < BlockBytes)
            {
                position += end;
                return;
            }

            hasher.ProcessBlock(lastBlock, 0, lastBlock.Length);
            position = 0;

            // We proccessed the entire string already
            if (end == bytes.Length)
            {
                return;
            }

            int start = 0;
            while (end < bytes.Length)
            {
                start = end;
                end += BlockBytes;
                if (end > bytes.Length)
                {
                    break;
                }

                hasher.ProcessBlock(bytes, start, end - start);
            }

            if (end > bytes.Length)
            {
                position = bytes.Length - start;
                Buffer.BlockCopy(bytes, start, lastBlock, 0, position);
            }
        }

        public byte[] GetHash()
        {
            if (hash != null)
            {
                return hash;
            }

            if (position != 0)
            {
                hasher.PadMessage(ref lastBlock, position);
                hasher.ProcessBlock(lastBlock, 0, lastBlock.Length);
            }

            hash = hasher.ProcessFinalBlock();

            return hash;
        }

        public Guid GetId()
        {
            if (id != Guid.Empty)
            {
                return id;
            }

            var toGuid = new byte[16];
            Array.Copy(GetHash(), toGuid, 16);
            id = new Guid(toGuid);

            return id;
        }

        /// <summary>
        /// SHA-1 Implementation as in https://tools.ietf.org/html/rfc3174
        /// </summary>
        /// <remarks>
        /// This implementation only works with messages with a length 
        /// that is a multiple of the size of 8-bits.
        /// </remarks>
        internal class Sha1Implementation
        {
            /* 
             * Many of the variable, function and parameter names in this code 
             * were used because those were the names used in the publication.
             * 
             * For more information please refer to https://tools.ietf.org/html/rfc3174.
             */

            private int streamSize = 0;
            private bool messagePadded = false;

            public Sha1Implementation()
            {
                Reset();
            }

            /// <summary>
            /// A sequence of logical functions to be used in SHA-1. 
            /// Each f(t), 0 <= t <= 79, operates on three 32-bit words B, C, D and produces a 32-bit word as output.  
            /// </summary>
            /// <param name="t">Function index. 0 <= t <= 79</param>
            /// <param name="B">Word B</param>
            /// <param name="C">Word C</param>
            /// <param name="D">Word D</param>
            /// <returns>
            /// f(t;B,C,D) = (B AND C) OR ((NOT B) AND D)         ( 0 <= t <= 19)
            /// f(t;B,C,D) = B XOR C XOR D                        (20 <= t <= 39)
            /// f(t;B,C,D) = (B AND C) OR (B AND D) OR (C AND D)  (40 <= t <= 59)
            /// f(t;B,C,D) = B XOR C XOR D                        (60 <= t <= 79)
            /// </returns>
            private static uint F(int t, uint B, uint C, uint D)
            {
                if (t >= 0 && t <= 19)
                {
                    return (B & C) | (~B & D);
                }
                else if ((t >= 20 && t <= 39) || (t >= 60 && t <= 79))
                {
                    return B ^ C ^ D;
                }
                else if (t >= 40 && t <= 59)
                {
                    return (B & C) | (B & D) | (C & D);
                }
                else
                {
                    throw new ArgumentException("Argument out of bounds! 0 <= t < 80", nameof(t));
                }
            }

            /// <summary>
            /// Returns a constant word K(t) which is used in the SHA-1.
            /// </summary>
            /// <param name="t">Word index.</param>
            /// <returns>
            /// K(t) = 0x5A827999 ( 0 <= t <= 19)
            /// K(t) = 0x6ED9EBA1 (20 <= t <= 39)
            /// K(t) = 0x8F1BBCDC (40 <= t <= 59)
            /// K(t) = 0xCA62C1D6 (60 <= t <= 79)
            /// </returns>
            private static uint K(int t)
            {
                if (t >= 0 && t <= 19)
                {
                    return 0x5A827999u;
                }
                else if (t >= 20 && t <= 39)
                {
                    return 0x6ED9EBA1u;
                }
                else if (t >= 40 && t <= 59)
                {
                    return 0x8F1BBCDCu;
                }
                else if (t >= 60 && t <= 79)
                {
                    return 0xCA62C1D6u;
                }
                else
                {
                    throw new ArgumentException("Argument out of bounds! 0 <= t < 80", nameof(t));
                }
            }

            /// <summary>
            /// The circular left shift operation.
            /// </summary>
            /// <param name="x">An uint word.</param>
            /// <param name="n">0 <= n < 32</param>
            /// <returns>S^n(X)  =  (X << n) OR (X >> 32-n)</returns>
            private static uint S(uint X, byte n)
            {
                if (n > 32)
                {
                    throw new ArgumentOutOfRangeException(nameof(n));
                }

                return (X << n) | (X >> (32 - n));
            }

            /// <summary>
            /// Ensures that given bytes are in big endian notation.
            /// </summary>
            /// <param name="array">An array of bytes</param>
            private static void EnsureBigEndian(ref byte[] array)
            {
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(array);
                }
            }

            private readonly uint[] H = new uint[5];

            private void Reset()
            {
                streamSize = 0;
                messagePadded = false;

                // as defined in https://tools.ietf.org/html/rfc3174#section-6.1
                H[0] = 0x67452301u;
                H[1] = 0xEFCDAB89u;
                H[2] = 0x98BADCFEu;
                H[3] = 0x10325476u;
                H[4] = 0xC3D2E1F0u;
            }

            public byte[] ComputeHash(byte[] message)
            {
                Reset();
                streamSize = 0;
                PadMessage(ref message);

                ProcessBlock(message, 0, message.Length);

                return ProcessFinalBlock();
            }

            private void ProcessMultipleBlocks(byte[] message)
            {
                var messageCount = message.Length / BlockBytes;
                for (var i = 0; i < messageCount; i += 1)
                {
                    ProcessBlock(message, i * BlockBytes, BlockBytes);
                }
            }

            public byte[] ProcessFinalBlock()
            {
                if (!messagePadded)
                {
                    var pad = new byte[0];
                    PadMessage(ref pad, 0);
                    ProcessBlock(pad, 0, pad.Length);
                }

                var digest = new byte[DigestBytes];
                for (int t = 0; t < H.Length; t++)
                {
                    var hi = BitConverter.GetBytes(H[t]);
                    EnsureBigEndian(ref hi);

                    Buffer.BlockCopy(hi, 0, digest, t * hi.Length, hi.Length);
                }

                return digest;
            }

            public void PadMessage(ref byte[] message, int length = 0)
            {
                if (messagePadded)
                {
                    throw new InvalidOperationException();
                }

                if (length == 0)
                {
                    length = message.Length;
                }
                else
                {
                    Array.Resize(ref message, length);
                }

                streamSize += length;

                var paddingBytes = BlockBytes - (length % BlockBytes);

                // 64bit uint message size will be appended to end of the padding, making sure we have space for it.
                if (paddingBytes <= 8)
                    paddingBytes += BlockBytes;

                var padding = new byte[paddingBytes];
                padding[0] = 0b10000000;

                var messageBits = (ulong)streamSize << 3;
                var messageSize = BitConverter.GetBytes(messageBits);
                EnsureBigEndian(ref messageSize);

                Buffer.BlockCopy(messageSize, 0, padding, padding.Length - messageSize.Length, messageSize.Length);

                Array.Resize(ref message, message.Length + padding.Length);
                Buffer.BlockCopy(padding, 0, message, length, padding.Length);

                messagePadded = true;
            }

            public void ProcessBlock(byte[] message, int start, int length)
            {
                if (start + length > message.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(length));
                }
                if (length % BlockBytes != 0)
                {
                    throw new ArgumentException($"Invalid block size. Actual: {length}, Expected: Multiples of {BlockBytes}", nameof(length));
                }
                if (length != BlockBytes)
                {
                    ProcessMultipleBlocks(message);
                    return;
                }

                streamSize += BlockBytes;
                var W = new uint[80];

                // Get W(0) .. W(15)
                for (int t = 0; t <= 15; t++)
                {
                    var wordBytes = new byte[sizeof(uint)];
                    Buffer.BlockCopy(message, start + (t * sizeof(uint)), wordBytes, 0, sizeof(uint));
                    EnsureBigEndian(ref wordBytes);

                    W[t] = BitConverter.ToUInt32(wordBytes, 0);
                }

                // Calculate W(16) .. W(79)
                for (int t = 16; t <= 79; t++)
                {
                    W[t] = S(W[t - 3] ^ W[t - 8] ^ W[t - 14] ^ W[t - 16], 1);
                }

                uint A = H[0],
                     B = H[1],
                     C = H[2],
                     D = H[3],
                     E = H[4];

                for (int t = 0; t < 80; t++)
                {
                    var temp = S(A, 5) + F(t, B, C, D) + E + W[t] + K(t);
                    E = D;
                    D = C;
                    C = S(B, 30);
                    B = A;
                    A = temp;
                }

                H[0] += A;
                H[1] += B;
                H[2] += C;
                H[3] += D;
                H[4] += E;
            }
        }
    }
}
