// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TraceCollector;
    using TestPlatform.ObjectModel;
    using TraceCollector.Interfaces;
    using TraceDataCollector.Resources;

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
        private const string CorProfilerVariable = "COR_PROFILER";
        private const string CodeCoverageSessionNameVariable = "CODE_COVERAGE_SESSION_NAME";

        private readonly IEnvironment environment;

        /// <summary>
        /// Data collector implementation
        /// </summary>
        private IDynamicCoverageDataCollectorImpl implementation;

        private IVanguardLocationProvider vanguardLocationProvider;

        /// <summary>
        /// To show warning on non windows.
        /// </summary>
        private bool isWindowsOS;

        public DynamicCoverageDataCollector()
        : this(
            new VanguardLocationProvider(),
            null, /* DynamicCoverageDataCollectorImpl .ctor has dependency on WinAPIs */
            new PlatformEnvironment())
        {
        }

        internal DynamicCoverageDataCollector(
            IVanguardLocationProvider vanguardLocationProvider,
            IDynamicCoverageDataCollectorImpl dynamicCoverageDataCollectorImpl,
            IEnvironment environment)
        {
            this.vanguardLocationProvider = vanguardLocationProvider;
            this.environment = environment;

            // Create DynamicCoverageDataCollectorImpl .ctor only when running on windows, because it has dependency on WinAPIs.
            if (dynamicCoverageDataCollectorImpl == null)
            {
                this.isWindowsOS = this.environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows);
                if (this.isWindowsOS)
                {
                    this.implementation = new DynamicCoverageDataCollectorImpl();
                }
            }
            else
            {
                this.isWindowsOS = true;
                this.implementation = dynamicCoverageDataCollectorImpl;
            }
        }

        protected override void OnInitialize(XmlElement configurationElement)
        {
            if (this.isWindowsOS == false)
            {
                EqtTrace.Warning($"DynamicCoverageDataCollector.OnInitialize: Code coverage not supported for operating system: {this.environment.OperatingSystem}");

                this.Logger.LogWarning(
                    this.AgentContext.SessionDataCollectionContext,
                    string.Format(CultureInfo.CurrentUICulture, Resources.CodeCoverageOnlySupportsWindows));

                return;
            }

            try
            {
                this.implementation.Initialize(configurationElement, this.DataSink, this.Logger);
                this.Events.SessionStart += this.SessionStart;
                this.Events.SessionEnd += this.SessionEnd;
            }
            catch (Exception ex)
            {
                EqtTrace.Error("DynamicCoverageDataCollector.OnInitialize: Failed to initialize code coverage datacollector with exception: {0}", ex);
                this.Logger.LogError(
                    this.AgentContext.SessionDataCollectionContext,
                    string.Format(CultureInfo.CurrentUICulture, Resources.FailedToInitializeCodeCoverageDataCollector, ex));
                throw;
            }
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
            if (this.isWindowsOS == false)
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }

            var vanguardDirectory = this.vanguardLocationProvider.GetVanguardDirectory();
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
                new KeyValuePair<string, string>(DynamicCoverageDataCollector.CorProfilerVariable, VanguardProfilerGuid),
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