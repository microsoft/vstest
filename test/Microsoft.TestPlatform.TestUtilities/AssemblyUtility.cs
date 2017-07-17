// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities
{
    using System.Reflection;
#if !NET451
    using System.Runtime.Loader;
#endif

    /// <summary>
    /// Assembly utility to perform assembly related functions.
    /// </summary>
    public class AssemblyUtility
    {
        /// <summary>
        /// Gets the assembly name at a given location.
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <returns></returns>
        public static AssemblyName GetAssemblyName(string assemblyPath)
        {
#if !NET451
            return AssemblyLoadContext.GetAssemblyName(assemblyPath);
#else
            return AssemblyName.GetAssemblyName(assemblyPath);
#endif
        }
    }
}
