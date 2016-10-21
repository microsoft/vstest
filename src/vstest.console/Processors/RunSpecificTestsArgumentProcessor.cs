// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
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

#if TODO
        /// <summary>
        /// Gets the instance of logger object.
        /// </summary>
        private IMessageLogger logger;
#endif
        
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
        HashSet<String> undiscoveredFilters = new HashSet<String>();

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

            this.runSettingsManager = RunSettingsManager.Instance;
            this.output = ConsoleOutput.Instance;
            this.discoveryEventsRegistrar = new DiscoveryEventsRegistrar(discoveryRequest_OnDiscoveredTests);
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
                selectedTestNames = new Collection<string>(argument.Split(new string[] { CommandLineResources.SearchStringDelimiter }, StringSplitOptions.RemoveEmptyEntries));
            }
            if (selectedTestNames == null || selectedTestNames.Count <= 0)
            {
                throw new CommandLineException(CommandLineResources.SpecificTestsRequired);
            }

            // by default all filters are not discovered on launch
            undiscoveredFilters = new HashSet<string>(selectedTestNames);
        }

        /// <summary>
        /// Execute specific tests that match any of the given strings.
        /// </summary>
        /// <returns></returns>
        public ArgumentProcessorResult Execute()
        {
            Contract.Assert(output != null);
            Contract.Assert(commandLineOptions != null);
            Contract.Assert(testRequestManager != null);

            if (commandLineOptions.Sources.Count() <= 0)
            {
#if TODO
                logger.SendMessage(TestMessageLevel.Error, CommandLineResources.MissingTestSourceFile);
#endif
                return ArgumentProcessorResult.Fail;
            }

            if (!string.IsNullOrWhiteSpace(commandLineOptions.TestCaseFilterValue))
            {
#if TODO
                logger.SendMessage(TestMessageLevel.Error, CommandLineResources.InvalidTestCaseFilterValueForSpecificTests);
#endif
                return ArgumentProcessorResult.Fail;
            }

            bool result = false;

            this.effectiveRunSettings = RunSettingsUtilities.GetRunSettings(this.runSettingsManager, this.commandLineOptions);

            // Discover tests from sources and filter on every discovery reported.
            result = DiscoverTestsAndSelectSpecified(commandLineOptions.Sources);

            // Now that tests are discovered and filtered, we run only those selected tests.
            result = result && ExecuteSelectedTests();

            return result ? ArgumentProcessorResult.Success : ArgumentProcessorResult.Fail;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Discovers tests from the given sources and selects only specified tests.
        /// </summary>
        /// <param name="testPlatform">TestPlatform created based on the command line options</param>
        private bool DiscoverTestsAndSelectSpecified(IEnumerable<string> sources)
        {
            output.WriteLine(CommandLineResources.StartingDiscovery, OutputLevel.Information);
            return this.testRequestManager.DiscoverTests(
                new DiscoveryRequestPayload() { Sources = sources, RunSettings = effectiveRunSettings }, this.discoveryEventsRegistrar);
        }

        /// <summary>
        ///  Executes the selected tests
        /// </summary>
        private bool ExecuteSelectedTests()
        {
            bool result = true;
            if (selectedTestCases.Count > 0)
            {
                if (undiscoveredFilters.Count() != 0)
                {
                    string missingFilters = string.Join(", ", undiscoveredFilters);
                    string warningMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.SomeTestsUnavailableAfterFiltering, discoveredTestCount, missingFilters);
                    output.Warning(warningMessage);
                }
                
                // for command line keep alive is always false.
                bool keepAlive = false;

                EqtTrace.Verbose("RunSpecificTestsArgumentProcessor:Execute: Test run is queued.");
                var runRequestPayload = new TestRunRequestPayload() { TestCases = selectedTestCases.ToList(), RunSettings = effectiveRunSettings, KeepAlive = keepAlive };
                result &= this.testRequestManager.RunTests(runRequestPayload, null, null);
            }
            else
            {
                string warningMessage;
                if (discoveredTestCount > 0)
                {
                    // No tests that matched any of the given strings.
                    warningMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.NoTestsAvailableAfterFiltering, discoveredTestCount, String.Join(", ", selectedTestNames));
                }
                else
                {
                    // No tests were discovered from the given sources.
                    warningMessage = string.Format(CultureInfo.CurrentUICulture, CommandLineResources.NoTestsAvailableInSources, string.Join(", ", commandLineOptions.Sources));
                     
                    if (!commandLineOptions.UseVsixExtensions)
                    {
                        warningMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.NoTestsFoundWarningMessageWithSuggestionToUseVsix, warningMessage, CommandLineResources.SuggestUseVsixExtensionsIfNoTestsIsFound);
                    }
                }
                output.Warning(warningMessage);
            }
            return result;
        }

        /// <summary>
        /// Filter discovered tests and find matching tests from given search strings.
        /// Any name of the test that can match multiple strings will be added only once.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void discoveryRequest_OnDiscoveredTests(Object sender, DiscoveredTestsEventArgs args)
        {
            discoveredTestCount += args.DiscoveredTestCases.Count();
            foreach (var testCase in args.DiscoveredTestCases)
            {
                foreach (var nameCriteria in selectedTestNames)
                {
                    if (testCase.FullyQualifiedName.IndexOf(nameCriteria, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        selectedTestCases.Add(testCase);

                        // If a testcase matched then a filter matched - so remove the filter from not found list
                        undiscoveredFilters.Remove(nameCriteria);
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