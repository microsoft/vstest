// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// An argument processor that allows the user to enable a specific logger
    /// from the command line using the --Logger|/Logger command line switch.
    /// </summary>
    internal class EnableLoggerArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The command name.
        /// </summary>
        public const string CommandName = "/Logger";

        #endregion

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
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableLoggerArgumentExecutor(RunSettingsManager.Instance));
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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new EnableLoggerArgumentProcessorCapabilities());
                }

                return this.metadata;
            }
        }

        #endregion
    }

    internal class EnableLoggerArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        /// <summary>
        /// Gets the command name.
        /// </summary>
        public override string CommandName => EnableLoggerArgumentProcessor.CommandName;

        /// <summary>
        /// Gets a value indicating whether allow multiple.
        /// </summary>
        public override bool AllowMultiple => true;

        /// <summary>
        /// Gets a value indicating whether is action.
        /// </summary>
        public override bool IsAction => false;

        /// <summary>
        /// Gets the priority.
        /// </summary>
        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Logging;

        /// <summary>
        /// Gets the help content resource name.
        /// </summary>
#if NET451
        public override string HelpContentResourceName => CommandLineResources.EnableLoggersArgumentHelp;
#else
        public override string HelpContentResourceName => CommandLineResources.EnableLoggerArgumentsInNetCore;
#endif

        /// <summary>
        /// Gets the help priority.
        /// </summary>
        public override HelpContentPriority HelpPriority => HelpContentPriority.EnableLoggerArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class EnableLoggerArgumentExecutor : IArgumentExecutor
    {
        private readonly IRunSettingsProvider runSettingsManager;

#region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableLoggerArgumentExecutor"/> class.
        /// </summary>
        public EnableLoggerArgumentExecutor(IRunSettingsProvider runSettingsManager)
        {
            Contract.Requires(runSettingsManager != null);
            this.runSettingsManager = runSettingsManager;
        }

#endregion

#region IArgumentProcessor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            AddLoggerToRunSettings(argument, runSettingsManager);
        }

        /// <summary>
        /// Add logger to runsettings.
        /// </summary>
        /// <param name="loggerArgument"></param>
        /// <param name="runSettingsManager"></param>
        public static void AddLoggerToRunSettings(string loggerArgument, IRunSettingsProvider runSettingsManager)
        {
            if (string.IsNullOrWhiteSpace(loggerArgument))
            {
                HandleInvalidArgument(loggerArgument);
            }

            var settings = runSettingsManager.ActiveRunSettings?.SettingsXml;
            if (settings == null)
            {
                runSettingsManager.AddDefaultRunSettings();
                settings = runSettingsManager.ActiveRunSettings?.SettingsXml;
            }

            var loggerRunSettings = XmlRunSettingsUtilities.GetLoggerRunSettings(settings) ?? new LoggerRunSettings();
            string loggerIdentifier = null;
            Dictionary<string, string> parameters = null;
            var parseSucceeded = LoggerUtilities.TryParseLoggerArgument(loggerArgument, out loggerIdentifier, out parameters);

            if (parseSucceeded)
            {
                var logger = default(LoggerSettings);

                try
                {
                    // Logger as uri in command line.
                    var loggerUri = new Uri(loggerIdentifier);
                    logger = new LoggerSettings
                    {
                        Uri = loggerUri,
                        IsEnabled = true
                    };
                }
                catch (UriFormatException)
                {
                    // Logger as friendlyName in command line.
                    logger = new LoggerSettings
                    {
                        FriendlyName = loggerIdentifier,
                        IsEnabled = true
                    };
                }

                // Converting logger console params to Configuration element
                if (parameters != null && parameters.Count > 0)
                {
                    var XmlDocument = new XmlDocument();
                    var outerNode = XmlDocument.CreateElement("Configuration");
                    foreach (KeyValuePair<string, string> entry in parameters)
                    {
                        var node = XmlDocument.CreateElement(entry.Key);
                        node.InnerText = entry.Value;
                        outerNode.AppendChild(node);
                    }

                    logger.Configuration = outerNode;
                }

                // Remove existing logger.
                var existingLoggerIndex = loggerRunSettings.GetExistingLoggerIndex(logger);
                if (existingLoggerIndex >= 0)
                {
                    loggerRunSettings.LoggerSettingsList.RemoveAt(existingLoggerIndex);
                }

                loggerRunSettings.LoggerSettingsList.Add(logger);
            }
            else
            {
                HandleInvalidArgument(loggerArgument);
            }

            runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.LoggerRunSettingsName, loggerRunSettings.ToXml().InnerXml);
        }

        /// <summary>
        /// Throws an exception indicating that the argument is invalid.
        /// </summary>
        /// <param name="argument">Argument which is invalid.</param>
        private static void HandleInvalidArgument(string argument)
        {
            throw new CommandLineException(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    CommandLineResources.LoggerUriInvalid,
                    argument));
        }

        /// <summary>
        /// Execute logger argument.
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
