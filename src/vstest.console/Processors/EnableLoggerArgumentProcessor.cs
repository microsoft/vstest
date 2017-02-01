// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;

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
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableLoggerArgumentExecutor(TestLoggerManager.Instance));
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
        public override string HelpContentResourceName => CommandLineResources.EnableLoggersArgumentHelp;

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
        #region Fields

        private readonly TestLoggerManager loggerManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableLoggerArgumentExecutor"/> class.
        /// </summary>
        /// <param name="loggerManager">
        /// The logger manager.
        /// </param>
        public EnableLoggerArgumentExecutor(TestLoggerManager loggerManager)
        {
            Contract.Requires(loggerManager != null);
            this.loggerManager = loggerManager;
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
                HandleInvalidArgument(argument);
            }
            else
            {
                string loggerIdentifier = null;
                Dictionary<string, string> parameters = null;
                var parseSucceeded = LoggerUtilities.TryParseLoggerArgument(argument, out loggerIdentifier, out parameters);

                if (parseSucceeded)
                {
                    if (loggerIdentifier.Equals(ConsoleLogger.FriendlyName, StringComparison.OrdinalIgnoreCase))
                    {
                        this.loggerManager.AddLogger(new ConsoleLogger(), ConsoleLogger.ExtensionUri, parameters);
                    }
                    else
                    {
                        // First assume the logger is specified by URI. If that fails try with friendly name.
                        try
                        {
                            this.AddLoggerByUri(loggerIdentifier, parameters);
                        }
                        catch (CommandLineException)
                        {
                            string loggerUri;
                            if (this.loggerManager.TryGetUriFromFriendlyName(loggerIdentifier, out loggerUri))
                            {
                                this.AddLoggerByUri(loggerUri, parameters);
                            }
                            else
                            {
                                throw new CommandLineException(
                                String.Format(
                                CultureInfo.CurrentUICulture,
                                CommandLineResources.LoggerNotFound,
                                argument));
                            }
                        }
                    }
                }
                else
                {
                    HandleInvalidArgument(argument);
                }
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

        #region Private Methods

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

        private void AddLoggerByUri(string argument, Dictionary<string, string> parameters)
        {
            // Get the uri and if it is not valid, throw.
            Uri loggerUri = null;
            try
            {
                loggerUri = new Uri(argument);
            }
            catch (UriFormatException)
            {
                HandleInvalidArgument(argument);
            }

            // Add the logger and if it is a non-existent logger, throw.
            try
            {
                this.loggerManager.AddLogger(loggerUri, parameters);
            }
            catch (InvalidOperationException e)
            {
                throw new CommandLineException(e.Message, e);
            }
        }

        #endregion
    }
}
