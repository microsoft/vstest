// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Globalization;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// An argument processor that allows the user to disable fakes
    /// from the command line using the --DisableAutoFakes|/DisableAutoFakes command line switch.
    /// </summary>
    internal class DisableAutoFakesArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        public const string CommandName = "/DisableAutoFakes";

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        #endregion

        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                return this.executor ?? (this.executor =
                                             new Lazy<IArgumentExecutor>(
                                                 () => new DisableAutoFakesArgumentExecutor(
                                                     CommandLineOptions.Instance)));
            }

            set => this.executor = value;
        }

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                return this.metadata ?? (this.metadata =
                                             new Lazy<IArgumentProcessorCapabilities>(
                                                 () => new DisableAutoFakesArgumentProcessorCapabilities()));
            }
        }
    }

    internal class DisableAutoFakesArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override bool AllowMultiple => false;
        public override string CommandName => DisableAutoFakesArgumentProcessor.CommandName;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override HelpContentPriority HelpPriority => HelpContentPriority.DisableAutoFakesArgumentProcessorHelpPriority;
    }

    internal class DisableAutoFakesArgumentExecutor : IArgumentExecutor
    {
        private readonly CommandLineOptions commandLineOptions;

        #region Constructors
        public DisableAutoFakesArgumentExecutor(CommandLineOptions commandLineOptions)
        {
            this.commandLineOptions = commandLineOptions;
        }
        #endregion

        #region IArgumentProcessor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.UseVsixExtensionsValueRequired, argument));
            }

            bool value;
            if (!bool.TryParse(argument, out value))
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidUseVsixExtensionsCommand, argument));
            }

            this.commandLineOptions.DisableAutoFakes = value;
        }

        /// <summary>
        /// Execute.
        /// </summary>
        /// <returns>
        /// The <see cref="ArgumentProcessorResult"/>.
        /// </returns>
        public ArgumentProcessorResult Execute()
        {
            // Nothing to do since we updated the parameter during initialize parameter
            return ArgumentProcessorResult.Success;
        }

        #endregion
    }
}