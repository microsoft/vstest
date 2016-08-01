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
    /// Argument Executor for the "/Output" command line argument.
    /// </summary>
    internal class OutputArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the OutputArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Output";

        /// <summary>
        /// The short name of the command line argument that the OutputArgumentExecutor handles.
        /// </summary>
        public const string ShortCommandName = "/o";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new OutputArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new OutputArgumentExecutor(CommandLineOptions.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class OutputArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => OutputArgumentProcessor.CommandName;

        public override string ShortCommandName => OutputArgumentProcessor.ShortCommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => Resources.OutputArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.OutputArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// Argument Executor for the "/Output" command line argument.
    /// </summary>
    internal class OutputArgumentExecutor : IArgumentExecutor
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
        public OutputArgumentExecutor(CommandLineOptions options)
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
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, Resources.OutputPathNotFound));
            }

            this.commandLineOptions.Output = argument;
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
