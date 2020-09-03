// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

    /// <summary>
    ///     The portable symbol reader.
    /// </summary>
    internal class PortableSymbolReader : ISymbolReader
    {
        /// <summary>
        ///     Key in first dict is Type FullName
        ///     Key in second dict is method name
        /// </summary>
        private Dictionary<string, Dictionary<string, DiaNavigationData>> methodsNavigationDataForType =
            new Dictionary<string, Dictionary<string, DiaNavigationData>>();

        /// <summary>
        /// The cache symbols.
        /// </summary>
        /// <param name="binaryPath">
        /// The binary path.
        /// </param>
        /// <param name="searchPath">
        /// The search path.
        /// </param>
        public void CacheSymbols(string binaryPath, string searchPath)
        {
            this.PopulateCacheForTypeAndMethodSymbols(binaryPath);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            foreach (var methodsNavigationData in this.methodsNavigationDataForType.Values)
            {
                methodsNavigationData.Clear();
            }

            this.methodsNavigationDataForType.Clear();
            this.methodsNavigationDataForType = null;
        }

        /// <summary>
        /// The get navigation data.
        /// </summary>
        /// <param name="declaringTypeName">
        /// The declaring type name.
        /// </param>
        /// <param name="methodName">
        /// The method name.
        /// </param>
        /// <returns>
        /// The <see cref="INavigationData"/>.
        /// </returns>
        public INavigationData GetNavigationData(string declaringTypeName, string methodName)
        {
            INavigationData navigationData = null;
            if (this.methodsNavigationDataForType.ContainsKey(declaringTypeName))
            {
                var methodDict = this.methodsNavigationDataForType[declaringTypeName];
                if (methodDict.ContainsKey(methodName))
                {
                    navigationData = methodDict[methodName];
                }
            }

            return navigationData;
        }

        /// <summary>
        /// The populate cache for type and method symbols.
        /// </summary>
        /// <param name="binaryPath">
        /// The binary path.
        /// </param>
        private void PopulateCacheForTypeAndMethodSymbols(string binaryPath)
        {
            try
            {
                var pdbFilePath = Path.ChangeExtension(binaryPath, ".pdb");
                using (var pdbReader = new PortablePdbReader(new FileHelper().GetStream(pdbFilePath, FileMode.Open, FileAccess.Read)))
                {
                    // At this point, the assembly should be already loaded into the load context. We query for a reference to
                    // find the types and cache the symbol information. Let the loader follow default lookup order instead of
                    // forcing load from a specific path.
                    Assembly asm;
                    try
                    {
                        asm = Assembly.Load(new PlatformAssemblyLoadContext().GetAssemblyNameFromPath(binaryPath));
                    }
                    catch (FileNotFoundException)
                    {
#if !NETSTANDARD1_3
                        // fallback when the assembly is not loaded
                        asm = Assembly.LoadFile(binaryPath);
#else
                        // fallback is not supported
                        throw;
#endif
                    }

                    foreach (var type in asm.GetTypes())
                    {
                        // Get declared method infos
                        var methodInfoList = type.GetTypeInfo().DeclaredMethods;
                        var methodsNavigationData = new Dictionary<string, DiaNavigationData>();

                        foreach (var methodInfo in methodInfoList)
                        {
                            var diaNavigationData = pdbReader.GetDiaNavigationData(methodInfo);
                            if (diaNavigationData != null)
                            {
                                methodsNavigationData[methodInfo.Name] = diaNavigationData;
                            }
                            else
                            {
                                EqtTrace.Error(
                                    string.Format(
                                        "Unable to find source information for method: {0} type: {1}",
                                        methodInfo.Name,
                                        type.FullName));
                            }
                        }

                        if (methodsNavigationData.Count != 0)
                        {
                            this.methodsNavigationDataForType[type.FullName] = methodsNavigationData;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("PortableSymbolReader: Failed to load symbols for binary: {0}", binaryPath);
                EqtTrace.Error(ex);
                this.Dispose();
                throw;
            }
        }
    }
}
