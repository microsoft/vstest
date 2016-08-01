// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics.Contracts;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Resources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;

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
                        TestRequestManager.Instance));
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

        public override string HelpContentResourceName => Resources.RunTestsArgumentHelp;

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
            ITestRequestManager testRequestManager)
        {
            Contract.Requires(commandLineOptions != null);

            this.commandLineOptions = commandLineOptions;

            this.output = ConsoleOutput.Instance;

            this.runSettingsManager = RunSettingsManager.Instance;
            this.testRequestManager = testRequestManager;
            this.testRunEventsRegistrar = new TestRunRequestEventsRegistrar();
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

            // Ensure a test source file was provided
            var anySource = this.commandLineOptions.Sources.FirstOrDefault();
            if (anySource == null)
            {
#if TODO
                logger.SendMessage(TestMessageLevel.Error, Resources.MissingTestSourceFile);
#endif
                return ArgumentProcessorResult.Fail;
            }

            this.output.WriteLine(Resources.StartingExecution, OutputLevel.Information);

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

            var runSettings = RunSettingsUtilities.GetRunSettings(this.runSettingsManager, this.commandLineOptions);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("RunTestsArgumentProcessor:Execute: Queuing Test run.");
            }

            // for command line keep alive is always false.
            // for Windows Store apps it should be false, as Windows Store apps executor should terminate after finishing the test execution.
            var keepAlive = false;
            
            var runRequestPayload = new TestRunRequestPayload() { Sources = this.commandLineOptions.Sources.ToList(), RunSettings = runSettings, KeepAlive = keepAlive };
            var result = this.testRequestManager.RunTests(runRequestPayload, null, this.testRunEventsRegistrar);

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("RunTestsArgumentProcessor:Execute: Test run is completed.");
            }

            return result;
        }

        private class TestRunRequestEventsRegistrar : ITestRunEventsRegistrar
        {
            /// <summary>
            /// Specifies whether some tests were found in the test run or not. 
            /// </summary>
            protected bool? testsFoundInAnySource = null;

            public void RegisterTestRunEvents(ITestRunRequest testRunRequest)
            {
                testRunRequest.OnRunCompletion += TestRunRequest_OnRunCompletion;
            }

            public void UnregisterTestRunEvents(ITestRunRequest testRunRequest)
            {
                testRunRequest.OnRunCompletion -= TestRunRequest_OnRunCompletion;
                // reset
                this.testsFoundInAnySource = null;
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
                    this.testsFoundInAnySource = (e.TestRunStatistics == null) ? false : (e.TestRunStatistics.ExecutedTests > 0);

                    // TODO: We need to show a message to check for vsix extensions if no tests are executed
                    // Indicate the user to use vsix extensions command if there are no tests found
                    //if (Utilities.ShouldIndicateTheUserToUseVsixExtensionsCommand(testsFoundInAnySource, commandLineOptions))
                    //{
                    //    output.Information(Resources.SuggestUseVsixExtensionsIfNoTestsIsFound);
                    //    output.WriteLine(string.Empty, OutputLevel.Information);
                    //}
                }
            }
        }
    }
}
