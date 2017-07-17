// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

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
            this.AssemblyResolve(this, null);
        }
    }
}