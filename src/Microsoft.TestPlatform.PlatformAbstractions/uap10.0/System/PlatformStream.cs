// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <inheritdoc/>
    public class PlatformStream : IStream
    {
        private static PlatformStream instance;

        /// <summary>
        /// Gets the PlatformStream instance.
        /// </summary>
        public static PlatformStream Instance
        {
            get
            {
                return instance ?? (instance = new PlatformStream());
            }
        }

        /// <inheritdoc/>
        public Stream PlaformBufferedStream(Stream stream)
        {
            return stream;
        }

        /// <inheritdoc/>
        public Stream PlaformBufferedStreamWithBufferSize(Stream stream, int bufferSize)
        {
            return stream;
        }
    }
}