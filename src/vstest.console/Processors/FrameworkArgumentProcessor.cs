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

    using CommandLineResources = Resources.Resources;

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
                if (metadata == null)
                {
                    metadata = new Lazy<IArgumentProcessorCapabilities>(() => new FrameworkArgumentProcessorCapabilities());
                }

                return metadata;
            }
        }

        /// <summary>
        /// Gets or sets the executor.
        /// </summary>
        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (executor == null)
                {
                    executor = new Lazy<IArgumentExecutor>(() => new FrameworkArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
                }

                return executor;
            }

            set
            {
                executor = value;
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
        private readonly CommandLineOptions commandLineOptions;

        private readonly IRunSettingsProvider runSettingsManager;

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
            commandLineOptions = options;
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
            commandLineOptions.TargetFrameworkVersion = validFramework ?? throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidFrameworkVersion, argument));

            if (commandLineOptions.TargetFrameworkVersion != Framework.DefaultFramework
                && !string.IsNullOrWhiteSpace(commandLineOptions.SettingsFile)
                && MSTestSettingsUtilities.IsLegacyTestSettingsFile(commandLineOptions.SettingsFile))
            {
                // Legacy testsettings file support only default target framework.
                IOutput output = ConsoleOutput.Instance;
                output.Warning(
                    false,
                    CommandLineResources.TestSettingsFrameworkMismatch,
                    commandLineOptions.TargetFrameworkVersion.ToString(),
                    Framework.DefaultFramework.ToString());
            }
            else
            {
                runSettingsManager.UpdateRunSettingsNode(RunSettingsPath,
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
