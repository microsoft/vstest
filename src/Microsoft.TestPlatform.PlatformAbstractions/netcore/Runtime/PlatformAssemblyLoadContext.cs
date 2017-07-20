// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;

    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <inheritdoc/>
    public class PlatformAssemblyLoadContext : IAssemblyLoadContext
    {
        /// <inheritdoc/>
        public string GetAssemblyLocation(Assembly assembly)
        {
            Type type = assembly.GetType();
            PropertyInfo property = type.GetTypeInfo().GetDeclaredProperty("Location");
            return Path.GetDirectoryName(property.GetMethod.Invoke(assembly, null) as string);
        }

        /// <inheritdoc/>
        public AssemblyName GetAssemblyNameFromPath(string assemblyPath)
        {
            return AssemblyLoadContext.GetAssemblyName(assemblyPath);
        }

        public Assembly LoadAssemblyFromPath(string assemblyPath)
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        }
    }
}
