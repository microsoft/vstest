// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
  
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using CommonResources = Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

    /// <summary>
    /// Discovers test extensions in a directory.
    /// </summary>
    internal class TestPluginDiscoverer
    {
        private static HashSet<string> UnloadableFiles = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPluginDiscoverer"/> class.
        /// </summary>
        public TestPluginDiscoverer()
        {
        }

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
        ///     The path to the extensions.
        /// </param>
        /// <returns>
        /// A dictionary of assembly qualified name and test plugin information.
        /// </returns>
        public Dictionary<string, TPluginInfo> GetTestExtensionsInformation<TPluginInfo, TExtension>(IEnumerable<string> extensionPaths) where TPluginInfo : TestPluginInformation
        {
            Debug.Assert(extensionPaths != null);

            var pluginInfos = new Dictionary<string, TPluginInfo>();

            // C++ UWP adapters do not follow TestAdapater naming convention, so making this exception
            if (!extensionPaths.Any())
            {
                this.AddKnownExtensions(ref extensionPaths);
            }

            this.GetTestExtensionsFromFiles<TPluginInfo, TExtension>(extensionPaths.ToArray(), pluginInfos);

            return pluginInfos;
        }

        #endregion

        #region Private Methods

        private void AddKnownExtensions(ref IEnumerable<string> extensionPaths)
        {
            // For C++ UWP adapter, & OLD C# UWP(MSTest V1) adapter
            // In UWP .Net Native Compilation mode managed dll's are packaged differently, & File.Exists() fails.
            // Include these two dll's if so far no adapters(extensions) were found, & let Assembly.Load() fail if they are not present.
            extensionPaths = extensionPaths.Concat(new[] { "Microsoft.VisualStudio.TestTools.CppUnitTestFramework.CppUnitTestExtension.dll", "Microsoft.VisualStudio.TestPlatform.Extensions.MSAppContainerAdapter.dll" });
        }

        /// <summary>
        /// Gets test extension information from the given collection of files.
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
        /// <param name="pluginInfos">
        /// Test plugins collection to add to.
        /// </param>
        private void GetTestExtensionsFromFiles<TPluginInfo, TExtension>(
            string[] files,
            Dictionary<string, TPluginInfo> pluginInfos) where TPluginInfo : TestPluginInformation
        {
            Debug.Assert(files != null, "null files");
            Debug.Assert(pluginInfos != null, "null pluginInfos");

            // Scan each of the files for data extensions.
            foreach (var file in files)
            {
                if (UnloadableFiles.Contains(file))
                {
                    continue;
                }
                try
                {
                    Assembly assembly = null;
                    var assemblyName = Path.GetFileNameWithoutExtension(file);
                    assembly = Assembly.Load(new AssemblyName(assemblyName));
                    if (assembly != null)
                    {
                        this.GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(assembly, pluginInfos);
                    }
                }
                catch (FileLoadException e)
                {
                    EqtTrace.Warning("TestPluginDiscoverer-FileLoadException: Failed to load extensions from file '{0}'.  Skipping test extension scan for this file.  Error: {1}", file, e);
                    string fileLoadErrorMessage = string.Format(CultureInfo.CurrentUICulture, CommonResources.FailedToLoadAdapaterFile, file);
                    TestSessionMessageLogger.Instance.SendMessage(TestMessageLevel.Warning, fileLoadErrorMessage);
                    UnloadableFiles.Add(file);
                }
                catch (Exception e)
                {
                    EqtTrace.Warning("TestPluginDiscoverer: Failed to load extensions from file '{0}'.  Skipping test extension scan for this file.  Error: {1}", file, e);
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
            IEnumerable<Type> types;
            Type extension = typeof(TExtension);

            try
            {
                types = TypesToLoadUtilities.GetTypesToLoad(assembly);

                if (!types.Any())
                {
                    types = assembly.GetTypes().Where(type => type.GetTypeInfo().IsClass && !type.GetTypeInfo().IsAbstract);
                }
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

            if (types != null && types.Any())
            {
                foreach (var type in types)
                {
                    GetTestExtensionFromType(type, extension, pluginInfos);
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

                if (pluginInfo == null || pluginInfo.IdentifierData == null)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error(
                        "TryGetTestExtensionFromType: Either PluginInformation is null or PluginInformation doesn't contain IdentifierData for type {0}.", type.FullName);
                    }
                    return;
                }

                if (extensionCollection.ContainsKey(pluginInfo.IdentifierData))
                {
                    EqtTrace.Warning(
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
