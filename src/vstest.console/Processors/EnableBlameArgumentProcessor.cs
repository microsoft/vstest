// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using System.Globalization;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    internal class EnableBlameArgumentProcessor : IArgumentProcessor
    {
        /// <summary>
        /// The name of the command line argument that the ListTestsArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Blame";

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableBlameArgumentProcessor"/> class.
        /// </summary>
        public EnableBlameArgumentProcessor() 
        {
        }

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new EnableBlameArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableBlameArgumentExecutor(RunSettingsManager.Instance, TestLoggerManager.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    /// <summary>
    /// The argument capabilities.
    /// </summary>
    internal class EnableBlameArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => EnableBlameArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Diag;

        public override string HelpContentResourceName => CommandLineResources.EnableDiagUsage;

        public override HelpContentPriority HelpPriority => HelpContentPriority.EnableDiagArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class EnableBlameArgumentExecutor : IArgumentExecutor
    {

        #region Constructor

        private IRunSettingsProvider runSettingsManager;
        internal static List<string> EnabledDataCollectors = new List<string>();
        private readonly TestLoggerManager loggerManager;
        private static string BlameFriendlyName = "Blame";
        internal EnableBlameArgumentExecutor(IRunSettingsProvider runSettingsManager, TestLoggerManager loggerManager)
        {
            this.runSettingsManager = runSettingsManager;
            Contract.Requires(loggerManager != null);
            this.loggerManager = loggerManager;
        }
        #endregion

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            
            string loggerIdentifier = null;
            Dictionary<string, string> parameters = null;
            var parseSucceeded = LoggerUtilities.TryParseLoggerArgument(argument, out loggerIdentifier, out parameters);
            this.loggerManager.UpdateLoggerList(BlameFriendlyName.ToLower(), BlameFriendlyName.ToLower(), parameters);

            CollectArgumentExecutor.EnabledDataCollectors.Add(BlameFriendlyName.ToLower());

            var settings = this.runSettingsManager.ActiveRunSettings?.SettingsXml;
            if (settings == null)
            {
                this.runSettingsManager.AddDefaultRunSettings();
                settings = this.runSettingsManager.ActiveRunSettings?.SettingsXml;
            }

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settings);
            if (dataCollectionRunSettings == null)
            {
                dataCollectionRunSettings = new DataCollectionRunSettings();
            }

            CollectArgumentExecutor.EnableDataCollectorUsingFriendlyName(BlameFriendlyName.ToLower(), dataCollectionRunSettings);

            this.runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);
        }

        /// <summary>
        /// Executes the argument processor.
        /// </summary>
        /// <returns>The <see cref="ArgumentProcessorResult"/>.</returns>
        public ArgumentProcessorResult Execute()
        {
            // Nothing to do since we updated the parameter during initialize parameter
            return ArgumentProcessorResult.Success;
        }

        #endregion
    }
}
