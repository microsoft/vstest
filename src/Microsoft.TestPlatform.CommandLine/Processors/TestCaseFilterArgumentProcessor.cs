// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using Microsoft.TestPlatform.CommandLine;
    using CommandLineResources = Microsoft.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Argument Executor for the "/TestCaseFilter" command line argument.
    /// </summary>
    internal class TestCaseFilterArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the TestCaseFilterArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/TestCaseFilter";
        
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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new TestCaseFilterArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new TestCaseFilterArgumentExecutor(CommandLineOptions.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class TestCaseFilterArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => TestCaseFilterArgumentProcessor.CommandName;
        
        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => CommandLineResources.TestCaseFilterArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.TestCaseFilterArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// Argument Executor for the "/TestCaseFilter" command line argument.
    /// </summary>
    internal class TestCaseFilterArgumentExecutor : IArgumentExecutor
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
        public TestCaseFilterArgumentExecutor(CommandLineOptions options)
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
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, CommandLineResources.TestCaseFilterValueRequired));
            }

            this.commandLineOptions.TestCaseFilterValue = argument;
        }

        /// <summary>
        /// The TestCaseFilter is already set, return success.
        /// </summary>
        /// <returns> The <see cref="ArgumentProcessorResult"/> Success </returns>
        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }
        #endregion
    }
}
