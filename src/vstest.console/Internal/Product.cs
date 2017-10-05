// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    using System.Reflection;

    public class Product
    {
        public static readonly string Version = GetProductVersion();

        private static string GetProductVersion()
        {
            var attr = typeof(Product)
                .GetTypeInfo()
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attr?.InformationalVersion;
        }
    }
}
