// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    // <summary>
    //     Argument Executor for the "-?|--Help|/?|/Help" Help command line argument.
    // </summary>
    internal class HelpArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the HelpArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Help";

        /// <summary>
        /// The short name of the command line argument that the HelpArgumentExecutor handles.
        /// </summary>
        public const string ShortCommandName = "/?";


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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new HelpArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new HelpArgumentExecutor());
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
    /// The help argument processor capabilities.
    /// </summary>
    internal class HelpArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => HelpArgumentProcessor.CommandName;

        public override string ShortCommandName => HelpArgumentProcessor.ShortCommandName;

        public override string HelpContentResourceName => CommandLineResources.HelpArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.HelpArgumentProcessorHelpPriority;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Help;
    }

    /// <summary>
    /// Argument Executor for the "/?" Help command line argument.
    /// </summary>
    internal class HelpArgumentExecutor : IArgumentExecutor
    {
        #region Constructor

        /// <summary>
        /// Constructs the HelpArgumentExecutor
        /// </summary>
        public HelpArgumentExecutor()
        {
            this.Output = ConsoleOutput.Instance;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the output object
        /// </summary>
        internal IOutput Output { get; set; }

        #endregion

        #region IArgumentExecutor Members

        public void Initialize(string argument)
        {
        }

        public ArgumentProcessorResult Execute()
        {
            // Output the stock ouput text
            OutputSection(CommandLineResources.HelpUsageText);
            OutputSection(CommandLineResources.HelpDescriptionText);
            OutputSection(CommandLineResources.HelpArgumentsText);

            var argumentProcessorFactory = ArgumentProcessorFactory.Create();
            List<IArgumentProcessor> processors = new List<IArgumentProcessor>();
            processors.AddRange(argumentProcessorFactory.AllArgumentProcessors);
            processors.Sort((p1, p2) => Comparer<HelpContentPriority>.Default.Compare(p1.Metadata.Value.HelpPriority, p2.Metadata.Value.HelpPriority));

            // Output the help description for RunTestsArgumentProcessor
            IArgumentProcessor runTestsArgumentProcessor = processors.Find(p1 => p1.GetType() == typeof(RunTestsArgumentProcessor));
            processors.Remove(runTestsArgumentProcessor);
            var helpDescription = LookupHelpDescription(runTestsArgumentProcessor);
            if (helpDescription != null)
            {
                OutputSection(helpDescription);
            }

            // Output the help description for each available argument processor
            OutputSection(CommandLineResources.HelpOptionsText);
            foreach (var argumentProcessor in processors)
            {
                helpDescription = LookupHelpDescription(argumentProcessor);
                if (helpDescription != null)
                {
                    OutputSection(helpDescription);
                }
            }
            OutputSection(CommandLineResources.Examples);

            // When Help has finished abort any subsequent argument processor operations
            return ArgumentProcessorResult.Abort;
        }

        /// <inheritdoc />
        public bool LazyExecuteInDesignMode => false;
        #endregion

        #region Private Methods

        /// <summary>
        /// Lookup the help description for the argument procesor.
        /// </summary>
        /// <param name="argumentProcessor">The argument processor for which to discover any help content</param>
        /// <returns>The formatted string containing the help description if foundl null otherwise</returns>
        private string LookupHelpDescription(IArgumentProcessor argumentProcessor)
        {
            string result = null;

            if (argumentProcessor.Metadata.Value.HelpContentResourceName != null)
            {
                try
                {
                    result = string.Format(
                        CultureInfo.CurrentUICulture,
                        argumentProcessor.Metadata.Value.HelpContentResourceName);
                    //ResourceHelper.GetString(argumentProcessor.Metadata.HelpContentResourceName, assembly, CultureInfo.CurrentUICulture);
                }
                catch (Exception e)
                {
                    Output.Warning(false, e.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Output a section followed by an empty line.
        /// </summary>
        /// <param name="message">Message to output.</param>
        private void OutputSection(string message)
        {
            Output.WriteLine(message, OutputLevel.Information);
            Output.WriteLine(string.Empty, OutputLevel.Information);
        }

        #endregion
    }
}
