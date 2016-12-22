// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// The argument processor for runsettings passed as argument through cli
    /// </summary>
    internal class CLIRunSettingsArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the PortArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "--";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new CLIRunSettingsArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new CLIRunSettingsArgumentExecutor(RunSettingsManager.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class CLIRunSettingsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => CLIRunSettingsArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.CLIRunSettings;

        public override string HelpContentResourceName => CommandLineResources.CLIRunSettingsArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.CLIRunSettingsArgumentProcessorHelpPriority;
    }

    internal class CLIRunSettingsArgumentExecutor : IArgumentExecutor
    {
        // Matches key="value"
        private const string Pattern1 = "[:_a-zA-Z][[:_\\-\\.0-9a-zA-Z]*=\"[^\\s\"][^\"]*\"";
        // Matches key='value'
        private const string Pattern2 = "[:_a-zA-Z][[:_\\-\\.0-9a-zA-Z]*=\'[^\\s\'][^\']*\'";
        // Matches key=value
        private const string Pattern3 = "[:_a-zA-Z][[:_\\-\\.0-9a-zA-Z]*=[^\'\\s\"][^\\s]*[^\\s\"']";

        private IRunSettingsProvider runSettingsManager;

        internal CLIRunSettingsArgumentExecutor(IRunSettingsProvider runSettingsManager)
        {
            this.runSettingsManager = runSettingsManager;
        }

        public void Initialize(string argument)
        {
            // if argument is null or white space, don't do anything.
            if (string.IsNullOrWhiteSpace(argument))
            {
                return;
            }

            Contract.EndContractBlock();

            // Load up the run settings and set it as the active run settings.
            try
            {
                var doc = new XmlDocument();

                if (this.runSettingsManager.ActiveRunSettings != null && !string.IsNullOrEmpty(this.runSettingsManager.ActiveRunSettings.SettingsXml))
                {
                    var settingsXml = this.runSettingsManager.ActiveRunSettings.SettingsXml;

#if net46
                    using (var reader = XmlReader.Create(new StringReader(settingsXml), new XmlReaderSettings() { XmlResolver = null, CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                    {
#else
                    using (var reader = XmlReader.Create(new StringReader(settingsXml), new XmlReaderSettings() { CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                    {
#endif
                        doc.Load(reader);
                    }
                }
                else
                {
#if net46
                    doc = (XmlDocument)XmlRunSettingsUtilities.CreateDefaultRunSettings();
#else
                    using (var reader = XmlReader.Create(new StringReader(XmlRunSettingsUtilities.CreateDefaultRunSettings().CreateNavigator().OuterXml), new XmlReaderSettings() { CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                    {
                        doc.Load(reader);
                    }
#endif
                }

                // Append / Override run settings supplied in CLI
                CreateOrOverwriteRunSettings(doc, argument);

                // Set Active Run Settings.
                var runSettings = new RunSettings();
                runSettings.LoadSettingsXml(doc.OuterXml);
                this.runSettingsManager.SetActiveRunSettings(runSettings);
            }
            catch (SettingsException exception)
            {
                throw new CommandLineException(exception.Message, exception);
            }
        }

        public ArgumentProcessorResult Execute()
        {
            // Nothing to do here, the work was done in initialization.
            return ArgumentProcessorResult.Success;
        }

        private void CreateOrOverwriteRunSettings(XmlDocument xmlDoc, string arg)
        {
            var args = ParseArgument(arg);
            var length = args.Length;

            for (int index = 0; index < length; index++)
            {
                var keyValuePair = args[index];
                var indexOfSeparator = keyValuePair.IndexOf("=");
                var key = keyValuePair.Substring(0, indexOfSeparator);
                var value = keyValuePair.Substring(indexOfSeparator + 1);

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                // Check if the key exists. 
                var xPath = key.Replace('.', '/');
                var node = xmlDoc.SelectSingleNode(string.Format("//RunSettings/{0}", xPath));

                if (node == null)
                {
                    node = CreateNode(xmlDoc, key.Split('.'));
                }

                node.InnerText = value;
            }
        }

        internal string[] ParseArgument(string arg)
        {
            var args = new List<string>();

            foreach (Match match in Regex.Matches(arg, Pattern1))
            {
                args.Add(match.Value);
            }

            foreach (Match match in Regex.Matches(arg, Pattern2))
            {
                args.Add(match.Value);
            }

            foreach (Match match in Regex.Matches(arg, Pattern3))
            {
                args.Add(match.Value);
            }

            return args.ToArray();
        }

        private XmlNode CreateNode(XmlDocument doc, string[] xPath)
        {
            XmlNode node = null;
            XmlNode parent = doc.DocumentElement;

            for (int i = 0; i < xPath.Length; i++)
            {
                node = parent.SelectSingleNode(xPath[i]);

                if (node == null)
                {
                    node = parent.AppendChild(doc.CreateElement(xPath[i]));
                }

                parent = node;
            }

            return node;
        }
    }
}
