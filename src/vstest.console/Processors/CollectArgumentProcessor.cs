// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.IO;
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

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        public override string HelpContentResourceName => CommandLineResources.CollectArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.CollectArgumentProcessorHelpPriority;
    }

    /// <inheritdoc />
    internal class CollectArgumentExecutor : IArgumentExecutor
    {
        private readonly IRunSettingsProvider runSettingsManager;
        private readonly IFileHelper _fileHelper;
        internal static List<string> EnabledDataCollectors = new List<string>();
        internal CollectArgumentExecutor(IRunSettingsProvider runSettingsManager, IFileHelper fileHelper)
        {
            this.runSettingsManager = runSettingsManager;
            this._fileHelper = fileHelper;
        }

        /// <inheritdoc />
        public void Initialize(string argument)
        {
            // 1. Disable all other data collectors. Enable only those data collectors that are explicitely specified by user.
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
            AddDataCollectorToRunSettings(argument, this.runSettingsManager);
        }

        /// <summary>
        /// We try to fix inproc coverlet codebase searching coverlet.collector.dll assembly inside adaptersPaths
        /// </summary>
        private void FixCoverletInProcessCollectorCodeBase()
        {
            DataCollectionRunSettings inProcDataCollectionRunSettings = XmlRunSettingsUtilities.GetInProcDataCollectionRunSettings(this.runSettingsManager.ActiveRunSettings.SettingsXml);
            if (DoesDataCollectorSettingsExist(CoverletConstants.CoverletDataCollectorFriendlyName, inProcDataCollectionRunSettings, out DataCollectorSettings inProcDataCollector))
            {
                foreach (string adapterPath in RunSettingsUtilities.GetTestAdaptersPaths(this.runSettingsManager.ActiveRunSettings.SettingsXml))
                {
                    string collectorPath = Path.Combine(adapterPath, CoverletConstants.CoverletDataCollectorCodebase);
                    if (_fileHelper.Exists(collectorPath))
                    {
                        inProcDataCollector.CodeBase = collectorPath;
                        runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.InProcDataCollectionRunSettingsName, inProcDataCollectionRunSettings.ToXml().InnerXml);
                        EqtTrace.Verbose("CoverletDataCollector in-process codeBase updated to '{0}'", inProcDataCollector.CodeBase);
                        break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public ArgumentProcessorResult Execute()
        {
            FixCoverletInProcessCollectorCodeBase();
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
        /// Enables coverlet inproc datacollector
        /// </summary>
        internal static void EnableCoverletInProcDataCollector(string argument, DataCollectionRunSettings dataCollectionRunSettings)
        {
            DataCollectorSettings dataCollectorSettings = null;

            if (!DoesDataCollectorSettingsExist(argument, dataCollectionRunSettings, out dataCollectorSettings))
            {
                // Create a new setting with deafult values
                dataCollectorSettings = new DataCollectorSettings();
                dataCollectorSettings.FriendlyName = argument;
                dataCollectorSettings.AssemblyQualifiedName = CoverletConstants.CoverletDataCollectorAssemblyQualifiedName;
                dataCollectorSettings.CodeBase = CoverletConstants.CoverletDataCollectorCodebase;
                dataCollectorSettings.IsEnabled = true;
                dataCollectionRunSettings.DataCollectorSettingsList.Add(dataCollectorSettings);
            }
            else
            {
                // Set Assembly qualified name and codebase if not already set
                dataCollectorSettings.AssemblyQualifiedName = dataCollectorSettings.AssemblyQualifiedName ?? CoverletConstants.CoverletDataCollectorAssemblyQualifiedName;
                dataCollectorSettings.CodeBase = dataCollectorSettings.CodeBase ?? CoverletConstants.CoverletDataCollectorCodebase;
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

        internal static void AddDataCollectorToRunSettings(string argument, IRunSettingsProvider runSettingsManager)
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
                // Add inproc data collector to runsetings if coverlet code coverage is enabled
                EnableCoverletInProcDataCollector(argument, inProcDataCollectionRunSettings);
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
            /// Coverlet inproc data collector friendlyname
            /// </summary>
            public const string CoverletDataCollectorFriendlyName = "XPlat Code Coverage";

            /// <summary>
            /// Coverlet inproc data collector assembly qualified name
            /// </summary>
            public const string CoverletDataCollectorAssemblyQualifiedName = "Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            /// <summary>
            /// Coverlet inproc data collector codebase
            /// </summary>
            public const string CoverletDataCollectorCodebase = "coverlet.collector.dll";
        }
    }
}