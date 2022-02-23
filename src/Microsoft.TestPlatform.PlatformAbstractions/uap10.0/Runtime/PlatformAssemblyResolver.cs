// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

#if WINDOWS_UWP

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <inheritdoc/>
public class PlatformAssemblyResolver : IAssemblyResolver
{
    public PlatformAssemblyResolver()
    {
    }

    /// <inheritdoc/>
    public event AssemblyResolveEventHandler AssemblyResolve;

    public void Dispose()
    {
    }

    private void DummyEventThrower()
    {
        // need to raise this event, else compiler throws error
        AssemblyResolve(this, null);
    }
}

#endif
