// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Navigation;

    /// <summary>
    /// The class that enables us to get debug information from both managed and native binaries.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Dia is a specific name.")]
    public class DiaSession : INavigationSession
    {
        /// <summary>
        /// Characters that should be stripped off the end of test names.
        /// </summary>
        private static readonly char[] s_testNameStripChars = { '(', ')', ' ' };

        private ISymbolReader symbolReader;

        public DiaSession(string binaryPath) : this(binaryPath, null)
        {
        }

        public DiaSession(string binaryPath, string searchPath):
#if NET46
            this(binaryPath, searchPath, new FullSymbolReader())
#else
            this(binaryPath, searchPath, new PortableSymbolReader())
#endif
        {
        }

        internal DiaSession(string binaryPath, string searchPath, ISymbolReader symbolReader)
        {
            this.symbolReader = symbolReader;
            ValidateArg.NotNullOrEmpty(binaryPath, "binaryPath");
            this.symbolReader.CacheSymbols(binaryPath, searchPath);
        }

        public void Dispose()
        {
            this.symbolReader?.Dispose();
        }


        /// <summary>
        /// Gets the navigation data for a method declared in a type.
        /// </summary>
        /// <param name="declaringTypeName"> The declaring type name. </param>
        /// <param name="methodName"> The method name. </param>
        /// <returns> The <see cref="INavigationData"/> for that method. </returns>
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
        /// <returns> The <see cref="INavigationData"/> for that method. </returns>
        public INavigationData GetNavigationDataForMethod(string declaringTypeName, string methodName)
        {
            ValidateArg.NotNullOrEmpty(declaringTypeName, "declaringTypeName");
            ValidateArg.NotNullOrEmpty(methodName, "methodName");
            methodName = methodName.TrimEnd(s_testNameStripChars);
            return this.symbolReader.GetNavigationData(declaringTypeName, methodName);
        }
    }
}
