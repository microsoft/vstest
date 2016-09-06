// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Resources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;

    /// <summary>
    /// Argument Executor for the "-c|--Configuration|/c|/Configuration" command line argument.
    /// </summary>
    internal class ConfigurationArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the ConfigurationArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Configuration";

        /// <summary>
        /// The short name of the command line argument that the ConfigurationArgumentExecutor handles.
        /// </summary>
        public const string ShortCommandName = "/c";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ConfigurationArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new ConfigurationArgumentExecutor(CommandLineOptions.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class ConfigurationArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => ConfigurationArgumentProcessor.CommandName;

        public override string ShortCommandName => ConfigurationArgumentProcessor.ShortCommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => Resources.ConfigurationArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.ConfigurationArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// Argument Executor for the "/Configuration" command line argument.
    /// </summary>
    internal class ConfigurationArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        private string[] ValidConfigs = { "Debug", "Release" };

        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options">
        /// The options.
        /// </param>
        public ConfigurationArgumentExecutor(CommandLineOptions options)
        {
            Contract.Requires(options != null);
            this.commandLineOptions = options;
        }
        #endregion

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument) || !ValidConfigs.Contains(argument))
            {
                //We might want to check for Debug/Release
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, Resources.InvalidConfiguration));
            }

            this.commandLineOptions.Configuration = argument;
        }

        /// <summary>
        /// The configuration is already set, return success.
        /// </summary>
        /// <returns> The <see cref="ArgumentProcessorResult"/> Success </returns>
        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }
        #endregion
    }
}
