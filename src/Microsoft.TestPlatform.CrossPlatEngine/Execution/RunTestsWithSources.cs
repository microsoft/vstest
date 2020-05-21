// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;

    using ObjectModel.Client;
    using ObjectModel.Logging;
    using Utilities;
    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    internal class RunTestsWithSources : BaseRunTests
    {
        private Dictionary<string, IEnumerable<string>> adapterSourceMap;

        private Dictionary<Tuple<Uri,string>, IEnumerable<string>> executorUriVsSourceList;

        private ITestCaseEventsHandler testCaseEventsHandler;

        public RunTestsWithSources(IRequestData requestData, Dictionary<string, IEnumerable<string>> adapterSourceMap, string package, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler)
            : this(requestData, adapterSourceMap, package, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, null)
        {
        }

        /// <summary>
        /// Used for unit testing only.
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="adapterSourceMap"></param>
        /// <param name="package">The user input test source(package) if it differ from actual test source otherwise null.</param>
        /// <param name="runSettings"></param>
        /// <param name="testExecutionContext"></param>
        /// <param name="testCaseEventsHandler"></param>
        /// <param name="testRunEventsHandler"></param>
        /// <param name="executorUriVsSourceList"></param>
        /// <param name="testRunCache"></param>
        internal RunTestsWithSources(IRequestData requestData, Dictionary<string, IEnumerable<string>> adapterSourceMap, string package, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, Dictionary<Tuple<Uri, string>, IEnumerable<string>> executorUriVsSourceList)
            : base(requestData, package, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, TestPlatformEventSource.Instance)
        {
            this.adapterSourceMap = adapterSourceMap;
            this.executorUriVsSourceList = executorUriVsSourceList;
            this.testCaseEventsHandler = testCaseEventsHandler;
        }

        protected override void BeforeRaisingTestRunComplete(bool exceptionsHitDuringRunTests)
        {
            // If run was with sources and no test was executed and cancellation was not requested,
            // then raise a warning saying that no test was present in the sources.
            // The warning is raised only if total no of tests that have been run is zero.
            if (!exceptionsHitDuringRunTests && this.executorUriVsSourceList?.Count > 0 && !this.IsCancellationRequested
                && this.TestRunCache?.TotalExecutedTests <= 0)
            {
                this.LogWarningOnNoTestsExecuted();
            }
        }

        private void LogWarningOnNoTestsExecuted()
        {
            IEnumerable<string> sources = new List<string>();
            var sourcesArray = this.adapterSourceMap.Values
                .Aggregate(sources, (current, enumerable) => current.Concat(enumerable)).ToArray();
            var sourcesString = string.Join(" ", sourcesArray);

            if (this.TestExecutionContext.TestCaseFilter != null)
            {
                var testCaseFilterToShow = TestCaseFilterDeterminer.ShortenTestCaseFilterIfRequired(this.TestExecutionContext.TestCaseFilter);
                this.TestRunEventsHandler?.HandleLogMessage(
                    TestMessageLevel.Warning,
                    string.Format(CrossPlatEngineResources.NoTestsAvailableForGivenTestCaseFilter, testCaseFilterToShow, sourcesString));
            }
            else
            {
                this.TestRunEventsHandler?.HandleLogMessage(
                    TestMessageLevel.Warning,
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatEngineResources.TestRunFailed_NoDiscovererFound_NoTestsAreAvailableInTheSources,
                        sourcesString));
            }
        }

        protected override IEnumerable<Tuple<Uri,string>> GetExecutorUriExtensionMap(IFrameworkHandle testExecutorFrameworkHandle, RunContext runContext)
        {
            this.executorUriVsSourceList = this.GetExecutorVsSourcesList(testExecutorFrameworkHandle);
            var executorUris = this.executorUriVsSourceList.Keys;

            if (!string.IsNullOrEmpty(this.TestExecutionContext.TestCaseFilter))
            {
                runContext.FilterExpressionWrapper = new FilterExpressionWrapper(this.TestExecutionContext.TestCaseFilter, this.TestExecutionContext.FilterOptions);
            }
            else
            {
                runContext.FilterExpressionWrapper = null;
            }

            return executorUris;
        }

        protected override void InvokeExecutor(
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
            Tuple<Uri, string> executorUriExtensionTuple,
            RunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            executor?.Value.RunTests(this.executorUriVsSourceList[executorUriExtensionTuple], runContext, frameworkHandle);
        }

        /// <inheritdoc />
        protected override bool ShouldAttachDebuggerToTestHost(
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
            Tuple<Uri, string> executorUriExtensionTuple,
            RunContext runContext)
        {
            // If the adapter doesn't implement the new test executor interface we should attach to
            // the default test host by default to preserve old behavior.
            if (!(executor?.Value is ITestExecutor2 convertedExecutor))
            {
                return true;
            }

            return convertedExecutor.ShouldAttachToTestHost(
                this.executorUriVsSourceList[executorUriExtensionTuple],
                runContext);
        }

        /// <summary>
        /// Returns executor Vs sources list
        /// </summary>
        private Dictionary<Tuple<Uri, string>, IEnumerable<string>> GetExecutorVsSourcesList(IMessageLogger logger)
        {
            var result = new Dictionary<Tuple<Uri, string>, IEnumerable<string>>();

            var verifiedExtensionSourceMap = new Dictionary<string, IEnumerable<string>>();

            // Validate the sources 
            foreach (var kvp in this.adapterSourceMap)
            {
                var verifiedSources = DiscoveryManager.GetValidSources(kvp.Value, logger);
                if (verifiedSources.Any())
                {
                    verifiedExtensionSourceMap.Add(kvp.Key, kvp.Value);
                }
            }

            // No valid source is found => no executor vs source map
            if (!verifiedExtensionSourceMap.Any())
            {
                return result;
            }

            foreach (var kvp in verifiedExtensionSourceMap)
            {
                Dictionary<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>, IEnumerable<string>>
                    discovererToSourcesMap = DiscovererEnumerator.GetDiscovererToSourcesMap(
                        kvp.Key,
                        kvp.Value,
                        logger,
                        new AssemblyProperties());

                // Warning is logged by the inner layer
                if (discovererToSourcesMap == null || discovererToSourcesMap.Count == 0)
                {
                    continue;
                }

                foreach (var discoverer in discovererToSourcesMap.Keys)
                {
                    var executorUri = discoverer.Metadata.DefaultExecutorUri;
                    if (executorUri == null)
                    {
                        string errorMessage = string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatEngineResources.IgnoringExecutorAsNoDefaultExecutorUriAttribute,
                            discoverer.Value);
                        logger.SendMessage(TestMessageLevel.Warning, errorMessage);
                        continue;
                    }

                    var executorUriExtensionTuple = new Tuple<Uri, string>(executorUri, kvp.Key);

                    if (!result.ContainsKey(executorUriExtensionTuple))
                    {
                        result.Add(executorUriExtensionTuple, discovererToSourcesMap[discoverer]);
                    }
                    else
                    {
                        string errorMessage = string.Format(
                            CultureInfo.CurrentUICulture,
                            CrossPlatEngineResources.DuplicateAdaptersFound,
                            executorUri,
                            discoverer.Value);
                        logger.SendMessage(TestMessageLevel.Warning, errorMessage);
                    }
                }
            }

            return result;
        }

        private static string TestCaseFilterToShow(string testCaseFilter)
        {
            var maxTestCaseFilterToShowLength = 63;
            string testCaseFilterToShow;

            if (testCaseFilter.Length > maxTestCaseFilterToShowLength)
            {
                testCaseFilterToShow = testCaseFilter.Substring(0, maxTestCaseFilterToShowLength - 3) + "...";
            }
            else
            {
                testCaseFilterToShow = testCaseFilter;
            }

            return testCaseFilterToShow;
        }

        /// <summary>
        /// Sends Session-End event on in-proc datacollectors
        /// </summary>
        protected override void SendSessionEnd()
        {
            this.testCaseEventsHandler?.SendSessionEnd();
        }

        /// <summary>
        /// Sends Session-Start event on in-proc datacollectors
        /// </summary>
        protected override void SendSessionStart()
        {
            // Send session start with test sources in property bag for session start event args.
            if (this.testCaseEventsHandler == null)
            {
                return;
            }

            var properties = new Dictionary<string, object>();
            properties.Add("TestSources", TestSourcesUtility.GetSources(this.adapterSourceMap));

            this.testCaseEventsHandler.SendSessionStart(properties);
        }
    }
}
