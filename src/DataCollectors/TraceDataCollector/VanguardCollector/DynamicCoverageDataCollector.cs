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
/*#if !NETSTANDARD
    using Microsoft.VisualStudio.TraceLog;
#endif*/
    using Microsoft.VisualStudio.Collector;
    using TestPlatform.ObjectModel;

    /// <summary>
    /// DynamicCoverageDataCollector class
    /// </summary>
    [DataCollectorTypeUri("datacollector://Microsoft/CodeCoverage/2.0")]
    [DataCollectorFriendlyName("Code Coverage")]

// CommonDataCollector not required for .NET Standard because currently we don't support fakes and test impact with .NET Core SDK.
/*#if !NETSTANDARD
    public class DynamicCoverageDataCollector : CommonDataCollector
#else */
    public class DynamicCoverageDataCollector : BaseDataCollector

// #endif
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

/* #if !NETSTANDARD
        /// <summary>
        /// Called when the the collector is the first collector to initialize.
        /// </summary>
        /// <param name="configurationElement">The configuration element of the collector.</param>
        /// <returns>Returns the collection plan to use.</returns>
        internal override ConfigMessagePacker._CollectionPlan OnFirstCollectorToInitialize(XmlElement configurationElement)
        {            
            ConfigMessagePacker._CollectionPlan plan = new ConfigMessagePacker._CollectionPlan(true);
            Initialize(configurationElement, plan, isFirstCollectorToInitialize:true);
            return plan;
        }

        /// <summary>
        /// Called when the collector is the second collector to initialize
        /// </summary>
        /// <param name="plan">The collection plan returned from the first collector.</param>
        /// <param name="configurationElement">The configuration element of this collector.</param>
        /// <returns>Returns the collection plan to use.</returns>
        internal override ConfigMessagePacker._CollectionPlan OnSecondCollectorToInitialize(ConfigMessagePacker._CollectionPlan plan, XmlElement configurationElement)
        {            
            Initialize(configurationElement, plan, isFirstCollectorToInitialize:false);
            return plan;
        }

        /// <summary>
        /// Adds information about code coverage in the intellitrace collection plan
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        /// If Vanguard is the only collector then we don't want to use the collection plan because
        /// Intellitrace requires Admin privileges for collecting data from IIS but if any other 
        /// intellitrace based collector is enabled along with Vanguard we still want to follow the old path. 
        /// For this we need to check if code coverage is the only collector enabled in the test settings. 
        /// If that is the case we will not add Vanguard to the collection plan.
        /// If vanguard is the first collector to initialize, we don't know whether there are other 
        /// collectors also. In that case, we want to get notified before any other collector is initialized 
        /// so that we can update the collection plan with vanguard info.
        /// We don't need to call this in the case where OnSecondCollectorToInitialize is called because 
        /// there we definitely know that there are other collectors enabled for the run
        internal override ConfigMessagePacker._CollectionPlan BeforeSecondCollectorInitialize(ConfigMessagePacker._CollectionPlan plan)
        {
            return this.implementation.UpdateCollectionPlanWithVanguardInfo(plan);
        }
#else */
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

 // #endif
        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <param name="plan">Collection plan</param>
        /// <param name="isFirstCollectorToInitialize">Whether this is the first collector to get initialized</param>
/* #if !NETSTANDARD
        private void Initialize(XmlElement configurationElement, ConfigMessagePacker._CollectionPlan plan, bool isFirstCollectorToInitialize)
#else        */
        private void Initialize(XmlElement configurationElement)
// #endif
        {
            CollectorUtility.RemoveChildNodeAndReturnValue(ref configurationElement, "Framework", out this.framework);
            CollectorUtility.RemoveChildNodeAndReturnValue(ref configurationElement, "TargetPlatform", out this.targetPlatform);

            this.implementation = DynamicCoverageDataCollectorImpl.Create(this.AgentContext);
/* #if !NETSTANDARD
            this.implementation.Initialize(configurationElement, plan, this.DataSink, this.Logger, isFirstCollectorToInitialize);
#else */
            this.implementation.Initialize(configurationElement, this.DataSink, this.Logger);
// #endif
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
/*#if !NETSTANDARD
            this.implementation.InitializeConfiguration(this.IISInjector);
#else*/
            this.implementation.InitializeConfiguration();
//#endif
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
            if ( IsDotnetCoreTargetFramework()
                || IsDataCollectorLaunchedByDotnet())
            {
                // netcore scenario, dont call base EnvironmentVars

                string profilerPath = string.Empty;
                if (IsDotnetCoreTargetFramework())
                {
                    profilerPath = GetProfilerPathBasedOnDotnet();
                }

                else if (IsDataCollectorLaunchedByDotnet())
                {
                    profilerPath = GetProfilerPathBasedOnTargetPlatform();
                }

/*#if !NETSTANDARD
                var vanguardProfilerPath = Path.Combine(CollectorUtility.GetVSInstallPath(), "..", "..", profilerPath);
#else*/
                var vanguardProfilerPath = Path.Combine(CollectorUtility.GetVanguardDirectory(), profilerPath);
//#endif
                List<KeyValuePair<string, string>> vars = new List<KeyValuePair<string, string>>();
                vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.CORECLR_ENABLE_PROFILING_VARIABLE, "1"));
                vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.CORECLR_PROFILER_PATH_VARIABLE, vanguardProfilerPath));
                vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.CORECLR_PROFILER_VARIABLE, VANGUARD_PROFILER_GUID));
                vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.CODE_COVERAGE_SESSION_NAME_VARIABLE, this.implementation.sessionName));

                /*vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.COR_ENABLE_PROFILING_VARIABLE, "1"));
                vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.COR_PROFILER_PATH_VARIABLE, vanguardProfilerPath));
                vars.Add(new KeyValuePair<string, string>(DynamicCoverageDataCollector.FULL_COR_PROFILER, VANGUARD_PROFILER_GUID));*/
/*#if !NETSTANDARD
                base.SetEnvironmentVariableRequested(true);
#endif*/
                return vars.AsReadOnly();
            }
/*#if !NETSTANDARD
            return base.GetEnvironmentVariables();
#else*/
            return new List<KeyValuePair<string, string>>();
// #endif
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

        private bool IsDataCollectorLaunchedByDotnet()
        {
            var currentProcessPath = Process.GetCurrentProcess().MainModule.FileName;

            return currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase) ||
                   currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDotnetCoreTargetFramework()
        {
            return this.framework?.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                   || this.framework?.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
