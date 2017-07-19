// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces
{
    using System.Reflection;

    /// <summary>
    /// Abstraction for Assembly Methods
    /// </summary>
    public interface IAssemblyLoadContext
    {
        /// <summary>
        /// Gets the location of current assembly
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <returns>Assembly location</returns>
        string GetAssemblyLocation(Assembly assembly);

        /// <summary>
        /// Loads assembly from given path
        /// </summary>
        /// <param name="assemblyPath">Assembly path</param>
        /// <returns>Assembly from given path</returns>
        Assembly LoadAssemblyFromPath(string assemblyPath);

        /// <summary>
        /// Gets Assembly Name from given path
        /// </summary>
        /// <param name="assemblyPath">Assembly path</param>
        /// <returns>AssemblyName from given path</returns>
        AssemblyName GetAssemblyNameFromPath(string assemblyPath);
    }
}
