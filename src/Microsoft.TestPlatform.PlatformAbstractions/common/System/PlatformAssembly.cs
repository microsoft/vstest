// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.IO;
    using System.Reflection;

#if !NET451
    using System.Runtime.Loader;
#endif

    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <inheritdoc/>
    public class PlatformAssembly : IAssembly
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
#if NET451
            return AssemblyName.GetAssemblyName(assemblyPath);
#else
            return AssemblyLoadContext.GetAssemblyName(assemblyPath);
#endif
        }

        /// <inheritdoc/>
        public Assembly GetProcessEntryAssembly()
        {
            return Assembly.GetEntryAssembly();
        }

        public Assembly LoadAssemblyFromPath(string assemblyPath)
        {
#if NET451
            return Assembly.LoadFrom(assemblyPath);
#else
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#endif
        }
    }
}
