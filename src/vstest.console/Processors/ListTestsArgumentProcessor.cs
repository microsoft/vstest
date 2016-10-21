// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Argument Executor for the "-lt|--ListTests|/lt|/ListTests" command line argument.
    /// </summary>
    internal class ListTestsArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The short name of the command line argument that the ListTestsArgumentExecutor handles.
        /// </summary>
        public const string ShortCommandName = "/lt";

        /// <summary>
        /// The name of the command line argument that the ListTestsArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/ListTests";

        #endregion

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ListTestsArgumentProcessorCapabilities());
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
                    this.executor =
                        new Lazy<IArgumentExecutor>(
                            () =>
                            new ListTestsArgumentExecutor(
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

    internal class ListTestsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => ListTestsArgumentProcessor.CommandName;

        public override string ShortCommandName => ListTestsArgumentProcessor.ShortCommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => true; 

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => CommandLineResources.ListTestsHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.ListTestsArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// Argument Executor for the "/ListTests" command line argument.
    /// </summary>
    internal class ListTestsArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        /// <summary>
        /// Used for getting tests.
        /// </summary>
        private ITestRequestManager testRequestManager;

        /// <summary>
        /// Used for sending output.
        /// </summary>
        internal IOutput output;

        /// <summary>
        /// RunSettingsManager to get currently active run settings.
        /// </summary>
        private IRunSettingsProvider runSettingsManager;

        /// <summary>
        /// Registers for discovery events during discovery
        /// </summary>
        private ITestDiscoveryEventsRegistrar discoveryEventsRegistrar;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options">
        /// The options.
        /// </param>
        public ListTestsArgumentExecutor(
            CommandLineOptions options,
            IRunSettingsProvider runSettingsProvider,
            ITestRequestManager testRequestManager) : 
                this(options, runSettingsProvider, testRequestManager, ConsoleOutput.Instance)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options">
        /// The options.
        /// </param>
        internal ListTestsArgumentExecutor(
            CommandLineOptions options,
            IRunSettingsProvider runSettingsProvider,
            ITestRequestManager testRequestManager,
            IOutput output)
        {
            Contract.Requires(options != null);

            this.commandLineOptions = options;
            this.output = output;
            this.testRequestManager = testRequestManager;

            this.runSettingsManager = runSettingsProvider;
            this.discoveryEventsRegistrar = new DiscoveryEventsRegistrar(output);
        }

        #endregion

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            if (!string.IsNullOrWhiteSpace(argument))
            {
                this.commandLineOptions.AddSource(argument);
            }
        }

        /// <summary>
        /// Lists out the available discoverers.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public ArgumentProcessorResult Execute()
        {
            Contract.Assert(this.output != null);
            Contract.Assert(this.commandLineOptions != null);

            if (this.commandLineOptions.Sources.Count() <= 0)
            {
#if TODO
                this.logger.SendMessage(TestMessageLevel.Error, CommandLineResources.MissingTestSourceFile);
#endif
                return ArgumentProcessorResult.Fail;
            }

            this.output.WriteLine(CommandLineResources.ListTestsHeaderMessage, OutputLevel.Information);

            var runSettings = RunSettingsUtilities.GetRunSettings(this.runSettingsManager, this.commandLineOptions);

            var success = this.testRequestManager.DiscoverTests(
                new DiscoveryRequestPayload() { Sources = this.commandLineOptions.Sources, RunSettings = runSettings },
                this.discoveryEventsRegistrar);

            return success ? ArgumentProcessorResult.Success : ArgumentProcessorResult.Fail;
        }

        #endregion

        private class DiscoveryEventsRegistrar : ITestDiscoveryEventsRegistrar
        {
            private IOutput output;

            /// <summary>
            /// Specifies whether some tests were found in the sources or not.        
            /// </summary>
            private bool? testsFoundInAnySource = false;

            public DiscoveryEventsRegistrar(IOutput output)
            {
                this.output = output;
            }

            public void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
            {
                discoveryRequest.OnDiscoveredTests += this.discoveryRequest_OnDiscoveredTests;
            }

            public void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
            {
                discoveryRequest.OnDiscoveredTests -= this.discoveryRequest_OnDiscoveredTests;
                this.testsFoundInAnySource = null;
            }

            private void discoveryRequest_OnDiscoveredTests(Object sender, DiscoveredTestsEventArgs args)
            {
                // List out each of the tests.
                foreach (var test in args.DiscoveredTestCases)
                {
                    if (!testsFoundInAnySource.Value)
                    {
                        testsFoundInAnySource = true;
                    }

                    output.WriteLine(String.Format(CultureInfo.CurrentUICulture,
                                                    CommandLineResources.AvailableTestsFormat,
                                                    test.DisplayName),
                                       OutputLevel.Information);
                }
            }
        }
    }
}
