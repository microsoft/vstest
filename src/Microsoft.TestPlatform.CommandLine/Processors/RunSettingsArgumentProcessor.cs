// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.TestPlatform.CommandLine.Resources.Resources;

    internal class RunSettingsArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the PortArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Settings";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new RunSettingsArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new RunSettingsArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class RunSettingsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => RunSettingsArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.RunSettings;

        public override string HelpContentResourceName => CommandLineResources.RunSettingsArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.RunSettingsArgumentProcessorHelpPriority;
    }

    internal class RunSettingsArgumentExecutor : IArgumentExecutor
    {
        private CommandLineOptions commandLineOptions;
        private IRunSettingsProvider runSettingsManager;

        internal IFileHelper FileHelper { get; set; }

        internal RunSettingsArgumentExecutor(CommandLineOptions commandLineOptions, IRunSettingsProvider runSettingsManager)
        {
            this.commandLineOptions = commandLineOptions;
            this.runSettingsManager = runSettingsManager;
            this.FileHelper = new FileHelper();
        }

        public void Initialize(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new CommandLineException(CommandLineResources.RunSettingsRequired);
            }

            if (!this.FileHelper.Exists(argument))
            {
                throw new CommandLineException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CommandLineResources.RunSettingsFileNotFound,
                        argument));
            }

            Contract.EndContractBlock();

            // Load up the run settings and set it as the active run settings.
            try
            {
                IXPathNavigable document = this.GetRunSettingsDocument(argument);
                this.runSettingsManager.UpdateRunSettings(document.CreateNavigator().OuterXml);

                //Add default runsettings values if not exists in given runsettings file.
                this.runSettingsManager.AddDefaultRunSettings();

                this.commandLineOptions.SettingsFile = argument;
            }
            catch (XmlException exception)
            {
                throw new CommandLineException(CommandLineResources.MalformedRunSettingsFile, exception);
            }
            catch (SettingsException exception)
            {
                throw new CommandLineException(exception.Message, exception);
            }
        }

        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        protected virtual XmlReader GetReaderForFile(string runSettingsFile)
        {
            return XmlReader.Create(runSettingsFile, XmlRunSettingsUtilities.ReaderSettings);
        }

        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        private IXPathNavigable GetRunSettingsDocument(string runSettingsFile)
        {
            IXPathNavigable runSettingsDocument;

            if (!MSTestSettingsUtilities.IsLegacyTestSettingsFile(runSettingsFile))
            {
                using (XmlReader reader = this.GetReaderForFile(runSettingsFile))
                {
                    var settingsDocument = new XmlDocument();
                    settingsDocument.Load(reader);
                    ClientUtilities.FixRelativePathsInRunSettings(settingsDocument, runSettingsFile);
#if NET451
                    runSettingsDocument = settingsDocument;
#else
                    runSettingsDocument = settingsDocument.ToXPathNavigable();
#endif
                }
            }
            else
            {
                runSettingsDocument = XmlRunSettingsUtilities.CreateDefaultRunSettings();
                runSettingsDocument = MSTestSettingsUtilities.Import(runSettingsFile, runSettingsDocument, Architecture.X86, FrameworkVersion.Framework45);
            }

            if (this.commandLineOptions.EnableCodeCoverage == true)
            {
                try
                {
                    CodeCoverageDataAdapterUtilities.UpdateWithCodeCoverageSettingsIfNotConfigured(runSettingsDocument);
                }
                catch (XPathException e)
                {
                    throw new SettingsException(CommandLineResources.MalformedRunSettingsFile, e);
                }
            }

            return runSettingsDocument;
        }

        public ArgumentProcessorResult Execute()
        {
            // Nothing to do here, the work was done in initialization.
            return ArgumentProcessorResult.Success;
        }
    }
}
