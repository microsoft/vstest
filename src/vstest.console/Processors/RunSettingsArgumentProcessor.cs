// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

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

        public override bool AlwaysExecute => true;

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
            // If no runsettings file is passed in and a single assembly is invoked,
            // search for one in the assembly's directory.
            if (argument == null)
            {
                if (commandLineOptions.Sources.Count() != 1 ||
                    !AutoDetectRunSettingsFile(out argument, commandLineOptions.Sources.Single()))
                {
                    // No runsettings file found via auto-detection. Stopping initialization.
                    return;
                }
            }
            else
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
            }

            Contract.EndContractBlock();

            // Load up the run settings and set it as the active run settings.
            try
            {
                XmlDocument document = this.GetRunSettingsDocument(argument);

                this.runSettingsManager.UpdateRunSettings(document.OuterXml);

                // To determine whether to infer framework and platform.
                ExtractFrameworkAndPlatform();

                //Add default runsettings values if not exists in given runsettings file.
                this.runSettingsManager.AddDefaultRunSettings();

                this.commandLineOptions.SettingsFile = argument;

                if (this.runSettingsManager.QueryRunSettingsNode("RunConfiguration.EnvironmentVariables") != null)
                {
                    this.commandLineOptions.InIsolation = true;
                    this.runSettingsManager.UpdateRunSettingsNode(InIsolationArgumentExecutor.RunSettingsPath, "true");
                }

                var testCaseFilter = this.runSettingsManager.QueryRunSettingsNode("RunConfiguration.TestCaseFilter");
                if (testCaseFilter != null)
                {
                    this.commandLineOptions.TestCaseFilterValue = testCaseFilter;
                }
            }
            catch (XmlException exception)
            {
                throw new SettingsException(
                        string.Format(CultureInfo.CurrentCulture, "{0} {1}", ObjectModel.Resources.CommonResources.MalformedRunSettingsFile, exception.Message),
                        exception);
            }
        }

        private static bool AutoDetectRunSettingsFile(out string runsettingsFile, string source)
        {
            try
            {
                // Search for ".runsettings" file direct matches only.
                FileInfo[] files = new FileInfo(source).Directory.GetFiles(".runsettings");
                runsettingsFile = files.FirstOrDefault()?.FullName;
                if (runsettingsFile != null)
                {
                    EqtTrace.Verbose("Executor.Execute: Runsettings auto detection, using: {0}", runsettingsFile);
                    return true;
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Verbose("Failed runsettings auto-detection: {0}", ex.Message);
            }

            runsettingsFile = null;
            return false;
        }

        private void ExtractFrameworkAndPlatform()
        {
            var framworkStr = this.runSettingsManager.QueryRunSettingsNode(FrameworkArgumentExecutor.RunSettingsPath);
            Framework framework = Framework.FromString(framworkStr);
            if (framework != null)
            {
                this.commandLineOptions.TargetFrameworkVersion = framework;
            }

            var platformStr = this.runSettingsManager.QueryRunSettingsNode(PlatformArgumentExecutor.RunSettingsPath);
            if (Enum.TryParse<Architecture>(platformStr, true, out var architecture))
            {
                this.commandLineOptions.TargetArchitecture = architecture;
            }
        }

        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        protected virtual XmlReader GetReaderForFile(string runSettingsFile)
        {
            return XmlReader.Create(new StringReader(File.ReadAllText(runSettingsFile, Encoding.UTF8)), XmlRunSettingsUtilities.ReaderSettings);
        }

        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        private XmlDocument GetRunSettingsDocument(string runSettingsFile)
        {
            XmlDocument runSettingsDocument;

            if (!MSTestSettingsUtilities.IsLegacyTestSettingsFile(runSettingsFile))
            {
                using (XmlReader reader = this.GetReaderForFile(runSettingsFile))
                {
                    var settingsDocument = new XmlDocument();
                    settingsDocument.Load(reader);
                    ClientUtilities.FixRelativePathsInRunSettings(settingsDocument, runSettingsFile);
                    runSettingsDocument = settingsDocument;
                }
            }
            else
            {
                runSettingsDocument = XmlRunSettingsUtilities.CreateDefaultRunSettings();
                runSettingsDocument = MSTestSettingsUtilities.Import(runSettingsFile, runSettingsDocument);
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
