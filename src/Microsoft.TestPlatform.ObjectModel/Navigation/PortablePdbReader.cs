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

    /// <summary>
    /// The portable pdb reader.
    /// </summary>
    internal class PortablePdbReader : IDisposable
    {
        /// <summary>
        /// Use to get method token 
        /// </summary>
        private static readonly PropertyInfo MethodInfoMethodTokenProperty =
            typeof(MethodInfo).GetProperty("MetadataToken");

        /// <summary>
        /// Metadata reader provider from portable pdb stream
        /// To get Metadate reader
        /// </summary>
        private MetadataReaderProvider provider;

        /// <summary>
        /// Metadata reader from portable pdb stream
        /// To get method debug info from mehthod info
        /// </summary>
        private MetadataReader reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortablePdbReader"/> class.
        /// </summary>
        /// <param name="stream">
        /// Portable pdb stream
        /// </param>
        /// <exception cref="Exception">
        /// Raises Exception on given stream is not portable pdb stream
        /// </exception>
        public PortablePdbReader(Stream stream)
        {
            if (!IsPortable(stream))
            {
                throw new Exception("Given stream is not portable stream");
            }

            this.provider = MetadataReaderProvider.FromPortablePdbStream(stream);
            this.reader = this.provider.GetMetadataReader();
        }

        /// <summary>
        /// Dispose Metadata reader
        /// </summary>
        public void Dispose()
        {
            this.provider?.Dispose();
            this.provider = null;
            this.reader = null;
        }

        /// <summary>
        /// Gets dia navigation data from Metadata reader 
        /// </summary>
        /// <param name="methodInfo">
        /// Method info.
        /// </param>
        /// <returns>
        /// The <see cref="DiaNavigationData"/>.
        /// </returns>
        public DiaNavigationData GetDiaNavigationData(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return null;
            }

            var handle = GetMethodDebugInformationHandle(methodInfo);

            return this.GetDiaNavigationData(handle);
        }

        internal static MethodDebugInformationHandle GetMethodDebugInformationHandle(MethodInfo methodInfo)
        {
            var methodToken = (int)MethodInfoMethodTokenProperty.GetValue(methodInfo);
            var handle = ((MethodDefinitionHandle)MetadataTokens.Handle(methodToken)).ToDebugInformationHandle();
            return handle;
        }

        private static void GetMethodStartAndEndLineNumber(
            MethodDebugInformation methodDebugDefinition,
            out int startLineNumber,
            out int endLineNumber)
        {
            var startPoint = methodDebugDefinition.GetSequencePoints().OrderBy(s => s.StartLine).FirstOrDefault();
            startLineNumber = startPoint.StartLine;
            var endPoint =
                methodDebugDefinition.GetSequencePoints().OrderByDescending(s => s.StartLine).FirstOrDefault();
            endLineNumber = endPoint.StartLine;
        }

        /// <summary>
        /// Checks gives stream is from portable pdb or not
        /// </summary>
        /// <param name="stream">
        /// Stream.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool IsPortable(Stream stream)
        {
            // First four bytes should be 'BSJB'
            var result = (stream.ReadByte() == 'B') && (stream.ReadByte() == 'S') && (stream.ReadByte() == 'J')
                         && (stream.ReadByte() == 'B');
            stream.Position = 0;
            return result;
        }

        private DiaNavigationData GetDiaNavigationData(MethodDebugInformationHandle handle)
        {
            if (this.reader == null)
            {
                throw new ObjectDisposedException(nameof(PortablePdbReader));
            }

            DiaNavigationData diaNavigationData = null;
            try
            {
                var methodDebugDefinition = this.reader.GetMethodDebugInformation(handle);
                var fileName = this.GetMethodFileName(methodDebugDefinition);
                int startLineNumber, endLineNumber;
                GetMethodStartAndEndLineNumber(methodDebugDefinition, out startLineNumber, out endLineNumber);

                diaNavigationData = new DiaNavigationData(fileName, startLineNumber, endLineNumber);
            }
            catch (BadImageFormatException exception)
            {
                EqtTrace.Error("failed to get dia navigation data: {0}", exception);
            }

            return diaNavigationData;
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
    }
#endif
}
