// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
#if NETFRAMEWORK
    using System.Threading;
#endif
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

    /// <summary>
    /// The test plugin cache.
    /// </summary>
    /// <remarks>Making this a singleton to offer better unit testing.</remarks>
    public class TestPluginCache
    {
        #region Private Members

        private readonly Dictionary<string, Assembly> resolvedAssemblies;

        private List<string> filterableExtensionPaths;
        private List<string> unfilterableExtensionPaths;

        /// <summary>
        /// Assembly resolver used to resolve the additional extensions
        /// </summary>
        private AssemblyResolver assemblyResolver;

        /// <summary>
        /// Lock for extensions update
        /// </summary>
        private object lockForExtensionsUpdate;

        private static TestPluginCache instance;

        private List<string> defaultExtensionPaths = new List<string>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPluginCache"/> class.
        /// </summary>
        protected TestPluginCache()
        {
            this.resolvedAssemblies = new Dictionary<string, Assembly>();
            this.filterableExtensionPaths = new List<string>();
            this.unfilterableExtensionPaths = new List<string>();
            this.lockForExtensionsUpdate = new object();
            this.TestExtensions = null;
        }

        #endregion

        #region Public Properties

        public static TestPluginCache Instance
        {
            get
            {
                return instance ?? (instance = new TestPluginCache());
            }

            internal set
            {
                instance = value;
            }
        }

        /// <summary>
        /// Gets the test extensions discovered by the cache until now.
        /// </summary>
        /// <remarks>Returns null if discovery of extensions is not done.</remarks>
        internal TestExtensions TestExtensions { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a list of all extension paths filtered by input string.
        /// </summary>
        /// <param name="endsWithPattern">Pattern to filter extension paths.</param>
        public List<string> GetExtensionPaths(string endsWithPattern, bool skipDefaultExtensions = false)
        {
            var extensions = this.GetFilteredExtensions(this.filterableExtensionPaths, endsWithPattern);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose(
                    "TestPluginCache.GetExtensionPaths: Filtered extension paths: {0}", string.Join(Environment.NewLine, extensions));
            }

            if (!skipDefaultExtensions)
            {
                extensions = extensions.Concat(this.defaultExtensionPaths);
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose(
                        "TestPluginCache.GetExtensionPaths: Added default extension paths: {0}", string.Join(Environment.NewLine, this.defaultExtensionPaths));
                }
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose(
                    "TestPluginCache.GetExtensionPaths: Added unfilterableExtensionPaths: {0}", string.Join(Environment.NewLine, this.unfilterableExtensionPaths));
            }

            return extensions.Concat(this.unfilterableExtensionPaths).ToList();
        }

        /// <summary>
        /// Performs discovery of specific type of test extensions in files ending with the specified pattern.
        /// </summary>
        /// <typeparam name="TPluginInfo">
        /// Type of Plugin info.
        /// </typeparam>
        /// <typeparam name="TExtension">
        /// Type of extension.
        /// </typeparam>
        /// <param name="endsWithPattern">
        /// Pattern used to select files using String.EndsWith
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary"/>. of test plugin info.
        /// </returns>
        public Dictionary<string, TPluginInfo> DiscoverTestExtensions<TPluginInfo, TExtension>(string endsWithPattern)
            where TPluginInfo : TestPluginInformation
        {
            EqtTrace.Verbose("TestPluginCache.DiscoverTestExtensions: finding test extensions in assemblies ends with: {0} TPluginInfo: {1} TExtension: {2}", endsWithPattern, typeof(TPluginInfo), typeof(TExtension));
            // Return the cached value if cache is valid.
            if (this.TestExtensions != null && this.TestExtensions.AreTestExtensionsCached<TPluginInfo>())
            {
                return this.TestExtensions.GetTestExtensionCache<TPluginInfo>();
            }

            Dictionary<string, TPluginInfo> pluginInfos = null;
            this.SetupAssemblyResolver(null);

            // Some times TestPlatform.core.dll assembly fails to load in the current appdomain (from devenv.exe).
            // Reason for failures are not known. Below handler, again calls assembly.load() in failing assembly
            // and that succeeds.
            // Because of this assembly failure, below domain.CreateInstanceAndUnwrap() call fails with error
            // "Unable to cast transparent proxy to type 'Microsoft.VisualStudio.TestPlatform.Core.TestPluginsFramework.TestPluginDiscoverer"
            var platformAssemblyResolver = new PlatformAssemblyResolver();
            platformAssemblyResolver.AssemblyResolve += this.CurrentDomainAssemblyResolve;

            try
            {
                EqtTrace.Verbose("TestPluginCache.DiscoverTestExtensions: Discovering the extensions using extension path.");

                // Combine all the possible extensions - both default and additional.
                var allExtensionPaths = this.GetExtensionPaths(endsWithPattern);

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose(
                        "TestPluginCache.DiscoverTestExtensions: Discovering the extensions using allExtensionPaths: {0}", string.Join(Environment.NewLine, allExtensionPaths));
                }

                // Discover the test extensions from candidate assemblies.
                pluginInfos = this.GetTestExtensions<TPluginInfo, TExtension>(allExtensionPaths);

                if (this.TestExtensions == null)
                {
                    this.TestExtensions = new TestExtensions();
                }

                this.TestExtensions.AddExtension<TPluginInfo>(pluginInfos);

                // Set the cache bool to true.
                this.TestExtensions.SetTestExtensionsCacheStatus<TPluginInfo>();

                if (EqtTrace.IsVerboseEnabled)
                {
                    var extensionString = this.filterableExtensionPaths != null
                                              ? string.Join(",", this.filterableExtensionPaths.ToArray())
                                              : null;
                    EqtTrace.Verbose(
                        "TestPluginCache: Discovered the extensions using extension path '{0}'.",
                        extensionString);
                }

                this.LogExtensions();
            }
