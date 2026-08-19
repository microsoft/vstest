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
    /// <summary>
    /// Files that we already failed to load. The same file is scanned once per extension type we look for
    /// (test adapters, loggers, data collectors, settings providers, ...), so this both avoids repeating a load
    /// that is known to fail, and makes sure the user is told about the failure only once.
    /// </summary>
    private static readonly ConcurrentDictionary<string, object?> UnloadableFiles = new();

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
        var probeForKnownExtensions = !extensionPaths.Any();
        if (probeForKnownExtensions)
        {
            AddKnownExtensions(ref extensionPaths);
        }

        // The known extensions are just a guess, they are not expected to be present, so failing to load them is
        // normal and must not be reported to the user.
        GetTestExtensionsFromFiles<TPluginInfo, TExtension>(extensionPaths.ToArray(), pluginInfos, reportFailures: !probeForKnownExtensions);

        return pluginInfos;
    }

    private static void AddKnownExtensions(ref IEnumerable<string> extensionPaths)
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
    /// <param name="reportFailures">
    /// When true, files that fail to load are reported to the user as warnings.
    /// </param>
    private static void GetTestExtensionsFromFiles<TPluginInfo, TExtension>(
        string[] files,
        Dictionary<string, TPluginInfo> pluginInfos,
        bool reportFailures)
        where TPluginInfo : TestPluginInformation
    {
        TPDebug.Assert(files != null, "null files");
        TPDebug.Assert(pluginInfos != null, "null pluginInfos");

        // Scan each of the files for data extensions.
        foreach (var file in files)
        {
            if (UnloadableFiles.ContainsKey(file))
            {
                continue;
            }

            Assembly assembly;
            try
            {
                var assemblyName = Path.GetFileNameWithoutExtension(file);
                assembly = Assembly.Load(new AssemblyName(assemblyName));
            }
            catch (Exception e)
            {
                EqtTrace.Warning("TestPluginDiscoverer: Failed to load extensions from file '{0}'.  Skipping test extension scan for this file.  Error: {1}", file, e);

                // The file cannot be loaded at all, don't try again for the other extension types, and tell the user
                // about it once. Without this the extension is just silently missing, and the tests it provides are
                // silently not run.
                var isFirstFailureForFile = UnloadableFiles.TryAdd(file, null);
                if (isFirstFailureForFile && reportFailures)
                {
                    ReportFailureToLoadExtensions(file, e.Message);
                }

                continue;
            }

            try
            {
                GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(assembly, pluginInfos, file, reportFailures);
            }
            catch (Exception e)
            {
                // The assembly itself loaded, only inspecting it failed. Keep this quiet, it is a problem of a
                // single extension type and the file may still provide extensions of another type.
                EqtTrace.Warning("TestPluginDiscoverer: Failed to get extensions from file '{0}'.  Skipping test extension scan for this file.  Error: {1}", file, e);
            }
        }
    }

    /// <summary>
    /// Gets test extensions from a given assembly.
    /// </summary>
    /// <param name="assembly">Assembly to check for test extension availability</param>
    /// <param name="pluginInfos">Test extensions collection to add to.</param>
    /// <param name="filePath">File path of the assembly.</param>
    /// <param name="reportFailures">When true, an assembly from which no type can be loaded is reported to the user as a warning.</param>
    /// <typeparam name="TPluginInfo">
    /// Type of Test Plugin Information.
    /// </typeparam>
    /// <typeparam name="TExtension">
    /// Type of Extensions.
    /// </typeparam>
    private static void GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(Assembly assembly, Dictionary<string, TPluginInfo> pluginInfos, string filePath, bool reportFailures)
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

            // Not a single type came out of the assembly, so it cannot provide any extension. When some types did
            // load we stay quiet, the extension most likely still works, and the types that did not load are
            // usually not extensions at all.
            if (types.Count == 0)
            {
                var isFirstFailureForFile = UnloadableFiles.TryAdd(filePath, null);
                if (isFirstFailureForFile && reportFailures)
                {
                    ReportFailureToLoadExtensions(filePath, GetLoaderExceptionsMessage(e));
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

    /// <summary>
    /// Tells the user that a file that looks like a test extension could not be loaded.
    /// </summary>
    /// <param name="filePath">File path of the extension.</param>
    /// <param name="reason">Why the file could not be loaded.</param>
    private static void ReportFailureToLoadExtensions(string filePath, string reason)
        => TestSessionMessageLogger.Instance.SendMessage(
            TestMessageLevel.Warning,
            string.Format(CultureInfo.CurrentCulture, CommonResources.FailedToLoadExtensionFile, filePath, reason));

    /// <summary>
    /// Joins the messages of the loader exceptions, they say why the types could not be loaded, e.g. which
    /// dependency is missing.
    /// </summary>
    private static string GetLoaderExceptionsMessage(ReflectionTypeLoadException exception)
    {
        var reasons = exception.LoaderExceptions?
            .Where(loaderException => loaderException != null)
            .Select(loaderException => loaderException!.Message)
            .Distinct()
            .ToArray();

        return reasons?.Length > 0
            ? string.Join(Environment.NewLine, reasons)
            : exception.Message;
    }
}
