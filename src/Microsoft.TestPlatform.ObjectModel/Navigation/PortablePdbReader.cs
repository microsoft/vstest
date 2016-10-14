// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation
{
#if !NET46
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;

    public class PortablePdbReader:IDisposable
    {
        private MetadataReader _reader;
        private MetadataReaderProvider _provider;

        public PortablePdbReader(Stream stream)
            : this(MetadataReaderProvider.FromPortablePdbStream(stream))
        {
        }

        internal PortablePdbReader(MetadataReaderProvider provider)
        {
            _provider = provider;
            _reader = provider.GetMetadataReader();
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
            if (_reader == null)
            {
                throw new ObjectDisposedException(nameof(PortablePdbReader));
            }

            SourceInformation sourceInformation = null;
            try
            {
                var methodDebugDefinition = _reader.GetMethodDebugInformation(handle);
                var fileName = GetMethodFileName(methodDebugDefinition);
                var lineNumber = GetMethodStartLineNumber(methodDebugDefinition);

                sourceInformation = new SourceInformation(fileName, lineNumber);
            }
            catch (BadImageFormatException)
            {
            }

            return sourceInformation;
        }

        private static int GetMethodStartLineNumber(MethodDebugInformation methodDebugDefinition)
        {
            var sequencePoint =
                methodDebugDefinition.GetSequencePoints().OrderBy(s => s.StartLine).FirstOrDefault();
            var lineNumber = sequencePoint.StartLine;
            return lineNumber;
        }

        private string GetMethodFileName(MethodDebugInformation methodDebugDefinition)
        {
            var fileName = string.Empty;
            if (!methodDebugDefinition.Document.IsNil)
            {
                var document = _reader.GetDocument(methodDebugDefinition.Document);
                fileName = _reader.GetString(document.Name);
            }

            return fileName;
        }

        public void Dispose()
        {
            _provider?.Dispose();
            _provider = null;
            _reader = null;
        }
    }

    public class SourceInformation
    {
        public SourceInformation(string filename, int lineNumber)
        {
            Filename = filename;
            LineNumber = lineNumber;
        }

        public string Filename { get; }

        public int LineNumber { get; }
    }

    internal static class MetadataExtensions
    {
        private static PropertyInfo s_methodInfoMethodTokenProperty = typeof(MethodInfo).GetProperty("MetadataToken");

        internal static int GetMethodToken(this MethodInfo methodInfo)
        {
            return (int)s_methodInfoMethodTokenProperty.GetValue(methodInfo);
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
