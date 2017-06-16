// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

    /// <summary>
    /// The class that enables us to get debug information from both managed and native binaries.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
         Justification = "Dia is a specific name.")]
    public class DiaSession : INavigationSession
    {
        /// <summary>
        /// Characters that should be stripped off the end of test names.
        /// </summary>
        private static readonly char[] TestNameStripChars = { '(', ')', ' ' };

        /// <summary>
        /// The symbol reader.
        /// </summary>
        private readonly ISymbolReader symbolReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiaSession"/> class.
        /// </summary>
        /// <param name="binaryPath">
        /// The binary path.
        /// </param>
        public DiaSession(string binaryPath)
            : this(binaryPath, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiaSession"/> class.
        /// </summary>
        /// <param name="binaryPath">
        /// The binary path is assembly path Ex: \path\to\bin\Debug\simpleproject.dll
        /// </param>
        /// <param name="searchPath">
        /// search path.
        /// </param>
        public DiaSession(string binaryPath, string searchPath)
            : this(binaryPath, searchPath, GetSymbolReader(binaryPath))
        {
        }

        internal DiaSession(string binaryPath, string searchPath, ISymbolReader symbolReader)
        {
            this.symbolReader = symbolReader;
            ValidateArg.NotNullOrEmpty(binaryPath, "binaryPath");
            this.symbolReader.CacheSymbols(binaryPath, searchPath);
        }

        /// <summary>
        /// Dispose symbol reader
        /// </summary>
        public void Dispose()
        {
            this.symbolReader?.Dispose();
        }

        /// <summary>
        /// Gets the navigation data for a method declared in a type.
        /// </summary>
        /// <param name="declaringTypeName"> The declaring type name. </param>
        /// <param name="methodName"> The method name. </param>
        /// <returns> The <see cref="INavigationData" /> for that method. </returns>
        /// <remarks> Leaving this method in place to preserve back compatibility. </remarks>
        public DiaNavigationData GetNavigationData(string declaringTypeName, string methodName)
        {
            return (DiaNavigationData)this.GetNavigationDataForMethod(declaringTypeName, methodName);
        }

        /// <summary>
        /// Gets the navigation data for a method declared in a type.
        /// </summary>
        /// <param name="declaringTypeName"> The declaring type name. </param>
        /// <param name="methodName"> The method name. </param>
        /// <returns> The <see cref="INavigationData" /> for that method. </returns>
        public INavigationData GetNavigationDataForMethod(string declaringTypeName, string methodName)
        {
            ValidateArg.NotNullOrEmpty(declaringTypeName, "declaringTypeName");
            ValidateArg.NotNullOrEmpty(methodName, "methodName");
            methodName = methodName.TrimEnd(TestNameStripChars);
            return this.symbolReader.GetNavigationData(declaringTypeName, methodName);
        }

        private static ISymbolReader GetSymbolReader(string binaryPath)
        {
#if NET46
            var pdbFilePath = Path.ChangeExtension(binaryPath, ".pdb");
            using (var stream = new FileHelper().GetStream(pdbFilePath, FileMode.Open, FileAccess.Read))
            {
                if (PortablePdbReader.IsPortable(stream))
                {
                    return new PortableSymbolReader();
                }

                return new FullSymbolReader();
            }
#else
            // We don't support full PDB files with .net core
            return new PortableSymbolReader();
#endif
        }
    }
}