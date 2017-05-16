// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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

    internal class RunSpecificTestsArgumentProcessor : IArgumentProcessor
    {
        public const string CommandName = "/Tests";

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new RunSpecificTestsArgumentProcessorCapabilities());
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
                    new RunSpecificTestsArgumentExecutor(
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

    internal class RunSpecificTestsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => RunSpecificTestsArgumentProcessor.CommandName;

        public override bool IsAction => true;

        public override bool AllowMultiple => false;

        public override string HelpContentResourceName => CommandLineResources.RunSpecificTestsHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.RunSpecificTestsArgumentProcessorHelpPriority;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;
    }

    internal class RunSpecificTestsArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        /// <summary>
        /// The instance of testPlatforms
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
        /// Given Collection of strings for filtering test cases
        /// </summary>
        private Collection<string> selectedTestNames;

        /// <summary>
        /// Used for tracking the total no. of tests discovered from the given sources.
        /// </summary>
        private long discoveredTestCount = 0;

        /// <summary>
        /// Collection of test cases that match atleast one of the given search strings
        /// </summary>
        private Collection<TestCase> selectedTestCases = new Collection<TestCase>();

        /// <summary>
        /// Effective run settings applicable to test run after inferring the multi-targeting settings.
        /// </summary>
        private string effectiveRunSettings = null;

        /// <summary>
        /// List of filters that have not yet been discovered
        /// </summary>
        HashSet<string> undiscoveredFilters = new HashSet<string>();

        /// <summary>
        /// Registers for discovery events during discovery
        /// </summary>
        private ITestDiscoveryEventsRegistrar discoveryEventsRegistrar;

#endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RunSpecificTestsArgumentExecutor(
            CommandLineOptions options,
            IRunSettingsProvider runSettingsProvider,
            ITestRequestManager testRequestManager)
        {
            Contract.Requires(options != null);
            Contract.Requires(testRequestManager != null);

            this.commandLineOptions = options;
            this.testRequestManager = testRequestManager;

            this.runSettingsManager = runSettingsProvider;
            this.output = ConsoleOutput.Instance;
            this.discoveryEventsRegistrar = new DiscoveryEventsRegistrar(this.discoveryRequest_OnDiscoveredTests);
        }

