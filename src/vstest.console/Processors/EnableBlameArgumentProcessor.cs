﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using Microsoft.VisualStudio.TestPlatform.Common;
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

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Blame;

        public override string HelpContentResourceName => CommandLineResources.EnableBlameUsage;

        public override HelpContentPriority HelpPriority => HelpContentPriority.EnableBlameArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class EnableBlameArgumentExecutor : IArgumentExecutor
    {
        /// <summary>
        /// Blame logger and data collector friendly name
        /// </summary>
        private static string BlameFriendlyName = "Blame";

        /// <summary>
        /// Test logger manager instance
        /// </summary>
        private readonly TestLoggerManager loggerManager;

        /// <summary>
        /// Run settings manager
        /// </summary>
        private IRunSettingsProvider runSettingsManager;

        #region Constructor

        internal EnableBlameArgumentExecutor(IRunSettingsProvider runSettingsManager, TestLoggerManager loggerManager)
        {
            Contract.Requires(loggerManager != null);

            this.runSettingsManager = runSettingsManager;
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
            // Add Blame Logger
            this.loggerManager.UpdateLoggerList(BlameFriendlyName.ToLower(), BlameFriendlyName.ToLower(), null);

            // Add Blame Data Collector
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

            CollectArgumentExecutor.EnableDataCollectorUsingFriendlyName(BlameFriendlyName, dataCollectionRunSettings);

            this.runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);
        }

        /// <summary>
        /// Executes the argument processor.
        /// </summary>
        /// <returns>The <see cref="ArgumentProcessorResult"/>.</returns>
        public ArgumentProcessorResult Execute()
        {
            // Nothing to do since we updated the logger and data collector list in initialize
            return ArgumentProcessorResult.Success;
        }

        #endregion
    }
}
