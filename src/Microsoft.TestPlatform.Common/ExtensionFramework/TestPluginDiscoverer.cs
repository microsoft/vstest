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
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
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
        /// <param name="pathToExtensions"> The path to the extensions. </param>
        /// <param name="loadOnlyWellKnownExtensions"> Should load only well known extensions or all. </param>
        /// <returns> The <see cref="TestExtensions"/>. </returns>
        public TestExtensions GetTestExtensionsInformation(
            IEnumerable<string> pathToExtensions,
            bool loadOnlyWellKnownExtensions)
        {
            Debug.Assert(pathToExtensions != null);

            var testExtensions = new TestExtensions
            {
                TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>(StringComparer.OrdinalIgnoreCase),
                TestExecutors = new Dictionary<string, TestExecutorPluginInformation>(StringComparer.OrdinalIgnoreCase),
                TestSettingsProviders = new Dictionary<string, TestSettingsProviderPluginInformation>(StringComparer.OrdinalIgnoreCase),
                TestLoggers = new Dictionary<string, TestLoggerPluginInformation>(StringComparer.OrdinalIgnoreCase)
            };


#if !WINDOWS_UAP

            this.GetTestExtensionsFromFiles(
                pathToExtensions.ToArray(),
                loadOnlyWellKnownExtensions,
                testExtensions.TestDiscoverers,
                testExtensions.TestExecutors,
                testExtensions.TestSettingsProviders,
                testExtensions.TestLoggers);

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

            GetTestExtensionsFromFiles(
                    binaries.ToArray(),
                    loadOnlyWellKnownExtensions,
                    testDiscoverers,
                    testExecutors,
                    testSettingsProviders,
                    testLoggers);

            // In Release mode - managed dlls are packaged differently
            // So, file search will not find them - do it manually
            if (testDiscoverers.Count < 1)
            {
                GetTestExtensionsFromFiles(
                        new string[3] {
                        "Microsoft.VisualStudio.TestPlatform.Extensions.MSAppContainerAdapter.dll",
                        "Microsoft.VisualStudio.TestTools.CppUnitTestFramework.CppUnitTestExtension.dll",
                        "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.dll",},
                        loadOnlyWellKnownExtensions,
                        testDiscoverers,
                        testExecutors,
                        testSettingsProviders,
                        testLoggers);
            }
#endif
            return testExtensions;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets test extension information from the given colletion of files.
        /// </summary>
        /// <param name="files">List of dll's to check for test extension availability</param>
        /// <param name="loadOnlyWellKnownExtensions">Should load only well known extensions or all.</param>
        /// <param name="testDiscoverers">Test discoverers collection to add to.</param>
        /// <param name="testExecutors">Test executors collection to add to.</param>
        /// <param name="testSettingsProviders">Test settings providers collection to add to.</param>
        /// <param name="testLoggers">Test loggers collection to add to.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We would like to continue discovering all plugins even if some dll in Extensions folder is not able to be load properly")]
        private void GetTestExtensionsFromFiles(
                        string[] files,
                        bool loadOnlyWellKnownExtensions,
                        Dictionary<string, TestDiscovererPluginInformation> testDiscoverers,
                        Dictionary<string, TestExecutorPluginInformation> testExecutors,
                        Dictionary<string, TestSettingsProviderPluginInformation> testSettingsProviders,
                        Dictionary<string, TestLoggerPluginInformation> testLoggers)
        {
            Debug.Assert(files != null, "null files");
            Debug.Assert(testDiscoverers != null, "null testDiscoverers");
            Debug.Assert(testExecutors != null, "null testExecutors");
            Debug.Assert(testSettingsProviders != null, "null testSettingsProviders");
            Debug.Assert(testLoggers != null, "null testLoggers");

            // TODO: Do not see why loadOnlyWellKnowExtensions is still needed.
            //AssemblyName executingAssemblyName = null;
            //if (loadOnlyWellKnownExtensions)
            //{
            //    executingAssemblyName = new AssemblyName(typeof(TestPluginDiscoverer).GetTypeInfo().Assembly.FullName);
            //}

            // Scan each of the files for data extensions.
            foreach (var file in files)
            {
                Assembly assembly = null;
                try
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(file);
                    assembly = Assembly.Load(new AssemblyName(assemblyName));
                    
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
                    EqtTrace.Warning("TestPluginDiscoverer: Failed to load file '{0}'.  Skipping test extension scan for this file.  Error: {1}", file, e.ToString());
                    continue;
                }

                if (assembly != null)
                {
                    this.GetTestExtensionsFromAssembly(assembly, testDiscoverers, testExecutors, testSettingsProviders, testLoggers);
                }
            }
        }

        /// <summary>
        /// Gets test extensions from a given assembly.
        /// </summary>
        /// <param name="assembly">Assembly to check for test extension availability</param>
        /// <param name="testDiscoverers">Test discoverers collection to add to.</param>
        /// <param name="testExecutors">Test executors collection to add to.</param>
        /// <param name="testSettingsProviders">Test settings providers collection to add to.</param>
        /// <param name="testLoggers">Test loggers collection to add to.</param>
        private void GetTestExtensionsFromAssembly(
                        Assembly assembly,
                        Dictionary<string, TestDiscovererPluginInformation> testDiscoverers,
                        Dictionary<string, TestExecutorPluginInformation> testExecutors,
                        Dictionary<string, TestSettingsProviderPluginInformation> testSettingsProviders,
                        Dictionary<string, TestLoggerPluginInformation> testLoggers)
        {
            Debug.Assert(assembly != null, "null assembly");
            Debug.Assert(testDiscoverers != null, "null testDiscoverers");
            Debug.Assert(testExecutors != null, "null testExecutors");
            Debug.Assert(testSettingsProviders != null, "null testSettingsProviders");
            Debug.Assert(testLoggers != null, "null testLoggers");

            Type[] types = null;
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
                        this.GetTestExtensionFromType<TestDiscovererPluginInformation>(type, typeof(ITestDiscoverer), testDiscoverers);
                        this.GetTestExtensionFromType<TestExecutorPluginInformation>(type, typeof(ITestExecutor), testExecutors);
                        this.GetTestExtensionFromType<TestLoggerPluginInformation>(type, typeof(ITestLogger), testLoggers);
                        this.GetTestExtensionFromType<TestSettingsProviderPluginInformation>(type, typeof(ISettingsProvider), testSettingsProviders);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to find a test extension from given type.
        /// </summary>
        /// <typeparam name="TPluginInfo">Data type of the test plugin information</typeparam>
        /// <param name="type">Type to inspect for being test extension</param>
        /// <param name="extensionType">Test extension type to look for.</param>
        /// <param name="extensionCollection">Test extensions collection to add to.</param>
        /// <returns>True if test extension is found, false otherwise.</returns>
        private void GetTestExtensionFromType<TPluginInfo>(
                                                    Type type,
                                                    Type extensionType,
                                                    Dictionary<string, TPluginInfo> extensionCollection)
                                                    where TPluginInfo : TestPluginInformation
        {
            if (extensionType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                var dataObject = Activator.CreateInstance(typeof(TPluginInfo), type);
                var pluginInfo = (TPluginInfo)dataObject;

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
