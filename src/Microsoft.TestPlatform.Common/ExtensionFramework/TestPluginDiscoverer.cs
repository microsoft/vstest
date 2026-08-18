// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;

/// <summary>
/// Discovers test extensions in a directory.
/// </summary>
internal static class TestPluginDiscoverer
{
    private static readonly HashSet<string> UnloadableFiles = new();

    /// <summary>
    /// Files we already told the user about, so that a file that fails for every extension type is
    /// reported once per run instead of once per scan.
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> ReportedFiles = new();

    /// <summary>
    /// Extensions that are probed speculatively when no other extension was found, see <see cref="AddKnownExtensions"/>.
    /// They are absent in every environment except UWP, so failing to load them is expected and is not reported.
    /// </summary>
    private static readonly string[] KnownExtensions =
    {
        "Microsoft.VisualStudio.TestTools.CppUnitTestFramework.CppUnitTestExtension.dll",
        "Microsoft.VisualStudio.TestPlatform.Extensions.MSAppContainerAdapter.dll",
    };

    /// <summary>
    /// Gets information about each of the test extensions available.
    /// </summary>
    /// <param name="extensionPaths">
    ///     The path to the extensions.
    /// </param>
    /// <returns>
    /// A dictionary of assembly qualified name and test plugin information.
    /// </returns>
    public static Dictionary<string, TPluginInfo> GetTestExtensionsInformation<TPluginInfo, TExtension>(IEnumerable<string> extensionPaths) where TPluginInfo : TestPluginInformation
    {
        TPDebug.Assert(extensionPaths != null);

        var pluginInfos = new Dictionary<string, TPluginInfo>();

        // C++ UWP adapters do not follow TestAdapater naming convention, so making this exception
        if (!extensionPaths.Any())
        {
            AddKnownExtensions(ref extensionPaths);
        }

        GetTestExtensionsFromFiles<TPluginInfo, TExtension>(extensionPaths.ToArray(), pluginInfos);

        return pluginInfos;
    }

    private static void AddKnownExtensions(ref IEnumerable<string> extensionPaths)
    {
        // For C++ UWP adapter, & OLD C# UWP(MSTest V1) adapter
        // In UWP .Net Native Compilation mode managed dll's are packaged differently, & File.Exists() fails.
        // Include these two dll's if so far no adapters(extensions) were found, & let Assembly.Load() fail if they are not present.
        extensionPaths = extensionPaths.Concat(KnownExtensions);
    }

