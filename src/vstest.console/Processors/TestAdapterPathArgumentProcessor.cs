// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Allows the user to specify a path to load custom adapters from.
    /// </summary>
    internal class TestAdapterPathArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the ListTestsArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/TestAdapterPath";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new TestAdapterPathArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, TestPlatformFactory.GetTestPlatform(), ConsoleOutput.Instance));
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
    internal class TestAdapterPathArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => TestAdapterPathArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.TestAdapterPath;

        public override string HelpContentResourceName => CommandLineResources.TestAdapterPathHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.TestAdapterPathArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class TestAdapterPathArgumentExecutor : IArgumentExecutor
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
        public TestAdapterPathArgumentExecutor(CommandLineOptions options, ITestPlatform testPlatform, IOutput output)
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
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestAdapterPathValueRequired));
            }

            string customAdaptersPath;

            try
            {
                // Remove leading and trailing ' " ' chars...
                argument = argument.Trim().Trim(new char[] { '\"' });

                customAdaptersPath = Path.GetFullPath(argument);
                if (!Directory.Exists(customAdaptersPath))
                {
                    throw new DirectoryNotFoundException(CommandLineResources.TestAdapterPathDoesNotExist);
                }
            }
            catch (Exception e)
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidTestAdapterPathCommand, argument, e.Message));
            }

            this.commandLineOptions.TestAdapterPath = customAdaptersPath;
            var adapterFiles = new List<string>(this.GetTestAdaptersFromDirectory(customAdaptersPath));

            if (adapterFiles.Count > 0)
            {
                this.testPlatform.UpdateExtensions(adapterFiles, false);
            }
            else
            {
                // Print a warning about not finding any test adapter in provided path...
                this.output.Warning(CommandLineResources.NoAdaptersFoundInTestAdapterPath, argument);
                this.output.WriteLine(string.Empty, OutputLevel.Information);
            }
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

        /// <summary>
        /// Gets the test adapters from directory.
        /// </summary>
        /// <param name="directory"> The directory. </param>
        /// <returns> The list of test adapter assemblies. </returns>
        internal virtual IEnumerable<string> GetTestAdaptersFromDirectory(string directory)
        {
            return Directory.EnumerateFiles(directory, @"*.TestAdapter.dll", SearchOption.AllDirectories);
        }

        #endregion
    }
}
