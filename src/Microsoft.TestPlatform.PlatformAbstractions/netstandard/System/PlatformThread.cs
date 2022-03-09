// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD && !NETSTANDARD2_0

using System;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <inheritdoc />
public class PlatformThread : IThread
{
    /// <inheritdoc />
    public void Run(Action action, PlatformApartmentState platformApartmentState, bool waitForCompletion)
    {
        throw new NotImplementedException();
    }
}

#endif
