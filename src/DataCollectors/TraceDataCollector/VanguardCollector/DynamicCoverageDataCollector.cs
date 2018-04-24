// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System.Collections.Generic;
    using System.Xml;
    using System;
    using System.Diagnostics;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TraceCollector;
    using Microsoft.VisualStudio.Collector;
    using TestPlatform.ObjectModel;

    /// <summary>
    /// DynamicCoverageDataCollector class
    /// </summary>
    [DataCollectorTypeUri("datacollector://Microsoft/CodeCoverage/2.0")]
    [DataCollectorFriendlyName("Code Coverage")]

    public class DynamicCoverageDataCollector : BaseDataCollector
    {
        /// <summary>
        /// Data collector implementation
        /// </summary>
        private DynamicCoverageDataCollectorImpl implementation;

        private string framework;
        private string targetPlatform;

        private const string VanguardX86ProfilerPath = @"covrun32.dll";
        private const string VanguardX64ProfilerPath = @"amd64\covrun64.dll";

        private const string CORECLR_PROFILER_PATH_VARIABLE = "CORECLR_PROFILER_PATH";
        private const string CORECLR_ENABLE_PROFILING_VARIABLE = "CORECLR_ENABLE_PROFILING";
        private const string CORECLR_PROFILER_VARIABLE = "CORECLR_PROFILER";
        private const string COR_PROFILER_PATH_VARIABLE = "COR_PROFILER_PATH";
        private const string COR_ENABLE_PROFILING_VARIABLE = "COR_ENABLE_PROFILING";
        private const string VANGUARD_PROFILER_GUID = "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}";
        private const string FULL_COR_PROFILER = "COR_PROFILER ";
        private const string CODE_COVERAGE_SESSION_NAME_VARIABLE = "CODE_COVERAGE_SESSION_NAME";
        private const string DefaultConfiguration = @"<Configuration></Configuration>";

        /// <summary>
        /// Boolean variable to track if testcase events were unsubscribed in SessionStart
        /// so that these events can be (re)subscribed to in SessionEnd
        /// </summary>
        private bool testcaseEventsUnsubscribed = false;

        public DynamicCoverageDataCollector()
        {
        }

        protected override void OnInitialize(XmlElement configurationElement)
        {
            if (configurationElement == null)
            {
                var doc = new XmlDocument();
                using (
                    var xmlReader = XmlReader.Create(
                        new StringReader(DefaultConfiguration),
                        new XmlReaderSettings()
                        {
                            /* XmlResolver = null,*/
                            CloseInput = true,
                            DtdProcessing = DtdProcessing.Prohibit
                        }))
                {
                    doc.Load(xmlReader);
                }
                configurationElement = doc.DocumentElement;
            }

            this.Initialize(configurationElement);
        }

        internal override void SetCollectionPerProcess(Dictionary<string, XmlElement> processCPMap)
        {
            // No-op for .NET Core
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <param name="plan">Collection plan</param>
        /// <param name="isFirstCollectorToInitialize">Whether this is the first collector to get initialized</param>
        private void Initialize(XmlElement configurationElement)
        {
            CollectorUtility.RemoveChildNodeAndReturnValue(ref configurationElement, "Framework", out this.framework);
            CollectorUtility.RemoveChildNodeAndReturnValue(ref configurationElement, "TargetPlatform", out this.targetPlatform);

            this.implementation = DynamicCoverageDataCollectorImpl.Create(this.AgentContext);
            this.implementation.Initialize(configurationElement, this.DataSink, this.Logger);
            this.Events.SessionStart += SessionStart;
            this.Events.SessionEnd += SessionEnd;
        }

        /// <summary>
        /// Cleanup temp folder
        /// <param name="disposing">Whether it's called by destructor</param>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            // Bug 1437108
            // this can be null only when the base implementation does not initialize all datacollectors
            if(this.implementation != null)
                this.implementation.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// On session end
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void SessionEnd(object sender, SessionEndEventArgs e)
        {
            this.implementation.SessionEnd(sender, e);
            if (testcaseEventsUnsubscribed)
            {
                // Resubscribing to the Testcase events. If in the next session, if a data collector is added which needs TestCase events
                // and code coverage data collector is not reloaded, base class will not be subscribed to TestCase events.
                SubscribeToTestCaseEvents();
                testcaseEventsUnsubscribed = false;
            }
        }

        /// <summary>
        /// On session start
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void SessionStart(object sender, SessionStartEventArgs e)
        {
            System.Diagnostics.Debug.Assert(testcaseEventsUnsubscribed == false);

            // If DynamicCodeCoverage is the only collector loaded, then we can safely unsubscribe from test case events.
            // The events are not sent - which improves performance.
            // If other collectors are present and code coverage data collector owns collection plan, we do not want to
            // unsubscribe from these events. Only the data collector which owns collection plan can subscribe to these events.

            if (BaseDataCollector.Collectors.Count == 1)
            {
                UnsubscribeFromTestCaseEvents();
                testcaseEventsUnsubscribed = true;
            }

            this.implementation.InitializeBeforeSessionStart();
            this.implementation.InitializeConfiguration();
            this.implementation.SessionStart(sender, e);
        }

        /// <summary>
        /// GetEnvironmentVariables is called after all the collectors have been initialized
        /// so we know whether or not Vanguard is the only collector in the test settings
        /// We need to stop all running w3wp processes so that any new request is served
        /// by the instrumented processes. We can do this as part of SessionStart but
        /// IISReset /stop takes a lot of time and so its better to do it here as part of Initialize
        /// where timeouts are larger.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables()
        {
            string profilerPath = string.Empty;

            if (IsDotnetCoreTargetFramework())
            {
                // Currently TestPlatform doesn't honor the TargetPlatform(architecture - x86, x64) option for .NET Core tests.
                // Set the profiler path based on dotnet process architecture.
                profilerPath = GetProfilerPathBasedOnDotnet();
            }
            else
            {
                profilerPath = GetProfilerPathBasedOnTargetPlatform();
            }

            var vanguardProfilerPath = Path.Combine(CollectorUtility.GetVanguardDirectory(), profilerPath);
            List<KeyValuePair<string, string>> vars = new List<KeyValuePair<string, string>>();
            vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.CORECLR_ENABLE_PROFILING_VARIABLE, "1"));
            vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.CORECLR_PROFILER_PATH_VARIABLE, vanguardProfilerPath));
            vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.CORECLR_PROFILER_VARIABLE, VANGUARD_PROFILER_GUID));
            vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.CODE_COVERAGE_SESSION_NAME_VARIABLE, this.implementation.sessionName));

            vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.COR_PROFILER_PATH_VARIABLE, vanguardProfilerPath));
            vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.COR_ENABLE_PROFILING_VARIABLE, "1"));
            vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.FULL_COR_PROFILER, VANGUARD_PROFILER_GUID));
            return vars.AsReadOnly();
        }

        private static string GetProfilerPathBasedOnDotnet()
        {
            string profilerPath;
            var dotnetPath = CollectorUtility.GetDotnetHostFullPath();
            var processArchitecture = CollectorUtility.GetMachineType(dotnetPath);

            if (processArchitecture == CollectorUtility.MachineType.I386)
            {
                profilerPath = VanguardX86ProfilerPath;
            }
            else if (processArchitecture == CollectorUtility.MachineType.x64)
            {
                profilerPath = VanguardX64ProfilerPath;
            }
            else
            {
                throw new VanguardException("Invalid Architecture");
            }

            return profilerPath;
        }

        private string GetProfilerPathBasedOnTargetPlatform()
        {
            string profilerPath;
            if (string.IsNullOrWhiteSpace(this.targetPlatform))
            {
                throw new VanguardException("Invalid Architecture");
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
                throw new VanguardException("Invalid Architecture");
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
