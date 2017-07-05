// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The AssemblyResolver interface.
    /// </summary>
    public interface IAssemblyResolver : IDisposable
    {
        void SetCurrentDomainAssemblyResolve();

        void RemoveCurrentDomainAssemblyResolve();

        void AddSearchDirectories(IEnumerable<string> directories);
    }
}
