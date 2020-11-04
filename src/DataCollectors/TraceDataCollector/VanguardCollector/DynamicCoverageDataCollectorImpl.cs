// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using TestPlatform.ObjectModel;
    using TraceCollector;
    using TraceCollector.Interfaces;
    using TraceDataCollector.Resources;
    using IDataCollectionSink = TraceCollector.IDataCollectionSink;

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
        /// File name which contains config for vanguard process.
        /// </summary>
        private const string VanguardConfigFileName = "CodeCoverage.config";

        /// <summary>
        /// Name of element for custom coverage filename.
        /// </summary>
        private const string CoverageFileSettingName = "CoverageFileName";

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
        /// Gets or sets session name
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
            IDataCollectionSink dataSink,
            IDataCollectionLogger logger)
        {
            var defaultConfigurationElement = DynamicCoverageDataCollectorImpl.GetDefaultConfiguration();

            try
            {
                // WARNING: Do NOT remove this function call !!!
                //
                // Due to a dependency we took on Microsoft.TestPlatform.Utilities.dll, an
                // exception may be thrown if we cannot resolve CodeCoverageRunSettingsProcessor.
                // If such an exception is thrown we cannot catch it in this try-catch block
                // because all method dependencies must be resolved before the method call, thus
                // we introduced an additional layer of indirection.
                configurationElement = this.AddDefaultExclusions(configurationElement, defaultConfigurationElement);
            }
            catch (Exception ex)
            {
                EqtTrace.Warning(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        string.Join(
                            " ",
                            "DynamicCoverageDataCollectorImpl.Initialize: Exception encountered while processing the configuration element.",
                            "Keeping the configuration element unaltered. More info about the exception: {0}"),
                        ex.Message));
            }

            EqtTrace.Info("DynamicCoverageDataCollectorImpl.Initialize: Initialize configuration. ");
            if (string.IsNullOrEmpty(configurationElement?.InnerXml))
            {
                configurationElement = defaultConfigurationElement;
            }

            this.logger = logger;
            this.dataSink = dataSink;

            this.dataSink.SendFileCompleted += this.OnSendFileCompletedEvent;

            this.SessionName = Guid.NewGuid().ToString();

            this.sessionDirectory = Path.Combine(Path.GetTempPath(), this.SessionName);
            this.directoryHelper.CreateDirectory(this.sessionDirectory);

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

        private static XmlElement GetDefaultConfiguration()
        {
            // Add default configuration specific to CodeCoverage. https://msdn.microsoft.com/en-us/library/jj635153.aspx
            var doc = new XmlDocument();
            Assembly a = typeof(DynamicCoverageDataCollectorImpl).GetTypeInfo().Assembly;
            using (Stream s = a.GetManifestResourceStream(
                "Microsoft.VisualStudio.TraceDataCollector.VanguardCollector.DefaultCodeCoverageConfig.xml"))
            {
                doc.Load(s);
            }

            return doc.DocumentElement;
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

            XmlElement config = configurationElement[ConfigCodeCoverageElementName]
                                ?? DynamicCoverageDataCollectorImpl.GetDefaultConfiguration()[ConfigCodeCoverageElementName];

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
                this.logger.LogError(
                    context,
                    string.Format(CultureInfo.CurrentUICulture, Resources.FailedToCreateDirectory, path, ex));
                throw;
            }
        }

        /// <summary>
        /// Adding default exclusions to the configuration element.
        /// </summary>
        /// <param name="configurationElement">The configuration element.</param>
        /// <param name="defaultConfigurationElement">The default configuration element.</param>
        /// <returns>The original configuration element with additional default exclusions.</returns>
        private XmlElement AddDefaultExclusions(XmlElement configurationElement, XmlElement defaultConfigurationElement)
        {
            var processor = new CodeCoverageRunSettingsProcessor(defaultConfigurationElement);
            return (XmlElement)processor.Process(configurationElement);
        }
    }
}