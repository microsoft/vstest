// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Discovers data collectors in a directory.
    /// </summary>
    internal class DataCollectorDiscoveryHelper
    {
        #region Fields

        /// <summary>
        /// The data collectors directory name.
        /// </summary>
        public const string DataCollectorsDirectoryName = @"PrivateAssemblies\DataCollectors";

        /// <summary>
        /// The current process location.
        /// </summary>
        private static readonly string CurrentProcessLocation = Process.GetCurrentProcess().MainModule.FileName;

        /// <summary>
        /// The is portable.
        /// </summary>
        private static readonly bool IsPortable = ClientUtilities.CheckIfTestProcessIsRunningInXcopyableMode();

        #endregion

        /// <summary>
        /// Gets the directory which contains the data collectors.
        /// </summary>
        public static string DataCollectorsDirectory
        {
            get
            {
                if (IsPortable)
                {
                    return Path.GetDirectoryName(Path.GetFullPath(CurrentProcessLocation));
                }
                else
                {
                    return GetDataCollectorPluginDirectory();
                }
            }
        }

        /// <summary>
        /// Gets the configuration file for the assembly.
        /// </summary>
        /// <param name="assembly">The assembly the configuration file is for.</param>
        /// <returns>The configuration file.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Shouldn't block loading of subsequent plugins.")]
        internal static XmlDocument GetConfigurationForAssembly(Assembly assembly)
        {
            Debug.Assert(assembly != null, "null assembly");

            // E.g. "C:\Program Files\Microsoft Visual Studio 14.0\Common7\IDE\PrivateAssemblies\DataCollectors"
            var assemblyFolder = Path.GetDirectoryName(assembly.Location);

            // E.g. "Microsoft.VisualStudio.TraceCollector.dll.config"
            var configFileName = Path.GetFileName(assembly.Location) + ".config";

            // Search for the config file. 
            // Note that this config file is localized for some collectors (like Intellitrace), and is present in language specific sub-directory.
            // The config file is unlocalized for some collectors, and is present in parent directory itself.
            // So we need to look at all places.
            // We go from more language specific to less language specific

            // VSTLM's UI is controlled by the OS's UI Language, which is available as current thread's UI Culture. We use it first.
            var subFolders = GetOsNeutralCultureNames();

            // On non-enu OS, if an enu product is installed, then we will not find the config in osUILanguages in which case
            // we will fallback to assembly's default language. The default language can be null as well. 
            //
            // TODO: Currently we are doing a partial fix by falling back to neutral language which might not work on jpn OS with german VS.
            var assemblyUiLanguage = GetAssemblyCultureLanguage(assembly);
            if (assemblyUiLanguage != null && !subFolders.Contains(assemblyUiLanguage))
            {
                subFolders.Add(assemblyUiLanguage);
            }

            // To search in current folder
            subFolders.Add(string.Empty);

            var configFilePath = string.Empty;
            var iFolder = 0;
            for (; iFolder < subFolders.Count; iFolder++)
            {
                configFilePath =
                    Path.Combine(
                        Path.Combine(assemblyFolder, subFolders[iFolder]),
                        configFileName);

                if (File.Exists(configFilePath))
                {
                    break;
                }
            }

            // We couldn't find a config file for this assembly anywhere. Return null
            if (iFolder >= subFolders.Count)
            {
                EqtTrace.Warning("DataCollectorDiscovery: No configuration file found for data collector collector assembly {0} in all {1} locations", assembly.FullName, subFolders.Count);
                return null;
            }
            else
            {
                EqtTrace.Verbose(
                    "DataCollectorDiscovery: using configuration file {0}.", configFilePath);
            }

            // Load the config file located
            try
            {
                var configFile = new XmlDocument();
                using (var xmlReader = new XmlTextReader(configFilePath))
                {
                    xmlReader.DtdProcessing = DtdProcessing.Prohibit;
                    xmlReader.XmlResolver = null;
                    configFile.Load(xmlReader);
                }

                return configFile;
            }
            catch (Exception e)
            {
                EqtTrace.Error("DataCollectorDiscovery: Error occurred while loading the configuration file {0}. Error: {1}", configFilePath, e.ToString());
                return null;
            }
        }

        #region Private Methods

        /// <summary>
        /// The get data collector plugin directory.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to ignore failures to read the registry settings")]
        private static string GetDataCollectorPluginDirectory()
        {
            // First option: Look into app settings. If specified and valid use it.
            var settings = ConfigurationManager.AppSettings;
            if (settings != null && settings.HasKeys())
            {
                string directoryFromConfig = settings.Get(TestPlatformDefaults.PluginDirectorySettingsKeyName);
                if (!string.IsNullOrEmpty(directoryFromConfig))
                {
                    directoryFromConfig = Environment.ExpandEnvironmentVariables(directoryFromConfig);
                    directoryFromConfig = Path.GetFullPath(directoryFromConfig);
                    if (Directory.Exists(directoryFromConfig))
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("TestPlatformDataCollectorDiscovery.GetDataCollectorPluginDirectory: Using config specified plugin directory '{0}'.", directoryFromConfig);
                        }

                        return directoryFromConfig;
                    }
                    else
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("TestPlatformDataCollectorDiscovery.GetDataCollectorPluginDirectory: Config specified plugin directory '{0}' not found. Value ignored.", directoryFromConfig);
                        }
                    }
                }
            }

            // Second option: <Current process assembly location>\PrivateAssemblies\DataCollectors
            var currentProcessDirectory = Path.GetDirectoryName(Path.GetFullPath(CurrentProcessLocation));
            var directoryFromProcess = Path.Combine(currentProcessDirectory, DataCollectorDiscoveryHelper.DataCollectorsDirectoryName);

            if (Directory.Exists(directoryFromProcess))
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestPlatformDataCollectorDiscovery.GetDataCollectorPluginDirectory: Using plugin directory '{0}' relative to main module assembly location.", directoryFromProcess);
                }
                return directoryFromProcess;
            }
            else
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestPlatformDataCollectorDiscovery.GetDataCollectorPluginDirectory: Can not find plugin directory realtive to main module assembly location.");
                }
            }

            // Third option: Look for VS Installation directory from registry key. If found and valid use that.
            var path = GetInstallLocationFromRegistry(ClientUtilities.GetVSInstallPath());

            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the OS UI Language's neutral culture names. 
        /// Typically, there is only one:
        ///     For the japanese (japan) ja-jp OS, return ja.
        ///     For english (us) en-us , return en.
        /// But sometimes, there can be many:
        ///     For chinese-traditional (taiwan) zh-TW, this would return zh-CHT, zh-Hant, zh
        /// </summary>
        /// <returns>The OS Neutral Culture Names</returns>
        private static List<string> GetOsNeutralCultureNames()
        {
            var cultNames = new List<string>();

            // CurrentUICulture represents OS's UI language in the specific culture form.
            // Parent represents the neutral culture form
            var c = System.Globalization.CultureInfo.CurrentUICulture.Parent;
            while (!string.IsNullOrEmpty(c.Name) && c.IsNeutralCulture)
            {
                cultNames.Add(c.Name);
                c = c.Parent;
            }

            Debug.Assert(cultNames.Count >= 1, "There is always a neutral culture");
            return cultNames;
        }

        /// <summary>
        /// Gets the language of the assembly
        /// </summary>
        /// <param name="assembly">The Assembly</param>
        /// <returns>The Assembly Culture Language</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static string GetAssemblyCultureLanguage(Assembly assembly)
        {
            try
            {
                System.Resources.NeutralResourcesLanguageAttribute neutralLangAttr = Attribute.GetCustomAttribute(assembly, typeof(System.Resources.NeutralResourcesLanguageAttribute)) as System.Resources.NeutralResourcesLanguageAttribute; /* Typically the value is en-US */
                if (neutralLangAttr != null)
                {
                    return (new System.Globalization.CultureInfo(neutralLangAttr.CultureName)).TwoLetterISOLanguageName;
                }

                return null;
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("TestPlatformDataCollectorDiscovery: Error occurred while finding the fallback language for assembly {0}. Error: {1}", assembly.FullName, ex);
                }

                return null;
            }
        }

        /// <summary>
        /// The get install location from registry.
        /// </summary>
        /// <param name="skuInstallLocation">
        /// The sku install location.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetInstallLocationFromRegistry(string skuInstallLocation)
        {
            try
            {
                var directoryFromRegistry = Path.Combine(skuInstallLocation, DataCollectorsDirectoryName);
                if (Directory.Exists(directoryFromRegistry))
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("TestPlatformDataCollectorDiscovery.GetDataCollectorPluginDirectory: Using plugin directory '{0}' relative to directory location in registry.", directoryFromRegistry);
                    }

                    return directoryFromRegistry;
                }
                else
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("TestPlatformDataCollectorDiscovery.GetDataCollectorPluginDirectory: Can not find plugin directory realtive to directory location in registry.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("TestPlatformDataCollectorDiscovery.GetDataCollectorPluginDirectory: Error finding plugin directory from registry. Error details:{0}.", ex.Message);
                }

                // ignore the exception.
            }

            return null;
        }

        #endregion
    }
}