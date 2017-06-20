// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
#if !NET46
    using System.Runtime.Loader;
#endif

    /// <summary>
    /// Discovers test extensions in a directory.
    /// </summary>
    internal class TestPluginDiscoverer
    {
        #region Fields

#if WINDOWS_UAP
        private static HashSet<string> platformAssemblies = new HashSet<string>(new string[] {
            "MICROSOFT.VISUALSTUDIO.TESTPLATFORM.UNITTESTFRAMEWORK.DLL",
            "MICROSOFT.VISUALSTUDIO.TESTPLATFORM.TESTEXECUTOR.CORE.DLL",
            "MICROSOFT.VISUALSTUDIO.TESTPLATFORM.OBJECTMODEL.DLL",
            "VSTEST_EXECUTIONENGINE_PLATFORMBRIDGE.DLL",
            "VSTEST_EXECUTIONENGINE_PLATFORMBRIDGE.WINMD",
            "VSTEST.EXECUTIONENGINE.WINDOWSPHONE.DLL",
            "MICROSOFT.CSHARP.DLL",
            "MICROSOFT.VISUALBASIC.DLL",
            "CLRCOMPRESSION.DLL",
        });

        private const string SYSTEM_ASSEMBLY_PREFIX = "system.";
#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets information about each of the test extensions available.
        /// </summary>
        /// <param name="extensionPaths">
        /// The path to the extensions.
        /// </param>
        /// <param name="loadOnlyWellKnownExtensions">
        /// Should load only well known extensions or all.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary"/>` of assembly qualified name and testplugin information.
        /// </returns>
        public Dictionary<string, TPluginInfo> GetTestExtensionsInformation<TPluginInfo, TExtension>(
            IEnumerable<string> extensionPaths,
            bool loadOnlyWellKnownExtensions) where TPluginInfo : TestPluginInformation
        {
            Debug.Assert(extensionPaths != null);

            var pluginInfos = new Dictionary<string, TPluginInfo>();

#if !WINDOWS_UAP

            this.GetTestExtensionsFromFiles<TPluginInfo, TExtension>(extensionPaths.ToArray(), loadOnlyWellKnownExtensions, pluginInfos);

#else
            var fileSearchTask = Windows.ApplicationModel.Package.Current.InstalledLocation.GetFilesAsync().AsTask();
            fileSearchTask.Wait();

            var binaries = fileSearchTask.Result.Where(storageFile =>

                                (storageFile.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                                || storageFile.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                && !storageFile.Name.StartsWith(SYSTEM_ASSEMBLY_PREFIX, StringComparison.OrdinalIgnoreCase)
                                && !platformAssemblies.Contains(storageFile.Name.ToUpperInvariant())

                                ).
                                Select<IStorageFile, string>(file => file.Name);

            GetTestExtensionsFromFiles<TPluginInfo, TExtension>(binaries.ToArray(), loadOnlyWellKnownExtensions, pluginInfos);

            // In Release mode - managed dlls are packaged differently
            // So, file search will not find them - do it manually
            if (testDiscoverers.Count < 1)
            {
                GetTestExtensionsFromFiles<TPluginInfo, TExtension>(
                        new string[3] {
                        "Microsoft.VisualStudio.TestPlatform.Extensions.MSAppContainerAdapter.dll",
                        "Microsoft.VisualStudio.TestTools.CppUnitTestFramework.CppUnitTestExtension.dll",
                        "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.dll",},
                        loadOnlyWellKnownExtensions,
                        pluginInfos);
            }
#endif
            return pluginInfos;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets test extension information from the given colletion of files.
        /// </summary>
        /// <typeparam name="TPluginInfo">
        /// Type of Test Plugin Information.
        /// </typeparam>
        /// <typeparam name="TExtension">
        /// Type of extension.
        /// </typeparam>
        /// <param name="files">
        /// List of dll's to check for test extension availability
        /// </param>
        /// <param name="loadOnlyWellKnownExtensions">
        /// Should load only well known extensions or all.
        /// </param>
        /// <param name="pluginInfos">
        /// Test plugins collection to add to.
        /// </param>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We would like to continue discovering all plugins even if some dll in Extensions folder is not able to be load properly")]
        private void GetTestExtensionsFromFiles<TPluginInfo, TExtension>(
            string[] files,
            bool loadOnlyWellKnownExtensions,
            Dictionary<string, TPluginInfo> pluginInfos) where TPluginInfo : TestPluginInformation
        {
            Debug.Assert(files != null, "null files");
            Debug.Assert(pluginInfos != null, "null pluginInfos");

            // TODO: Do not see why loadOnlyWellKnowExtensions is still needed.
            //AssemblyName executingAssemblyName = null;
            //if (loadOnlyWellKnownExtensions)
            //{
            //    executingAssemblyName = new AssemblyName(typeof(TestPluginDiscoverer).GetTypeInfo().Assembly.FullName);
            //}

            // Scan each of the files for data extensions.
            foreach (var file in files)
            {
                try
                {
                    Assembly assembly = null;
                    var assemblyName = Path.GetFileNameWithoutExtension(file);
                    assembly = Assembly.Load(new AssemblyName(assemblyName));
                    if (assembly != null)
                    {
                        this.GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(assembly, pluginInfos);
                    }

                    // Check whether this assembly is known or not.
                    //if (loadOnlyWellKnownExtensions && assembly != null)
                    //{
                    //    var extensionAssemblyName = new AssemblyName(assembly.FullName);
                    //    if (!AssemblyUtilities.PublicKeyTokenMatches(extensionAssemblyName, executingAssemblyName))
                    //    {
                    //        EqtTrace.Warning("TestPluginDiscoverer: Ignoring extensions in assembly {0} as it is not a known assembly.", assembly.FullName);
                    //        continue;
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    EqtTrace.Warning("TestPluginDiscoverer: Failed to load extensions from file '{0}'.  Skipping test extension scan for this file.  Error: {1}", file, e);
                    continue;
                }
            }
        }

        /// <summary>
        /// Gets test extensions from a given assembly.
        /// </summary>
        /// <param name="assembly">Assembly to check for test extension availability</param>
        /// <param name="pluginInfos">Test extensions collection to add to.</param>
        /// <typeparam name="TPluginInfo">
        /// Type of Test Plugin Information.
        /// </typeparam>
        /// <typeparam name="TExtension">
        /// Type of Extensions.
        /// </typeparam>
        private void GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(Assembly assembly, Dictionary<string, TPluginInfo> pluginInfos) where TPluginInfo : TestPluginInformation
        {
            Debug.Assert(assembly != null, "null assembly");
            Debug.Assert(pluginInfos != null, "null pluginInfos");

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                EqtTrace.Warning("TestPluginDiscoverer: Failed to get types from assembly '{0}'.  Skipping test extension scan for this assembly.  Error: {1}", assembly.FullName, e.ToString());

                if (e.LoaderExceptions != null)
                {
                    foreach (var ex in e.LoaderExceptions)
                    {
                        EqtTrace.Warning("LoaderExceptions: {0}", ex);
                    }
                }

                return;
            }

            if ((types != null) && (types.Length > 0))
            {
                foreach (var type in types)
                {
                    if (type.GetTypeInfo().IsClass && !type.GetTypeInfo().IsAbstract)
                    {
                        this.GetTestExtensionFromType(type, typeof(TExtension), pluginInfos);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to find a test extension from given type.
        /// </summary>
        /// <typeparam name="TPluginInfo">
        /// Type of the test plugin information
        /// </typeparam>
        /// <param name="type">
        /// Type to inspect for being test extension
        /// </param>
        /// <param name="extensionType">
        /// Test extension type to look for.
        /// </param>
        /// <param name="extensionCollection">
        /// Test extensions collection to add to.
        /// </param>
        private void GetTestExtensionFromType<TPluginInfo>(
            Type type,
            Type extensionType,
            Dictionary<string, TPluginInfo> extensionCollection)
            where TPluginInfo : TestPluginInformation
        {
            if (extensionType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                var rawPluginInfo = Activator.CreateInstance(typeof(TPluginInfo), type);
                var pluginInfo = (TPluginInfo)rawPluginInfo;

                if (extensionCollection.ContainsKey(pluginInfo.IdentifierData))
                {
                    EqtTrace.Error(
                        "TryGetTestExtensionFromType: Discovered multiple test extensions with identifier data '{0}'; keeping the first one.",
                        pluginInfo.IdentifierData);
                }
                else
                {
                    extensionCollection.Add(pluginInfo.IdentifierData, pluginInfo);
                }
            }
        }

        #endregion
    }
}