    /// <summary>
    /// Tells the user that an extension file did not load. Extension scanning is best effort, so this is a
    /// warning and never stops the run, but staying silent leaves the user with fewer extensions than they
    /// expect and no way to find out why short of re-running with /diag.
    /// </summary>
    /// <param name="file">The file that failed to load.</param>
    private static void ReportExtensionLoadFailure(string file)
    {
        // Many files are scanned per run, and the same file is scanned once per extension type, so report it once.
        if (!ReportedFiles.TryAdd(file, 0))
        {
            return;
        }

        // This runs inside a catch block. Reporting a load failure must never turn into a second failure that
        // escapes and takes down a run that would otherwise have finished, so swallow anything that goes wrong
        // here, for instance a satellite assembly that cannot be resolved while formatting the message.
        try
        {
            string message = string.Format(CultureInfo.CurrentCulture, CommonResources.FailedToLoadAdapaterFile, file);
            TestSessionMessageLogger.Instance.SendMessage(TestMessageLevel.Warning, message);
        }
        catch (Exception e)
        {
            EqtTrace.Warning("TestPluginDiscoverer: Failed to report the load failure of file '{0}'. Error: {1}", file, e);
        }
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
    private static void GetTestExtensionsFromFiles<TPluginInfo, TExtension>(
        string[] files,
        Dictionary<string, TPluginInfo> pluginInfos)
        where TPluginInfo : TestPluginInformation
    {
        TPDebug.Assert(files != null, "null files");
        TPDebug.Assert(pluginInfos != null, "null pluginInfos");

        // Scan each of the files for data extensions.
        foreach (var file in files)
        {
            if (UnloadableFiles.Contains(file))
            {
                continue;
            }
            try
            {
                var assemblyName = Path.GetFileNameWithoutExtension(file);
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                if (assembly != null)
                {
                    GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(assembly, pluginInfos, file);
                }
            }
            catch (FileLoadException e)
            {
                EqtTrace.Warning("TestPluginDiscoverer-FileLoadException: Failed to load extensions from file '{0}'.  Skipping test extension scan for this file.  Error: {1}", file, e);
                ReportExtensionLoadFailure(file);
                UnloadableFiles.Add(file);
            }
            catch (Exception e)
            {
                EqtTrace.Warning("TestPluginDiscoverer: Failed to load extensions from file '{0}'.  Skipping test extension scan for this file.  Error: {1}", file, e);

                // This is the handler that catches FileNotFoundException, which is what Assembly.Load throws when
                // the extension, or one of its dependencies, cannot be found. That is a real problem for the user,
                // so report it instead of only tracing it. The file is deliberately not added to UnloadableFiles:
                // unlike FileLoadException this also catches failures from scanning an assembly that did load, and
                // resolution can succeed on a later pass once more extension directories are registered.
                //
                // The speculatively probed extensions are the exception, they are expected to be missing everywhere
                // except UWP and reporting them would warn on every run.
                if (!KnownExtensions.Contains(file, StringComparer.OrdinalIgnoreCase))
                {
                    ReportExtensionLoadFailure(file);
                }
            }
        }
    }

    /// <summary>
    /// Gets test extensions from a given assembly.
    /// </summary>
    /// <param name="assembly">Assembly to check for test extension availability</param>
    /// <param name="pluginInfos">Test extensions collection to add to.</param>
    /// <param name="filePath">File path of the assembly.</param>
    /// <typeparam name="TPluginInfo">
    /// Type of Test Plugin Information.
    /// </typeparam>
    /// <typeparam name="TExtension">
    /// Type of Extensions.
    /// </typeparam>
    internal static void GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(Assembly assembly, Dictionary<string, TPluginInfo> pluginInfos, string filePath)
        where TPluginInfo : TestPluginInformation
    {
        TPDebug.Assert(assembly != null, "null assembly");
        TPDebug.Assert(pluginInfos != null, "null pluginInfos");

        List<Type> types = new();
        Type extension = typeof(TExtension);

        try
        {
            var discoveredExtensions = MetadataReaderExtensionsHelper.DiscoverTestExtensionTypesV2Attribute(assembly, filePath);
            if (discoveredExtensions?.Length > 0)
            {
                types.AddRange(discoveredExtensions);
            }
        }
        catch (Exception e)
        {
            EqtTrace.Warning("TestPluginDiscoverer: Failed to get types searching for 'TestPlatformExtensionVersionAttribute' from assembly '{0}'. Error: {1}", assembly.FullName, e.ToString());
        }

        try
        {
            var typesToLoad = TypesToLoadUtilities.GetTypesToLoad(assembly);
            if (typesToLoad?.Any() == true)
            {
                types.AddRange(typesToLoad);
            }

            if (types.Count == 0)
            {
                types.AddRange(assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract));
            }
        }
        catch (ReflectionTypeLoadException e)
        {
            EqtTrace.Warning("TestPluginDiscoverer: Failed to get types from assembly '{0}'. Error: {1}", assembly.FullName, e.ToString());

            // The assembly itself loaded, but some of its types did not, usually because a dependency is
            // missing. Scanning continues with the types that did load, so without this the user silently
            // gets fewer extensions than the assembly declares. The file is deliberately not added to
            // UnloadableFiles, the types that did load are still worth scanning on the next pass.
            ReportExtensionLoadFailure(filePath);

            if (e.Types?.Length > 0)
            {
                // Unloaded types on e.Types are null, make sure we skip them.
                types.AddRange(e.Types.Where(type => type != null && type.IsClass && !type.IsAbstract)!);
            }

            if (e.LoaderExceptions != null)
            {
                foreach (var ex in e.LoaderExceptions)
                {
                    EqtTrace.Warning("LoaderExceptions: {0}", ex);
                }
            }
        }

        if (types != null && types.Count != 0)
        {
            foreach (var type in types)
            {
                GetTestExtensionFromType(type, extension, pluginInfos, filePath);
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
    /// <param name="filePath">File path of the assembly.</param>
    private static void GetTestExtensionFromType<TPluginInfo>(
        Type type,
        Type extensionType,
        Dictionary<string, TPluginInfo> extensionCollection,
        string filePath)
        where TPluginInfo : TestPluginInformation
    {
        if (!extensionType.IsAssignableFrom(type))
        {
            return;
        }

        var rawPluginInfo = Activator.CreateInstance(typeof(TPluginInfo), type);
        TPDebug.Assert(rawPluginInfo is TPluginInfo, "rawPluginInfo is not of type TPluginInfo");
        var pluginInfo = (TPluginInfo)rawPluginInfo;
        pluginInfo.FilePath = filePath;

        if (pluginInfo == null || pluginInfo.IdentifierData == null)
        {
            EqtTrace.Error(
                "GetTestExtensionFromType: Either PluginInformation is null or PluginInformation doesn't contain IdentifierData for type {0}.", type.FullName);
            return;
        }

        if (extensionCollection.ContainsKey(pluginInfo.IdentifierData))
        {
            EqtTrace.Warning(
                "GetTestExtensionFromType: Discovered multiple test extensions with identifier data '{0}' and type '{1}' inside file '{2}'; keeping the first one '{3}'.",
                pluginInfo.IdentifierData, pluginInfo.AssemblyQualifiedName, filePath, extensionCollection[pluginInfo.IdentifierData].AssemblyQualifiedName);
        }
        else
        {
            extensionCollection.Add(pluginInfo.IdentifierData, pluginInfo);
            EqtTrace.Info("GetTestExtensionFromType: Register extension with identifier data '{0}' and type '{1}' inside file '{2}'",
                pluginInfo.IdentifierData, pluginInfo.AssemblyQualifiedName, filePath);
        }
    }

}
