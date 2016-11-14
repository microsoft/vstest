// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// The test extension information.
    /// </summary>
    public class TestExtensions
    {
        #region Properties

        /// <summary>
        /// Gets or sets test discoverer extensions.
        /// </summary>
        internal Dictionary<string, TestDiscovererPluginInformation> TestDiscoverers { get; set; }

        /// <summary>
        /// Gets or sets test executor extensions.
        /// </summary>
        internal Dictionary<string, TestExecutorPluginInformation> TestExecutors { get; set; }

        /// <summary>
        /// Gets or sets test setting provider extensions.
        /// </summary>
        internal Dictionary<string, TestSettingsProviderPluginInformation> TestSettingsProviders { get; set; }

        /// <summary>
        /// Gets or sets test logger extensions.
        /// </summary>
        internal Dictionary<string, TestLoggerPluginInformation> TestLoggers { get; set; }

        #endregion

        #region Internal methods

        /// <summary>
        /// Adds the extensions specified to the current set of extensions.
        /// </summary>
        /// <param name="extensions"> The test extensions to add. </param>
        internal void AddExtensions(TestExtensions extensions)
        {
            if (extensions == null)
            {
                return;
            }

            this.TestDiscoverers = this.AddExtension<TestDiscovererPluginInformation>(this.TestDiscoverers, extensions.TestDiscoverers);
            this.TestExecutors = this.AddExtension<TestExecutorPluginInformation>(this.TestExecutors, extensions.TestExecutors);
            this.TestSettingsProviders = this.AddExtension<TestSettingsProviderPluginInformation>(this.TestSettingsProviders, extensions.TestSettingsProviders);
            this.TestLoggers = this.AddExtension<TestLoggerPluginInformation>(this.TestLoggers, extensions.TestLoggers);
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
                this.GetExtensionsDiscoveredFromAssembly<TestDiscovererPluginInformation>(
                    this.TestDiscoverers,
                    extensionAssembly);
            testExtensions.TestExecutors =
                this.GetExtensionsDiscoveredFromAssembly<TestExecutorPluginInformation>(
                    this.TestExecutors,
                    extensionAssembly);
            testExtensions.TestSettingsProviders =
                this.GetExtensionsDiscoveredFromAssembly<TestSettingsProviderPluginInformation>(
                    this.TestSettingsProviders,
                    extensionAssembly);
            testExtensions.TestLoggers =
                this.GetExtensionsDiscoveredFromAssembly<TestLoggerPluginInformation>(
                    this.TestLoggers,
                    extensionAssembly);

            if (testExtensions.TestDiscoverers.Any() || testExtensions.TestExecutors.Any() || testExtensions.TestSettingsProviders.Any() || testExtensions.TestLoggers.Any())
            {
                // This extension has already been discovered.
                return testExtensions;
            }

            return null;
        }

        #endregion

        #region Private methods

        private Dictionary<string, TPluginInfo> AddExtension<TPluginInfo>(
            Dictionary<string, TPluginInfo> existingExtensions,
            Dictionary<string, TPluginInfo> newExtensions)
        {
            if (newExtensions == null)
            {
                return null;
            }

            if (existingExtensions == null)
            {
                return newExtensions;
            }
            else
            {
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
        }

        private Dictionary<string, TPluginInfo> GetExtensionsDiscoveredFromAssembly<TPluginInfo>(Dictionary<string, TPluginInfo> extensionCollection, string extensionAssembly)
        {
            var extensions = new Dictionary<string, TPluginInfo>();
            if (extensionCollection != null)
            {
                foreach (var extension in extensionCollection)
                {
                    var testPluginInformation = extension.Value as TestPluginInformation;
                    var extensionType = Type.GetType(testPluginInformation?.AssemblyQualifiedName);
                    if (string.Equals(extensionType.GetTypeInfo().Assembly.Location, extensionAssembly))
                    {
                        extensions.Add(extension.Key, extension.Value);
                    }
                }
            }

            return extensions;
        }

        #endregion
    }
}