#endregion

        #region IArgumentProcessor

        /// <summary>
        /// Splits given the search strings and adds to selectTestNamesCollection.
        /// </summary>
        /// <param name="argument"></param>
        public void Initialize(string argument)
        {
            if (!string.IsNullOrWhiteSpace(argument))
            {
                this.selectedTestNames = new Collection<string>(argument.Split(new[] { CommandLineResources.SearchStringDelimiter }, StringSplitOptions.RemoveEmptyEntries));
            }

            if (this.selectedTestNames == null || this.selectedTestNames.Count <= 0)
            {
                throw new CommandLineException(CommandLineResources.SpecificTestsRequired);
            }

            // by default all filters are not discovered on launch
            this.undiscoveredFilters = new HashSet<string>(this.selectedTestNames);
        }

        /// <summary>
        /// Execute specific tests that match any of the given strings.
        /// </summary>
        /// <returns></returns>
        public ArgumentProcessorResult Execute()
        {
            Contract.Assert(this.output != null);
            Contract.Assert(this.commandLineOptions != null);
            Contract.Assert(this.testRequestManager != null);
            Contract.Assert(!string.IsNullOrWhiteSpace(this.runSettingsManager.ActiveRunSettings.SettingsXml));

            if (this.commandLineOptions.Sources.Count() <= 0)
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, CommandLineResources.MissingTestSourceFile));
            }

            if (!string.IsNullOrWhiteSpace(this.commandLineOptions.TestCaseFilterValue))
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, CommandLineResources.InvalidTestCaseFilterValueForSpecificTests));
            }

            bool result = false;

            this.effectiveRunSettings = this.runSettingsManager.ActiveRunSettings.SettingsXml;

            // Discover tests from sources and filter on every discovery reported.
            result = this.DiscoverTestsAndSelectSpecified(this.commandLineOptions.Sources);

            // Now that tests are discovered and filtered, we run only those selected tests.
            result = result && this.ExecuteSelectedTests();

            return result ? ArgumentProcessorResult.Success : ArgumentProcessorResult.Fail;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Discovers tests from the given sources and selects only specified tests.
        /// </summary>
        /// <param name="sources"> Test source assemblies paths. </param>
        private bool DiscoverTestsAndSelectSpecified(IEnumerable<string> sources)
        {
            this.output.WriteLine(CommandLineResources.StartingDiscovery, OutputLevel.Information);
            if (!string.IsNullOrEmpty(EqtTrace.LogFile))
            {
                this.output.Information(CommandLineResources.VstestDiagLogOutputPath, EqtTrace.LogFile);
            }

            return this.testRequestManager.DiscoverTests(
                new DiscoveryRequestPayload() { Sources = sources, RunSettings = this.effectiveRunSettings }, this.discoveryEventsRegistrar, Constants.DefaultProtocolConfig);
        }

        /// <summary>
        ///  Executes the selected tests
        /// </summary>
        private bool ExecuteSelectedTests()
        {
            bool result = true;
            if (this.selectedTestCases.Count > 0)
            {
                if (this.undiscoveredFilters.Count() != 0)
                {
                    string missingFilters = string.Join(", ", this.undiscoveredFilters);
                    string warningMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.SomeTestsUnavailableAfterFiltering, this.discoveredTestCount, missingFilters);
                    this.output.Warning(warningMessage);
                }

                // for command line keep alive is always false.
                bool keepAlive = false;

                GenerateFakesUtilities.GenerateFakesSettings(this.commandLineOptions, this.commandLineOptions.Sources.ToList(), ref this.effectiveRunSettings);

                EqtTrace.Verbose("RunSpecificTestsArgumentProcessor:Execute: Test run is queued.");
                var runRequestPayload = new TestRunRequestPayload() { TestCases = this.selectedTestCases.ToList(), RunSettings = this.effectiveRunSettings, KeepAlive = keepAlive };
                result &= this.testRequestManager.RunTests(runRequestPayload, null, null, Constants.DefaultProtocolConfig);
            }
            else
            {
                string warningMessage;
                if (this.discoveredTestCount > 0)
                {
                    // No tests that matched any of the given strings.
                    warningMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.NoTestsAvailableAfterFiltering, this.discoveredTestCount, string.Join(", ", this.selectedTestNames));
                }
                else
                {
                    // No tests were discovered from the given sources.
                    warningMessage = string.Format(CultureInfo.CurrentUICulture, CommandLineResources.NoTestsAvailableInSources, string.Join(", ", this.commandLineOptions.Sources));

                    if (!this.commandLineOptions.UseVsixExtensions)
                    {
                        warningMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.NoTestsFoundWarningMessageWithSuggestionToUseVsix, warningMessage, CommandLineResources.SuggestUseVsixExtensionsIfNoTestsIsFound);
                    }
                }

                this.output.Warning(warningMessage);
            }

            return result;
        }

        /// <summary>
        /// Filter discovered tests and find matching tests from given search strings.
        /// Any name of the test that can match multiple strings will be added only once.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void discoveryRequest_OnDiscoveredTests(object sender, DiscoveredTestsEventArgs args)
        {
            this.discoveredTestCount += args.DiscoveredTestCases.Count();
            foreach (var testCase in args.DiscoveredTestCases)
            {
                foreach (var nameCriteria in this.selectedTestNames)
                {
                    if (testCase.FullyQualifiedName.IndexOf(nameCriteria, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        this.selectedTestCases.Add(testCase);

                        // If a testcase matched then a filter matched - so remove the filter from not found list
                        this.undiscoveredFilters.Remove(nameCriteria);
                        break;
                    }
                }
            }
        }

        #endregion

        private class DiscoveryEventsRegistrar : ITestDiscoveryEventsRegistrar
        {
            private EventHandler<DiscoveredTestsEventArgs> discoveredTestsHandler;

            public DiscoveryEventsRegistrar(EventHandler<DiscoveredTestsEventArgs> discoveredTestsHandler)
            {
                this.discoveredTestsHandler = discoveredTestsHandler;
            }

            public void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
            {
                discoveryRequest.OnDiscoveredTests += this.discoveredTestsHandler;
            }

            public void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
            {
                discoveryRequest.OnDiscoveredTests -= this.discoveredTestsHandler;
            }
        }
    }
}