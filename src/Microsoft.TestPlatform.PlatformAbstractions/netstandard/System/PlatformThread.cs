// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD && !NETSTANDARD2_0

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

using System;

using Interfaces;

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
