// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Allows the user to specify a order of loading custom adapters from.
    /// </summary>
    internal class TestAdapterLoadingStrategyArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the TestAdapterLoadingStrategyArgumentProcessor handles.
        /// </summary>
        public const string CommandName = "/TestAdapterLoadingStrategy";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new TestAdapterLoadingStrategyArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new TestAdapterLoadingStrategyArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, ConsoleOutput.Instance, new FileHelper()));
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
    internal class TestAdapterLoadingStrategyArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => TestAdapterLoadingStrategyArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override bool AlwaysExecute => true;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.TestAdapterLoadingStrategy;

        public override string HelpContentResourceName => CommandLineResources.TestAdapterLoadingStrategyHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.TestAdapterLoadingStrategyArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class TestAdapterLoadingStrategyArgumentExecutor : IArgumentExecutor
    {
        #region Fields
        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        /// <summary>
        /// Run settings provider.
        /// </summary>
        private IRunSettingsProvider runSettingsManager;

        /// <summary>
        /// Used for sending output.
        /// </summary>
        private IOutput output;

        /// <summary>
        /// For file related operation
        /// </summary>
        private IFileHelper fileHelper;

        #endregion

        public const string DefaultStrategy = "Default";
        public const string ExplicitStrategy = "Explicit";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options"> The options. </param>
        /// <param name="testPlatform">The test platform</param>
        public TestAdapterLoadingStrategyArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager, IOutput output, IFileHelper fileHelper)
        {
            Contract.Requires(options != null);

            this.commandLineOptions = options;
            this.runSettingsManager = runSettingsManager;
            this.output = output;
            this.fileHelper = fileHelper;
        }

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            var strategy = TestAdapterLoadingStrategy.Default;

            if (!string.IsNullOrEmpty(argument) && !Enum.TryParse(argument, out strategy))
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestAdapterLoadingStrategyValueInvalid, argument));
            }

            if (strategy == TestAdapterLoadingStrategy.Recursive) {
                throw new CommandLineException(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestAdapterLoadingStrategyValueInvalidRecursive, $"{nameof(TestAdapterLoadingStrategy.Explicit)}, {nameof(TestAdapterLoadingStrategy.NextToSource)}"));
            }

            if (string.IsNullOrWhiteSpace(argument))
            {
                InitializeDefaultStrategy();
                return;
            }

            InitializeStrategy(strategy);
        }

        private void InitializeDefaultStrategy()
        {
            ValidateTestAdapterPaths(TestAdapterLoadingStrategy.Default);

            SetStrategy(TestAdapterLoadingStrategy.Default);
        }

        private void InitializeStrategy(TestAdapterLoadingStrategy strategy)
        {
            ValidateTestAdapterPaths(strategy);

            if (!commandLineOptions.TestAdapterPathsSet && (strategy & TestAdapterLoadingStrategy.Explicit) == TestAdapterLoadingStrategy.Explicit)
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestAdapterPathValueRequiredWhenStrategyXIsUsed, ExplicitStrategy));
            }

            SetStrategy(strategy);
        }

        private void ForceIsolation()
        {
            if (this.commandLineOptions.InIsolation)
            {
                return;
            }

            this.commandLineOptions.InIsolation = true;
            this.runSettingsManager.UpdateRunSettingsNode(InIsolationArgumentExecutor.RunSettingsPath, "true");
        }

        private void ValidateTestAdapterPaths(TestAdapterLoadingStrategy strategy)
        {
            var testAdapterPaths = commandLineOptions.TestAdapterPath ?? new string[0];
            if (!commandLineOptions.TestAdapterPathsSet)
            {
                testAdapterPaths = TestAdapterPathArgumentExecutor.SplitPaths(this.runSettingsManager.QueryRunSettingsNode("RunConfiguration.TestAdaptersPaths")).Union(testAdapterPaths).Distinct().ToArray();
            }

            for (var i = 0; i < testAdapterPaths.Length; i++)
            {
                var adapterPath = testAdapterPaths[i];
                var testAdapterPath = this.fileHelper.GetFullPath(Environment.ExpandEnvironmentVariables(adapterPath));

                if (strategy == TestAdapterLoadingStrategy.Default)
                {
                    if (!this.fileHelper.DirectoryExists(testAdapterPath))
                    {
                        throw new CommandLineException(
                            string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidTestAdapterPathCommand, adapterPath, CommandLineResources.TestAdapterPathDoesNotExist)
                        );
                    }
                }

                testAdapterPaths[i] = testAdapterPath;
            }

            this.runSettingsManager.UpdateRunSettingsNode("RunConfiguration.TestAdaptersPaths", string.Join(";", testAdapterPaths));
        }

        private void SetStrategy(TestAdapterLoadingStrategy strategy)
        {
            var adapterStrategy = strategy.ToString();

            commandLineOptions.TestAdapterLoadingStrategy = strategy;
            this.runSettingsManager.UpdateRunSettingsNode("RunConfiguration.TestAdapterLoadingStrategy", adapterStrategy);
            if ((strategy & TestAdapterLoadingStrategy.Explicit) == TestAdapterLoadingStrategy.Explicit)
            {
                ForceIsolation();
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

        #endregion
    }
}