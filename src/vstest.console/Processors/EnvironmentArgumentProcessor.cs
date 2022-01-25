﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using CommandLineResources = Resources.Resources;

    /// <summary>
    /// Argument Executor for the "-e|--Environment|/e|/Environment" command line argument.
    /// </summary>
    internal class EnvironmentArgumentProcessor : IArgumentProcessor
    {
        #region Constants
        /// <summary>
        /// The short name of the command line argument that the EnvironmentArgumentProcessor handles.
        /// </summary>
        public const string ShortCommandName = "/e";

        /// <summary>
        /// The name of the command line argument that the EnvironmentArgumentProcessor handles.
        /// </summary>
        public const string CommandName = "/Environment";
        #endregion

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (executor == null)
                {
                    executor = new Lazy<IArgumentExecutor>(
                        () => new ArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, ConsoleOutput.Instance)
                    );
                }

                return executor;
            }
            set
            {
                executor = value;
            }
        }

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (metadata == null)
                {
                    metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ArgumentProcessorCapabilities());
                }

                return metadata;
            }
        }

        internal class ArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
        {
            public override string CommandName => EnvironmentArgumentProcessor.CommandName;
            public override string ShortCommandName => EnvironmentArgumentProcessor.ShortCommandName;
            public override bool AllowMultiple => true;
            public override bool IsAction => false;
            public override string HelpContentResourceName => CommandLineResources.EnvironmentArgumentHelp;
            public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;
            public override HelpContentPriority HelpPriority => HelpContentPriority.EnvironmentArgumentProcessorHelpPriority;
        }

        internal class ArgumentExecutor : IArgumentExecutor
        {
            #region Fields
            /// <summary>
            /// Used when warning about overriden environment variables.
            /// </summary>
            private readonly IOutput output;

            /// <summary>
            /// Used when setting Environemnt variables.
            /// </summary>
            private readonly IRunSettingsProvider runSettingsProvider;

            /// <summary>
            /// Used when checking and forcing InIsolation mode.
            /// </summary>
            private readonly CommandLineOptions commandLineOptions;
            #endregion

            public ArgumentExecutor(CommandLineOptions commandLineOptions, IRunSettingsProvider runSettingsProvider, IOutput output)
            {
                this.commandLineOptions = commandLineOptions;
                this.output = output;
                this.runSettingsProvider = runSettingsProvider;
            }

            /// <summary>
            /// Set the environment variables in RunSettings.xml
            /// </summary>
            /// <param name="argument">
            /// Environment variable to set. 
            /// </param>
            public void Initialize(string argument)
            {
                Contract.Assert(!string.IsNullOrWhiteSpace(argument));
                Contract.Assert(output != null);
                Contract.Assert(commandLineOptions != null);
                Contract.Assert(!string.IsNullOrWhiteSpace(runSettingsProvider.ActiveRunSettings.SettingsXml));
                Contract.EndContractBlock();

                var key = argument;
                var value = string.Empty;

                if(key.Contains("="))
                {
                    value = key.Substring(key.IndexOf("=") + 1);
                    key = key.Substring(0, key.IndexOf("="));
                }

                var node = runSettingsProvider.QueryRunSettingsNode($"RunConfiguration.EnvironmentVariables.{key}");
                if(node != null)
                {
                    output.Warning(true, CommandLineResources.EnvironmentVariableXIsOverriden, key);
                }

                runSettingsProvider.UpdateRunSettingsNode($"RunConfiguration.EnvironmentVariables.{key}", value);
                
                if(!commandLineOptions.InIsolation) { 
                    commandLineOptions.InIsolation = true;
                    runSettingsProvider.UpdateRunSettingsNode(InIsolationArgumentExecutor.RunSettingsPath, "true");
                }
            }

            // Nothing to do here, the work was done in initialization.
            public ArgumentProcessorResult Execute() => ArgumentProcessorResult.Success;
        }
    }
}
