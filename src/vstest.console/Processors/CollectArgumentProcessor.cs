// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// The argument processor for enabling data collectors.
    /// </summary>
    internal class CollectArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of command for enabling code coverage.
        /// </summary>
        public const string CommandName = "/Collect";

        #endregion

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new CollectArgumentProcessorCapabilities());
                }

                return this.metadata;
            }
        }

        /// <summary>
        /// Gets or sets the executor.
        /// </summary>
        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (this.executor == null)
                {
                    this.executor = new Lazy<IArgumentExecutor>(() => new CollectArgumentExecutor(RunSettingsManager.Instance, new FileHelper()));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }


    internal class CollectArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => CollectArgumentProcessor.CommandName;

        public override bool AllowMultiple => true;

        public override bool IsAction => false;

        public override bool AlwaysExecute => true;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        public override string HelpContentResourceName => CommandLineResources.CollectArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.CollectArgumentProcessorHelpPriority;
    }

    /// <inheritdoc />
    internal class CollectArgumentExecutor : IArgumentExecutor
    {
        private readonly IRunSettingsProvider runSettingsManager;
        private readonly IFileHelper fileHelper;
        internal static List<string> EnabledDataCollectors = new List<string>();
        internal CollectArgumentExecutor(IRunSettingsProvider runSettingsManager, IFileHelper fileHelper)
        {
            this.runSettingsManager = runSettingsManager;
            this.fileHelper = fileHelper;
        }

        /// <inheritdoc />
        public void Initialize(string argument)
        {
            // If coverlet data collector is enabled in the runsettings file, add the corresponding
            // inproc data collector. This code runs always, without specifying a data collector from the CLI.
            if (argument == null)
            {
                EnableInProcCollectorFromRunSettings();
                return;
            }

            // 1. Disable all other data collectors. Enable only those data collectors that are explicitly specified by user.
            // 2. Check if Code Coverage Data Collector is specified in runsettings, if not add it and also set enable to true.

            // if argument is null or doesn't contain any element, don't do anything.
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new CommandLineException(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    CommandLineResources.DataCollectorFriendlyNameInvalid,
                    argument));
            }

            if (InferRunSettingsHelper.IsTestSettingsEnabled(this.runSettingsManager.ActiveRunSettings.SettingsXml))
            {
                throw new SettingsException(string.Format(CommandLineResources.CollectWithTestSettingErrorMessage, argument));
            }
            AddDataCollectorToRunSettings(argument, this.runSettingsManager, this.fileHelper);
        }

        private void EnableInProcCollectorFromRunSettings()
        {
            var settings = runSettingsManager.ActiveRunSettings?.SettingsXml;
            if (settings == null)
            {
                runSettingsManager.AddDefaultRunSettings();
                settings = runSettingsManager.ActiveRunSettings?.SettingsXml;
            }

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settings) ?? new DataCollectionRunSettings();
            bool isCoverletEnabled = dataCollectionRunSettings.DataCollectorSettingsList.Any(dcs => dcs.IsEnabled &&
                (string.Equals(dcs.FriendlyName, CoverletConstants.CoverletDataCollectorFriendlyName, StringComparison.InvariantCultureIgnoreCase) || dcs.Uri == CoverletConstants.CoverletDataCollectorUri));

            if (isCoverletEnabled)
            {
                var inProcDataCollectionRunSettings = XmlRunSettingsUtilities.GetInProcDataCollectionRunSettings(settings)
                ?? new DataCollectionRunSettings(
                    Constants.InProcDataCollectionRunSettingsName,
                    Constants.InProcDataCollectorsSettingName,
                    Constants.InProcDataCollectorSettingName);

                EnabledDataCollectors.Add(CoverletConstants.CoverletDataCollectorFriendlyName);

                // Add in-proc data collector to runsettings if coverlet code coverage is enabled
                EnableCoverletInProcDataCollector(CoverletConstants.CoverletDataCollectorFriendlyName, inProcDataCollectionRunSettings, runSettingsManager, fileHelper);
                runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.InProcDataCollectionRunSettingsName, inProcDataCollectionRunSettings.ToXml().InnerXml);
            }
        }

        /// <summary>
        /// Returns coverlet code base searching coverlet.collector.dll assembly inside adaptersPaths
        /// </summary>
        private static string GetCoverletCodeBasePath(IRunSettingsProvider runSettingProvider, IFileHelper fileHelper)
        {
            foreach (string adapterPath in RunSettingsUtilities.GetTestAdaptersPaths(runSettingProvider.ActiveRunSettings.SettingsXml))
            {
                string collectorPath = Path.Combine(adapterPath, CoverletConstants.CoverletDataCollectorCodebase);
                if (fileHelper.Exists(collectorPath))
                {
                    EqtTrace.Verbose("CoverletDataCollector in-process codeBase path '{0}'", collectorPath);
                    return collectorPath;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }

        internal static void EnableDataCollectorUsingFriendlyName(string argument, DataCollectionRunSettings dataCollectionRunSettings)
        {
            DataCollectorSettings dataCollectorSettings = null;

            if (!DoesDataCollectorSettingsExist(argument, dataCollectionRunSettings, out dataCollectorSettings))
            {
                dataCollectorSettings = new DataCollectorSettings();
                dataCollectorSettings.FriendlyName = argument;
                dataCollectorSettings.IsEnabled = true;
                dataCollectionRunSettings.DataCollectorSettingsList.Add(dataCollectorSettings);
            }
            else
            {
                dataCollectorSettings.IsEnabled = true;
            }
        }

        /// <summary>
        /// Enables coverlet in-proc datacollector
        /// </summary>
        internal static void EnableCoverletInProcDataCollector(string argument, DataCollectionRunSettings dataCollectionRunSettings, IRunSettingsProvider runSettingProvider, IFileHelper fileHelper)
        {
            DataCollectorSettings dataCollectorSettings = null;

            if (!DoesDataCollectorSettingsExist(argument, dataCollectionRunSettings, out dataCollectorSettings))
            {
                // Create a new setting with default values
                dataCollectorSettings = new DataCollectorSettings();
                dataCollectorSettings.FriendlyName = argument;
                dataCollectorSettings.AssemblyQualifiedName = CoverletConstants.CoverletDataCollectorAssemblyQualifiedName;
                dataCollectorSettings.CodeBase = GetCoverletCodeBasePath(runSettingProvider, fileHelper) ?? CoverletConstants.CoverletDataCollectorCodebase;
                dataCollectorSettings.IsEnabled = true;
                dataCollectionRunSettings.DataCollectorSettingsList.Add(dataCollectorSettings);
            }
            else
            {
                // Set Assembly qualified name and code base if not already set
                dataCollectorSettings.AssemblyQualifiedName = dataCollectorSettings.AssemblyQualifiedName ?? CoverletConstants.CoverletDataCollectorAssemblyQualifiedName;
                dataCollectorSettings.CodeBase = (dataCollectorSettings.CodeBase ?? GetCoverletCodeBasePath(runSettingProvider, fileHelper)) ?? CoverletConstants.CoverletDataCollectorCodebase;
                dataCollectorSettings.IsEnabled = true;
            }
        }

        private static bool DoesDataCollectorSettingsExist(string friendlyName,
            DataCollectionRunSettings dataCollectionRunSettings,
            out DataCollectorSettings dataCollectorSettings)
        {
            dataCollectorSettings = null;
            foreach (var dataCollectorSetting in dataCollectionRunSettings.DataCollectorSettingsList)
            {
                if (dataCollectorSetting.FriendlyName.Equals(friendlyName, StringComparison.OrdinalIgnoreCase))
                {
                    dataCollectorSettings = dataCollectorSetting;
                    return true;
                }
            }

            return false;
        }

        internal static void AddDataCollectorToRunSettings(string argument, IRunSettingsProvider runSettingsManager, IFileHelper fileHelper)
        {
            EnabledDataCollectors.Add(argument.ToLower());

            var settings = runSettingsManager.ActiveRunSettings?.SettingsXml;
            if (settings == null)
            {
                runSettingsManager.AddDefaultRunSettings();
                settings = runSettingsManager.ActiveRunSettings?.SettingsXml;
            }

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settings) ?? new DataCollectionRunSettings();
            var inProcDataCollectionRunSettings = XmlRunSettingsUtilities.GetInProcDataCollectionRunSettings(settings)
                ?? new DataCollectionRunSettings(
                    Constants.InProcDataCollectionRunSettingsName,
                    Constants.InProcDataCollectorsSettingName,
                    Constants.InProcDataCollectorSettingName);

            // Add data collectors if not already present, enable if already present.
            EnableDataCollectorUsingFriendlyName(argument, dataCollectionRunSettings);

            runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);

            if (string.Equals(argument, CoverletConstants.CoverletDataCollectorFriendlyName, StringComparison.OrdinalIgnoreCase))
            {
                // Add in-proc data collector to runsettings if coverlet code coverage is enabled
                EnableCoverletInProcDataCollector(argument, inProcDataCollectionRunSettings, runSettingsManager, fileHelper);
                runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.InProcDataCollectionRunSettingsName, inProcDataCollectionRunSettings.ToXml().InnerXml);
            }
        }

        internal static void AddDataCollectorFriendlyName(string friendlyName)
        {
            EnabledDataCollectors.Add(friendlyName.ToLower());
        }

        internal static class CoverletConstants
        {
            /// <summary>
            /// Coverlet in-proc data collector friendly name
            /// </summary>
            public const string CoverletDataCollectorFriendlyName = "XPlat Code Coverage";

            /// <summary>
            /// Coverlet in-prod data collector uri
            /// </summary>
            public static Uri CoverletDataCollectorUri = new Uri(@"datacollector://Microsoft/CoverletCodeCoverage/1.0");

            /// <summary>
            /// Coverlet in-proc data collector assembly qualified name
            /// </summary>
            public const string CoverletDataCollectorAssemblyQualifiedName = "Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            /// <summary>
            /// Coverlet in-proc data collector code base
            /// </summary>
            public const string CoverletDataCollectorCodebase = "coverlet.collector.dll";
        }
    }
}