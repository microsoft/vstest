// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
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

            if(InferRunSettingsHelper.IsTestSettingsEnabled(this.runSettingsManager.ActiveRunSettings.SettingsXml))
            {
                throw new SettingsException(string.Format(CommandLineResources.CollectWithTestSettingErrorMessage, argument));
            }
            AddDataCollectorToRunSettings(argument, this.runSettingsManager);
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

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settings);
            if (dataCollectionRunSettings == null)
            {
                dataCollectionRunSettings = new DataCollectionRunSettings();
            }

            // Add data collectors if not already present, enable if already present.
            EnableDataCollectorUsingFriendlyName(argument, dataCollectionRunSettings);

            runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);

            // Add inproc data collector to runsetings if coverlet code coverage is enabled
            if (string.Equals(argument, Constants.CoverletDataCollectorFriendlyName, StringComparison.OrdinalIgnoreCase))
            {
                runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.InProcDataCollectionRunSettingsName, GetInprocDataCollectorSettings().InnerXml);
            }
        }

        internal static void AddDataCollectorFriendlyName(string friendlyName)
        {
            EnabledDataCollectors.Add(friendlyName.ToLower());
        }

        private static XmlElement GetInprocDataCollectorSettings()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement(Constants.InProcDataCollectionRunSettingsName);
            XmlElement subRoot = doc.CreateElement(Constants.InProcDataCollectorsSettingName);
            XmlElement inprocRoot = doc.CreateElement(Constants.InProcDataCollectorSettingName);

            AppendAttribute(doc, inprocRoot, "codebase", Constants.CoverletDataCollectorCodebase);
            AppendAttribute(doc, inprocRoot, "friendlyName", Constants.CoverletDataCollectorFriendlyName);
            AppendAttribute(doc, inprocRoot, "assemblyQualifiedName", Constants.CoverletDataCollectorAssemblyQualifiedName);
            AppendAttribute(doc, inprocRoot, "uri", Constants.CoverletDataCollectorUri);

            subRoot.AppendChild(inprocRoot);
            root.AppendChild(subRoot);

            return root;
        }

        private static void AppendAttribute(XmlDocument doc, XmlElement owner, string attributeName, string attributeValue)
        {
            if (string.IsNullOrWhiteSpace(attributeValue))
            {
                return;
            }

            XmlAttribute attribute = doc.CreateAttribute(attributeName);
            attribute.Value = attributeValue;
            owner.Attributes.Append(attribute);
        }
    }
}
