// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

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
            this.AssemblyResolve(this, null);
        }
    }
}

#endif
