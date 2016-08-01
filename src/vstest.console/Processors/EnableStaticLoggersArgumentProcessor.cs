// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An argument processor that enables all configured loggers.
    /// </summary>
    internal class EnableStaticLoggersArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The command name.
        /// </summary>
        public const string CommandName = "/EnableStaticLoggers";

        #endregion

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
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableStaticLoggersArgumentExecutor());
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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new EnableStaticLoggersArgumentProcessorCapabilities());
                }

                return this.metadata;
            }
        }
    }

    /// <summary>
    /// The argument capabilities.
    /// </summary>
    internal class EnableStaticLoggersArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        /// <summary>
        /// Gets the command name.
        /// </summary>
        public override string CommandName => EnableStaticLoggersArgumentProcessor.CommandName;

        /// <summary>
        /// Gets a value indicating whether allow multiple.
        /// </summary>
        public override bool AllowMultiple => false;

        /// <summary>
        /// Gets a value indicating whether is action.
        /// </summary>
        public override bool IsAction => false;

        /// <summary>
        /// Gets the priority.
        /// </summary>
        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Logging;
    }

    /// <summary>
    /// The argument e xecutor.
    /// </summary>
    internal class EnableStaticLoggersArgumentExecutor : IArgumentExecutor
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public EnableStaticLoggersArgumentExecutor()
        {
        }

        #endregion

        #region IArgumentProcessor

        /// <summary>
        /// Enables the configured loggers.
        /// </summary>
        /// <param name="argument">The argument used to initialize the processor.</param>
        /// <remarks><paramref name="argument"/> is not used.</remarks>
        public void Initialize(string argument)
        {
            var logManager = TestLoggerManager.Instance;
#if NET451
            foreach (var logger in TestPlatFormSettings.Loggers.Cast<LoggerElement>())
            {
                string loggerIdentifier = null;
                Dictionary<string, string> parameters = null;
                bool parseSucceeded = LoggerUtilities.TryParseLoggerArgument(logger.Uri, out loggerIdentifier, out parameters);

                if (parseSucceeded)
                {
                    Uri loggerUri = null;
                    try
                    {
                        loggerUri = new Uri(loggerIdentifier);
                    }
                    catch (UriFormatException)
                    {
                        Logger.SendMessage(
                            TestMessageLevel.Error,
                            String.Format(
                                CultureInfo.CurrentUICulture,
                                Resources.LoggerUriInvalid,
                                logger.Uri));
                    }

                    if (loggerUri != null)
                    {
                        try
                        {
                            logManager.AddLogger(loggerUri, null);
                        }
                        catch (InvalidOperationException e)
                        {
                            Logger.SendMessage(
                                TestMessageLevel.Error,
                                e.Message);
                        }
                    }
                }
                else
                {
                    Logger.SendMessage(
                           TestMessageLevel.Error,
                           String.Format(
                               CultureInfo.CurrentUICulture,
                               Resources.LoggerUriInvalid,
                               logger.Uri));
                }
            }
#else
            //// todo : write code after getting clarity on config file format in dotnet core
#endif

        }

        public ArgumentProcessorResult Execute()
        {
            // Nothing to do.
            return ArgumentProcessorResult.Success;
        }

        #endregion

        #region Private Methods

#if NET451
        /// <summary>
        /// The settings for the test platform.
        /// </summary>
        private static TestPlatformSection TestPlatFormSettings
        {
            get
            {
                var section = ConfigurationManager.GetSection(TestPlatformSection.SectionName) as TestPlatformSection;

                if (section == null)
                {
                    section = new TestPlatformSection();
                }

                return section;
            }
        }
#endif
        #endregion

    }
}
