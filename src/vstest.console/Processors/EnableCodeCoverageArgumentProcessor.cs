// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// The argument processor for enabling data collectors.
    /// </summary>
    internal class EnableCodeCoverageArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of command for enabling code coverage.
        /// </summary>
        public const string CommandName = "/EnableCodeCoverage";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new EnableCodeCoverageArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableCodeCoverageArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    /// <inheritdoc />
    internal class EnableCodeCoverageArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => EnableCodeCoverageArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        //public override string HelpContentResourceName => CommandLineResources.EnableCodeCoverageArgumentProcessorHelp;

        //public override HelpContentPriority HelpPriority => HelpContentPriority.EnableCodeCoverageArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The enable code coverage argument executor.
    /// </summary>
    internal class EnableCodeCoverageArgumentExecutor : IArgumentExecutor
    {
        #region private variables

        private IRunSettingsProvider runSettingsManager;
        private CommandLineOptions commandLineOptions;

        private const string FriendlyName = "Code Coverage";

        private static string xPathSeperator = "/";
        private static string[] nodeNames = new string[] { Constants.RunSettingsName, Constants.DataCollectionRunSettingsName, Constants.DataCollectorsSettingName, Constants.DataCollectorSettingName };

        #region Default  CodeCoverage Settings String

        private static string codeCoverageCollectorSettingsTemplate =
@"      <DataCollector uri=""datacollector://microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=15.0.0.0 " + @", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" friendlyName=""Code Coverage"">" + Environment.NewLine +
@"      </DataCollector>";

        #endregion

        #endregion

        internal EnableCodeCoverageArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager)
        {
            this.commandLineOptions = options;
            this.runSettingsManager = runSettingsManager;
        }

        /// <inheritdoc />
        public void Initialize(string argument)
        {
            this.commandLineOptions.EnableCodeCoverage = true;

            // Add this enabled data collectors list, this will ensure Code Coverage isn't disabled when other DCs are configured using /Collect.
            CollectArgumentExecutor.AddDataCollectorFriendlyName(FriendlyName);
            try
            {
                this.UpdateWithCodeCoverageSettingsIfNotConfigured();
            }
            catch (XPathException e)
            {
                throw new SettingsException(
                    string.Format(CultureInfo.CurrentCulture, "{0} {1}", ObjectModel.Resources.CommonResources.MalformedRunSettingsFile, e.Message), e);
            }
        }

        /// <inheritdoc />
        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }

        /// <summary>
        /// Updates with code coverage settings if not configured.
        /// </summary>
        /// <param name="runSettingsDocument"> The run settings document. </param>
        private void UpdateWithCodeCoverageSettingsIfNotConfigured()
        {
            var runsettingsXml = this.runSettingsManager.ActiveRunSettings?.SettingsXml;
            if (runsettingsXml == null)
            {
                this.runSettingsManager.AddDefaultRunSettings();
                runsettingsXml = this.runSettingsManager.ActiveRunSettings?.SettingsXml;
            }

            IXPathNavigable runSettingsDocument;
            using (var stream = new StringReader(runsettingsXml))
            using (var reader = XmlReader.Create(stream, XmlRunSettingsUtilities.ReaderSettings))
            {
                var document = new XmlDocument();
                document.Load(reader);
#if NET451
                runSettingsDocument = document;
#else
                runSettingsDocument = document.ToXPathNavigable();
#endif
            }

            var runSettingsNavigator = runSettingsDocument.CreateNavigator();

            if (ContainsDataCollectorWithFriendlyName(runSettingsNavigator, FriendlyName))
            {
                // runsettings already has Code coverage data collector, just enable it.
                CollectArgumentExecutor.AddDataCollectorToRunSettings(FriendlyName, this.runSettingsManager);
            }
            else
            {
                var existingPath = string.Empty;
                var xpaths = new string[]
                             {
                                 string.Join(xPathSeperator, nodeNames, 0, 1),
                                 string.Join(xPathSeperator, nodeNames, 0, 2),
                                 string.Join(xPathSeperator, nodeNames, 0, 3)
                             };

                foreach (var xpath in xpaths)
                {
                    if (runSettingsNavigator.SelectSingleNode(xpath) != null)
                    {
                        existingPath = xpath;
                    }
                    else
                    {
                        break;
                    }
                }

                // If any nodes are missing to add code coverage deafult settings, add the missing xml nodes.
                XPathNavigator dataCollectorsNavigator;
                if (existingPath.Equals(xpaths[2]) == false)
                {
                    dataCollectorsNavigator = runSettingsNavigator.SelectSingleNode(existingPath);
                    var missingNodesText = GetMissingNodesTextIfAny(existingPath, xpaths[2]);
                    dataCollectorsNavigator.AppendChild(missingNodesText);
                }

                dataCollectorsNavigator = runSettingsNavigator.SelectSingleNode(xpaths[2]);
                dataCollectorsNavigator.AppendChild(codeCoverageCollectorSettingsTemplate);

                this.runSettingsManager.UpdateRunSettings(runSettingsDocument.CreateNavigator().OuterXml);
            }
        }

        private static string GetMissingNodesTextIfAny(string existingPath, string fullpath)
        {
            var xmlText = "{0}";
            var nonExistingPath = fullpath.Substring(existingPath.Length);
            var requiredNodeNames = nonExistingPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var format = "<{0}>{1}</{0}>";

            foreach (var nodeName in requiredNodeNames)
            {
                xmlText = string.Format(CultureInfo.InvariantCulture, xmlText, string.Format(CultureInfo.InvariantCulture, format, nodeName, "{0}"));
            }

            xmlText = string.Format(CultureInfo.InvariantCulture, xmlText, string.Empty);
            return xmlText;
        }

        /// <summary>
        /// Check data collector exist with friendly name
        /// </summary>
        /// <param name="runSettingDocument"> XPathNavigable representation of a runsettings file </param>
        /// <param name="dataCollectorFriendlyName"> The data Collector friendly name. </param>
        /// <returns> True if there is a datacollector configured. </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#")]
        private static bool ContainsDataCollectorWithFriendlyName(IXPathNavigable runSettingDocument, string dataCollectorFriendlyName)
        {
            if (runSettingDocument == null)
            {
                throw new ArgumentNullException(nameof(runSettingDocument));
            }

            if (dataCollectorFriendlyName == null)
            {
                throw new ArgumentNullException(nameof(dataCollectorFriendlyName));
            }

            var navigator = runSettingDocument.CreateNavigator();
            var nodes = navigator.Select("/RunSettings/DataCollectionRunSettings/DataCollectors/DataCollector");

            foreach (XPathNavigator dataCollectorNavigator in nodes)
            {
                var fn = dataCollectorNavigator.GetAttribute("friendlyName", string.Empty);
                if (string.Equals(dataCollectorFriendlyName, fn, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
