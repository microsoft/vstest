// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation
{
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
        /// The method info method token property.
        /// </summary>
        private static readonly PropertyInfo MethodInfoMethodTokenProperty =
            typeof(MethodInfo).GetProperty("MetadataToken");

        /// <summary>
        /// The provider.
        /// </summary>
        private MetadataReaderProvider provider;

        /// <summary>
        /// The reader.
        /// </summary>
        private MetadataReader reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortablePdbReader"/> class.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <exception cref="Exception">
        /// </exception>
        public PortablePdbReader(Stream stream)
        {
            if (!IsPortable(stream))
            {
                throw new Exception("Given stream is not portable stream");
            }

            this.Setup(MetadataReaderProvider.FromPortablePdbStream(stream));
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.provider?.Dispose();
            this.provider = null;
            this.reader = null;
        }

        /// <summary>
        /// The get dia navigation data.
        /// </summary>
        /// <param name="methodInfo">
        /// The method info.
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

        /// <summary>
        /// The get method debug information handle.
        /// </summary>
        /// <param name="methodInfo">
        /// The method info.
        /// </param>
        /// <returns>
        /// The <see cref="MethodDebugInformationHandle"/>.
        /// </returns>
        internal static MethodDebugInformationHandle GetMethodDebugInformationHandle(MethodInfo methodInfo)
        {
            var methodToken = (int)MethodInfoMethodTokenProperty.GetValue(methodInfo);
            var handle = ((MethodDefinitionHandle)MetadataTokens.Handle(methodToken)).ToDebugInformationHandle();
            return handle;
        }

        /// <summary>
        /// The get method start and end line number.
        /// </summary>
        /// <param name="methodDebugDefinition">
        /// The method debug definition.
        /// </param>
        /// <param name="startLineNumber">
        /// The start line number.
        /// </param>
        /// <param name="endLineNumber">
        /// The end line number.
        /// </param>
        private static void GetMethodStartAndEndLineNumber(
            MethodDebugInformation methodDebugDefinition,
            out int startLineNumber,
            out int endLineNumber)
        {
            var stratPoint = methodDebugDefinition.GetSequencePoints().OrderBy(s => s.StartLine).FirstOrDefault();
            startLineNumber = stratPoint.StartLine;
            var endPoint =
                methodDebugDefinition.GetSequencePoints().OrderByDescending(s => s.StartLine).FirstOrDefault();
            endLineNumber = endPoint.StartLine;
        }

        /// <summary>
        /// The is portable.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool IsPortable(Stream stream)
        {
            var result = (stream.ReadByte() == 'B') && (stream.ReadByte() == 'S') && (stream.ReadByte() == 'J')
                         && (stream.ReadByte() == 'B');
            stream.Position = 0;
            return result;
        }

        /// <summary>
        /// The get dia navigation data.
        /// </summary>
        /// <param name="handle">
        /// The handle.
        /// </param>
        /// <returns>
        /// The <see cref="DiaNavigationData"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// </exception>
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
            catch (BadImageFormatException)
            {
            }

            return diaNavigationData;
        }

        /// <summary>
        /// The get method file name.
        /// </summary>
        /// <param name="methodDebugDefinition">
        /// The method debug definition.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
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

        /// <summary>
        /// The setup.
        /// </summary>
        /// <param name="provider">
        /// The provider.
        /// </param>
        private void Setup(MetadataReaderProvider provider)
        {
            this.provider = provider;
            this.reader = provider.GetMetadataReader();
        }
    }
}