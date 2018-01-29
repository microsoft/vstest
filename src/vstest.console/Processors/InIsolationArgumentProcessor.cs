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
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    ///  An argument processor that allows the user to specify whether the execution
    ///  should happen in the current vstest.console.exe process or a new different process.
    /// </summary>
    internal class InIsolationArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        public const string CommandName = "/InIsolation";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new InIsolationArgumentProcessorCapabilities());
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
                    this.executor =
                        new Lazy<IArgumentExecutor>(
                            () =>
                            new InIsolationArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class InIsolationArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => InIsolationArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        public override string HelpContentResourceName => CommandLineResources.InIsolationHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.InIsolationArgumentProcessorHelpPriority;
    }

    internal class InIsolationArgumentExecutor : IArgumentExecutor
    {
        private CommandLineOptions commandLineOptions;
        private IRunSettingsProvider runSettingsManager;

        public const string RunSettingsPath = "RunConfiguration.InIsolation";

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">Commandline options</param>
        /// <param name="runSettingsManager">the runsettings manager</param>
        public InIsolationArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager)
        {
            Contract.Requires(options != null);
            this.commandLineOptions = options;
            this.runSettingsManager = runSettingsManager;
        }
        #endregion

        #region IArgumentProcessor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            // InIsolation does not require any argument, throws exception if argument specified
            if (!string.IsNullOrWhiteSpace(argument))
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidInIsolationCommand, argument));
            }

            this.commandLineOptions.InIsolation = true;
        }

        /// <summary>
        /// Execute.
        /// </summary>
        public ArgumentProcessorResult Execute()
        {

            this.runSettingsManager.UpdateRunSettingsNode(InIsolationArgumentExecutor.RunSettingsPath, "true");

            // Nothing to do since we updated the parameter during initialize parameter
            return ArgumentProcessorResult.Success;
        }

        /// <inheritdoc />
        public bool LazyExecuteInDesignMode => false;

        #endregion
    }
}
