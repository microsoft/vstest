// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    internal class EnableDiagArgumentProcessor : IArgumentProcessor
    {
        /// <summary>
        /// The name of the command line argument that the ListTestsArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Diag";

        private readonly IFileHelper fileHelper;

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableDiagArgumentProcessor"/> class.
        /// </summary>
        public EnableDiagArgumentProcessor() : this(new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableDiagArgumentProcessor"/> class.
        /// </summary>
        /// <param name="fileHelper">A file helper instance.</param>
        protected EnableDiagArgumentProcessor(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new EnableDiagArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableDiagArgumentExecutor(this.fileHelper));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    /// <summary>
    /// The argument capabilities.
    /// </summary>
    internal class EnableDiagArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => EnableDiagArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Diag;

        public override string HelpContentResourceName => CommandLineResources.EnableDiagUsage;

        public override HelpContentPriority HelpPriority => HelpContentPriority.EnableDiagArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class EnableDiagArgumentExecutor : IArgumentExecutor
    {
        private readonly IFileHelper fileHelper;

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="fileHelper">The file helper.</param>
        public EnableDiagArgumentExecutor(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
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
                throw new CommandLineException(CommandLineResources.EnableDiagUsage);
            }

            if (string.IsNullOrWhiteSpace(Path.GetExtension(argument)))
            {
                // Throwing error if the argument is just path and not a file
                throw new CommandLineException(CommandLineResources.EnableDiagUsage);
            }

            // Create the base directory for logging if doesn't exist. Directory could be empty if just a
            // filename is provided. E.g. log.txt
            var logDirectory = Path.GetDirectoryName(argument);
            if (!string.IsNullOrEmpty(logDirectory) && !this.fileHelper.DirectoryExists(logDirectory))
            {
                this.fileHelper.CreateDirectory(logDirectory);
            }

            // Find full path and send this to testhost so that vstest and testhost create logs at same location.
            argument = Path.GetFullPath(argument);

            // Catch exception(UnauthorizedAccessException, PathTooLongException...) if there is any at time of initialization.
            if (!EqtTrace.InitializeVerboseTrace(argument))
            {
                if (!string.IsNullOrEmpty(EqtTrace.ErrorOnInitialization))
                    ConsoleOutput.Instance.Warning(false, EqtTrace.ErrorOnInitialization);
            }
        }

        /// <summary>
        /// Executes the argument processor.
        /// </summary>
        /// <returns>The <see cref="ArgumentProcessorResult"/>.</returns>
        public ArgumentProcessorResult Execute()
        {
            // Nothing to do since we updated the parameter during initialize parameter
            return ArgumentProcessorResult.Success;
        }

        /// <inheritdoc />
        public bool LazyExecuteInDesignMode => false;
        #endregion
    }
}
