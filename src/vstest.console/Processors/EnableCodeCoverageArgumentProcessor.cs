// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// The argument processor for enabling data collectors.
    /// </summary>
    internal class EnableCodeCoverageArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of command for enabling code coverage.
        /// </summary>
        public const string CommandName = "/EnableCodeCoverage";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new EnableCodeCoverageArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableCodeCoverageArgumentExecutor(RunSettingsManager.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    /// <inheritdoc />
    internal class EnableCodeCoverageArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => EnableCodeCoverageArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        //public override string HelpContentResourceName => CommandLineResources.EnableCodeCoverageArgumentProcessorHelp;

        //public override HelpContentPriority HelpPriority => HelpContentPriority.EnableCodeCoverageArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The enable code coverage argument executor.
    /// </summary>
    internal class EnableCodeCoverageArgumentExecutor : IArgumentExecutor
    {
        private IRunSettingsProvider runSettingsManager;

        private const string FriendlyName = "Code Coverage";
        private const string UriString = "datacollector://Microsoft/CodeCoverage/2.0";

        internal EnableCodeCoverageArgumentExecutor(IRunSettingsProvider runSettingsManager)
        {
            this.runSettingsManager = runSettingsManager;
        }

        /// <inheritdoc />
        public void Initialize(string argument)
        {
            // Add this enabled data collectors list, this will ensure Code Coverage isn't disabled when other DCs are configured using /Collect.
            CollectArgumentExecutor.AddDataCollectorToRunSettings(FriendlyName, this.runSettingsManager, UriString);
        }

        /// <inheritdoc />
        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }
    }
}
