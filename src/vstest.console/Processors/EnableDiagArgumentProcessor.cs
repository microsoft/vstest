// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using Resources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;

    internal class EnableDiagArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the ListTestsArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Diag";

        #endregion

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

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
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableDiagArgumentExecutor(CommandLineOptions.Instance, TestPlatformFactory.GetTestPlatform(), ConsoleOutput.Instance));
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

        public override string HelpContentResourceName => CommandLine.Resources.EnableDiagUsage;

        public override HelpContentPriority HelpPriority => HelpContentPriority.EnableDiagArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class EnableDiagArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        /// <summary>
        /// The test platform instance.
        /// </summary>
        private ITestPlatform testPlatform;

        /// <summary>
        /// Used for sending output.
        /// </summary>
        private IOutput output;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options"> The options. </param>
        /// <param name="testPlatform">The test platform</param>
        /// <param name="output">The output</param>
        public EnableDiagArgumentExecutor(CommandLineOptions options, ITestPlatform testPlatform, IOutput output)
        {
            Contract.Requires(options != null);

            this.commandLineOptions = options;
            this.testPlatform = testPlatform;
            this.output = output;
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
                throw new CommandLineException(Resources.EnableDiagUsage);
            }

            FileInfo fileInfo = new FileInfo(argument);

            // Checking if the file is readonly
            if (fileInfo.Exists && this.IsFileReadOnly(fileInfo))
            {
                throw new CommandLineException(string.Format(Resources.LoggerFileIsReadOnly, argument));
            }
            else if (string.IsNullOrWhiteSpace(fileInfo.Extension))
            {
                // Throwing error if the argument is just path and not a file
                throw new CommandLineException(Resources.EnableDiagUsage);
            }

            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            EqtTrace.InitializeVerboseTrace(argument);
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

        internal virtual bool IsFileReadOnly(FileInfo fileInfo)
        {
            return (fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
        }

        #endregion
    }
}
