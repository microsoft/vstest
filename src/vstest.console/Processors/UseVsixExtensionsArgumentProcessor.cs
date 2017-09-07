// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    
    using System.Globalization;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The argument processor for initializing the vsix based adapters.
    /// </summary>
    internal class UseVsixExtensionsArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of command
        /// </summary>
        public const string CommandName = "/UseVsixExtensions";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new UseVsixExtensionsArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new UseVsixExtensionsArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance, new VSExtensionManager(), ConsoleOutput.Instance));
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
    internal class UseVsixExtensionsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => UseVsixExtensionsArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

        // Commenting out the help for this processor as it on the deprecation path.
        // public override string HelpContentResourceName => CommandLineResources.UseVsixExtensionsHelp;
        // public override HelpContentPriority HelpPriority => HelpContentPriority.UseVsixArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The use vsix extensions argument executor.
    /// </summary>
    internal class UseVsixExtensionsArgumentExecutor : IArgumentExecutor
    {
        private CommandLineOptions commandLineOptions;
        private ITestRequestManager testRequestManager;
        private IVSExtensionManager extensionManager;
        private IOutput output;

        internal UseVsixExtensionsArgumentExecutor(CommandLineOptions commandLineOptions, ITestRequestManager testRequestManager, IVSExtensionManager extensionManager, IOutput output)
        {
            this.commandLineOptions = commandLineOptions;
            this.testRequestManager = testRequestManager;
            this.extensionManager = extensionManager;
            this.output = output;
        }

        /// <inheritdoc />
        public void Initialize(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentCulture, CommandLineResources.UseVsixExtensionsValueRequired));
            }

            bool value;
            if (!bool.TryParse(argument, out value))
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidUseVsixExtensionsCommand, argument));
            }

            this.output.Warning(false, string.Format(CultureInfo.CurrentCulture, CommandLineResources.UseVsixExtensionsDeprecation));
            commandLineOptions.UseVsixExtensions = value;

            if (commandLineOptions.UseVsixExtensions)
            {
                var vsixExtensions = extensionManager.GetUnitTestExtensions();
                testRequestManager.InitializeExtensions(vsixExtensions);
            }
        }

        /// <inheritdoc />
        public ArgumentProcessorResult Execute()
        {
            return ArgumentProcessorResult.Success;
        }
    }
}
