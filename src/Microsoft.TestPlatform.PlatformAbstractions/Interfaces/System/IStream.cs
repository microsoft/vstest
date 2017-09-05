// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces
{
    using System.IO;

    /// <summary>
    /// Helper class to return plaform specific stream.
    /// </summary>
    public interface IStream
    {
        /// <summary>
        /// Returns plarform specific Buffered Stream
        /// </summary>
        /// <param name="stream">Input Stream</param>
        /// <returns>Buffered Stream</returns>
        Stream PlatformBufferedStream(Stream stream);

        /// <summary>
        /// Returns platrform specific Buffered Stream
        /// </summary>
        /// <param name="stream">Input Stream</param>
        /// <param name="bufferSize">Buffer Size</param>
        /// <returns>Buffered Stream</returns>
        Stream PlatformBufferedStream(Stream stream, int bufferSize);
    }
}