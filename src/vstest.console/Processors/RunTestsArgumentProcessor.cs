// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    internal class RunTestsArgumentProcessor : IArgumentProcessor
    {
        public const string CommandName = "/RunTests";

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new RunTestsArgumentProcessorCapabilities());
                }
                return this.metadata;
            }
        }

        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (this.executor == null)
                {
                    this.executor = new Lazy<IArgumentExecutor>(() =>
                    new RunTestsArgumentExecutor(
                        CommandLineOptions.Instance,
                        RunSettingsManager.Instance,
                        TestRequestManager.Instance,
                        ConsoleOutput.Instance));
                }

                return this.executor;
            }
            set
            {
                this.executor = value;
            }
        }
    }

    internal class RunTestsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => RunTestsArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => true;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => CommandLineResources.RunTestsArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.RunTestsArgumentProcessorHelpPriority;

        public override bool IsSpecialCommand => true;

        public override bool AlwaysExecute => false;
    }

    internal class RunTestsArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting tests to run.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        /// <summary>
        /// The instance of testPlatforms
        /// </summary>
        private ITestRequestManager testRequestManager;

        /// <summary>
        /// Used for sending discovery messages.
        /// </summary>
        internal IOutput output;

        /// <summary>
        /// Settings manager to get currently active settings.
        /// </summary>
        private IRunSettingsProvider runSettingsManager;

        /// <summary>
        /// Registers and Unregisters for test run events before and after test run
        /// </summary>
        private ITestRunEventsRegistrar testRunEventsRegistrar;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RunTestsArgumentExecutor(
            CommandLineOptions commandLineOptions,
            IRunSettingsProvider runSettingsProvider,
            ITestRequestManager testRequestManager,
            IOutput output)
        {
            Contract.Requires(commandLineOptions != null);

            this.commandLineOptions = commandLineOptions;
            this.runSettingsManager = runSettingsProvider;
            this.testRequestManager = testRequestManager;
            this.output = output;
            this.testRunEventsRegistrar = new TestRunRequestEventsRegistrar(this.output);
        }

        #endregion

        public void Initialize(string argument)
        {
            // Nothing to do.
        }

        /// <summary>
        /// Execute all of the tests.
        /// </summary>
        public ArgumentProcessorResult Execute()
        {
            Contract.Assert(this.commandLineOptions != null);
            Contract.Assert(!string.IsNullOrWhiteSpace(this.runSettingsManager?.ActiveRunSettings?.SettingsXml));

            if (this.commandLineOptions.IsDesignMode)
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
                this.output.Information(false, CommandLineResources.VstestDiagLogOutputPath, EqtTrace.LogFile);
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
                EqtTrace.Info("RunTestsArgumentProcessor:Execute: Test run is starting.");
            }

            var runSettings = this.runSettingsManager.ActiveRunSettings.SettingsXml;

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("RunTestsArgumentProcessor:Execute: Queuing Test run.");
            }

            // for command line keep alive is always false.
            // for Windows Store apps it should be false, as Windows Store apps executor should terminate after finishing the test execution.
            var keepAlive = false;

            var runRequestPayload = new TestRunRequestPayload() { Sources = this.commandLineOptions.Sources.ToList(), RunSettings = runSettings, KeepAlive = keepAlive, TestPlatformOptions= new TestPlatformOptions() { TestCaseFilter = this.commandLineOptions.TestCaseFilterValue } };
            var result = this.testRequestManager.RunTests(runRequestPayload, null, this.testRunEventsRegistrar, Constants.DefaultProtocolConfig);

            if (EqtTrace.IsInfoEnabled)
            {
                // TODO: add result also here.
                EqtTrace.Info("RunTestsArgumentProcessor:Execute: Test run is completed.");
            }

            return result;
        }

        private class TestRunRequestEventsRegistrar : ITestRunEventsRegistrar
        {
            private IOutput output;

            public TestRunRequestEventsRegistrar(IOutput output)
            {
                this.output = output;
            }

            public void RegisterTestRunEvents(ITestRunRequest testRunRequest)
            {
                testRunRequest.OnRunCompletion += TestRunRequest_OnRunCompletion;
            }

            public void UnregisterTestRunEvents(ITestRunRequest testRunRequest)
            {
                testRunRequest.OnRunCompletion -= TestRunRequest_OnRunCompletion;
            }

            /// <summary>
            /// Handles the TestRunRequest complete event
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e">RunCompletion args</param>
            private void TestRunRequest_OnRunCompletion(object sender, TestRunCompleteEventArgs e)
            {
                // If run is not aborted/cancelled then check the count of executed tests.
                // we need to check if there are any tests executed - to try show some help info to user to check for installed vsix extensions
                if (!e.IsAborted && !e.IsCanceled)
                {
                    var testsFoundInAnySource = (e.TestRunStatistics == null) ? false : (e.TestRunStatistics.ExecutedTests > 0);

                    // Indicate the user to use testadapterpath command if there are no tests found
                    if (!testsFoundInAnySource && string.IsNullOrEmpty(CommandLineOptions.Instance.TestAdapterPath))
                    {
                        this.output.Warning(false, CommandLineResources.SuggestTestAdapterPathIfNoTestsIsFound);
                    }
                }
            }
        }
    }
}
