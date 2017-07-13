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
        /// <summary>
        /// Sets up the Assembly resovler for current Appdomain
        /// </summary>
        void SetCurrentDomainAssemblyResolve();

        /// <summary>
        /// Removes the Assembly resolver from current Appdomain
        /// </summary>
        void RemoveCurrentDomainAssemblyResolve();

        /// <summary>
        /// Add the probing directories look for in case of Aseembly Resolve Event
        /// </summary>
        /// <param name="directories"></param>
        void AddSearchDirectories(IEnumerable<string> directories);
    }
}
