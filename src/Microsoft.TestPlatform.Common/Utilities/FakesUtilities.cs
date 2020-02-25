// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Provides helper to configure run settings for Fakes. Works even when Fakes are not installed on the machine.
    /// </summary>
    public static class FakesUtilities
    {
        private const string ConfiguratorAssemblyQualifiedName = "Microsoft.VisualStudio.TestPlatform.Fakes.FakesDataCollectorConfiguration";

        private const string ConfiguratorMethodName = "GetDataCollectorSettingsOrDefault";

        private const string FakesConfiguratorAssembly = "Microsoft.VisualStudio.TestPlatform.Fakes, Version=16.0.0.0, Culture=neutral";

        /// <summary>
        /// Dynamically compute the Fakes data collector settings, given a set of test assemblies
        /// </summary>
        /// <param name="sources">test sources</param>
        /// <param name="runSettingsXml">runsettings</param>
        /// <returns>updated runsettings for fakes</returns>
        public static string GenerateFakesSettingsForRunConfiguration(string[] sources, string runSettingsXml)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            if (runSettingsXml == null)
            {
                throw new ArgumentNullException(nameof(runSettingsXml));
            }

            var doc = new XmlDocument();
            using (var xmlReader = XmlReader.Create(
                new StringReader(runSettingsXml),
                new XmlReaderSettings() { CloseInput = true }))
            {
                doc.Load(xmlReader);
            }

            var isNetFramework = !IsNetCoreFramework(runSettingsXml);

            return !TryAddFakesDataCollectorSettings(doc, sources, isNetFramework) ? runSettingsXml : doc.OuterXml;
        }

        private static bool IsNetCoreFramework(string runSettingsXml)
        {
            var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runSettingsXml);

            return config.TargetFramework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                   || config.TargetFramework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Tries to embed the Fakes data collector settings for the given run settings.
        /// </summary>
        /// <param name="runSettings">runsettings</param>
        /// <param name="sources">test sources</param>
        /// <returns>true if runSettings was modified; false otherwise.</returns>
        private static bool TryAddFakesDataCollectorSettings(
            XmlDocument runSettings,
            IEnumerable<string> sources,
            bool isNetFramework)
        {
            // If user provided fakes settings don't do anything
            if (XmlRunSettingsUtilities.ContainsDataCollector(runSettings.CreateNavigator(), FakesMetadata.DataCollectorUri))
            {
                return false;
            }

            // A new Fakes Congigurator API makes the decision to add the right datacollector uri to the configuration
            // There now exist two data collector URIs to support two different scenarios. The new scanrio involves 
            // using the CLRIE profiler, and the old involves using the Intellitrace profiler (which isn't supported in 
            // .NET Core scenarios). The old API still exists for fallback measures. 

            Func<IEnumerable<string>, bool, DataCollectorSettings> newConfigurator;
            if (TryGetFakesDataCollectorConfigurator(out newConfigurator))
            {
                var fakesSettings = newConfigurator(sources, isNetFramework);
                XmlRunSettingsUtilities.InsertDataCollectorsNode(runSettings.CreateNavigator(), fakesSettings);
                return true;
            }

            return AddFallbackFakesSettings(runSettings, sources, isNetFramework);
        }

        private static bool AddFallbackFakesSettings(
            XmlDocument runSettings,
            IEnumerable<string> sources,
            bool isNetFramework)
        {

            // The fallback settings is for the old implementation of fakes 
            // that only supports .Net Framework versions
            if (!isNetFramework)
            {
                return false;
            }

            Func<IEnumerable<string>, string> oldConfigurator;
            if (!TryGetFakesDataCollectorConfigurator(out oldConfigurator))
            {
                return false;
            }

            // if no fakes, return settings unchanged
            var fakesConfiguration = oldConfigurator(sources);
            if (fakesConfiguration == null)
            {
                return false;
            }

            // integrate fakes settings in configuration
            // if the settings doesn't have any data collector settings, populate with empty settings
            EnsureSettingsNode(runSettings, new DataCollectionRunSettings());

            // embed fakes settings
            var fakesSettings = CreateFakesDataCollectorSettings();
            var doc = new XmlDocument();
            using (var xmlReader = XmlReader.Create(
                new StringReader(fakesConfiguration),
                new XmlReaderSettings() { CloseInput = true }))
            {
                doc.Load(xmlReader);
            }

            fakesSettings.Configuration = doc.DocumentElement;
            XmlRunSettingsUtilities.InsertDataCollectorsNode(runSettings.CreateNavigator(), fakesSettings);

            return true;
        }

        /// <summary>
        /// Ensures that an xml element corresponding to the test run settings exists in the setting document.
        /// </summary>
        /// <param name="settings">settings</param>
        /// <param name="settingsNode">settingsNode</param>
        private static void EnsureSettingsNode(XmlDocument settings, TestRunSettings settingsNode)
        {
            Debug.Assert(settingsNode != null, "Invalid Settings Node");
            Debug.Assert(settings != null, "Invalid Settings");

            var root = settings.DocumentElement;
            if (root[settingsNode.Name] == null)
            {
                var newElement = settingsNode.ToXml();
                XmlNode newNode = settings.ImportNode(newElement, true);
                root.AppendChild(newNode);
            }
        }

        private static bool TryGetFakesDataCollectorConfigurator(out Func<IEnumerable<string>, string> configurator)
        {
#if NET451
            try
            {
                Assembly assembly = Assembly.Load(FakesConfiguratorAssembly);

                var type = assembly?.GetType(ConfiguratorAssemblyQualifiedName, false);
                if (type != null)
                {
                    var method = type.GetMethod(ConfiguratorMethodName, BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                    {
                        configurator = (Func<IEnumerable<string>, string>)method.CreateDelegate(typeof(Func<IEnumerable<string>, string>));
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("Failed to create Fakes Configurator. Reason:{0} ", ex);
                }
            }
#endif
            configurator = null;
            return false;
        }

        private static bool TryGetFakesDataCollectorConfigurator(out Func<IEnumerable<string>, bool, DataCollectorSettings> configurator)
        {
            try
            {
                Assembly assembly = Assembly.Load(FakesConfiguratorAssembly);

                var type = assembly?.GetType(ConfiguratorAssemblyQualifiedName, false);
                if (type != null)
                {
                    var method = type.GetMethod(ConfiguratorMethodName, BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                    {
                        configurator = (Func<IEnumerable<string>, bool, DataCollectorSettings>)method.CreateDelegate(typeof(Func<IEnumerable<string>, bool, DataCollectorSettings>));
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("Failed to create newly implemented Fakes Configurator. Reason:{0} ", ex);
                }
            }
            configurator = null;
            return false;
        }

        /// <summary>
        /// Adds the Fakes data collector settings in the run settings document.
        /// </summary>
        /// <returns>
        /// The <see cref="DataCollectorSettings"/>.
        /// </returns>
        private static DataCollectorSettings CreateFakesDataCollectorSettings()
        {
            // embed the fakes run settings
            var settings = new DataCollectorSettings
            {
                AssemblyQualifiedName = FakesMetadata.DataCollectorAssemblyQualifiedName,
                FriendlyName = FakesMetadata.FriendlyName,
                IsEnabled = true,
                Uri = new Uri(FakesMetadata.DataCollectorUri)
            };
            return settings;
        }

        internal static class FakesMetadata
        {
            /// <summary>
            /// Friendly name of the data collector
            /// </summary>
            public const string FriendlyName = "UnitTestIsolation";

            /// <summary>
            /// Gets the URI of the data collector
            /// </summary>
            public const string DataCollectorUri = "datacollector://microsoft/unittestisolation/1.0";

            /// <summary>
            /// Gets the assembly qualified name of the data collector type
            /// </summary>
            public const string DataCollectorAssemblyQualifiedName = "Microsoft.VisualStudio.TraceCollector.UnitTestIsolationDataCollector, Microsoft.VisualStudio.TraceCollector, Version=16.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        }
    }
}