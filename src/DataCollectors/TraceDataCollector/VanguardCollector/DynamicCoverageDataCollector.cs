// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Interfaces;
    using Microsoft.VisualStudio.Collector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TraceCollector;
    using TestPlatform.ObjectModel;
    using TraceDataCollector.Resources;

    /// <summary>
    /// DynamicCoverageDataCollector class
    /// </summary>
    [DataCollectorTypeUri("datacollector://Microsoft/CodeCoverage/2.0")]
    [DataCollectorFriendlyName("Code Coverage")]
    public class DynamicCoverageDataCollector : BaseDataCollector
    {
        private const string VanguardX86ProfilerPath = @"covrun32.dll";
        private const string VanguardX64ProfilerPath = @"amd64\covrun64.dll";

        private const string CoreclrProfilerPathVariable = "CORECLR_PROFILER_PATH";
        private const string CoreclrEnableProfilingVariable = "CORECLR_ENABLE_PROFILING";
        private const string CoreclrProfilerVariable = "CORECLR_PROFILER";
        private const string CorProfilerPathVariable = "COR_PROFILER_PATH";
        private const string CorEnableProfilingVariable = "COR_ENABLE_PROFILING";
        private const string VanguardProfilerGuid = "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}";
        private const string FullCorProfiler = "COR_PROFILER";
        private const string CodeCoverageSessionNameVariable = "CODE_COVERAGE_SESSION_NAME";

        /// <summary>
        /// Data collector implementation
        /// </summary>
        private IDynamicCoverageDataCollectorImpl implementation;

        private string framework;
        private string targetPlatform;
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
            this.collectorUtility.RemoveChildNodeAndReturnValue(ref configurationElement, "Framework", out this.framework);
            this.collectorUtility.RemoveChildNodeAndReturnValue(ref configurationElement, "TargetPlatform", out this.targetPlatform);

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
            var vanguardProfilerPath = this.GetVanguardProfilerPath();
            var envVaribles = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CoreclrEnableProfilingVariable, "1"),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CoreclrProfilerPathVariable, vanguardProfilerPath),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CoreclrProfilerVariable, VanguardProfilerGuid),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CodeCoverageSessionNameVariable, this.implementation.GetSessionName()),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CorEnableProfilingVariable, "1"),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CorProfilerPathVariable, vanguardProfilerPath),
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.FullCorProfiler, VanguardProfilerGuid),
            };

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DynamicCoverageDataCollector.GetEnvironmentVariables: Returning following environment variables: {0}", string.Join(",", envVaribles));
            }

            return envVaribles.AsReadOnly();
        }

        private static void ThrowOnNotSupportedTargetPlatform(string targetPlatform)
        {
            EqtTrace.Error(
                "DynamicCoverageDataCollector.ThrowOnNotSupportedTargetPlatform: code coverage profiler not available for TargetPlatform: \"{0}\"",
                targetPlatform);
            throw new VanguardException(string.Format(
                CultureInfo.CurrentUICulture,
                Resources.NotSupportedTargetPlatform,
                targetPlatform));
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

        private string GetVanguardProfilerPath()
        {
            var profilerPath = this.IsDotnetCoreTargetFramework()
                ? this.GetProfilerPathBasedOnDotnet()
                : this.GetProfilerPathBasedOnTargetPlatform();

            return Path.Combine(this.collectorUtility.GetVanguardDirectory(), profilerPath);
        }

        private string GetProfilerPathBasedOnDotnet()
        {
            string profilerPath = string.Empty;
            var dotnetPath = this.collectorUtility.GetDotnetHostFullPath();
            var targetPlatform = this.collectorUtility.GetMachineType(dotnetPath);

            if (targetPlatform == CollectorUtility.MachineType.I386)
            {
                profilerPath = VanguardX86ProfilerPath;
            }
            else if (targetPlatform == CollectorUtility.MachineType.X64)
            {
                profilerPath = VanguardX64ProfilerPath;
            }
            else
            {
                DynamicCoverageDataCollector.ThrowOnNotSupportedTargetPlatform(targetPlatform.ToString());
            }

            return profilerPath;
        }

        private string GetProfilerPathBasedOnTargetPlatform()
        {
            string profilerPath = string.Empty;
            if (string.IsNullOrWhiteSpace(this.targetPlatform))
            {
                DynamicCoverageDataCollector.ThrowOnNotSupportedTargetPlatform(this.targetPlatform);
            }

            Architecture arch = (Architecture)Enum.Parse(typeof(Architecture), this.targetPlatform, true);
            if (arch == Architecture.X86)
            {
                profilerPath = VanguardX86ProfilerPath;
            }
            else if (arch == Architecture.X64)
            {
                profilerPath = VanguardX64ProfilerPath;
            }
            else
            {
                DynamicCoverageDataCollector.ThrowOnNotSupportedTargetPlatform(this.targetPlatform);
            }

            return profilerPath;
        }

        private bool IsDotnetCoreTargetFramework()
        {
            return this.framework?.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                   || this.framework?.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}