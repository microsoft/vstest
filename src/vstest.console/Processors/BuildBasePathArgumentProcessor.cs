// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Resources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;
    using TestPlatform.Utilities.Helpers;
    using TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// Argument Executor for the "--BuildBasePath|/BuildBasePath" command line argument.
    /// </summary>
    internal class BuildBasePathArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the BuildBasePathArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/BuildBasePath";

        /// <summary>
        /// The short name of the command line argument that the BuildBasePathArgumentExecutor handles.
        /// </summary>
        public const string ShortCommandName = "/b";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new BuildBasePathArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new BuildBasePathArgumentExecutor(CommandLineOptions.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class BuildBasePathArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => BuildBasePathArgumentProcessor.CommandName;

        public override string ShortCommandName => BuildBasePathArgumentProcessor.ShortCommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => Resources.BuildBasePathArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.BuildBasePathArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// Argument Executor for the "/BuildBasePath" command line argument.
    /// </summary>
    internal class BuildBasePathArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        internal IFileHelper FileHelper { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options">
        /// The options.
        /// </param>
        public BuildBasePathArgumentExecutor(CommandLineOptions options)
        {
            Contract.Requires(options != null);
            this.commandLineOptions = options;
            this.FileHelper = new FileHelper();
        }
        #endregion

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            if (!FileHelper.Exists(argument))
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, Resources.BuildBasePathNotFound));
            }

            this.commandLineOptions.BuildBasePath = argument;
        }

        /// <summary>
        /// The BuildBasePath is already set, return success.
        /// </summary>
        /// <returns> The <see cref="ArgumentProcessorResult"/> Success </returns>
        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }

        #endregion
    }
}
