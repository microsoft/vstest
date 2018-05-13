// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using TestPlatform.ObjectModel;
    using TraceCollector;
    using TraceDataCollector.Resources;

    /// <summary>
    /// Create config file and output directory required for vanguard process and manages life cycle of vanguard process.
    /// </summary>
    internal class DynamicCoverageDataCollectorImpl : IDynamicCoverageDataCollectorImpl
    {
        /// <summary>
        ///  Name of elements under which all the config details required for vanguard process.
        /// </summary>
        private const string ConfigCodeCoverageElementName = "CodeCoverage";

        /// <summary>
        /// File name which conatins config for vanguard process.
        /// </summary>
        private const string VanguardConfigFileName = "CodeCoverage.config";

        /// <summary>
        /// Name of element for custom coverage filename.
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
        private string sessionDirectory;

        private string coverageFilePath;

        public DynamicCoverageDataCollectorImpl()
        : this(new Vanguard(), new DirectoryHelper(), new FileHelper())
        {
        }

        internal DynamicCoverageDataCollectorImpl(IVanguard vanguard, IDirectoryHelper directoryHelper, IFileHelper fileHelper)
        {
            this.Vanguard = vanguard;
            this.directoryHelper = directoryHelper;
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Gets or sets gets session name
        /// </summary>
        private string SessionName { get; set; }

        /// <summary>
        /// Gets or sets vanguard instance
        /// </summary>
        private IVanguard Vanguard { get; set; }

        public string GetSessionName()
        {
            return this.SessionName;
        }

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

            this.logger = logger;
            this.dataSink = dataSink;

            this.dataSink.SendFileCompleted += this.OnSendFileCompletedEvent;

            this.SessionName = Guid.NewGuid().ToString();

            this.sessionDirectory = Path.Combine(Path.GetTempPath(), this.SessionName);
            this.CreateDirectory(null, this.sessionDirectory);

            this.SetCoverageFileName(configurationElement);

            this.PrepareVanguardProcess(configurationElement);
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
        public void SessionStart(object sender, SessionStartEventArgs e)
        {
            this.StartVanguard(e.Context);
        }

        /// <summary>
        /// Session end
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        public void SessionEnd(object sender, SessionEndEventArgs e)
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
                string outputCoverageFolder = Path.Combine(this.sessionDirectory, Guid.NewGuid().ToString());
                this.CreateDirectory(context, outputCoverageFolder);

                this.coverageFilePath = Path.Combine(outputCoverageFolder, this.coverageFileName);
                try
                {
                    this.Vanguard.Start(this.coverageFilePath, context);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error(
                        "DynamicCoverageDataCollectorImpl.StartVanguard: Failed to start Vanguard for datacollection context sessionID: {0}, with exception: {1}",
                        context.SessionId,
                        ex);
                    this.logger.LogError(context, ex);
                    throw;
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

                if (this.fileHelper.Exists(this.coverageFilePath))
                {
                    this.dataSink.SendFileAsync(context, this.coverageFilePath, false);
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

        private void SetCoverageFileName(XmlElement configurationElement)
        {
            XmlElement coverageFileNameElement = configurationElement[CoverageFileSettingName];
            this.coverageFileName = coverageFileNameElement != null
                ? coverageFileNameElement.InnerText
                : GenerateCoverageFileName();
        }

        private void PrepareVanguardProcess(XmlElement configurationElement)
        {
            EqtTrace.Info("DynamicCoverageDataCollectorImpl.PrepareVanguardProcess: Preparing Vanguard process.");

            XmlElement config = configurationElement[ConfigCodeCoverageElementName];
            string configurationFileName = Path.Combine(this.sessionDirectory, VanguardConfigFileName);

            this.fileHelper.WriteAllText(configurationFileName, config.OuterXml);

            EqtTrace.Info("DynamicCoverageDataCollectorImpl.PrepareVanguardProcess: Initializing  with config: {0}.", config.OuterXml);
            this.Vanguard.Initialize(this.SessionName, configurationFileName, this.logger);
        }

        private void CleanupDirectory()
        {
            try
            {
                if (this.directoryHelper.Exists(this.sessionDirectory))
                {
                    this.directoryHelper.Delete(this.sessionDirectory, true);
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("DynamicCoverageDataCollectorImpl.CleanupDirectory:Failed to delete directory: {0}, with exception: {1}", this.sessionDirectory, ex);
            }
        }

        private void CreateDirectory(DataCollectionContext context, string path)
        {
            try
            {
                this.directoryHelper.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("DynamicCoverageDataCollectorImpl.CreateDirectory:Failed to create directory: {0}, with exception: {1}", path, ex);

                if (context != null)
                {
                    this.logger.LogError(
                        context,
                        string.Format(CultureInfo.CurrentUICulture, Resources.FailedToCreateDirectory, path, ex));
                }

                throw;
            }
        }
    }
}