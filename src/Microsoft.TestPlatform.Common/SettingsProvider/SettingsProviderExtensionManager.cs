// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;
    using ObjectModelCommonResources = Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources.CommonResources;

    /// <summary>
    /// Manages the settings provider extensions.
    /// </summary>
    /// <remarks>
    /// This is a non-shared instance because we want different settings provider instances to
    /// be used for each run settings instance.
    /// </remarks>
    public class SettingsProviderExtensionManager
    {
        #region Fields
        private static SettingsProviderExtensionManager settingsProviderExtensionManager;
        private static object synclock = new object();

        /// <summary>
        /// The settings providers which are available.
        /// </summary>
        private IEnumerable<LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>> settingsProviders;
        private Dictionary<string, LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>> settingsProvidersMap;

        /// <summary>
        /// Used for logging errors.
        /// </summary>
        private IMessageLogger logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes with the settings providers.
        /// </summary>
        /// <remarks>
        /// The settings providers are imported as non-shared because we need different settings provider
        /// instances to be used for each run settings.
        /// </remarks>        
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected SettingsProviderExtensionManager(
            IEnumerable<LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>> settingsProviders,
            IEnumerable<LazyExtension<ISettingsProvider, Dictionary<string, object>>> unfilteredSettingsProviders,
            IMessageLogger logger)
        {
            ValidateArg.NotNull<IEnumerable<LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>>>(settingsProviders, "settingsProviders");
            ValidateArg.NotNull<IEnumerable<LazyExtension<ISettingsProvider, Dictionary<string, object>>>>(unfilteredSettingsProviders, "unfilteredSettingsProviders");
            ValidateArg.NotNull<IMessageLogger>(logger, "logger");

            this.settingsProviders = settingsProviders;
            this.UnfilteredSettingsProviders = unfilteredSettingsProviders;
            this.logger = logger;

            // Populate the map to avoid threading issues
            this.PopulateMap();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Unfiltered list of settings providers.  Used for the /ListSettingsProviders command line argument.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IEnumerable<LazyExtension<ISettingsProvider, Dictionary<string, object>>> UnfilteredSettingsProviders { get; private set; }

        /// <summary>
        /// Gets the map of settings name to settings provider.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public Dictionary<string, LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>> SettingsProvidersMap
        {
            get
            {
                return this.settingsProvidersMap;
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Creates an instance of the settings provider.
        /// </summary>
        /// <returns>Instance of the settings provider.</returns>
        public static SettingsProviderExtensionManager Create()
        {
            if (settingsProviderExtensionManager == null)
            {
                lock (synclock)
                {
                    if (settingsProviderExtensionManager == null)
                    {
                        IEnumerable<LazyExtension<ISettingsProvider, Dictionary<string, object>>> unfilteredTestExtensions;
                        IEnumerable<LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>> testExtensions;

                        TestPluginManager.Instance
                            .GetSpecificTestExtensions<TestSettingsProviderPluginInformation, ISettingsProvider, ISettingsProviderCapabilities, TestSettingsProviderMetadata>(
                                TestPlatformConstants.SettingsProviderRegexPattern,
                                out unfilteredTestExtensions,
                                out testExtensions);

                        settingsProviderExtensionManager = new SettingsProviderExtensionManager(
                            testExtensions, unfilteredTestExtensions, TestSessionMessageLogger.Instance);
                    }
                }
            }

            return settingsProviderExtensionManager;
        }

        /// <summary>
        /// Destroy the extension manager.
        /// </summary>
        public static void Destroy()
        {
            lock (synclock)
            {
                settingsProviderExtensionManager = null;
            }
        }

        /// <summary>
        /// Load all the settings provider and fail on error
        /// </summary>
        /// <param name="shouldThrowOnError"> Indicates whether this method should throw on error. </param>
        public static void LoadAndInitializeAllExtensions(bool shouldThrowOnError)
        {
            var extensionManager = SettingsProviderExtensionManager.Create();

            try
            {
                foreach (var settingsProvider in extensionManager.SettingsProvidersMap)
                {
                    // Note: - The below Verbose call should not be under IsVerboseEnabled check as we want to 
                    // call executor.Value even if logging is not enabled. 
                    EqtTrace.Verbose("SettingsProviderExtensionManager: Loading settings provider {0}", settingsProvider.Value.Value);
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("SettingsProviderExtensionManager: LoadAndInitialize: Exception occured while loading extensions {0}", ex);
                }

                if (shouldThrowOnError)
                {
                    throw;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the settings with the provided name.
        /// </summary>
        /// <param name="settingsName">Name of the settings to get.</param>
        /// <returns>Settings provider with the provided name or null if one was not found.</returns>
        internal LazyExtension<ISettingsProvider, ISettingsProviderCapabilities> GetSettingsProvider(string settingsName)
        {
            if (string.IsNullOrWhiteSpace(settingsName))
            {
                throw new ArgumentException(ObjectModelCommonResources.CannotBeNullOrEmpty, "settingsName");
            }

            LazyExtension<ISettingsProvider, ISettingsProviderCapabilities> settingsProvider;
            this.SettingsProvidersMap.TryGetValue(settingsName, out settingsProvider);

            return settingsProvider;
        }

        #endregion


        /// <summary>
        /// Populate the settings provider map
        /// </summary>
        private void PopulateMap()
        {
            this.settingsProvidersMap = new Dictionary<string, LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>>();

            foreach (var settingsProvider in this.settingsProviders)
            {
                if (this.settingsProvidersMap.ContainsKey(settingsProvider.Metadata.SettingsName))
                {
                    this.logger.SendMessage(
                        TestMessageLevel.Error,
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CommonResources.DuplicateSettingsName,
                            settingsProvider.Metadata.SettingsName));
                }
                else
                {
                    this.settingsProvidersMap.Add(settingsProvider.Metadata.SettingsName, settingsProvider);
                }
            }
        }
    }

    /// <summary>
    /// Hold data about the Test settings provider.
    /// </summary>
    internal class TestSettingsProviderMetadata : ISettingsProviderCapabilities
    {
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="settingsName">Test settings name</param>
        public TestSettingsProviderMetadata(string settingsName)
        {
            this.SettingsName = settingsName;
        }

        /// <summary>
        /// Gets test settings name.
        /// </summary>
        public string SettingsName
        {
            get;
            private set;
        }
    }
}
