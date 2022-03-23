// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;

/// <summary>
/// The test extension information.
/// </summary>
public class TestExtensions
{
    /// <summary>
    /// Gets or sets test discoverer extensions.
    /// </summary>
    internal Dictionary<string, TestDiscovererPluginInformation> TestDiscoverers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether are test discoverers cached.
    /// </summary>
    internal bool AreTestDiscoverersCached { get; set; }

    /// <summary>
    /// Gets or sets test executor extensions.
    /// </summary>
    internal Dictionary<string, TestExecutorPluginInformation> TestExecutors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether are test executors cached.
    /// </summary>
    internal bool AreTestExecutorsCached { get; set; }

    /// <summary>
    /// Gets or sets test executor 2 extensions.
    /// </summary>
    internal Dictionary<string, TestExecutorPluginInformation2> TestExecutors2 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether are test executors 2 cached.
    /// </summary>
    internal bool AreTestExecutors2Cached { get; set; }

    /// <summary>
    /// Gets or sets test setting provider extensions.
    /// </summary>
    internal Dictionary<string, TestSettingsProviderPluginInformation> TestSettingsProviders { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether are test settings providers cached.
    /// </summary>
    internal bool AreTestSettingsProvidersCached { get; set; }

    /// <summary>
    /// Gets or sets test logger extensions.
    /// </summary>
    internal Dictionary<string, TestLoggerPluginInformation> TestLoggers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether are test loggers cached.
    /// </summary>
    internal bool AreTestLoggersCached { get; set; }

    /// <summary>
    /// Gets or sets test logger extensions.
    /// </summary>
    internal Dictionary<string, TestRuntimePluginInformation> TestHosts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether are test hosts cached.
    /// </summary>
    internal bool AreTestHostsCached { get; set; }

    /// <summary>
    /// Gets or sets data collectors extensions.
    /// </summary>
    internal Dictionary<string, DataCollectorConfig> DataCollectors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether are test hosts cached.
    /// </summary>
    internal bool AreDataCollectorsCached { get; set; }

    /// <summary>
    /// Merge two extension maps.
    /// </summary>
    /// 
    /// <param name="extensionMap1">First extension map.</param>
    /// <param name="extensionMap2">Second extension map.</param>
    /// 
    /// <returns>
    /// A map representing the merger between the two input maps.
    /// </returns>
    internal static IDictionary<string, ISet<string>> MergeExtensionMaps(
        IDictionary<string, ISet<string>> extensionMap1,
        IDictionary<string, ISet<string>> extensionMap2)
    {
        var isExtensionMap1Valid = (extensionMap1 != null && extensionMap1.Count > 0);
        var isExtensionMap2Valid = (extensionMap2 != null && extensionMap2.Count > 0);

        // Sanity checks.
        if (!isExtensionMap1Valid && !isExtensionMap2Valid) return new Dictionary<string, ISet<string>>();
        if (!isExtensionMap1Valid) return extensionMap2;
        if (!isExtensionMap2Valid) return extensionMap1;

        // We don't use an additional map for merging, instead we use the first map as a
        // destination for the merging operation.
        foreach (var kvp in extensionMap2)
        {
            // If the "source" set is empty there's no reason to continue.
            if (kvp.Value == null || kvp.Value.Count == 0)
            {
                continue;
            }

            // If there's no key-value pair entry in the "destination" map for the current key in
            // the "source" map, we copy the "source" set wholesale.
            if (!extensionMap1.ContainsKey(kvp.Key))
            {
                extensionMap1.Add(kvp);
                continue;
            }

            // Getting here means there's already an entry for the "source" key in the "destination"
            // map which means we need to copy individual set elements from the "source" set to the
            // "destination" set.
            // No need to worry about duplicates as the set implementation handles this already.
            foreach (var extension in kvp.Value)
            {
                extensionMap1[kvp.Key].Add(extension);
            }
        }

        return extensionMap1;
    }

    /// <summary>
    /// Write the extension map to the telemetry data.
    /// </summary>
    /// 
    /// <param name="metrics">A collection representing the telemetry data.</param>
    /// <param name="extensionMap">The input extension map.</param>
    internal static void WriteExtensionMapToTelemetryData(
        IDictionary<string, object> metrics,
        IDictionary<string, ISet<string>> extensionMap)
    {
        metrics.Add(
            TelemetryDataConstants.DiscoveredExtensions,
            SerializeExtensionMap(extensionMap));
    }

    /// <summary>
    /// Adds the extensions specified to the current set of extensions.
    /// </summary>
    /// <typeparam name="TPluginInfo">
    /// Type of plugin info.
    /// </typeparam>
    /// <param name="newExtensions">
    /// The info about new extensions discovered
    /// </param>
    /// <returns>
    /// The <see cref="Dictionary"/> of extensions discovered
    /// </returns>
    internal Dictionary<string, TPluginInfo> AddExtension<TPluginInfo>(
        Dictionary<string, TPluginInfo> newExtensions) where TPluginInfo : TestPluginInformation
    {
        var existingExtensions = GetTestExtensionCache<TPluginInfo>();
        if (newExtensions == null)
        {
            return existingExtensions;
        }

        if (existingExtensions == null)
        {
            SetTestExtensionCache(newExtensions);
            return newExtensions;
        }

        foreach (var extension in newExtensions)
        {
            if (existingExtensions.ContainsKey(extension.Key))
            {
                EqtTrace.Warning(
                    "TestExtensions.AddExtensions: Attempt to add multiple test extensions with identifier data '{0}'",
                    extension.Key);
            }
            else
            {
                existingExtensions.Add(extension.Key, extension.Value);
            }
        }

        return existingExtensions;
    }

    /// <summary>
    /// Gets the extensions already discovered that are defined in the specified assembly.
    /// </summary>
    /// <param name="extensionAssembly"> The extension assembly.</param>
    /// <returns> The test extensions defined the extension assembly if it is already discovered. null if not.</returns>
    internal TestExtensions GetExtensionsDiscoveredFromAssembly(string extensionAssembly)
    {
        var testExtensions = new TestExtensions();

        testExtensions.TestDiscoverers =
            GetExtensionsDiscoveredFromAssembly(
                TestDiscoverers,
                extensionAssembly);
        testExtensions.TestExecutors =
            GetExtensionsDiscoveredFromAssembly(
                TestExecutors,
                extensionAssembly);
        testExtensions.TestExecutors2 =
            GetExtensionsDiscoveredFromAssembly(
                TestExecutors2,
                extensionAssembly);
        testExtensions.TestSettingsProviders =
            GetExtensionsDiscoveredFromAssembly(
                TestSettingsProviders,
                extensionAssembly);
        testExtensions.TestLoggers =
            GetExtensionsDiscoveredFromAssembly(
                TestLoggers,
                extensionAssembly);
        testExtensions.TestHosts =
            GetExtensionsDiscoveredFromAssembly(
                TestHosts,
                extensionAssembly);
        testExtensions.DataCollectors =
            GetExtensionsDiscoveredFromAssembly(
                DataCollectors,
                extensionAssembly);

        if (testExtensions.TestDiscoverers.Any()
            || testExtensions.TestExecutors.Any()
            || testExtensions.TestExecutors2.Any()
            || testExtensions.TestSettingsProviders.Any()
            || testExtensions.TestLoggers.Any()
            || testExtensions.TestHosts.Any()
            || testExtensions.DataCollectors.Any())
        {
            // This extension has already been discovered.
            return testExtensions;
        }

        return null;
    }

    internal Dictionary<string, TPluginInfo> GetTestExtensionCache<TPluginInfo>() where TPluginInfo : TestPluginInformation
    {
        Type type = typeof(TPluginInfo);

        if (type == typeof(TestDiscovererPluginInformation))
        {
            return (Dictionary<string, TPluginInfo>)(object)TestDiscoverers;
        }
        else if (type == typeof(TestExecutorPluginInformation))
        {
            return (Dictionary<string, TPluginInfo>)(object)TestExecutors;
        }
        else if (type == typeof(TestExecutorPluginInformation2))
        {
            return (Dictionary<string, TPluginInfo>)(object)TestExecutors2;
        }
        else if (type == typeof(TestLoggerPluginInformation))
        {
            return (Dictionary<string, TPluginInfo>)(object)TestLoggers;
        }
        else if (type == typeof(TestSettingsProviderPluginInformation))
        {
            return (Dictionary<string, TPluginInfo>)(object)TestSettingsProviders;
        }
        else if (type == typeof(TestRuntimePluginInformation))
        {
            return (Dictionary<string, TPluginInfo>)(object)TestHosts;
        }
        else if (type == typeof(DataCollectorConfig))
        {
            return (Dictionary<string, TPluginInfo>)(object)DataCollectors;
        }

        return null;
    }

    /// <summary>
    /// The are test extensions cached.
    /// </summary>
    /// <typeparam name="TPluginInfo">
    /// </typeparam>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    internal bool AreTestExtensionsCached<TPluginInfo>() where TPluginInfo : TestPluginInformation
    {
        Type type = typeof(TPluginInfo);

        if (type == typeof(TestDiscovererPluginInformation))
        {
            return AreTestDiscoverersCached;
        }
        else if (type == typeof(TestExecutorPluginInformation))
        {
            return AreTestExecutorsCached;
        }
        else if (type == typeof(TestExecutorPluginInformation2))
        {
            return AreTestExecutors2Cached;
        }
        else if (type == typeof(TestLoggerPluginInformation))
        {
            return AreTestLoggersCached;
        }
        else if (type == typeof(TestSettingsProviderPluginInformation))
        {
            return AreTestSettingsProvidersCached;
        }
        else if (type == typeof(TestRuntimePluginInformation))
        {
            return AreTestHostsCached;
        }
        else if (type == typeof(DataCollectorConfig))
        {
            return AreDataCollectorsCached;
        }

        return false;
    }

    /// <summary>
    /// The set test extensions cache status.
    /// </summary>
    /// <typeparam name="TPluginInfo">
    /// </typeparam>
    internal void SetTestExtensionsCacheStatusToTrue<TPluginInfo>() where TPluginInfo : TestPluginInformation
    {
        Type type = typeof(TPluginInfo);

        if (type == typeof(TestDiscovererPluginInformation))
        {
            AreTestDiscoverersCached = true;
        }
        else if (type == typeof(TestExecutorPluginInformation))
        {
            AreTestExecutorsCached = true;
        }
        else if (type == typeof(TestExecutorPluginInformation2))
        {
            AreTestExecutors2Cached = true;
        }
        else if (type == typeof(TestLoggerPluginInformation))
        {
            AreTestLoggersCached = true;
        }
        else if (type == typeof(TestSettingsProviderPluginInformation))
        {
            AreTestSettingsProvidersCached = true;
        }
        else if (type == typeof(TestRuntimePluginInformation))
        {
            AreTestHostsCached = true;
        }
        else if (type == typeof(DataCollectorConfig))
        {
            AreDataCollectorsCached = true;
        }
    }

    /// <summary>
    /// Gets the cached extensions for the current process.
    /// </summary>
    /// 
    /// <returns>A map representing the cached extensions for the current process.</returns>
    internal IDictionary<string, ISet<string>> GetCachedExtensions()
    {
        var extensionMap = new Dictionary<string, ISet<string>>();

        // Write all "known" cached extension.
        AddCachedExtensionToExtensionMap(extensionMap, "TestDiscoverers", TestDiscoverers?.Values.ToList());
        AddCachedExtensionToExtensionMap(extensionMap, "TestExecutors", TestExecutors?.Values.ToList());
        AddCachedExtensionToExtensionMap(extensionMap, "TestExecutors2", TestExecutors2?.Values.ToList());
        AddCachedExtensionToExtensionMap(extensionMap, "TestSettingsProviders", TestSettingsProviders?.Values.ToList());
        AddCachedExtensionToExtensionMap(extensionMap, "TestLoggers", TestLoggers?.Values.ToList());
        AddCachedExtensionToExtensionMap(extensionMap, "TestHosts", TestHosts?.Values.ToList());
        AddCachedExtensionToExtensionMap(extensionMap, "DataCollectors", DataCollectors?.Values.ToList());

        return extensionMap;
    }

    /// <summary>
    /// The invalidate cache of plugin infos.
    /// </summary>
    internal void InvalidateCache()
    {
        AreTestDiscoverersCached = false;
        AreTestExecutorsCached = false;
        AreTestExecutors2Cached = false;
        AreTestLoggersCached = false;
        AreTestSettingsProvidersCached = false;
        AreTestHostsCached = false;
        AreDataCollectorsCached = false;
    }

    /// <summary>
    /// Gets extensions discovered from assembly.
    /// </summary>
    /// <param name="extensionCollection">
    /// The extension collection.
    /// </param>
    /// <param name="extensionAssembly">
    /// The extension assembly.
    /// </param>
    /// <typeparam name="TPluginInfo">
    /// </typeparam>
    /// <returns>
    /// The <see cref="Dictionary"/>. of extensions discovered in assembly
    /// </returns>
    internal Dictionary<string, TPluginInfo> GetExtensionsDiscoveredFromAssembly<TPluginInfo>(
        Dictionary<string, TPluginInfo> extensionCollection,
        string extensionAssembly)
    {
        var extensions = new Dictionary<string, TPluginInfo>();
        if (extensionCollection != null)
        {
            foreach (var extension in extensionCollection)
            {
                var testPluginInformation = extension.Value as TestPluginInformation;
                var extensionType = Type.GetType(testPluginInformation?.AssemblyQualifiedName);
                if (string.Equals(extensionType.GetTypeInfo().Assembly.GetAssemblyLocation(), extensionAssembly))
                {
                    extensions.Add(extension.Key, extension.Value);
                }
            }
        }

        return extensions;
    }

    private void SetTestExtensionCache<TPluginInfo>(Dictionary<string, TPluginInfo> testPluginInfos) where TPluginInfo : TestPluginInformation
    {
        Type type = typeof(TPluginInfo);

        if (type == typeof(TestDiscovererPluginInformation))
        {
            TestDiscoverers = (Dictionary<string, TestDiscovererPluginInformation>)(object)testPluginInfos;
        }
        else if (type == typeof(TestExecutorPluginInformation))
        {
            TestExecutors = (Dictionary<string, TestExecutorPluginInformation>)(object)testPluginInfos;
        }
        else if (type == typeof(TestExecutorPluginInformation2))
        {
            TestExecutors2 = (Dictionary<string, TestExecutorPluginInformation2>)(object)testPluginInfos;
        }
        else if (type == typeof(TestLoggerPluginInformation))
        {
            TestLoggers = (Dictionary<string, TestLoggerPluginInformation>)(object)testPluginInfos;
        }
        else if (type == typeof(TestSettingsProviderPluginInformation))
        {
            TestSettingsProviders = (Dictionary<string, TestSettingsProviderPluginInformation>)(object)testPluginInfos;
        }
        else if (type == typeof(TestRuntimePluginInformation))
        {
            TestHosts = (Dictionary<string, TestRuntimePluginInformation>)(object)testPluginInfos;
        }
        else if (type == typeof(DataCollectorConfig))
        {
            DataCollectors = (Dictionary<string, DataCollectorConfig>)(object)testPluginInfos;
        }
    }

    private void AddCachedExtensionToExtensionMap<T>(
        IDictionary<string, ISet<string>> extensionMap,
        string extensionType,
        IList<T> extensions) where T : TestPluginInformation
    {
        if (extensions == null) return;

        extensionMap.Add(extensionType, new HashSet<string>());
        foreach (var extension in extensions)
        {
            extensionMap[extensionType].Add(extension.IdentifierData);
        }
    }

    private static string SerializeExtensionMap(IDictionary<string, ISet<string>> extensionMap)
    {
        StringBuilder sb = new();

        foreach (var kvp in extensionMap)
        {
            sb.Append(
                kvp.Value?.Count == 0
                ? string.Empty
                : string.Format(
                    "{0}=[{1}];",
                    kvp.Key,
                    string.Join(",", kvp.Value.ToArray())));
        }

        return sb.ToString();
    }
}
