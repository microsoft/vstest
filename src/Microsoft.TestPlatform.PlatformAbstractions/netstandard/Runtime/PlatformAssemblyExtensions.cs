// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Reflection;

    public static class PlatformAssemblyExtensions
    {
        public static string GetAssemblyLocation(this Assembly assembly)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
