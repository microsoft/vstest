// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors

{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;


    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using System.Xml.Linq;


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
                if (this.executor == null)
                {
                    this.executor = new Lazy<IArgumentExecutor>(
                        () => new ArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, ConsoleOutput.Instance)
                    );
                }

                return this.executor;
            }
            set
            {
                this.executor = value;
            }
        }

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ArgumentProcessorCapabilities());
                }

                return this.metadata;
            }
        }

        internal class ArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
        {
            public override string CommandName => EnvironmentArgumentProcessor.CommandName;

            public override string ShortCommandName => EnvironmentArgumentProcessor.ShortCommandName;

            public override bool AllowMultiple => true;

            public override bool IsAction => false;

            public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;
            public override string HelpContentResourceName => CommandLineResources.EnvironmentArgumentHelp;

            public override HelpContentPriority HelpPriority => HelpContentPriority.EnvironmentArgumentProcessorHelpPriority;
        }

        internal class ArgumentExecutor : IArgumentExecutor
        {
            #region Fields
            /// <summary>
            /// Used when warning about overriden environment variables.
            /// </summary>
            private IOutput output;

            /// <summary>
            /// Used when setting Environemnt variables.
            /// </summary>
            private IRunSettingsProvider runSettingsProvider;

            private CommandLineOptions commandLineOptions;
            #endregion

            public ArgumentExecutor(
                CommandLineOptions commandLineOptions,
                IRunSettingsProvider runSettingsProvider,
                IOutput output)
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
                Contract.Assert(this.output != null);
                Contract.Assert(this.commandLineOptions != null);
                Contract.Assert(!string.IsNullOrWhiteSpace(this.runSettingsProvider.ActiveRunSettings.SettingsXml));
                Contract.EndContractBlock();

                var key = argument;
                var value = string.Empty;

                if(key.Contains("="))
                {
                    value = key.Substring(key.IndexOf("=") + 1);
                    key = key.Substring(0, key.IndexOf("="));
                }
                              
                var xml = this.runSettingsProvider.GetRunSettingXmlDocument();
                var runSettings = CreateOrSelectSingleNode(xml, "RunSettings");
                var runConfiguration = CreateOrSelectSingleNode(runSettings, "RunConfiguration");
                var environmentVariables = CreateOrSelectSingleNode(runConfiguration, "EnvironmentVariables");

                var variable = environmentVariables.SelectSingleNode(key);
                if(variable == null)
                {
                    variable = CreateOrSelectSingleNode(environmentVariables, key);
                }
                else
                {
                    output.Warning(true, CommandLineResources.EnvironmentVariableXIsOverriden, key);
                }

                variable.InnerText = value;

                this.runSettingsProvider.UpdateRunSettings(xml.OuterXml);

                if(!this.commandLineOptions.InIsolation) { 
                    this.commandLineOptions.InIsolation = true;
                    this.runSettingsProvider.UpdateRunSettingsNode(InIsolationArgumentExecutor.RunSettingsPath, "true");
                }
            }

            // Nothing to do here, the work was done in initialization.
            public ArgumentProcessorResult Execute() => ArgumentProcessorResult.Success;

            private XmlNode CreateOrSelectSingleNode(XmlNode parent, string name)
            {
                var node = parent.SelectSingleNode(name);
                if(node == null)
                {
                    var element = parent.OwnerDocument.CreateElement(name);
                    node = parent.AppendChild(element);
                }

                return node;
            }
        }
    }
}
