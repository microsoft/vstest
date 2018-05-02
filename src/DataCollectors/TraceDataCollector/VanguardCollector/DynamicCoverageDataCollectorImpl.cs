// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Collector;
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using TestPlatform.ObjectModel;
    using TraceCollector;
    using TraceDataCollector.Resources;

    /// <summary>
    /// Implementation class of dynamic code coverage data collector
    /// </summary>
    internal class DynamicCoverageDataCollectorImpl
    {
        /// <summary>
        /// A magic prefix used to differentiate MTM registered sessions from manually registered sessions
        /// </summary>
        public const string MagicMtmSessionPrefix = "MTM_";

        internal const string RootName = "CodeCoverage";

        /// <summary>
        /// File name for vanguard config file
        /// </summary>
        private const string VanguardConfigFileName = "CodeCoverage.config";

        /// <summary>
        /// The setting name for coverage file name
        /// </summary>
        private const string CoverageFileSettingName = "CoverageFileName";

        // Default CodeCoverage Configuration as specified here : https://msdn.microsoft.com/en-us/library/jj635153.aspx
        private static readonly string DefaultCodeCoverageSettings =
            @"        <Configuration>" + Environment.NewLine +
            @"          <CodeCoverage>" + Environment.NewLine +
            @"            <ModulePaths>" + Environment.NewLine +
            @"              <Exclude>" + Environment.NewLine +
            @"                 <ModulePath>.*CPPUnitTestFramework.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*vstest.console.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.intellitrace.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*testhost.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*datacollector.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.teamfoundation.testplatform.*</ModulePath>" +
            Environment.NewLine +
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
            @"                <Function>^Microsoft::VisualStudio::CppCodeCoverageFramework::.*</Function>" +
            Environment.NewLine +
            @"                <Function>^Microsoft::VisualStudio::CppUnitTestFramework::.*</Function>" +
            Environment.NewLine +
            @"                <Function>.*::YOU_CAN_ONLY_DESIGNATE_ONE_.*</Function>" + Environment.NewLine +
            @"                <Function>^__.*</Function>" + Environment.NewLine +
            @"                <Function>.*::__.*</Function>" + Environment.NewLine +
            @"              </Exclude>" + Environment.NewLine +
            @"            </Functions>" + Environment.NewLine +
            @"            <Attributes>" + Environment.NewLine +
            @"              <Exclude>" + Environment.NewLine +
            @"                <Attribute>^System.Diagnostics.DebuggerHidden.*</Attribute>" + Environment.NewLine +
            @"                <Attribute>^System.Diagnostics.DebuggerNonUserCode.*</Attribute>" + Environment.NewLine +
            @"                <Attribute>^System.Runtime.CompilerServices.CompilerGenerated.*</Attribute>" +
            Environment.NewLine +
            @"                <Attribute>^System.CodeDom.Compiler.GeneratedCode.*</Attribute>" + Environment.NewLine +
            @"                <Attribute>^System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage.*</Attribute>" +
            Environment.NewLine +
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
        /// Logger
        /// </summary>
        private IDataCollectionLogger logger;

        /// <summary>
        /// Data sink
        /// </summary>
        private TraceCollector.IDataCollectionSink dataSink;

        /// <summary>
        /// Directory helper
        /// </summary>
        private IDirectoryHelper directoryHelper;

        /// <summary>
        /// File helper
        /// </summary>
        private IFileHelper fileHelper;

        /// <summary>
        /// Folder to store temporary files
        /// </summary>
        private string tempPath;

        /// <summary>
        /// Cached configuration data
        /// </summary>
        private XmlElement configurationElement;

        public DynamicCoverageDataCollectorImpl()
        : this(new Vanguard(), new DirectoryHelper(), new FileHelper())
        {
        }

        internal DynamicCoverageDataCollectorImpl(IVangurd vanguard, IDirectoryHelper directoryHelper, IFileHelper fileHelper)
        {
            this.Vanguard = vanguard;
            this.directoryHelper = directoryHelper;
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Gets session name
        /// </summary>
        public string SessionName { get; private set; }

        /// <summary>
        /// Gets vanguard instance
        /// </summary>
        protected IVangurd Vanguard { get; private set; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <param name="dataSink">Data sink</param>
        /// <param name="logger">Logger</param>
        public virtual void Initialize(
            XmlElement configurationElement,
            TraceCollector.IDataCollectionSink dataSink,
            IDataCollectionLogger logger)
        {
            EqtTrace.Info("DynamicCoverageDataCollectorImpl.Initialize: Initialize configuration. ");
            if (string.IsNullOrEmpty(configurationElement?.InnerXml))
            {
                // Add default configuration specific to CodeCoverage.
                var doc = new XmlDocument();
                using (
                    var xmlReader = XmlReader.Create(
                        new StringReader(DefaultCodeCoverageSettings),
                        new XmlReaderSettings() { CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                {
                    doc.Load(xmlReader);
                }

                configurationElement = doc.DocumentElement;
            }

            this.configurationElement = configurationElement;
            this.logger = logger;
            this.dataSink = dataSink;

            this.dataSink.SendFileCompleted += this.OnSendFileCompletedEvent;

            // the magic prefix is used by UnregisterAll to tell MTM registered from manually registered entries
            this.SessionName = MagicMtmSessionPrefix + Guid.NewGuid();

            this.tempPath = Path.Combine(Path.GetTempPath(), this.SessionName);
            this.directoryHelper.CreateDirectory(this.tempPath);

            this.SetCoverageFileName();

            this.PrepareVanguardProcess();
        }

        /// <summary>
        /// Cleanup temp folder
        /// </summary>
        public virtual void Dispose()
        {
            if (this.Vanguard != null)
            {
                this.Vanguard.Stop();
                this.Vanguard.Dispose();
            }

            if (this.dataSink != null)
            {
                this.dataSink.SendFileCompleted -= this.OnSendFileCompletedEvent;
            }

            this.CleanupDirectory();
        }

        /// <summary>
        /// Session start
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        internal virtual void SessionStart(object sender, SessionStartEventArgs e)
        {
            this.StartVanguard(e.Context);
        }

        /// <summary>
        /// Session end
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        internal virtual void SessionEnd(object sender, SessionEndEventArgs e)
        {
            this.StopVanguard(e.Context);
        }

        /// <summary>
        /// Start vanguard
        /// </summary>
        /// <param name="context">Context</param>
        protected void StartVanguard(DataCollectionContext context)
        {
            if (this.Vanguard != null)
            {
                string folder = Path.Combine(this.tempPath, Guid.NewGuid().ToString());
                try
                {
                    this.directoryHelper.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(
                        context,
                        string.Format(CultureInfo.CurrentUICulture, Resources.FailedToCreateDirectory, folder, ex));

                    throw;
                }

                string outputCoverageFilePath = Path.Combine(folder, this.coverageFileName);
                try
                {
                    this.Vanguard.Start(outputCoverageFilePath, context);
                }
                catch (VanguardException ex)
                {
                    if (ex.IsCritical)
                    {
                        this.logger.LogError(context, ex);
                        throw;
                    }

                    this.logger.LogWarning(context, ex.Message);
                }
            }
        }

        /// <summary>
        /// Stop vanguard
        /// </summary>
        /// <param name="context">Context</param>
        protected void StopVanguard(DataCollectionContext context)
        {
            EqtTrace.Info("DynamicCoverageDataCollectorImpl.StopVanguard: Calling Stop Vanguard. datacollection context sessionID: {0}", context.SessionId);
            if (this.Vanguard != null)
            {
                this.Vanguard.Stop();

                if (this.fileHelper.Exists(this.Vanguard.OutputName))
                {
                    this.dataSink.SendFileAsync(context, this.Vanguard.OutputName, false);
                }

                this.Vanguard = null;
            }
        }

        protected void OnSendFileCompletedEvent(object sender, AsyncCompletedEventArgs e)
        {
             this.CleanupDirectory();
        }

        /// <summary>
        /// Generate the file name for coverage file.
        /// </summary>
        /// <returns>Returns code coverage file name.</returns>
        private static string GenerateCoverageFileName()
        {
            string GetUserName()
            {
                return Environment.GetEnvironmentVariable("USERNAME") ?? Environment.GetEnvironmentVariable("USER");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}_{2}.coverage",
                GetUserName(),
                Environment.MachineName,
                DateTime.Now.ToString("yyyy-MM-dd.HH_mm_ss", CultureInfo.InvariantCulture));
        }

        private void SetCoverageFileName()
        {
            XmlElement coverageFileNameElement = this.configurationElement[CoverageFileSettingName];
            this.coverageFileName = coverageFileNameElement != null
                ? coverageFileNameElement.InnerText
                : GenerateCoverageFileName();
        }

        private void PrepareVanguardProcess()
        {
            EqtTrace.Info("DynamicCoverageDataCollectorImpl.PrepareVanguardProcess: Preparing Vanguard process.");

            XmlElement config = this.configurationElement[RootName];
            string configurationFileName = Path.Combine(this.tempPath, VanguardConfigFileName);

            this.Vanguard.Initialize(this.SessionName, configurationFileName, config, this.logger);
        }

        private void CleanupDirectory()
        {
            try
            {
                this.directoryHelper.Delete(this.tempPath, true);
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("DynamicCoverageDataCollectorImpl.CleanupDirectory:Failed to delete directory: {0}, with exception: {1}", this.tempPath, ex);
            }
        }
    }
}