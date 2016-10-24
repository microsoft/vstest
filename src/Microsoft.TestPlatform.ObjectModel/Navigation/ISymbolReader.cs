using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation
{
    /// <summary>
    /// Caches filename and linenumber for symbols in assembly.
    /// </summary>
    internal interface ISymbolReader : IDisposable
    {
        /// <summary>
        /// Cache symbols from binary path
        /// </summary>
        /// <param name="binaryPath">
        /// The binary path is assembly path Ex: \path\to\bin\Debug\simpleproject.dll
        /// </param>
        /// <param name="searchPath">
        /// search path.
        /// </param>
        void CacheSymbols(string binaryPath, string searchPath);

        /// <summary>
        /// Gets Navigation data from caches
        /// </summary>
        /// <param name="declaringTypeName">
        /// Type name Ex: MyNameSpace.MyType
        /// </param>
        /// <param name="methodName">
        /// Method name in declaringTypeName Ex: Method1
        /// </param>
        /// <returns>
        /// <see cref="INavigationData"/>.
        /// Returns INavigationData which contains file name and line number.
        /// </returns>
        INavigationData GetNavigationData(string declaringTypeName, string methodName);
    }
}