#if NETFRAMEWORK
            catch (ThreadAbortException)
            {
                // Nothing to do here, we just do not want to do an EqtTrace.Fail for this thread
                // being aborted as it is a legitimate exception to receive.
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestPluginCache.DiscoverTestExtensions: Data extension discovery is being aborted due to a thread abort.");
                }
            }
#endif
            catch (Exception e)
            {
                EqtTrace.Error("TestPluginCache: Discovery failed! {0}", e);
                throw;
            }
            finally
            {
                if (platformAssemblyResolver != null)
                {
                    platformAssemblyResolver.AssemblyResolve -= this.CurrentDomainAssemblyResolve;
                    platformAssemblyResolver.Dispose();
                }

                // clear the assemblies
                lock (this.resolvedAssemblies)
                {
                    this.resolvedAssemblies?.Clear();
                }
            }

            return pluginInfos;
        }

        /// <summary>
        /// Use the parameter path to extensions
        /// </summary>
        /// <param name="additionalExtensionsPath">List of extension paths</param>
        /// <param name="skipExtensionFilters">Skip extension name filtering (if true)</param>
        public void UpdateExtensions(IEnumerable<string> additionalExtensionsPath, bool skipExtensionFilters)
        {
            lock (this.lockForExtensionsUpdate)
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestPluginCache: Update extensions started. Skip filter = " + skipExtensionFilters);
                }

                var extensions = additionalExtensionsPath?.ToList();
                if (extensions == null || extensions.Count == 0)
                {
                    return;
                }

                if (skipExtensionFilters)
                {
                    // Add the extensions to un-filter list. These extensions will never be filtered
                    // based on file name (e.g. *.testadapter.dll etc.).
                    if (TryMergeExtensionPaths(this.unfilterableExtensionPaths, extensions,
                        out this.unfilterableExtensionPaths))
                    {
                        // Set the extensions discovered to false so that the next time anyone tries
                        // to get the additional extensions, we rediscover.
                        this.TestExtensions?.InvalidateCache();
                    }
                }
                else
                {
                    if (TryMergeExtensionPaths(this.filterableExtensionPaths, extensions,
                        out this.filterableExtensionPaths))
                    {
                        this.TestExtensions?.InvalidateCache();
                    }
                }

                if (EqtTrace.IsVerboseEnabled)
                {
                    var directories = this.filterableExtensionPaths.Concat(this.unfilterableExtensionPaths).Select(e => Path.GetDirectoryName(Path.GetFullPath(e))).Distinct();
                    var directoryString = string.Join(",", directories);
                    EqtTrace.Verbose(
                        "TestPluginCache: Using directories for assembly resolution '{0}'.",
                        directoryString);

                    var extensionString = string.Join(",", this.filterableExtensionPaths.Concat(this.unfilterableExtensionPaths));
                    EqtTrace.Verbose("TestPluginCache: Updated the available extensions to '{0}'.", extensionString);
                }
            }
        }

        /// <summary>
        /// Clear the previously cached extensions
        /// </summary>
        public void ClearExtensions()
        {
            this.filterableExtensionPaths?.Clear();
            this.unfilterableExtensionPaths?.Clear();
            this.TestExtensions?.InvalidateCache();
        }

        /// <summary>
        /// Add search directories to assembly resolver
        /// </summary>
        /// <param name="directories"></param>
        public void AddResolverSearchDirectories(string[] directories)
        {
            assemblyResolver.AddSearchDirectories(directories);
        }

        #endregion

        #region Utility methods

        internal IEnumerable<string> DefaultExtensionPaths
        {
            get
            {
                return this.defaultExtensionPaths;
            }

            set
            {
                if (value != null)
                {
                    this.defaultExtensionPaths.AddRange(value);
                }
            }
        }

        /// <summary>
        /// The get test extensions.
        /// </summary>
        /// <param name="extensionAssembly">
        /// The extension assembly.
        /// </param>
        /// <typeparam name="TPluginInfo">
        /// Type of Test plugin info.
        /// </typeparam>
        /// <typeparam name="TExtension">
        /// Type of extension.
        /// </typeparam>
        /// <returns>
        /// The <see cref="Dictionary"/>.
        /// </returns>
        internal Dictionary<string, TPluginInfo> GetTestExtensions<TPluginInfo, TExtension>(string extensionAssembly) where TPluginInfo : TestPluginInformation
        {
            // Check if extensions from this assembly have already been discovered.
            var extensions = this.TestExtensions?.GetExtensionsDiscoveredFromAssembly<TPluginInfo>(
                this.TestExtensions.GetTestExtensionCache<TPluginInfo>(),
                extensionAssembly);

            if (extensions != null)
            {
                return extensions;
            }

            var pluginInfos = this.GetTestExtensions<TPluginInfo, TExtension>(new List<string>() { extensionAssembly });

            // Add extensions discovered to the cache.
            if (this.TestExtensions == null)
            {
                this.TestExtensions = new TestExtensions();
            }

            this.TestExtensions.AddExtension<TPluginInfo>(pluginInfos);
            return pluginInfos;
        }

        /// <summary>
        /// Gets the resolution paths for the extension assembly to facilitate assembly resolution.
        /// </summary>
        /// <param name="extensionAssembly">The extension assembly.</param>
        /// <returns>Resolution paths for the assembly.</returns>
        internal IList<string> GetResolutionPaths(string extensionAssembly)
        {
            var resolutionPaths = new List<string>();

            var extensionDirectory = Path.GetDirectoryName(Path.GetFullPath(extensionAssembly));
            resolutionPaths.Add(extensionDirectory);

            var currentDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.GetAssemblyLocation());
            if (!resolutionPaths.Contains(currentDirectory))
            {
                resolutionPaths.Add(currentDirectory);
            }

            return resolutionPaths;
        }

        /// <summary>
        /// Gets the default set of resolution paths for the assembly resolution
        /// </summary>
        /// <returns>List of paths.</returns>
        internal IList<string> GetDefaultResolutionPaths()
        {
            var resolutionPaths = new List<string>();

            // Add the extension directories for assembly resolution
            var extensionDirectories = this.GetExtensionPaths(string.Empty).Select(e => Path.GetDirectoryName(Path.GetFullPath(e))).Distinct().ToList();
            if (extensionDirectories.Any())
            {
                resolutionPaths.AddRange(extensionDirectories);
            }

            // Keep current directory for resolution
            var currentDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.GetAssemblyLocation());
            if (!resolutionPaths.Contains(currentDirectory))
            {
                resolutionPaths.Add(currentDirectory);
            }

            // If running in Visual Studio context, add well known directories for resolution
            var installContext = new InstallationContext(new FileHelper());
            if (installContext.TryGetVisualStudioDirectory(out string vsInstallPath))
            {
                resolutionPaths.AddRange(installContext.GetVisualStudioCommonLocations(vsInstallPath));
            }

            return resolutionPaths;
        }

        /// <summary>
        /// Get the files which match the regex pattern
        /// </summary>
        /// <param name="extensions">
        /// The extensions.
        /// </param>
        /// <param name="endsWithPattern">
        /// Pattern used to select files using String.EndsWith
        /// </param>
        /// <returns>
        /// The list of files which match the regex pattern
        /// </returns>
        protected virtual IEnumerable<string> GetFilteredExtensions(List<string> extensions, string endsWithPattern)
        {
            if (string.IsNullOrEmpty(endsWithPattern))
            {
                return extensions;
            }

            return extensions.Where(ext => ext.EndsWith(endsWithPattern, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryMergeExtensionPaths(List<string> extensionsList, List<string> additionalExtensions, out List<string> mergedExtensionsList)
        {
            if (additionalExtensions.Count == extensionsList.Count && additionalExtensions.All(extensionsList.Contains))
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    var extensionString = string.Join(",", extensionsList);
                    EqtTrace.Verbose(
                        "TestPluginCache: Ignoring extensions merge as there is no change. Current additionalExtensions are '{0}'.",
                        extensionString);
                }

                mergedExtensionsList = extensionsList;
                return false;
            }

            // Don't do a strict check for existence of the extension path. The extension paths may or may
            // not exist on the disk. In case of .net core, the paths are relative to the nuget packages
            // directory. The path to nuget directory is automatically setup for CLR to resolve.
            // Test platform tries to load every extension by assembly name. If it is not resolved, we don't throw
            // an error.
            additionalExtensions.AddRange(extensionsList);
            mergedExtensionsList = additionalExtensions.Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return true;
        }

        /// <summary>
        /// Gets the test extensions defined in the extension assembly list.
        /// </summary>
        /// <typeparam name="TPluginInfo">
        /// Type of PluginInfo.
        /// </typeparam>
        /// <typeparam name="TExtension">
        /// Type of Extension.
        /// </typeparam>
        /// <param name="extensionPaths">
        /// Extension assembly paths.
        /// </param>
        /// <returns>
        /// List of extensions.
        /// </returns>
        /// <remarks>
        /// Added to mock out dependency from the actual test plugin discovery as such.
        /// </remarks>
        private Dictionary<string, TPluginInfo> GetTestExtensions<TPluginInfo, TExtension>(IEnumerable<string> extensionPaths) where TPluginInfo : TestPluginInformation
        {
            foreach (var extensionPath in extensionPaths)
            {
                this.SetupAssemblyResolver(extensionPath);
            }

            var discoverer = new TestPluginDiscoverer();

            return discoverer.GetTestExtensionsInformation<TPluginInfo, TExtension>(extensionPaths);
        }

        protected void SetupAssemblyResolver(string extensionAssembly)
        {
            IList<string> resolutionPaths;

            if (string.IsNullOrEmpty(extensionAssembly))
            {
                resolutionPaths = this.GetDefaultResolutionPaths();
            }
            else
            {
                resolutionPaths = this.GetResolutionPaths(extensionAssembly);
            }

            // Add assembly resolver which can resolve the extensions from the specified directory.
            if (this.assemblyResolver == null)
            {
                this.assemblyResolver = new AssemblyResolver(resolutionPaths);
            }
            else
            {
                this.assemblyResolver.AddSearchDirectories(resolutionPaths);
            }
        }

        private Assembly CurrentDomainAssemblyResolve(object sender, AssemblyResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            Assembly assembly = null;
            lock (this.resolvedAssemblies)
            {
                try
                {
                    EqtTrace.Verbose("CurrentDomain_AssemblyResolve: Resolving assembly '{0}'.", args.Name);

                    if (this.resolvedAssemblies.TryGetValue(args.Name, out assembly))
                    {
                        return assembly;
                    }

                    // Put it in the resolved assembly so that if below Assembly.Load call
                    // triggers another assembly resolution, then we don't end up in stack overflow
                    this.resolvedAssemblies[args.Name] = null;

                    assembly = Assembly.Load(assemblyName);

                    // Replace the value with the loaded assembly
                    this.resolvedAssemblies[args.Name] = assembly;

                    return assembly;
                }
                finally
                {
                    if (assembly == null)
                    {
                        EqtTrace.Verbose("CurrentDomainAssemblyResolve: Failed to resolve assembly '{0}'.", args.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Log the extensions
        /// </summary>
        private void LogExtensions()
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                var discoverers = this.TestExtensions.TestDiscoverers != null ? string.Join(",", this.TestExtensions.TestDiscoverers.Keys.ToArray()) : null;
                EqtTrace.Verbose("TestPluginCache: Discoverers are '{0}'.", discoverers);

                var executors = this.TestExtensions.TestExecutors != null ? string.Join(",", this.TestExtensions.TestExecutors.Keys.ToArray()) : null;
                EqtTrace.Verbose("TestPluginCache: Executors are '{0}'.", executors);

                var executors2 = this.TestExtensions.TestExecutors2 != null ? string.Join(",", this.TestExtensions.TestExecutors2.Keys.ToArray()) : null;
                EqtTrace.Verbose("TestPluginCache: Executors2 are '{0}'.", executors2);

                var settingsProviders = this.TestExtensions.TestSettingsProviders != null ? string.Join(",", this.TestExtensions.TestSettingsProviders.Keys.ToArray()) : null;
                EqtTrace.Verbose("TestPluginCache: Setting providers are '{0}'.", settingsProviders);

                var loggers = this.TestExtensions.TestLoggers != null ? string.Join(",", this.TestExtensions.TestLoggers.Keys.ToArray()) : null;
                EqtTrace.Verbose("TestPluginCache: Loggers are '{0}'.", loggers);
            }
        }

        #endregion
    }
}
