// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    ///  An argument processor that allows the user to specify the target platform architecture
    ///  for test run.
    /// </summary>
    internal class FrameworkArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the OutputArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Framework";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new FrameworkArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new FrameworkArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class FrameworkArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => FrameworkArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        public override string HelpContentResourceName => CommandLineResources.FrameworkArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.FrameworkArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// Argument Executor for the "/Platform" command line argument.
    /// </summary>
    internal class FrameworkArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        private IRunSettingsProvider runSettingsManager;

        public const string RunSettingsPath = "RunConfiguration.TargetFrameworkVersion";

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options"> The options. </param>
        /// <param name="runSettingsManager"> The runsettings manager. </param>
        public FrameworkArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager)
        {
            Contract.Requires(options != null);
            Contract.Requires(runSettingsManager != null);
            this.commandLineOptions = options;
            this.runSettingsManager = runSettingsManager;
        }

        #endregion

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new CommandLineException(CommandLineResources.FrameworkVersionRequired);
            }
            
            var validFramework = Framework.FromString(argument);
            if (validFramework == null)
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidFrameworkVersion, argument));
            }

            this.commandLineOptions.TargetFrameworkVersion = validFramework;

            if (this.commandLineOptions.TargetFrameworkVersion != Framework.DefaultFramework
                && !string.IsNullOrWhiteSpace(this.commandLineOptions.SettingsFile)
                && MSTestSettingsUtilities.IsLegacyTestSettingsFile(this.commandLineOptions.SettingsFile))
            {
                // Legacy testsettings file support only default target framework.
                IOutput output = ConsoleOutput.Instance;
                output.Warning(
                    false,
                    CommandLineResources.TestSettingsFrameworkMismatch,
                    this.commandLineOptions.TargetFrameworkVersion.ToString(),
                    Framework.DefaultFramework.ToString());
            }
            else
            {
                this.runSettingsManager.UpdateRunSettingsNode(FrameworkArgumentExecutor.RunSettingsPath,
                    validFramework.ToString());
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Using .Net Framework version:{0}", commandLineOptions.TargetFrameworkVersion);
            }
        }

        /// <summary>
        /// The output path is already set, return success.
        /// </summary>
        /// <returns> The <see cref="ArgumentProcessorResult"/> Success </returns>
        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }

        #endregion
    }
}
