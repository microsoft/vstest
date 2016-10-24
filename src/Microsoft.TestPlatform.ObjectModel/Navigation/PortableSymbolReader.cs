// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation
{
#if !NET46
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;

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
                using (var pdbReader = new PortablePdbReader(new FileHelper().GetStream(pdbFilePath, FileMode.Open)))
                {
                    // Load assembly
                    var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(binaryPath);

                    // Get all types to dict, fullname as key
                    var typesDict = asm.GetTypes().ToDictionary(type => type.FullName);
                    foreach (var typeEntry in typesDict)
                    {
                        // Get method infos for all types in assembly
                        var methodInfoDict = typeEntry.Value.GetMethods().ToDictionary(methodInfo => methodInfo.Name);
                        var methodsNavigationData = new Dictionary<string, DiaNavigationData>();
                        this.methodsNavigationDataForType.Add(typeEntry.Key, methodsNavigationData);

                        foreach (var methodEntry in methodInfoDict)
                        {
                            if (string.CompareOrdinal(methodEntry.Value.Module.FullyQualifiedName, binaryPath) != 0)
                            {
                                continue;
                            }

                            var diaNavigationData = pdbReader.GetDiaNavigationData(methodEntry.Value);
                            if (diaNavigationData != null)
                            {
                                methodsNavigationData.Add(methodEntry.Key, diaNavigationData);
                            }
                            else
                            {
                                EqtTrace.Error(
                                    string.Format(
                                        "Unable to find source information for method: {0} type: {1}",
                                        methodEntry.Key,
                                        typeEntry.Key));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                this.Dispose();
                throw;
            }
        }
    }
#endif
}