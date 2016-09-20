// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
#if NET46

    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;

    using Dia;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation;

    /// <summary>
    /// The class that enables us to get debug information from both managed and native binaries.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Dia is a specific name.")]
    public class DiaSession : INavigationSession
    {
        private IDiaDataSource source;
        private IDiaSession session;
        private bool isDisposed;
        private RegistryFreeActivationContext activationContext;

        /// <summary>
        /// Holds type symbols avaiable in the source.
        /// </summary>
        private Dictionary<string, IDiaSymbol> typeSymbols = new Dictionary<string, IDiaSymbol>();

        /// <summary>
        /// Holds method symbols for all types in the source.
        /// Methods in different types can have same name, hence seprated dicitionary is created for each type.
        /// Bug: Method overrides in same type are not handled (not a regression)
        /// </summary>
        private Dictionary<string, Dictionary<string, IDiaSymbol>> methodSymbols = new Dictionary<string, Dictionary<string, IDiaSymbol>>();

        /// <summary>
        /// Manifest files for Reg Free Com. This is essentially put in place for the msdia dependency.
        /// </summary>
        private const string ManifestFileNameX86 = "TestPlatform.ObjectModel.x86.manifest";
        private const string ManifestFileNameX64 = "TestPlatform.ObjectModel.manifest";

        public DiaSession(string binaryPath) : this(binaryPath, null)
        {
        }

        public DiaSession(string binaryPath, string searchPath)
        {
            ValidateArg.NotNullOrEmpty(binaryPath, "binaryPath");
            try
            {
                this.activationContext = new RegistryFreeActivationContext(this.GetManifestFileForRegFreeCom());
                this.activationContext.ActivateContext();

                this.source = new DiaSource();
                this.source.loadDataForExe(binaryPath, searchPath, null);
                this.source.openSession(out this.session);
                PopulateCacheForTypeAndMethodSymbols();
            }
            catch (COMException)
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
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

            DiaNavigationData navigationData = null;

            IDiaSymbol methodSymbol = null;


            IDiaSymbol typeSymbol = GetTypeSymbol(declaringTypeName, SymTagEnum.SymTagCompiland);
            if (typeSymbol != null)
            {
                methodSymbol = GetMethodSymbol(typeSymbol, methodName);
            }
            else
            {
                // May be a managed C++ test assembly...
                string fullMethodName = declaringTypeName.Replace(".", "::");
                fullMethodName = fullMethodName + "::" + methodName;

                methodSymbol = GetTypeSymbol(fullMethodName, SymTagEnum.SymTagFunction);
            }

            if (methodSymbol != null)
            {
                navigationData = GetSymbolNavigationData(methodSymbol);
            }

            return navigationData;
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    this.activationContext?.Dispose();
                    foreach (Dictionary<string, IDiaSymbol> methodSymbolsForType in methodSymbols.Values)
                    {
                        foreach (IDiaSymbol methodSymbol in methodSymbolsForType.Values)
                        {
                            IDiaSymbol symToRelease = methodSymbol;
                            ReleaseComObject(ref symToRelease);
                        }
                        methodSymbolsForType.Clear();
                    }
                    methodSymbols.Clear();
                    methodSymbols = null;
                    foreach (IDiaSymbol typeSymbol in typeSymbols.Values)
                    {
                        IDiaSymbol symToRelease = typeSymbol;
                        ReleaseComObject(ref symToRelease);
                    }
                    typeSymbols.Clear();
                    typeSymbols = null;
                    ReleaseComObject(ref this.session);
                    ReleaseComObject(ref this.source);
                }

                isDisposed = true;
            }
        }

        /// <summary>
        /// Gets the appropraite manifest file for reg free COM. This is dependent on the architecture of the current running process.
        /// </summary>
        /// <returns></returns>
        private string GetManifestFileForRegFreeCom()
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var manifestFieName = string.Empty;
            if (IntPtr.Size == 4)
            {
                manifestFieName = ManifestFileNameX86;
            }
            else if (IntPtr.Size == 8)
            {
                manifestFieName = ManifestFileNameX64;
            }
            var manifestFile = Path.Combine(currentDirectory, manifestFieName);

            if (!File.Exists(manifestFile))
            {
                throw new TestPlatformException(string.Format("Could not find the manifest file {0} for Registry free Com registration.", manifestFile));
            }

            return manifestFile;
        }

        private static void ReleaseComObject<T>(ref T obj)
            where T : class
        {
            if (obj != null)
            {
                Marshal.FinalReleaseComObject(obj);
                obj = null;
            }
        }

        /// <summary>
        /// Characters that should be stripped off the end of test names.
        /// </summary>
        private static readonly char[] s_testNameStripChars = { '(', ')', ' ' };

        private DiaNavigationData GetSymbolNavigationData(IDiaSymbol symbol)
        {
            ValidateArg.NotNull(symbol, "symbol");

            DiaNavigationData navigationData = new DiaNavigationData(null, int.MaxValue, int.MinValue);

            IDiaEnumLineNumbers lines = null;

            try
            {
                this.session.findLinesByAddr(symbol.addressSection, symbol.addressOffset, (uint)symbol.length, out lines);

                uint celt;
                IDiaLineNumber lineNumber;

                while (true)
                {
                    lines.Next(1, out lineNumber, out celt);

                    if (celt != 1)
                    {
                        break;
                    }

                    IDiaSourceFile sourceFile = null;
                    try
                    {
                        sourceFile = lineNumber.sourceFile;

                        //The magic hex constant below works around weird data reported from GetSequencePoints.
                        //The constant comes from ILDASM's source code, which performs essentially the same test.
                        const uint Magic = 0xFEEFEE;
                        if (lineNumber.lineNumber >= Magic || lineNumber.lineNumberEnd >= Magic)
                        {
                            continue;
                        }

                        navigationData.FileName = sourceFile.fileName;
                        navigationData.MinLineNumber = Math.Min(navigationData.MinLineNumber, (int)lineNumber.lineNumber);
                        navigationData.MaxLineNumber = Math.Max(navigationData.MaxLineNumber, (int)lineNumber.lineNumberEnd);
                    }
                    finally
                    {
                        ReleaseComObject(ref sourceFile);
                        ReleaseComObject(ref lineNumber);
                    }
                }
            }
            finally
            {
                ReleaseComObject(ref lines);
            }

            return navigationData;
        }


        /// <summary>
        /// Create a cache for type symbols and method symbols contained in the type symbol.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Failure to build cache is not fatal exception, ignore it and continue.")]
        private void PopulateCacheForTypeAndMethodSymbols()
        {
            IDiaEnumSymbols enumTypeSymbols = null;
            IDiaSymbol global = null;
            try
            {
                global = this.session.globalScope;
                global.findChildren(SymTagEnum.SymTagCompiland, null, 0, out enumTypeSymbols);
                uint celtTypeSymbol;
                IDiaSymbol typeSymbol = null;
                // NOTE::
                // If foreach loop is used instead of Enumerator iterator, for some reason it leaves
                // the reference to pdb active, which prevents pdb from being rebuilt (in VS IDE scenario).
                enumTypeSymbols.Next(1, out typeSymbol, out celtTypeSymbol);
                while (celtTypeSymbol == 1 && null != typeSymbol)
                {
                    typeSymbols[typeSymbol.name] = typeSymbol;

                    IDiaEnumSymbols enumMethodSymbols = null;
                    try
                    {
                        Dictionary<string, IDiaSymbol> methodSymbolsForType = new Dictionary<string, IDiaSymbol>();
                        typeSymbol.findChildren(SymTagEnum.SymTagFunction, null, 0, out enumMethodSymbols);

                        uint celtMethodSymbol;
                        IDiaSymbol methodSymbol = null;

                        enumMethodSymbols.Next(1, out methodSymbol, out celtMethodSymbol);
                        while (celtMethodSymbol == 1 && null != methodSymbol)
                        {
                            UpdateMethodSymbolCache(methodSymbol.name, methodSymbol, methodSymbolsForType);
                            enumMethodSymbols.Next(1, out methodSymbol, out celtMethodSymbol);
                        }
                        methodSymbols[typeSymbol.name] = methodSymbolsForType;
                    }
                    catch (Exception ex)
                    {
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error("Ignoring the exception while iterating method symbols:{0} for type:{1}", ex, typeSymbol.name);
                        }
                    }
                    finally
                    {
                        ReleaseComObject(ref enumMethodSymbols);
                    }
                    enumTypeSymbols.Next(1, out typeSymbol, out celtTypeSymbol);
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("Ignoring the exception while iterating type symbols:{0}", ex);
                }
            }
            finally
            {
                ReleaseComObject(ref enumTypeSymbols);
                ReleaseComObject(ref global);
            }
        }




        private IDiaSymbol GetTypeSymbol(string typeName, SymTagEnum symTag)
        {
            ValidateArg.NotNullOrEmpty(typeName, "typeName");

            IDiaEnumSymbols enumSymbols = null;
            IDiaSymbol typeSymbol = null;
            IDiaSymbol global = null;

            uint celt;

            try
            {
                typeName = typeName.Replace('+', '.');
                if (typeSymbols.ContainsKey(typeName))
                {
                    return typeSymbols[typeName];
                }
                global = this.session.globalScope;
                global.findChildren(symTag, typeName, 0, out enumSymbols);

                enumSymbols.Next(1, out typeSymbol, out celt);

#if DEBUG
                if (typeSymbol == null)
                {
                    IDiaEnumSymbols enumAllSymbols = null;
                    try
                    {
                        global.findChildren(symTag, null, 0, out enumAllSymbols);
                        List<string> children = new List<string>();

                        IDiaSymbol childSymbol = null;
                        uint fetchedCount = 0;
                        while (true)
                        {
                            enumAllSymbols.Next(1, out childSymbol, out fetchedCount);
                            if (fetchedCount == 0 || childSymbol == null)
                            {
                                break;
                            }

                            children.Add(childSymbol.name);
                            ReleaseComObject(ref childSymbol);
                        }
                        Debug.Assert(children.Count > 0);
                    }
                    finally
                    {
                        ReleaseComObject(ref enumAllSymbols);
                    }
                }
#endif
            }
            finally
            {
                ReleaseComObject(ref enumSymbols);
                ReleaseComObject(ref global);
            }
            if (null != typeSymbol)
            {
                typeSymbols[typeName] = typeSymbol;
            }
            return typeSymbol;
        }

        private IDiaSymbol GetMethodSymbol(IDiaSymbol typeSymbol, string methodName)
        {
            ValidateArg.NotNull(typeSymbol, "typeSymbol");
            ValidateArg.NotNullOrEmpty(methodName, "methodName");

            IDiaEnumSymbols enumSymbols = null;
            IDiaSymbol methodSymbol = null;
            Dictionary<string, IDiaSymbol> methodSymbolsForType;

            try
            {

                if (methodSymbols.ContainsKey(typeSymbol.name))
                {
                    methodSymbolsForType = methodSymbols[typeSymbol.name];
                    if (methodSymbolsForType.ContainsKey(methodName))
                    {
                        return methodSymbolsForType[methodName];
                    }

                }
                else
                {
                    methodSymbolsForType = new Dictionary<string, IDiaSymbol>();
                    methodSymbols[typeSymbol.name] = methodSymbolsForType;
                }

                typeSymbol.findChildren(SymTagEnum.SymTagFunction, methodName, 0, out enumSymbols);

                uint celtFetched;
                enumSymbols.Next(1, out methodSymbol, out celtFetched);

#if DEBUG
                if (methodSymbol == null)
                {
                    IDiaEnumSymbols enumAllSymbols = null;
                    try
                    {
                        typeSymbol.findChildren(SymTagEnum.SymTagFunction, null, 0, out enumAllSymbols);
                        List<string> children = new List<string>();

                        IDiaSymbol childSymbol = null;
                        uint fetchedCount = 0;
                        while (true)
                        {
                            enumAllSymbols.Next(1, out childSymbol, out fetchedCount);
                            if (fetchedCount == 0 || childSymbol == null)
                            {
                                break;
                            }

                            children.Add(childSymbol.name);
                            ReleaseComObject(ref childSymbol);
                        }

                        Debug.Assert(children.Count > 0);
                    }
                    finally
                    {
                        ReleaseComObject(ref enumAllSymbols);
                    }
                }
#endif
            }
            finally
            {
                ReleaseComObject(ref enumSymbols);
            }
            if (null != methodSymbol)
            {
                methodSymbolsForType[methodName] = methodSymbol;
            }
            return methodSymbol;
        }

        /// <summary>
        /// Update the method symbol cache. 
        /// </summary>
        private static void UpdateMethodSymbolCache(string methodName, IDiaSymbol methodSymbol, Dictionary<string, IDiaSymbol> methodSymbolCache)
        {
            Debug.Assert(!string.IsNullOrEmpty(methodName), "MethodName cannot be empty.");
            Debug.Assert(methodSymbol != null, "Method symbol cannot be null.");
            Debug.Assert(methodSymbolCache != null, "Method symbol cache cannot be null.");

            // #827589, In case a type has overloaded methods, then there could be a method already in the 
            // cache which should be disposed. 
            // 
            IDiaSymbol oldSymbol;
            if (methodSymbolCache.TryGetValue(methodName, out oldSymbol))
            {
                ReleaseComObject(ref oldSymbol);
            }

            methodSymbolCache[methodName] = methodSymbol;
        }
    }

#endif
}
