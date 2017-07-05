// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    public class AssemblyResolver : IAssemblyResolver
    {
        public AssemblyResolver(IEnumerable<string> directories)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void AddSearchDirectories(IEnumerable<string> directories)
        {
            throw new NotImplementedException();
        }

        public void SetCurrentDomainAssemblyResolve()
        {
            throw new NotImplementedException();
        }

        public void RemoveCurrentDomainAssemblyResolve()
        {
            throw new NotImplementedException();
        }
    }
}