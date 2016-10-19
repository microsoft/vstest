using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation
{
    internal interface ISymbolReader : IDisposable
    {
        void CacheSymbols(string binaryPath, string searchPath);

        INavigationData GetNavigationData(string declaringTypeName, string methodName);
    }
}
