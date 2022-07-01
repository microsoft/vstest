// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD && !NETSTANDARD2_0

using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <inheritdoc/>
public class PlatformAssemblyResolver : IAssemblyResolver
{
    public PlatformAssemblyResolver()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public event AssemblyResolveEventHandler? AssemblyResolve;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        throw new NotImplementedException();
    }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Required to avoid compilation warning about unused event")]
    private void DummyEventThrower()
    {
        AssemblyResolve?.Invoke(this, null);
    }
}

#endif
