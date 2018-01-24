// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// The argument processor for runsettings passed as argument through cli
    /// </summary>
    internal class CLIRunSettingsArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the PortArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "--";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new CLIRunSettingsArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new CLIRunSettingsArgumentExecutor(RunSettingsManager.Instance, CommandLineOptions.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class CLIRunSettingsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => CLIRunSettingsArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.CLIRunSettings;

        public override string HelpContentResourceName => CommandLineResources.CLIRunSettingsArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.CLIRunSettingsArgumentProcessorHelpPriority;
    }

    internal class CLIRunSettingsArgumentExecutor : IArgumentsExecutor
    {
        private IRunSettingsProvider runSettingsManager;
        private CommandLineOptions commandLineOptions;

        internal CLIRunSettingsArgumentExecutor(IRunSettingsProvider runSettingsManager, CommandLineOptions commandLineOptions)
        {
            this.runSettingsManager = runSettingsManager;
            this.commandLineOptions = commandLineOptions;
        }

        public void Initialize(string argument)
        {
            throw new NotImplementedException();
        }

        public void Initialize(string[] arguments)
        {
            // if argument is null or doesn't contain any element, don't do anything.
            if (arguments == null || arguments.Length == 0)
            {
                return;
            }

            Contract.EndContractBlock();

            // Load up the run settings and set it as the active run settings.
            try
            {
                // Append / Override run settings supplied in CLI
                CreateOrOverwriteRunSettings(this.runSettingsManager, arguments);
            }
            catch (XPathException exception)
            {
                throw new CommandLineException(CommandLineResources.MalformedRunSettingsKey, exception);
            }
            catch (SettingsException exception)
            {
                throw new CommandLineException(exception.Message, exception);
            }
        }

        /// <inheritdoc />
        public bool LazyExecuteInDesignMode => false;

        public ArgumentProcessorResult Execute()
        {
            // Nothing to do here, the work was done in initialization.
            return ArgumentProcessorResult.Success;
        }

        private void CreateOrOverwriteRunSettings(IRunSettingsProvider runSettingsProvider, string[] args)
        {
            var length = args.Length;

            for (int index = 0; index < length; index++)
            {
                var keyValuePair = args[index];
                var indexOfSeparator = keyValuePair.IndexOf("=");
                if (indexOfSeparator <= 0 || indexOfSeparator >= keyValuePair.Length - 1)
                {
                    continue;
                }
                var key = keyValuePair.Substring(0, indexOfSeparator).Trim();
                var value = keyValuePair.Substring(indexOfSeparator + 1);

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                // To determine whether to infer framework and platform.
                UpdateFrameworkAndPlatform(key, value);

                runSettingsProvider.UpdateRunSettingsNode(key, value);
            }
        }

        private void UpdateFrameworkAndPlatform(string key, string value)
        {
            if (key.Equals(FrameworkArgumentExecutor.RunSettingsPath))
            {
                Framework framework = Framework.FromString(value);
                if (framework != null)
                {
                    this.commandLineOptions.TargetFrameworkVersion = framework;
                }
            }

            if (key.Equals(PlatformArgumentExecutor.RunSettingsPath))
            {
                bool success = Enum.TryParse<Architecture>(value, true, out var architecture);
                if (success)
                {
                    this.commandLineOptions.TargetArchitecture = architecture;
                }
            }
        }
    }
}
