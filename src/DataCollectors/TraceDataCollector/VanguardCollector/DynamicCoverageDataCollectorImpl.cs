// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Xml;
/*#if !NETSTANDARD
    using Microsoft.VisualStudio.Enterprise.WebInstrument;
#endif*/
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.Diagnostics;
    using Microsoft.VisualStudio.TraceCollector;
    using TraceDataCollector;
    using TraceDataCollector.Resources;

/*#if !NETSTANDARD
    using Microsoft.VisualStudio.TraceLog;
#endif*/

    /// <summary>
    /// Implementation class of dynamic code coverage data collector
    /// </summary>
    internal abstract class DynamicCoverageDataCollectorImpl
    {
        /// <summary>
        /// File name for vanguard config file
        /// </summary>
        private const string VanguardConfigFileName = "CodeCoverage.config";

        /// <summary>
        /// The setting name for coverage file name
        /// </summary>
        private const string CoverageFileSettingName = "CoverageFileName";

        /// <summary>
        /// A magic prefix used to differentiate MTM registered sessions from manually registered sessions
        /// </summary>
        public const string MagicMtmSessionPrefix = "MTM_";

        internal const string RootName = "CodeCoverage";

        // Default CodeCoverage Configuration as specified here : https://msdn.microsoft.com/en-us/library/jj635153.aspx
        private static string DefaultCodeCoverageSettings =
@"        <Configuration>" + Environment.NewLine +
@"          <CodeCoverage>" + Environment.NewLine +
@"            <ModulePaths>" + Environment.NewLine +
@"              <Exclude>" + Environment.NewLine +
@"                 <ModulePath>.*CPPUnitTestFramework.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*vstest.console.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.intellitrace.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*testhost.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*datacollector.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.teamfoundation.testplatform.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.visualstudio.testplatform.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.visualstudio.testwindow.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.visualstudio.mstest.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.visualstudio.qualitytools.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.vssdk.testhostadapter.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.vssdk.testhostframework.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*qtagent32.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*msvcr.*dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*msvcp.*dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*clr.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*clr.ni.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*clrjit.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*clrjit.ni.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscoree.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscoreei.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscoreei.ni.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscorlib.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscorlib.ni.dll$</ModulePath>" + Environment.NewLine +
@"               </Exclude>" + Environment.NewLine +
@"            </ModulePaths>" + Environment.NewLine +
@"            <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>" + Environment.NewLine +
@"            <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>" + Environment.NewLine +
@"            <CollectFromChildProcesses>True</CollectFromChildProcesses>" + Environment.NewLine +
@"            <CollectAspDotNet>false</CollectAspDotNet>" + Environment.NewLine +
@"            <SymbolSearchPaths />" + Environment.NewLine +
@"            <Functions>" + Environment.NewLine +
@"              <Exclude>" + Environment.NewLine +
@"                <Function>^std::.*</Function>" + Environment.NewLine +
@"                <Function>^ATL::.*</Function>" + Environment.NewLine +
@"                <Function>.*::__GetTestMethodInfo.*</Function>" + Environment.NewLine +
@"                <Function>.*__CxxPureMSILEntry.*</Function>" + Environment.NewLine +
@"                <Function>^Microsoft::VisualStudio::CppCodeCoverageFramework::.*</Function>" + Environment.NewLine +
@"                <Function>^Microsoft::VisualStudio::CppUnitTestFramework::.*</Function>" + Environment.NewLine +
@"                <Function>.*::YOU_CAN_ONLY_DESIGNATE_ONE_.*</Function>" + Environment.NewLine +
@"                <Function>^__.*</Function>" + Environment.NewLine +
@"                <Function>.*::__.*</Function>" + Environment.NewLine +
@"              </Exclude>" + Environment.NewLine +
@"            </Functions>" + Environment.NewLine +
@"            <Attributes>" + Environment.NewLine +
@"              <Exclude>" + Environment.NewLine +
@"                <Attribute>^System.Diagnostics.DebuggerHidden.*</Attribute>" + Environment.NewLine +
@"                <Attribute>^System.Diagnostics.DebuggerNonUserCode.*</Attribute>" + Environment.NewLine +
@"                <Attribute>^System.Runtime.CompilerServices.CompilerGenerated.*</Attribute>" + Environment.NewLine +
@"                <Attribute>^System.CodeDom.Compiler.GeneratedCode.*</Attribute>" + Environment.NewLine +
@"                <Attribute>^System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage.*</Attribute>" + Environment.NewLine +
@"              </Exclude>" + Environment.NewLine +
@"            </Attributes>" + Environment.NewLine +
@"            <Sources>" + Environment.NewLine +
@"              <Exclude>" + Environment.NewLine +
@"                <Source>.*\\atlmfc\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\vctools\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\public\\sdk\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\externalapis\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\microsoft sdks\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\vc\\include\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\msclr\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\ucrt\\.*</Source>" + Environment.NewLine +
@"              </Exclude>" + Environment.NewLine +
@"            </Sources>" + Environment.NewLine +
@"            <CompanyNames/>" + Environment.NewLine +
@"            <PublicKeyTokens/>" + Environment.NewLine +
@"          </CodeCoverage>" + Environment.NewLine +
@"        </Configuration>";

        /// <summary>
        /// Coverage file name
        /// </summary>
        private string coverageFileName;

        /// <summary>
        /// Vanguard object
        /// </summary>
        private Vanguard vanguard;

        /// <summary>
        /// Logger
        /// </summary>
        private IDataCollectionLogger logger;

        /// <summary>
        /// Data sink
        /// </summary>
        private Microsoft.VisualStudio.TraceCollector.IDataCollectionSink dataSink;

/*#if !NETSTANDARD
        /// <summary>
        /// IIS injector
        /// </summary>
        private IISEnvironmentInjector injector;
#endif*/

        /// <summary>
        /// Whether it's running a manual test
        /// </summary>
        private bool isManualTest;

        /// <summary>
        /// Whether the collector is running on a remote machine (environment flow)
        /// </summary>
        private bool isExecutedRemotely;

/*#if !NETSTANDARD
        /// <summary>
        /// True if only no intellitrace based collector other than code coverage is enabled
        /// </summary>
        private bool onlyVanguardCollectorEnabledForRemoteRole;

        /// <summary>
        /// Indicates whether collection plan has been populated atleast once
        /// </summary>
        private bool collectionPlanPopulated;
#endif*/

        /// <summary>
        /// Whether to collect ASP.Net
        /// </summary>
        protected bool collectAspDotNet;

        /// <summary>
        /// Folder to store temporary files
        /// </summary>
        private string tempPath;

        /// <summary>
        /// Session name
        /// </summary>
        public string sessionName
        {
            get;
            private set;
        }

        /// <summary>
        /// Cached configuration data
        /// </summary>
        private XmlElement configurationElement;

        /// <summary>
        /// Vanguard instance
        /// </summary>
        protected Vanguard Vanguard
        {
            get
            {
                return this.vanguard;
            }
        }

/*#if !NETSTANDARD
        /// <summary>
        /// True if only no intellitrace based collector other than code coverage is enabled
        /// </summary>
        internal bool OnlyVanguardEnabledForRemoteRole
        {
            get
            {
                return this.onlyVanguardCollectorEnabledForRemoteRole;
            }
        }
#endif*/

        /// <summary>
        /// Whether to collect ASP.Net
        /// </summary>
        internal bool CollectAspDotNet
        {
            get
            {
                return this.collectAspDotNet;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isManualTest">Whether it's running a manual test</param>
        /// <param name="isExecutedRemotely">Whether it's running inside a remote device</param>
        public DynamicCoverageDataCollectorImpl(bool isManualTest, bool isExecutedRemotely)
        {
            this.isManualTest = isManualTest;
            this.isExecutedRemotely = isExecutedRemotely;
        }

        /// <summary>
        /// Create a data collector implementation instance
        /// </summary>
        /// <param name="context">Context</param>
        /// <returns>Data collector implementation</returns>
        public static DynamicCoverageDataCollectorImpl Create(IDataCollectionAgentContext context)
        {
            return new UnitTestDataCollector(false, false);
        }

        /// <summary>
        /// Start vanguard
        /// </summary>
        /// <param name="context">Context</param>
        protected void StartVanguard(DataCollectionContext context)
        {
            if (this.vanguard != null)
            {
                if (this.isManualTest && !this.collectAspDotNet)
                {
                    // We will not check the number of entry points in the following cases (under these cases entry point is not necessary):
                    // 1. It's not a manual test run (in IDE or automated run in MTM).
                    // 2. Collect from ASP.Net is enabled.
                    if (this.vanguard.EntryPoints.Count == 0)
                    {
                        throw new VanguardException(Resources.ErrorNoEntryPoint);
                    }
                }

                string folder = Path.Combine(tempPath, Guid.NewGuid().ToString());
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (IOException)
                {
                    this.logger.LogWarning(context, Resources.GeneralErrorLaunchVanguard);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    this.logger.LogWarning(context, Resources.GeneralErrorLaunchVanguard);
                    return;
                }

                string outputName = Path.Combine(folder, coverageFileName);
                try
                {
                    this.vanguard.Start(outputName, context);
                }
                catch (VanguardException ex)
                {
                    if (ex.IsCritical)
                    {
                        this.logger.LogError(context, ex.Message);
                        throw;
                    }
                    else
                    {
                        this.logger.LogWarning(context, ex.Message);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Stop vanguard
        /// </summary>
        /// <param name="context">Context</param>
        protected void StopVanguard(DataCollectionContext context)
        {
            if (this.vanguard != null)
            {
                this.vanguard.Stop();

                if (File.Exists(this.vanguard.OutputName))
                {
                    this.dataSink.SendFileAsync(context, this.vanguard.OutputName, false);
                }

                vanguard = null;
            }
        }

        /// <summary>
        /// Sends intermediate coverage data to the client
        /// </summary>
        /// <param name="context"></param>
        protected void GetCoverageData(DataCollectionContext context)
        {
            if (this.vanguard != null)
            {
                string outputName = null;

                try
                {
                    // Create unique path for the output file
                    string folder = Path.Combine(tempPath, Guid.NewGuid().ToString());

                    Directory.CreateDirectory(folder);

                    outputName = Path.Combine(folder, GenerateCoverageFileName());

                    EqtTrace.Verbose("Calling GetCoverageData with output path {0}", outputName);

                    this.vanguard.GetCoverageData(outputName);
                }
                catch (IOException ex)
                {
                    this.logger.LogWarning(context, ex.Message);
                    return;
                }
                catch (UnauthorizedAccessException ex)
                {
                    this.logger.LogWarning(context, ex.Message);
                    return;
                }
                catch (VanguardException ex)
                {
                    // We do not want to throw errors from here because that will cause the collector to get disposed
                    this.logger.LogWarning(context, ex.Message);
                    return;
                }

                if (File.Exists(outputName))
                {
                    this.dataSink.SendFileAsync(context, outputName, false);
                }
            }

        }

        protected void OnSendFileCompletedEvent(object sender, AsyncCompletedEventArgs e)
        {
            // Collector can send multiple files for an IIS session. Delete the directory only if we
            // are not collecting CC for asp.net
            if (!this.collectAspDotNet)
            {
                CleanupDirectory();
            }
        }

        /// <summary>
        /// Session start
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        internal virtual void SessionStart(object sender, SessionStartEventArgs e)
        {
        }

        /// <summary>
        /// Session end
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        internal virtual void SessionEnd(object sender, SessionEndEventArgs e)
        {
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <param name="plan">Collection plan</param>
        /// <param name="dataSink">Data sink</param>
        /// <param name="logger">Logger</param>
        /// <param name="isFirstCollectorToInitalize">Whether it is the first collector to get initialized</param>        
/* #if !NETSTANDARD
        public virtual void Initialize(XmlElement configurationElement, ConfigMessagePacker._CollectionPlan plan, Microsoft.VisualStudio.TraceCollector.IDataCollectionSink dataSink, IDataCollectionLogger logger, bool isFirstCollectorToInitalize)
#else */
        public virtual void Initialize(XmlElement configurationElement, Microsoft.VisualStudio.TraceCollector.IDataCollectionSink dataSink, IDataCollectionLogger logger)
// #endif
        {
            if (configurationElement == null || string.IsNullOrEmpty(configurationElement.InnerXml))
            {
                //  Add default configuration specific to CodeCoverage.
                var doc = new XmlDocument();
                using (
                    var xmlReader = XmlReader.Create(
                        new StringReader(DefaultCodeCoverageSettings),
                        new XmlReaderSettings() { /*XmlResolver = null,*/ CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                {
                    doc.Load(xmlReader);
                }

                configurationElement = doc.DocumentElement;
            }

            this.configurationElement = configurationElement;
            this.logger = logger;
            this.dataSink = dataSink;

            this.dataSink.SendFileCompleted += OnSendFileCompletedEvent;

            // the magic prefix is used by UnregisterAll to tell MTM registered from manually registered entries
            sessionName = MagicMtmSessionPrefix + Guid.NewGuid().ToString();
            tempPath = Path.Combine(Path.GetTempPath(), sessionName);
            if (!CreateSessionDirectory())
                return;

            PrepareVanguardProcess();

/*#if !NETSTANDARD
            // For manual testing on test agent if Vanguard is the only collector
            // then we do not want to use the Intellitrace flow as it requires test
            // agent to run as elevated process.            
            if (isFirstCollectorToInitalize
                && this.collectAspDotNet
                && isExecutedRemotely
                && isManualTest)
            {
                this.onlyVanguardCollectorEnabledForRemoteRole = true;

                if (plan.collectorInfo == null)
                {
                    plan.collectorInfo = new ConfigMessagePacker._CollectorConfiguration(true);
                }

                plan.collectorInfo.instrumentIIS = plan.collectorInfo.remoteInstrumentIIS = false;
            }
            else
            {
                this.onlyVanguardCollectorEnabledForRemoteRole = false;
                PopulateCollectionPlan(plan, sessionName, this.collectAspDotNet);
            }
#endif*/
        }

/*#if !NETSTANDARD
        /// <summary>
        /// Adds information about code coverage in the intellitrace collection plan
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        internal ConfigMessagePacker._CollectionPlan UpdateCollectionPlanWithVanguardInfo(ConfigMessagePacker._CollectionPlan plan)
        {
            if (!collectionPlanPopulated)
            {
                PopulateCollectionPlan(plan, this.sessionName, this.collectAspDotNet);
                this.onlyVanguardCollectorEnabledForRemoteRole = false;
            }

            return plan;
        }
#endif*/

        internal void InitializeBeforeSessionStart()
        {
            if (!CreateSessionDirectory())
                return;

            PrepareVanguardProcess();
        }

        private bool CreateSessionDirectory()
        {
            try
            {
                Directory.CreateDirectory(tempPath);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generate the file name for coverage file.
        /// Copied from vset\QTools\Vanguard\src\AnalyzeCodeCoverageExtension\Settings\CodeCoverageDataCollectorSettingsService.cs
        /// </summary>
        /// <returns></returns>
        private static string GenerateCoverageFileName()
        {
            string GetUserName()
            {
                return Environment.GetEnvironmentVariable("USERNAME") ?? Environment.GetEnvironmentVariable("USER");
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}.coverage", GetUserName(), Environment.MachineName, DateTime.Now.ToString("yyyy-MM-dd.HH_mm_ss", CultureInfo.InvariantCulture));
        }

        private void PrepareVanguardProcess()
        {
            string configurationFileName = Path.Combine(tempPath, VanguardConfigFileName);
            XmlElement coverageFileNameElement = configurationElement[CoverageFileSettingName];
            this.coverageFileName = coverageFileNameElement != null ? coverageFileNameElement.InnerText : GenerateCoverageFileName();
            XmlElement config = configurationElement[RootName];
            DynamicCoverageModuleSettings settings = new DynamicCoverageModuleSettings(config, false);
            this.collectAspDotNet = settings.CollectAspDotNet;
            List<string> entryPoints = new List<string>();
            XmlElement entryPointNodes = config[DynamicCoverageModuleSettings.EntryPointListName];
            if (entryPointNodes != null)
            {
                foreach (XmlNode entryPoint in entryPointNodes)
                {
                    if (!string.IsNullOrEmpty(entryPoint.InnerText))
                    {
                        entryPoints.Add(entryPoint.InnerText);
                    }
                }
            }

            this.vanguard = new Vanguard(sessionName, configurationFileName, config, entryPoints, logger);
        }

        /// <summary>
        /// Initialize vanguard configuration
        /// </summary>
        /// <param name="injector">IIS injector</param>
/*#if !NETSTANDARD
        internal virtual void InitializeConfiguration(IISEnvironmentInjector injector)
#else*/
        internal virtual void InitializeConfiguration()
// #endif
        {
/*#if !NETSTANDARD
            this.Vanguard.InitializeConfiguration(injector != null ? injector.Users : null);
            this.injector = injector;
#else*/
            this.Vanguard.InitializeConfiguration();
// #endif
        }

/*#if !NETSTANDARD
        /// <summary>
        /// Generate the collection plan for vanguard
        /// </summary>
        /// <param name="plan">Collect plan</param>
        /// <param name="sessionName">Session name</param>
        /// <param name="collectAspDotNet">Whether to colect ASP.Net</param>
        private void PopulateCollectionPlan(ConfigMessagePacker._CollectionPlan plan, string sessionName, bool collectAspDotNet)
        {
            plan.dynamicCodeCoverageInstrumentation = new ConfigMessagePacker._DynamicCodeCoverageInstrumentation(true);
            plan.dynamicCodeCoverageInstrumentation.enabled = true;

            // The process list is required for any instrumentation method. We don't support process filter, so return an empty exclusion list to enable all processes.
            plan.dynamicCodeCoverageInstrumentation.processList = new ConfigMessagePacker._NameList(true);
            plan.dynamicCodeCoverageInstrumentation.sessionName = sessionName;

            if (plan.collectorInfo == null)
            {
                plan.collectorInfo = new ConfigMessagePacker._CollectorConfiguration(true);
            }

            plan.collectorInfo.instrumentIIS = plan.collectorInfo.remoteInstrumentIIS = collectAspDotNet;
            collectionPlanPopulated = true;
        }
#endif*/

        /// <summary>
        /// Cleanup temp folder
        /// </summary>
        public virtual void Dispose()
        {
            // Make sure vanguard is shut down
            if (this.vanguard != null)
            {
                this.vanguard.Stop();
                this.vanguard.Dispose();
            }

            if (dataSink != null)
            {
                dataSink.SendFileCompleted -= OnSendFileCompletedEvent;
            }

            CleanupDirectory();
        }

        private void CleanupDirectory()
        {
            try
            {
                Directory.Delete(this.tempPath, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
