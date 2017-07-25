// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using System.Collections.Generic;
    using System.Linq;
    using System.Globalization;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.TestPlatform.Extensions.BlameDataCollector;

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
                    this.executor = new Lazy<IArgumentExecutor>(() =>
                    new EnableBlameArgumentExecutor(
                        RunSettingsManager.Instance,
                        TestLoggerManager.Instance,
                        TestRequestManager.Instance,
                        CommandLineOptions.Instance,
                        new BlameModeTestHostLauncher()));
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

        public override bool IsAction => true;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => CommandLineResources.EnableBlameUsage;

        public override HelpContentPriority HelpPriority => HelpContentPriority.EnableDiagArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class EnableBlameArgumentExecutor : IArgumentExecutor
    {
        #region Private members

        /// <summary>
        /// Blame logger and data collector friendly name
        /// </summary>
        private static string BlameFriendlyName = "blame";

        /// <summary>
        /// Test logger manager instance
        /// </summary>
        private readonly TestLoggerManager loggerManager;

        /// <summary>
        /// Run settings manager
        /// </summary>
        private IRunSettingsProvider runSettingsManager;

        /// <summary>
        /// Test Request manager
        /// </summary>
        private ITestRequestManager testRequestManager;

        /// <summary>
        /// Command Line options
        /// </summary>
        private CommandLineOptions commandLineOptions;

        /// <summary>
        /// Is Dump Enabled
        /// </summary>
        private bool isDumpEnabled;

        /// <summary>
        /// Custom TestHostLauncher
        /// </summary>
        public ITestHostLauncher customTestHostLauncher;

        #endregion

        /// <summary>
        /// Ioutput
        /// </summary>
        internal IOutput output;

        #region Constructor

        internal EnableBlameArgumentExecutor(IRunSettingsProvider runSettingsManager, TestLoggerManager loggerManager, ITestRequestManager testRequestManager, CommandLineOptions commandLineOptions, ITestHostLauncher testHostLauncher)
        {
            Contract.Requires(loggerManager != null);

            this.runSettingsManager = runSettingsManager;
            this.loggerManager = loggerManager;
            this.commandLineOptions = commandLineOptions;
            this.customTestHostLauncher = testHostLauncher;
            this.testRequestManager = testRequestManager;
            this.output = ConsoleOutput.Instance;
        }

        #endregion

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            Contract.Assert(!string.IsNullOrWhiteSpace(this.runSettingsManager?.ActiveRunSettings?.SettingsXml));

            // Add Blame Logger
            if (argument != string.Empty)
            {
                if (argument.Equals("dump", StringComparison.OrdinalIgnoreCase))
                {
                    isDumpEnabled = true;
                    var parameters = new Dictionary<string, string>();
                    parameters["dump"] = string.Empty;
                    this.loggerManager.UpdateLoggerList(BlameFriendlyName, BlameFriendlyName, parameters);
                }
            }
            else
            {
                this.loggerManager.UpdateLoggerList(BlameFriendlyName, BlameFriendlyName, null);
            }

            // Add Blame Data Collector
            CollectArgumentExecutor.AddDataCollectorToRunSettings(BlameFriendlyName, this.runSettingsManager);

            // Get results directory from RunSettingsManager
            var runSettings = this.runSettingsManager.ActiveRunSettings;
            string resultsDirectory = null;
            if (runSettings != null)
            {
                try
                {
                    RunConfiguration runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runSettings.SettingsXml);
                    resultsDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfiguration);
                }
                catch (SettingsException se)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("EnableBlameArgumentProcessor: Unable to get the test results directory: Error {0}", se);
                    }
                }
            }

            // Add configuration element
            var settings = runSettings?.SettingsXml;
            if (settings == null)
            {
                runSettingsManager.AddDefaultRunSettings();
                settings = runSettings?.SettingsXml;
            }

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settings);
            if (dataCollectionRunSettings == null)
            {
                dataCollectionRunSettings = new DataCollectionRunSettings();
            }

            var XmlDocument = new XmlDocument();
            var outernode = XmlDocument.CreateElement("Configuration");
            var node = XmlDocument.CreateElement("ResultsDirectory");
            outernode.AppendChild(node);
            node.InnerText = resultsDirectory;

            foreach (var item in dataCollectionRunSettings.DataCollectorSettingsList)
            {
                if (item.FriendlyName.Equals(BlameFriendlyName))
                {
                    item.Configuration = outernode;
                }
            }

            runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);
        }

        /// <summary>
        /// Executes the argument processor.
        /// </summary>
        /// <returns>The <see cref="ArgumentProcessorResult"/>.</returns>
        public ArgumentProcessorResult Execute()
        {
            Contract.Assert(this.testRequestManager != null);
            if (this.commandLineOptions != null && this.commandLineOptions.IsDesignMode)
            {
                // Do not attempt execution in case of design mode. Expect execution to happen
                // via the design mode client.
                return ArgumentProcessorResult.Success;
            }
            // Ensure a test source file was provided
            var anySource = this.commandLineOptions.Sources.FirstOrDefault();
            if (anySource == null)
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, CommandLineResources.MissingTestSourceFile));

            }

            this.output.WriteLine(CommandLineResources.StartingExecution, OutputLevel.Information);
            if (!string.IsNullOrEmpty(EqtTrace.LogFile))
            {
                this.output.Information(CommandLineResources.VstestDiagLogOutputPath, EqtTrace.LogFile);
            }

            var success = true;
            if (this.commandLineOptions.Sources.Any())
            {

                success = this.RunTests(this.commandLineOptions.Sources);

            }
            return success ? ArgumentProcessorResult.Success : ArgumentProcessorResult.Fail;
        }

        private bool RunTests(IEnumerable<string> sources)
        {
            // create/start test run
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("EnableBlameArgumentProcessor:Execute: Test run is starting.");
            }

            var runSettings = this.runSettingsManager.ActiveRunSettings.SettingsXml;

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("EnableBlameArgumentProcessor:Execute: Queuing Test run.");
            }

            // for command line keep alive is always false.
            // for Windows Store apps it should be false, as Windows Store apps executor should terminate after finishing the test execution.
            var keepAlive = false;

            GenerateFakesUtilities.GenerateFakesSettings(this.commandLineOptions, this.commandLineOptions.Sources.ToList(), ref runSettings);

            var runRequestPayload = new TestRunRequestPayload() { Sources = this.commandLineOptions.Sources.ToList(), RunSettings = runSettings, KeepAlive = keepAlive };

            bool result;
            if (isDumpEnabled)
            {
                result = this.testRequestManager.RunTests(runRequestPayload, this.customTestHostLauncher, null, Constants.DefaultProtocolConfig);
            }
            else
            {
                result = this.testRequestManager.RunTests(runRequestPayload, null, null, Constants.DefaultProtocolConfig);
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("EnableBlameArgumentProcessor:Execute: Test run is completed.");
            }

            return result;
        }
    }
}

#endregion