// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

#if NETSTANDARD && !NETSTANDARD2_0

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <inheritdoc/>
public class PlatformAssemblyResolver : IAssemblyResolver
{
    public PlatformAssemblyResolver()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public event AssemblyResolveEventHandler AssemblyResolve;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    private void DummyEventThrower()
    {
        AssemblyResolve(this, null);
    }
}

#endif
