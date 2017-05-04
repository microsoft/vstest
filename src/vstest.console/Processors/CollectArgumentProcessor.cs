// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.IO;
    using System.Xml;

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

        public override bool AllowMultiple => true;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        public override string HelpContentResourceName => CommandLineResources.CollectArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.CollectArgumentProcessorHelpPriority;
    }

    /// <inheritdoc />
    internal class CollectArgumentExecutor : IArgumentExecutor
    {
        private IRunSettingsProvider runSettingsManager;
        internal static List<string> EnabledDataCollectors = new List<string>();

        private const string DefaultConfigurationSettings = @"<Configuration />";
        internal CollectArgumentExecutor(IRunSettingsProvider runSettingsManager)
        {
            this.runSettingsManager = runSettingsManager;
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

            EnabledDataCollectors.Add(argument.ToLower());

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
                // By default, all data collectors present in run settings are enabled, if enabled attribute is not specified.
                // So explicitely disable those data collectors and enable those which are specified. 
                DisableUnConfiguredDataCollectors(dataCollectionRunSettings);
            }

            // Add data collectors if not already present, enable if already present.
            EnableDataCollectorUsingFriendlyName(argument, dataCollectionRunSettings);

            this.runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);
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
                dataCollectionRunSettings.DataCollectorSettingsList.Add(dataCollectorSettings);
            }

            // todo: remove adding default configuration element once we pass full runsettings to datacollectors
            if (dataCollectorSettings.Configuration == null)
            {
                var doc = new XmlDocument();
                using (
                    var xmlReader = XmlReader.Create(
                        new StringReader(DefaultConfigurationSettings),
                        new XmlReaderSettings() { CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                {
                    doc.Load(xmlReader);
                }

                dataCollectorSettings.Configuration = doc.DocumentElement;
                var settings = RunSettingsManager.Instance.ActiveRunSettings?.SettingsXml;

                var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settings);
                var frameWork = runConfiguration.TargetFrameworkVersion;
                var architecture = runConfiguration.TargetPlatform;

                // Add Framework config, since it could be required by DataCollector, to determine whether they support this Framework or not
                if (frameWork != null)
                {
                    AppendChildNodeOrInnerText(doc, dataCollectorSettings.Configuration, "Framework", "", frameWork.Name);
                }
                // Add Architecture configuration, since native binaries are Architecture specific, & datacollectors might need this information
                // to load appropriate native dll.
                AppendChildNodeOrInnerText(doc, dataCollectorSettings.Configuration, "Architecture", "", architecture.ToString());
            }

            dataCollectorSettings.IsEnabled = true;
        }

        private static void DisableUnConfiguredDataCollectors(DataCollectionRunSettings dataCollectionRunSettings)
        {
            foreach (var dataCollectorSetting in dataCollectionRunSettings.DataCollectorSettingsList)
            {
                if (EnabledDataCollectors.Contains(dataCollectorSetting.FriendlyName.ToLower()))
                {
                    dataCollectorSetting.IsEnabled = true;
                }
                else
                {
                    dataCollectorSetting.IsEnabled = false;
                }
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

        private static void AppendChildNodeOrInnerText(XmlDocument doc, XmlElement owner, string elementName, string nameSpaceUri, string innerText)
        {
            var node = owner.SelectSingleNode(elementName) ?? doc.CreateNode("element", elementName, nameSpaceUri);
            node.InnerText = innerText;
            owner.AppendChild(node);
        }
    }
}
