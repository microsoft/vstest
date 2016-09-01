// Copyright (c) Microsoft. All rights reserved.

using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;

    using ObjectModel.Logging;
    using ObjectModel.Client;
    internal class RunTestsWithSources : BaseRunTests
    {
        private Dictionary<string, IEnumerable<string>> adapterSourceMap;

        private Dictionary<Tuple<Uri,string>, IEnumerable<string>> executorUriVsSourceList;
        private TestPlatformEventSource testPlatformEventSource;

        public RunTestsWithSources(Dictionary<string, IEnumerable<string>> adapterSourceMap, ITestRunCache testRunCache, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler)
            : this(adapterSourceMap, testRunCache, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, null, TestPlatformEventSource.Instance)
        {
        }

        /// <summary>
        /// Used for unit testing only.
        /// </summary>
        /// <param name="adapterSourceMap"></param>
        /// <param name="testRunCache"></param>
        /// <param name="runSettings"></param>
        /// <param name="testExecutionContext"></param>
        /// <param name="testCaseEventsHandler"></param>
        /// <param name="testRunEventsHandler"></param>
        /// <param name="executorUriVsSourceList"></param>
        /// <param name="testPlatformEventSource1"></param>
        internal RunTestsWithSources(Dictionary<string, IEnumerable<string>> adapterSourceMap, ITestRunCache testRunCache, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEventsHandler, ITestRunEventsHandler testRunEventsHandler, Dictionary<Tuple<Uri, string>, IEnumerable<string>> executorUriVsSourceList, TestPlatformEventSource testPlatformEventSource)
            : base(testRunCache, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler)
        {
            this.adapterSourceMap = adapterSourceMap;
            this.executorUriVsSourceList = executorUriVsSourceList;
            this.testPlatformEventSource = testPlatformEventSource;
        }

        protected override void BeforeRaisingTestRunComplete(bool exceptionsHitDuringRunTests)
        {
            // If run was with sources and no test was executed and cancellation was not requested,
            // then raise a warning saying that no test was present in the sources.  
            // The warning is raised only if total no of tests that have been run is zero.
            if (!exceptionsHitDuringRunTests && this.executorUriVsSourceList?.Count > 0 && !this.IsCancellationRequested
                && this.TestRunCache?.TotalExecutedTests <= 0)
            {
                IEnumerable<string> sources = new List<string>();
                var sourcesArray = this.adapterSourceMap.Values.Aggregate(sources, (current, enumerable) => current.Concat(enumerable)).ToArray();
                var sourcesString = string.Join(" ", sourcesArray);

                this.TestRunEventsHandler?.HandleLogMessage(
                    TestMessageLevel.Warning,
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatEngine.Resources.TestRunFailed_NoTestsAreAvailableInTheSources,
                        sourcesString));
            }
        }

        protected override IEnumerable<Tuple<Uri,string>> GetExecutorUriExtensionMap(IFrameworkHandle testExecutorFrameworkHandle, RunContext runContext)
        {
            this.executorUriVsSourceList = this.GetExecutorVsSourcesList(testExecutorFrameworkHandle);
            var executorUris = this.executorUriVsSourceList.Keys;

            if (!string.IsNullOrEmpty(this.TestExecutionContext.TestCaseFilter))
            {
                runContext.FilterExpressionWrapper = new FilterExpressionWrapper(this.TestExecutionContext.TestCaseFilter);
            }
            else
            {
                runContext.FilterExpressionWrapper = null;
            }

            return executorUris;
        }

        protected override void InvokeExecutor(LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor, Tuple<Uri, string> executorUriExtensionTuple, RunContext runContext, IFrameworkHandle frameworkHandle)
        {
            this.testPlatformEventSource?.Execution();
            executor?.Value.RunTests(this.executorUriVsSourceList[executorUriExtensionTuple], runContext, frameworkHandle);
            this.testPlatformEventSource?.ExecutionEnd();
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
                        logger);

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
                            CrossPlatEngine.Resources.IgnoringExecutorAsNoDefaultExecutorUriAttribute,
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
                            CrossPlatEngine.Resources.DuplicateAdaptersFound,
                            executorUri,
                            discoverer.Value);
                        logger.SendMessage(TestMessageLevel.Warning, errorMessage);
                    }
                }
            }

            return result;
        }
    }
}
