// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Argument Executor for the "--ListFullyQualifiedTests|/ListFullyQualifiedTests" command line argument.
    /// </summary>
    internal class ListFullyQualifiedTestsArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the ListFullyQualifiedTestsArgumentProcessor handles.
        /// </summary>
        public const string CommandName = "/ListFullyQualifiedTests";

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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ListFullyQualifiedTestsArgumentProcessorCapabilities());
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
                            new ListFullyQualifiedTestsArgumentExecutor(
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

    internal class ListFullyQualifiedTestsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => ListFullyQualifiedTestsArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => true;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Sources;
    }

    /// <summary>
    /// Argument Executor for the "/ListTests" command line argument.
    /// </summary>
    internal class ListFullyQualifiedTestsArgumentExecutor : IArgumentExecutor
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

        /// <summary>
        /// Test case filter instance
        /// </summary>
        private TestCaseFilter testCasefilter;

        /// <summary>
        /// List to store the discovered tests
        /// </summary>
        private List<string> discoveredTests = new List<string>();

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options">
        /// The options.
        /// </param>
        public ListFullyQualifiedTestsArgumentExecutor(
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
        internal ListFullyQualifiedTestsArgumentExecutor(
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
            this.testCasefilter = new TestCaseFilter();
            this.discoveryEventsRegistrar = new DiscoveryEventsRegistrar(output, this.testCasefilter, discoveredTests, this.commandLineOptions);
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
            Contract.Assert(!string.IsNullOrWhiteSpace(this.runSettingsManager?.ActiveRunSettings?.SettingsXml));

            if (!this.commandLineOptions.Sources.Any())
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, CommandLineResources.MissingTestSourceFile));
            }

            if (!string.IsNullOrEmpty(EqtTrace.LogFile))
            {
                this.output.Information(false, CommandLineResources.VstestDiagLogOutputPath, EqtTrace.LogFile);
            }

            var runSettings = this.runSettingsManager.ActiveRunSettings.SettingsXml;

            this.testRequestManager.DiscoverTests(
                new DiscoveryRequestPayload { Sources = this.commandLineOptions.Sources, RunSettings = runSettings },
                this.discoveryEventsRegistrar, Constants.DefaultProtocolConfig);

            if (string.IsNullOrEmpty(this.commandLineOptions.ListTestsTargetPath))
            {
                // This string does not need to go to Resources. Reason - only internal consumption
                throw new CommandLineException("Target Path should be specified for listing FQDN tests!");
            }

            File.WriteAllLines(this.commandLineOptions.ListTestsTargetPath, this.discoveredTests);
            return ArgumentProcessorResult.Success;
        }

        #endregion

        private class DiscoveryEventsRegistrar : ITestDiscoveryEventsRegistrar
        {
            private IOutput output;
            private TestCaseFilter testCasefilter;
            private List<string> discoveredTests;
            private CommandLineOptions options;

            public DiscoveryEventsRegistrar(IOutput output, TestCaseFilter filter, List<string> discoveredTests, CommandLineOptions cmdOptions)
            {
                this.output = output;
                this.testCasefilter = filter;
                this.discoveredTests = discoveredTests;
                this.options = cmdOptions;
            }

            public void LogWarning(string message)
            {
                ConsoleLogger.RaiseTestRunWarning(message);
            }
            public void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
            {
                discoveryRequest.OnDiscoveredTests += this.DiscoveryRequest_OnDiscoveredTests;
            }

            public void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
            {
                discoveryRequest.OnDiscoveredTests -= this.DiscoveryRequest_OnDiscoveredTests;
            }

            private void DiscoveryRequest_OnDiscoveredTests(Object sender, DiscoveredTestsEventArgs args)
            {
                if (args == null)
                {
                    throw new TestPlatformException("DiscoveredTestsEventArgs cannot be null.");
                }

                // Initializing the test case filter here because the filter value is read late.
                this.testCasefilter.Initialize(this.options.TestCaseFilterValue);
                var discoveredTests = args.DiscoveredTestCases.ToList();
                var filteredTests = this.testCasefilter.FilterTests(discoveredTests).ToList();

                // remove any duplicate tests
                filteredTests = filteredTests.Select(test => test.FullyQualifiedName)
                                                           .Distinct()
                                                           .Select(fqdn => filteredTests.First(test => test.FullyQualifiedName == fqdn))
                                                           .ToList();
                this.discoveredTests.AddRange(filteredTests.Select(test => test.FullyQualifiedName));
            }
        }

        private class TestCaseFilter
        {
            private static TestCaseFilterExpression filterExpression;
            private const string TestCategory = "TestCategory";
            private const string Category = "Category";
            private const string Traits = "Traits";

            public TestCaseFilter()
            {

            }

            public void Initialize(string filterString)
            {
                ValidateFilter(filterString);
            }

            /// <summary>
            /// Filter tests
            /// </summary>
            public IEnumerable<TestCase> FilterTests(IEnumerable<TestCase> testCases)
            {
                EqtTrace.Verbose("TestCaseFilter.FilterTests : Test Filtering invoked.");

                List<TestCase> filteredList;

                try
                {
                    filteredList = GetFilteredTestCases(testCases);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("TestCaseFilter.FilterTests : Exception during filtering : {0}", ex.ToString());
                    throw;
                }

                return filteredList;
            }

            private static void ValidateFilter(string filterString)
            {
                if (string.IsNullOrEmpty(filterString))
                {
                    filterExpression = null;
                    return;
                }

                var filterWrapper = new FilterExpressionWrapper(filterString);

                if (filterWrapper.ParseError != null)
                {
                    var fe = new FormatException(String.Format("Invalid Test Case Filter: {0}", filterString));
                    EqtTrace.Error("TestCaseFilter.ValidateFilter : Filtering failed with exception : " + fe.Message);
                    throw fe;
                }

                filterExpression = new TestCaseFilterExpression(filterWrapper);

            }

            /// <summary>
            /// get list of test cases that satisfy the filter criteria
            /// </summary>
            private static List<TestCase> GetFilteredTestCases(IEnumerable<TestCase> testCases)
            {
                var filteredList = new List<TestCase>();

                if (filterExpression == null)
                {
                    filteredList = testCases.ToList();
                    return filteredList;
                }

                foreach (var testCase in testCases)
                {
                    var traitDictionary = GetTestPropertiesInTraitDictionary(testCase);// Dictionary with trait key to value mapping
                    traitDictionary = GetTraitsInTraitDictionary(traitDictionary, testCase.Traits);

                    // Skip test if not fitting filter criteria.
                    if (!filterExpression.MatchTestCase(testCase, p => PropertyValueProvider(p, traitDictionary)))
                    {
                        continue;
                    }

                    filteredList.Add(testCase);
                }

                return filteredList;
            }

            /// <summary>
            /// fetch the test properties on this test method as traits and populate a trait dictionary
            /// </summary>
            private static Dictionary<string, List<string>> GetTestPropertiesInTraitDictionary(TestCase testCase)
            {
                var traitDictionary = new Dictionary<string, List<string>>();
                foreach (var testProperty in testCase.Properties)
                {
                    string testPropertyKey = testProperty.Label;

                    if (testPropertyKey.Equals(Traits))
                    {
                        // skip the "Traits" property. traits to be set separately
                        continue;
                    }

                    var testPropertyValue = testCase.GetPropertyValue(testProperty);

                    if (testPropertyKey.Equals(TestCategory))
                    {
                        var testPropertyValueArray = testPropertyValue as string[];
                        if (testPropertyValueArray != null)
                        {
                            var testPropertyValueList = new List<string>(testPropertyValueArray);
                            traitDictionary.Add(testPropertyKey, testPropertyValueList);
                            continue;
                        }
                    }

                    //always return value as a list of string
                    if (testPropertyValue != null)
                    {
                        var multiValue = new List<string> { testPropertyValue.ToString() };
                        traitDictionary.Add(testPropertyKey, multiValue);
                    }
                }

                return traitDictionary;
            }

            /// <summary>
            /// fetch the traits on this test method and populate a trait dictionary
            /// </summary>
            private static Dictionary<string, List<string>> GetTraitsInTraitDictionary(Dictionary<string, List<string>> traitDictionary, TraitCollection traits)
            {
                foreach (var trait in traits)
                {
                    var newTraitValueList = new List<string> { trait.Value };
                    List<string> currentTraitValue;
                    if (!traitDictionary.TryGetValue(trait.Name, out currentTraitValue))
                    {
                        // if the current trait's key is not already present, add the current trait key-value pair
                        traitDictionary.Add(trait.Name, newTraitValueList);
                    }
                    else
                    {
                        if (null == currentTraitValue)
                        {
                            // if the current trait's value is null, replace the previous value with the current value
                            traitDictionary[trait.Name] = newTraitValueList;
                        }
                        else
                        {
                            // if the current trait's key is already present and is not null, append current value to the previous value list
                            List<string> traitValueList = currentTraitValue;
                            traitValueList.Add(trait.Value);
                        }
                    }
                }

                //This is hack for NUnit, XUnit to understand test category -> This method is called only for NUnit/XUnit
                if (!traitDictionary.ContainsKey(TestCategory) && traitDictionary.ContainsKey(Category))
                {
                    traitDictionary.TryGetValue(Category, out var categoryValue);
                    traitDictionary.Add(TestCategory, categoryValue);
                }

                return traitDictionary;
            }

            /// <summary>
            /// Provides value for property name 'propertyName' as used in filter.
            /// </summary>
            private static string[] PropertyValueProvider(string propertyName, Dictionary<string, List<string>> traitDictionary)
            {
                List<string> propertyValueList;
                traitDictionary.TryGetValue(propertyName, out propertyValueList);
                if (propertyValueList != null)
                {
                    var propertyValueArray = propertyValueList.ToArray();

                    return propertyValueArray;
                }
                return null;
            }
        }
    }
}
