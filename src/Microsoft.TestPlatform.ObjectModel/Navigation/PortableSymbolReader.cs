// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation
{
#if !NET46
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.Loader;

    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

    /// <summary>
    /// The portable symbol reader.
    /// </summary>
    internal class PortableSymbolReader : ISymbolReader
    {
        /// <summary>
        /// Key in first dict is Type FullName
        /// Key in second dict is method name
        /// </summary>
        private Dictionary<string, Dictionary<string, DiaNavigationData>> methodsNavigationDataForType = new Dictionary<string, Dictionary<string, DiaNavigationData>>();
        public void Dispose()
        {
            foreach (Dictionary<string, DiaNavigationData> methodsNavigationData in this.methodsNavigationDataForType.Values)
            {
                methodsNavigationData.Clear();
            }

            this.methodsNavigationDataForType.Clear();
            this.methodsNavigationDataForType = null;
        }

        public void CacheSymbols(string binaryPath, string searchPath)
        {
            PopulateCacheForTypeAndMethodSymbols(binaryPath);
        }

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

        private void PopulateCacheForTypeAndMethodSymbols(string binaryPath)
        {
            try
            {
                var pdbFilePath = Path.ChangeExtension(binaryPath, ".pdb");
                using (var pdbReader = new PortablePdbReader(new FileHelper().GetStream(pdbFilePath, FileMode.Open)))
                {
                    // Load assembly
                    Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(binaryPath);


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
                                // Get source info for methods only defined in given binaryPath
                                continue;
                            }

                            var sourceInfo = pdbReader.GetSourceInformation(methodEntry.Value);
                            if (sourceInfo != null)
                            {
                                methodsNavigationData.Add(
                                    methodEntry.Key,
                                    new DiaNavigationData(sourceInfo.Filename, sourceInfo.startLineNumber, sourceInfo.endLineNumber));
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

    internal class PortablePdbReader : IDisposable
    {
        private MetadataReader reader;
        private MetadataReaderProvider provider;

        public PortablePdbReader(Stream stream)
            : this(MetadataReaderProvider.FromPortablePdbStream(stream))
        {
            if (!IsPortable(stream))
            {
                throw new Exception("Given stream is not portable stream");
            }
        }

        internal PortablePdbReader(MetadataReaderProvider provider)
        {
            this.provider = provider;
            this.reader = provider.GetMetadataReader();
        }

        public SourceInformation GetSourceInformation(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return null;
            }

            var handle = methodInfo.GetMethodDebugInformationHandle();

            return GetSourceInformation(handle);
        }

        private SourceInformation GetSourceInformation(MethodDebugInformationHandle handle)
        {
            if (this.reader == null)
            {
                throw new ObjectDisposedException(nameof(PortablePdbReader));
            }

            SourceInformation sourceInformation = null;
            try
            {
                var methodDebugDefinition = this.reader.GetMethodDebugInformation(handle);
                var fileName = GetMethodFileName(methodDebugDefinition);
                int startLineNumber, endLineNumber;
                GetMethodStartAndEndLineNumber(methodDebugDefinition, out startLineNumber, out endLineNumber);

                sourceInformation = new SourceInformation(fileName, startLineNumber, endLineNumber);
            }
            catch (BadImageFormatException)
            {
            }

            return sourceInformation;
        }

        private static void GetMethodStartAndEndLineNumber(MethodDebugInformation methodDebugDefinition, out int startLineNumber, out int endLineNumber)
        {
            var stratPoint =
                methodDebugDefinition.GetSequencePoints().OrderBy(s => s.StartLine).FirstOrDefault();
            startLineNumber = stratPoint.StartLine;
            var endPoint =
                methodDebugDefinition.GetSequencePoints().OrderByDescending(s => s.StartLine).FirstOrDefault();
            endLineNumber = endPoint.StartLine;
        }

        private string GetMethodFileName(MethodDebugInformation methodDebugDefinition)
        {
            var fileName = string.Empty;
            if (!methodDebugDefinition.Document.IsNil)
            {
                var document = this.reader.GetDocument(methodDebugDefinition.Document);
                fileName = this.reader.GetString(document.Name);
            }

            return fileName;
        }

        public void Dispose()
        {
            this.provider?.Dispose();
            this.provider = null;
            this.reader = null;
        }

        private static bool IsPortable(Stream stream)
        {
            bool result = stream.ReadByte() == 'B' && stream.ReadByte() == 'S' && stream.ReadByte() == 'J' && stream.ReadByte() == 'B';
            stream.Position = 0;
            return result;
        }
    }

    internal class SourceInformation
    {
        public SourceInformation(string filename, int startLineNumber, int endLineNumber)
        {
            Filename = filename;
            this.startLineNumber = startLineNumber;
            this.endLineNumber = endLineNumber;
        }

        public string Filename { get; }

        public int startLineNumber { get; }

        public int endLineNumber { get; }
    }

    internal static class MetadataExtensions
    {
        private static PropertyInfo methodInfoMethodTokenProperty = typeof(MethodInfo).GetProperty("MetadataToken");

        internal static int GetMethodToken(this MethodInfo methodInfo)
        {
            return (int)methodInfoMethodTokenProperty.GetValue(methodInfo);
        }

        internal static MethodDebugInformationHandle GetMethodDebugInformationHandle(this MethodInfo methodInfo)
        {
            var methodToken = methodInfo.GetMethodToken();
            var handle = ((MethodDefinitionHandle)MetadataTokens.Handle(methodToken)).ToDebugInformationHandle();
            return handle;
        }
    }
#endif
}