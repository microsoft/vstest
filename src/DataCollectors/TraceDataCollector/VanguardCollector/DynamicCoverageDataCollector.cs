// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TraceCollector;
    using TestPlatform.ObjectModel;

    /// <summary>
    /// DynamicCoverageDataCollector implements BaseDataCollector for "Code Coverage" . Handles datacollector's SessionStart and SessionsEnd events
    /// and provides environment variable required for code coverage profiler.
    /// </summary>
    [DataCollectorTypeUri("datacollector://Microsoft/CodeCoverage/2.0")]
    [DataCollectorFriendlyName("Code Coverage")]
    public class DynamicCoverageDataCollector : BaseDataCollector
    {
        private const string VanguardX86ProfilerPath = @"covrun32.dll";
        private const string VanguardX64ProfilerPath = @"amd64\covrun64.dll";

        private const string CoreclrProfilerPathVariable32 = "CORECLR_PROFILER_PATH_32";
        private const string CoreclrProfilerPathVariable64 = "CORECLR_PROFILER_PATH_64";
        private const string CoreclrEnableProfilingVariable = "CORECLR_ENABLE_PROFILING";
        private const string CoreclrProfilerVariable = "CORECLR_PROFILER";
        private const string CorProfilerPathVariable32 = "COR_PROFILER_PATH_32";
        private const string CorProfilerPathVariable64 = "COR_PROFILER_PATH_64";
        private const string CorEnableProfilingVariable = "COR_ENABLE_PROFILING";
        private const string VanguardProfilerGuid = "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}";
        private const string FullCorProfiler = "COR_PROFILER";
        private const string CodeCoverageSessionNameVariable = "CODE_COVERAGE_SESSION_NAME";

        /// <summary>
        /// Data collector implementation
        /// </summary>
        private IDynamicCoverageDataCollectorImpl implementation;

        private ICollectorUtility collectorUtility;

        public DynamicCoverageDataCollector()
        : this(new CollectorUtility(), new DynamicCoverageDataCollectorImpl())
        {
        }

        internal DynamicCoverageDataCollector(
            ICollectorUtility collectorUtility,
            IDynamicCoverageDataCollectorImpl dynamicCoverageDataCollectorImpl)
        {
            this.collectorUtility = collectorUtility;
            this.implementation = dynamicCoverageDataCollectorImpl;
        }

        protected override void OnInitialize(XmlElement configurationElement)
        {
            this.implementation.Initialize(configurationElement, this.DataSink, this.Logger);
            this.Events.SessionStart += this.SessionStart;
            this.Events.SessionEnd += this.SessionEnd;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing"> The disposing</param>
        protected override void Dispose(bool disposing)
        {
            this.Events.SessionStart -= this.SessionStart;
            this.Events.SessionEnd -= this.SessionEnd;
            this.implementation?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// The GetEnvironmentVariables
        /// </summary>
        /// <returns>Returns EnvironmentVariables required for code coverage profiler. </returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables()
        {
            var vanguardDirectory = this.collectorUtility.GetVanguardDirectory();
            var vanguardX86ProfilerFullPath = Path.Combine(vanguardDirectory, VanguardX86ProfilerPath);
            var vanguardX64ProfilerFullPath = Path.Combine(vanguardDirectory, VanguardX64ProfilerPath);
            var envVaribles = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CoreclrEnableProfilingVariable, "1"),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CoreclrProfilerPathVariable32, vanguardX86ProfilerFullPath),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CoreclrProfilerPathVariable64, vanguardX64ProfilerFullPath),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CoreclrProfilerVariable, VanguardProfilerGuid),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CodeCoverageSessionNameVariable, this.implementation.GetSessionName()),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CorEnableProfilingVariable, "1"),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CorProfilerPathVariable32, vanguardX86ProfilerFullPath),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CorProfilerPathVariable64, vanguardX64ProfilerFullPath),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.FullCorProfiler, VanguardProfilerGuid),
            };

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DynamicCoverageDataCollector.GetEnvironmentVariables: Returning following environment variables: {0}", string.Join(",", envVaribles));
            }

            return envVaribles.AsReadOnly();
        }

        /// <summary>
        /// On session end
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void SessionEnd(object sender, SessionEndEventArgs e)
        {
            this.implementation.SessionEnd(sender, e);
        }

        /// <summary>
        /// On session start
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void SessionStart(object sender, SessionStartEventArgs e)
        {
            this.implementation.SessionStart(sender, e);
        }
    }
}