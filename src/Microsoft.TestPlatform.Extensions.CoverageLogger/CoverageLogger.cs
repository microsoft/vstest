// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.CoverageLogger
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Xml;
    using Microsoft.VisualStudio.Setup.Interop;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CodeCoverageLoggerResources = Microsoft.TestPlatform.Extensions.Coverage.TestLogger.Resources.CoverageResource;

    /// <summary>
    /// Logger for Generating CodeCoverage Analysis
    /// </summary>
    [FriendlyName(FriendlyName)]
    [ExtensionUri(CoverageUri)]
    internal class CoverageLogger : ITestLogger
    {
        #region Constants

        private const string CoverageUri = "datacollector://microsoft/CodeCoverage/2.0";

        private const string FriendlyName = "CoverageLogger";

        private const string CodeCoverageExeRelativePath = @"Team Tools\Dynamic Code Coverage Tools\CodeCoverage.exe";

        private static readonly Uri CodeCoverageDataCollectorUri = new Uri(CoverageUri);

        private Process vanguardProcess;

        private ManualResetEvent coverageXmlGenerateEvent;

        #endregion

        public static XmlReaderSettings ReaderSettings => new XmlReaderSettings
                                                              {
                                                                  IgnoreComments = true,
                                                                  IgnoreWhitespace = true,
                                                                  DtdProcessing = DtdProcessing
                                                                      .Prohibit
                                                              };

        #region ITestLogger

        /// <inheritdoc/>
        public void Initialize(TestLoggerEvents events, string testResultsDirPath)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            // Register for the events.
            events.TestRunComplete += this.TestRunCompleteHandler;

            this.coverageXmlGenerateEvent = new ManualResetEvent(false);
        }
        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Test run complete events arguments.
        /// </param>
        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            if (e.AttachmentSets == null)
            {
                return;
            }

            var coverageAttachments = e.AttachmentSets
                .Where(dataCollectionAttachment => CodeCoverageDataCollectorUri.Equals(dataCollectionAttachment.Uri)).ToArray();

            if (coverageAttachments.Any())
            {
                var codeCoverageFiles = coverageAttachments.Select(coverageAttachment => coverageAttachment.Attachments[0].Uri.LocalPath).ToArray();

                foreach (var codeCoverageFile in codeCoverageFiles)
                {
                    var resultFile = Path.Combine(Path.GetDirectoryName(codeCoverageFile), Path.GetFileNameWithoutExtension(codeCoverageFile) + ".xml");
                    var arguments = "analyze /output:" + '"' + resultFile + '"' + " " + '"' + codeCoverageFile + '"';

                    this.AnalyzeCoverageFile(arguments);

                    if (this.coverageXmlGenerateEvent.WaitOne(10000))
                    {
                        ConsoleOutput.Instance.Information(resultFile);

                        this.ProcessCoverageXml(resultFile);
                    }
                    else
                    {
                        EqtTrace.Error("CodeCoverage.exe could not process file within 10 secs");
                        if (!this.vanguardProcess.HasExited)
                        {
                            this.vanguardProcess.Kill();
                        }
                    }

                    this.vanguardProcess.Dispose();
                }
            }
        }

        private void ProcessCoverageXml(string coverageXml)
        {
            using (XmlReader reader = XmlReader.Create(coverageXml, ReaderSettings))
            {
                var settingsDocument = new XmlDocument();
                settingsDocument.Load(reader);

                var modules = settingsDocument.GetElementsByTagName("module");

                if (modules.Count > 0)
                {
                    ConsoleOutput.Instance.Information("\n" + string.Format(CultureInfo.CurrentCulture, CodeCoverageLoggerResources.CodeCoverageHeader));
                    ConsoleOutput.Instance.Information("----------------");

                    var header = string.Concat(
                        string.Format(CultureInfo.CurrentCulture, CodeCoverageLoggerResources.ModuleName),
                        "\t\t\t",
                        string.Format(CultureInfo.CurrentCulture, CodeCoverageLoggerResources.NotBlocksCovered),
                        "\t\t",
                        string.Format(CultureInfo.CurrentCulture, CodeCoverageLoggerResources.PercNotBlocksCovered),
                        "\t\t",
                        string.Format(CultureInfo.CurrentCulture, CodeCoverageLoggerResources.BlocksCovered),
                        "\t\t",
                        string.Format(CultureInfo.CurrentCulture, CodeCoverageLoggerResources.PercBlocksCovered),
                        "\n");

                    ConsoleOutput.Instance.Information(header);
                }

                foreach (XmlNode moudle in modules)
                {
                    ConsoleOutput.Instance.Information(moudle.Attributes["name"].Value + "\t\t" +
                                                       moudle.Attributes["blocks_not_covered"].Value + "\t\t\t\t\t" +
                                                       (100 - float.Parse(moudle.Attributes["block_coverage"].Value)) + "\t\t\t\t" +
                                                       moudle.Attributes["blocks_covered"].Value + "\t\t\t\t" +
                                                       moudle.Attributes["block_coverage"].Value);
                }
            }
        }

        private void AnalyzeCoverageFile(string arguments)
        {
            try
            {
                string vsInstallPath = this.GetVSInstallPath();
                this.coverageXmlGenerateEvent.Reset();
                string codeCoverageExe = Path.Combine(vsInstallPath, CodeCoverageExeRelativePath);

                if (!File.Exists(codeCoverageExe))
                {
                    throw new FileNotFoundException(codeCoverageExe);
                }

                this.vanguardProcess = new Process
                                           {
                                               StartInfo =
                                                   {
                                                       UseShellExecute = false,
                                                       CreateNoWindow = true,
                                                       FileName = codeCoverageExe,
                                                       Arguments = arguments,
                                                       RedirectStandardError = true
                                                   },
                                               EnableRaisingEvents = true
                                           };

                this.vanguardProcess.Exited += this.CodeCoverageExited;
                this.vanguardProcess.Start();

                this.vanguardProcess.WaitForExit();
            }
            catch (DllNotFoundException ex)
            {
                ConsoleOutput.Instance.Information(ex.Message);
            }
            catch (NullReferenceException ex)
            {
                ConsoleOutput.Instance.Information(ex.Message);
            }
            catch (Exception ex)
            {
                ConsoleOutput.Instance.Information(ex.Message);
            }
        }

        private void CodeCoverageExited(object sender, EventArgs e)
        {
            if (this.vanguardProcess != null)
            {
                if (this.vanguardProcess.HasExited && this.vanguardProcess.ExitCode != 0)
                {
                    EqtTrace.Error(this.vanguardProcess.StandardError.ReadToEnd());
                }

                this.vanguardProcess.Exited -= this.CodeCoverageExited;
            }

            this.coverageXmlGenerateEvent.Set();
        }

        private string GetVSInstallPath()
        {
            var setupInstance = new SetupConfiguration() as ISetupConfiguration;

            IEnumSetupInstances vsInstances = setupInstance?.EnumInstances();

            if (vsInstances != null)
            {
                vsInstances.Next(1, out ISetupInstance currentInstance, out int fetched);
                while (currentInstance != null && fetched == 1)
                {
                    string installRoot = Path.GetFullPath(currentInstance.GetInstallationPath());
                    if (!string.IsNullOrEmpty(installRoot))
                    {
                        return installRoot;
                    }

                    vsInstances.Next(1, out currentInstance, out fetched);
                }
            }

            return string.Empty;
        }

        #endregion
    }
}