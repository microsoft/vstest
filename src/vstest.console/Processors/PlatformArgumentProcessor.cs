// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using Resources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    ///  An argument processor that allows the user to specify the target platform architecture
    ///  for test run.
    /// </summary>
    internal class PlatformArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the OutputArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Platform";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new PlatformArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new PlatformArgumentExecutor(CommandLineOptions.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class PlatformArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => PlatformArgumentProcessor.CommandName;
        
        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        public override string HelpContentResourceName =>  Resources.PlatformArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.PlatformArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// Argument Executor for the "/Platform" command line argument.
    /// </summary>
    internal class PlatformArgumentExecutor : IArgumentExecutor
    {
        #region Fields

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
        public PlatformArgumentExecutor(CommandLineOptions options)
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
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new CommandLineException(Resources.PlatformTypeRequired);
            }

            Architecture platform;
            var validPlatform = Enum.TryParse(argument, true, out platform);
            if (validPlatform)
            {
                validPlatform = platform == Architecture.X86 || platform == Architecture.X64 || platform == Architecture.ARM;
            }

            if (validPlatform)
            {
                this.commandLineOptions.TargetArchitecture = platform;
            }
            else
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, Resources.InvalidPlatformType, argument));
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Using platform:{0}", this.commandLineOptions.TargetArchitecture);
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
