﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || NETCOREAPP || NETSTANDARD2_0

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

using System.IO;

using Interfaces;

/// <inheritdoc/>
public class PlatformStream : IStream
{
    /// <inheritdoc/>
    public Stream CreateBufferedStream(Stream stream, int bufferSize)
    {
        return new BufferedStream(stream, bufferSize);
    }
}

#endif
