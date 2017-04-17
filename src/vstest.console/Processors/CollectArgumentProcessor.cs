// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

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
                    this.executor = new Lazy<IArgumentExecutor>(() => new CollectArgumentExecutor(RunSettingsManager.Instance));
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

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Collect;

        public override string HelpContentResourceName => CommandLineResources.CollectArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.CollectArgumentProcessorHelpPriority;
    }

    internal class CollectArgumentExecutor : IArgumentExecutor
    {
        private IRunSettingsProvider runSettingsManager;

        internal CollectArgumentExecutor(IRunSettingsProvider runSettingsManager)
        {
            this.runSettingsManager = runSettingsManager;
        }

        public void Initialize(string argument)
        {
            // 1. Disable all other data collectors. Enable only those data collectors that are explicitely specified by user.
            // 2. Check if Code Coverage Data Collector is specified in runsettings, if not add it and also set enable to true.

            // if argument is null or doesn't contain any element, don't do anything.
            if (string.IsNullOrWhiteSpace(argument))
            {
                return;
            }

            var args = argument.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var settings = this.runSettingsManager.ActiveRunSettings?.SettingsXml;
            if (settings == null)
            {
                this.runSettingsManager.AddDefaultRunSettings();
                settings = this.runSettingsManager.ActiveRunSettings?.SettingsXml;
            }

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settings);
            if (dataCollectionRunSettings == null)
            {
                dataCollectionRunSettings = new DataCollectionRunSettings();
            }
            else
            {
                DisableAllDataCollectors(dataCollectionRunSettings);
            }

            // Selectively enable data collectors as specified.
            foreach (var arg in args)
            {
                DataCollectorSettings dataCollectorSettings = null;
                if (DoesDataCollectorSettingsExist(arg, dataCollectionRunSettings, out dataCollectorSettings))
                {
                    dataCollectorSettings.IsEnabled = true;
                }
                else
                {
                    dataCollectorSettings = new DataCollectorSettings();
                    dataCollectorSettings.FriendlyName = arg;
                    dataCollectorSettings.IsEnabled = true;
                    dataCollectionRunSettings.DataCollectorSettingsList.Add(dataCollectorSettings);
                }
            }

            this.runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);
        }

        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }

        internal void DisableAllDataCollectors(DataCollectionRunSettings dataCollectionRunSettings)
        {
            foreach (var dataCollectorSetting in dataCollectionRunSettings.DataCollectorSettingsList)
            {
                dataCollectorSetting.IsEnabled = false;
            }
        }

        internal static bool DoesDataCollectorSettingsExist(string friendlyName,
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
    }
}
