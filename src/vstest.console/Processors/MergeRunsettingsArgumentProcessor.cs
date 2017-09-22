// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using System;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// An argument processor that merges runsettings from all sources like switches(/platform, /framework),
    /// runsettings file(/settings:path_to_runsettings_file), runsettings args(-- Runconfiguration.MaxCpuCount=1),
    /// auto generated(fakes settings) and auto detected (like platform and framework))
    ///
    /// This will avoid redundant code of runsettings updates in "action processor" like RunTestsArgumentProcessor, ListTestsArgumentProcessor,
    /// RunSpecificTestsArgumentProcessor and ListFullyQualifiedTestsArgumentProcessor
    /// and makes same runsettings used in all other processor(like EnableLoggerArgumentProcessor) which are initialize/execute before action processor.
    /// </summary>
    internal class MergeRunsettingsArgumentProcessor : IArgumentProcessor
    {
        /// <summary>
        /// The command name.
        /// </summary>
        public const string CommandName = "/MergeRunsettings";

        #region Fields

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        /// <summary>
        /// Gets or sets the executor.
        /// </summary>
        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (this.executor == null)
                {
                    this.executor = new Lazy<IArgumentExecutor>(() => new MergeRunsettingsArgumentExecutor(
                        CommandLineOptions.Instance,
                        RunSettingsManager.Instance,
                        ConsoleOutput.Instance,
                        InferHelper.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new MergeRunsettingsArgumentProcessorCapabilities());
                }

                return this.metadata;
            }
        }

        #endregion
    }

    internal class MergeRunsettingsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {

        // <summary>
        /// Gets the command name.
        /// </summary>
        public override string CommandName => MergeRunsettingsArgumentProcessor.CommandName;

        public override bool IsSpecialCommand => true;

        public override bool AlwaysExecute => true;

        /// <summary>
        /// Gets the priority.
        /// </summary>
        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.MergeRunsettings;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class MergeRunsettingsArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting tests to run.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        /// <summary>
        /// Used for sending discovery messages.
        /// </summary>
        internal IOutput output;

        /// <summary>
        /// Settings manager to get currently active settings.
        /// </summary>
        private IRunSettingsProvider runSettingsManager;

        /// <summary>
        /// To determine framework and platform.
        /// </summary>
        private IInferHelper inferHelper;


        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MergeRunsettingsArgumentExecutor(
            CommandLineOptions commandLineOptions,
            IRunSettingsProvider runSettingsProvider,
            IOutput output,
            IInferHelper inferHelper)
        {
            Contract.Requires(commandLineOptions != null);

            this.commandLineOptions = commandLineOptions;
            this.runSettingsManager = runSettingsProvider;
            this.output = output;
            this.inferHelper = inferHelper;
        }

        #endregion

        #region IArgumentProcessor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            // Updating framework and platform here.
            if (!commandLineOptions.ArchitectureSpecified)
            {
                var arch = inferHelper.AutoDetectArchitecture(commandLineOptions.Sources?.ToList());
                this.runSettingsManager?.UpdateRunSettingsNodeInnerXml(PlatformArgumentExecutor.RunSettingsPath,
                    arch.Equals(Architecture.Default) ? Constants.DefaultPlatform.ToString() : arch.ToString());
            }

            if (!commandLineOptions.FrameworkVersionSpecified)
            {
                var fx = inferHelper.AutoDetectFramework(commandLineOptions.Sources?.ToList());
                this.runSettingsManager?.UpdateRunSettingsNodeInnerXml(FrameworkArgumentExecutor.RunSettingsPath,
                    fx == null ? Framework.DefaultFramework.ToString() : fx.ToString());
            }
        }

        /// <summary>
        /// Execute.
        /// </summary>
        /// <returns>
        /// The <see cref="ArgumentProcessorResult"/>.
        /// </returns>
        public ArgumentProcessorResult Execute()
        {
            // Nothing to do since we enabled the logger in the initialize method.
            return ArgumentProcessorResult.Success;
        }

        #endregion
    }
}
