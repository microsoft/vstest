// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <inheritdoc/>
    public class PlatformStream : IStream
    {
        /// <inheritdoc/>
        public Stream PlatformBufferedStream(Stream stream)
        {
            return stream;
        }

        /// <inheritdoc/>
        public Stream PlatformBufferedStream(Stream stream, int bufferSize)
        {
            return stream;
        }
    }
}