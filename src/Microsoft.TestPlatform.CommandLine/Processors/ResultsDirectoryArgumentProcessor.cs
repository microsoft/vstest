// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Security;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using CommandLineResources = Microsoft.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Allows the user to specify a path to save test results.
    /// </summary>
    internal class ResultsDirectoryArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the ListTestsArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/ResultsDirectory";

        private const string RunSettingsPath = "RunConfiguration.ResultsDirectory";
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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ResultsDirectoryArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
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
    internal class ResultsDirectoryArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => ResultsDirectoryArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        public override string HelpContentResourceName => CommandLineResources.ResultsDirectoryArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.ResultsDirectoryArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class ResultsDirectoryArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        private IRunSettingsProvider runSettingsManager;

        public const string RunSettingsPath = "RunConfiguration.ResultsDirectory";

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options"> The options. </param>
        /// <param name="testPlatform">The test platform</param>
        public ResultsDirectoryArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager)
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
                throw new CommandLineException(CommandLineResources.ResultsDirectoryValueRequired);
            }

            try
            {
                // to check valid directory path
                var di = new DirectoryInfo(argument);

                if (!Path.IsPathRooted(argument))
                {
                    argument = Path.GetFullPath(argument);
                }
            }
            catch (Exception ex) when( ex is NotSupportedException || ex is SecurityException || ex is ArgumentException || ex is PathTooLongException)
            {
                throw new CommandLineException(string.Format(CommandLineResources.InvalidResultsDirectoryPathCommand, argument, ex.Message));
            }

            this.commandLineOptions.ResultsDirectory = argument;
            this.runSettingsManager.UpdateRunSettingsNode(ResultsDirectoryArgumentExecutor.RunSettingsPath, argument);
        }

        /// <summary>
        /// Executes the argument processor.
        /// </summary>
        /// <returns> The <see cref="ArgumentProcessorResult"/>. </returns>
        public ArgumentProcessorResult Execute()
        {
            // Nothing to do since we updated the parameter during initialize parameter
            return ArgumentProcessorResult.Success;
        }

        #endregion
    }
}
