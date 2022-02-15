﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

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
        return stream;
    }
}

#endif
