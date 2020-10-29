// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// The blame collector.
    /// </summary>
    [DataCollectorFriendlyName("Blame")]
    [DataCollectorTypeUri("datacollector://Microsoft/TestPlatform/Extensions/Blame/v1")]
    public class BlameCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        private const int DefaultInactivityTimeInMinutes = 60;

        private DataCollectionSink dataCollectionSink;
        private DataCollectionEnvironmentContext context;
        private DataCollectionEvents events;
        private DataCollectionLogger logger;
        private IProcessDumpUtility processDumpUtility;
        private List<Guid> testSequence;
        private Dictionary<Guid, BlameTestObject> testObjectDictionary;
        private IBlameReaderWriter blameReaderWriter;
        private IFileHelper fileHelper;
        private XmlElement configurationElement;
        private int testStartCount;
        private int testEndCount;
        private bool collectProcessDumpOnTrigger;
        private bool collectProcessDumpOnTestHostHang;
        private bool collectDumpAlways;
        private bool processFullDumpEnabled;
        private bool inactivityTimerAlreadyFired;
        private string attachmentGuid;
        private IInactivityTimer inactivityTimer;
        private TimeSpan inactivityTimespan = TimeSpan.FromMinutes(DefaultInactivityTimeInMinutes);
        private int testHostProcessId;
        private string targetFramework;
        private List<KeyValuePair<string, string>> environmentVariables = new List<KeyValuePair<string, string>>();
        private bool uploadDumpFiles;
        private string tempDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameCollector"/> class.
        /// Using XmlReaderWriter by default
        /// </summary>
        public BlameCollector()
            : this(new XmlReaderWriter(), new ProcessDumpUtility(), null, new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameCollector"/> class.
        /// </summary>
        /// <param name="blameReaderWriter">
        /// BlameReaderWriter instance.
        /// </param>
        /// <param name="processDumpUtility">
        /// IProcessDumpUtility instance.
        /// </param>
        /// <param name="inactivityTimer">
        /// InactivityTimer instance.
        /// </param>
        /// <param name="fileHelper">
        /// Filehelper instance.
        /// </param>
        internal BlameCollector(
            IBlameReaderWriter blameReaderWriter,
            IProcessDumpUtility processDumpUtility,
            IInactivityTimer inactivityTimer,
            IFileHelper fileHelper)
        {
            this.blameReaderWriter = blameReaderWriter;
            this.processDumpUtility = processDumpUtility;
            this.inactivityTimer = inactivityTimer;
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Gets environment variables that should be set in the test execution environment
        /// </summary>
        /// <returns>Environment variables that should be set in the test execution environment</returns>
        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return this.environmentVariables;
        }

        /// <summary>
        /// Initializes parameters for the new instance of the class <see cref="BlameDataCollector"/>
        /// </summary>
        /// <param name="configurationElement">The Xml Element to save to</param>
        /// <param name="events">Data collection events to which methods subscribe</param>
        /// <param name="dataSink">A data collection sink for data transfer</param>
        /// <param name="logger">Data Collection Logger to send messages to the client </param>
        /// <param name="environmentContext">Context of data collector environment</param>
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            ValidateArg.NotNull(logger, nameof(logger));

            this.events = events;
            this.dataCollectionSink = dataSink;
            this.context = environmentContext;
            this.configurationElement = configurationElement;
            this.testSequence = new List<Guid>();
            this.testObjectDictionary = new Dictionary<Guid, BlameTestObject>();
            this.logger = logger;

            // Subscribing to events
            this.events.TestHostLaunched += this.TestHostLaunchedHandler;
            this.events.SessionEnd += this.SessionEndedHandler;
            this.events.TestCaseStart += this.EventsTestCaseStart;
            this.events.TestCaseEnd += this.EventsTestCaseEnd;

            if (this.configurationElement != null)
            {
                var collectDumpNode = this.configurationElement[Constants.DumpModeKey];
                this.collectProcessDumpOnTrigger = collectDumpNode != null;

                if (this.collectProcessDumpOnTrigger)
                {
                    this.ValidateAndAddTriggerBasedProcessDumpParameters(collectDumpNode);

                    // enabling dumps on MacOS needs to be done explicitly https://github.com/dotnet/runtime/pull/40105
                    this.environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgEnableElfDumpOnMacOS", "1"));
                    this.environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgEnableMiniDump", "1"));

                    // https://github.com/dotnet/coreclr/blob/master/Documentation/botr/xplat-minidump-generation.md
                    // 2   MiniDumpWithPrivateReadWriteMemory
                    // 4   MiniDumpWithFullMemory
                    this.environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgMiniDumpType", this.processFullDumpEnabled ? "4" : "2"));
                    var dumpDirectory = this.GetDumpDirectory();
                    var dumpPath = Path.Combine(dumpDirectory, $"%e_%p_%t_crashdump.dmp");
                    this.environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgMiniDumpName", dumpPath));
                }

                var collectHangBasedDumpNode = this.configurationElement[Constants.CollectDumpOnTestSessionHang];
                this.collectProcessDumpOnTestHostHang = collectHangBasedDumpNode != null;
                if (this.collectProcessDumpOnTestHostHang)
                {
                    // enabling dumps on MacOS needs to be done explicitly https://github.com/dotnet/runtime/pull/40105
                    this.environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgEnableElfDumpOnMacOS", "1"));

                    this.ValidateAndAddHangBasedProcessDumpParameters(collectHangBasedDumpNode);
                }

                var tfm = this.configurationElement[Constants.TargetFramework]?.InnerText;
                if (!string.IsNullOrWhiteSpace(tfm))
                {
                    this.targetFramework = tfm;
                }
            }

            this.attachmentGuid = Guid.NewGuid().ToString().Replace("-", string.Empty);

            if (this.collectProcessDumpOnTestHostHang)
            {
                this.inactivityTimer = this.inactivityTimer ?? new InactivityTimer(this.CollectDumpAndAbortTesthost);
                this.ResetInactivityTimer();
            }
        }

        /// <summary>
        /// Disposes of the timer when called to prevent further calls.
        /// Kills the other instance of proc dump if launched for collecting trigger based dumps.
        /// Starts and waits for a new proc dump process to collect a single dump and then
        /// kills the testhost process.
        /// </summary>
        private void CollectDumpAndAbortTesthost()
        {
            this.inactivityTimerAlreadyFired = true;

            string value;
            string unit;

            if (this.inactivityTimespan.TotalSeconds <= 90)
            {
                value = ((int)this.inactivityTimespan.TotalSeconds).ToString();
                unit = Resources.Resources.Seconds;
            }
            else
            {
                value = Math.Round(this.inactivityTimespan.TotalMinutes, 2).ToString();
                unit = Resources.Resources.Minutes;
            }

            var message = string.Format(CultureInfo.CurrentUICulture, Resources.Resources.InactivityTimeout, value, unit);

            EqtTrace.Warning(message);
            this.logger.LogWarning(this.context.SessionDataCollectionContext, message);

            try
            {
                EqtTrace.Verbose("Calling dispose on Inactivity timer.");
                this.inactivityTimer.Dispose();
            }
            catch
            {
                EqtTrace.Verbose("Inactivity timer is already disposed.");
            }

            try
            {
                Action<string> logWarning = m => this.logger.LogWarning(this.context.SessionDataCollectionContext, m);
                var dumpDirectory = this.GetDumpDirectory();
                this.processDumpUtility.StartHangBasedProcessDump(this.testHostProcessId, dumpDirectory, this.processFullDumpEnabled, this.targetFramework, logWarning);
            }
            catch (Exception ex)
            {
                this.logger.LogError(this.context.SessionDataCollectionContext, $"Blame: Creating hang dump failed with error.", ex);
            }

            if (this.collectProcessDumpOnTrigger)
            {
                // Detach procdump from the testhost process to prevent testhost process from crashing
                // if/when we try to kill the existing proc dump process.
                this.processDumpUtility.DetachFromTargetProcess(this.testHostProcessId);
            }

            if (this.uploadDumpFiles)
            {
                try
                {
                    var dumpFiles = this.processDumpUtility.GetDumpFiles();
                    foreach (var dumpFile in dumpFiles)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(dumpFile))
                            {
                                var fileTransferInformation = new FileTransferInformation(this.context.SessionDataCollectionContext, dumpFile, true, this.fileHelper);
                                this.dataCollectionSink.SendFileAsync(fileTransferInformation);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Eat up any exception here and log it but proceed with killing the test host process.
                            EqtTrace.Error(ex);
                        }

                        if (!dumpFiles.Any())
                        {
                            EqtTrace.Error("BlameCollector.CollectDumpAndAbortTesthost: blame:CollectDumpOnHang was enabled but dump file was not generated.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(this.context.SessionDataCollectionContext, $"Blame: Collecting hang dump failed with error.", ex);
                }
            }
            else
            {
                EqtTrace.Info("BlameCollector.CollectDumpAndAbortTesthost: Custom path to dump directory was provided via VSTEST_DUMP_PATH. Skipping attachment upload, the caller is responsible for collecting and uploading the dumps themselves.");
            }

            try
            {
                var p = Process.GetProcessById(this.testHostProcessId);
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill();
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error(ex);
            }
        }

        private void ValidateAndAddTriggerBasedProcessDumpParameters(XmlElement collectDumpNode)
        {
            foreach (XmlAttribute blameAttribute in collectDumpNode.Attributes)
            {
                switch (blameAttribute)
                {
                    case XmlAttribute attribute when string.Equals(attribute.Name, Constants.CollectDumpAlwaysKey, StringComparison.OrdinalIgnoreCase):

                        if (string.Equals(attribute.Value, Constants.TrueConfigurationValue, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Value, Constants.FalseConfigurationValue, StringComparison.OrdinalIgnoreCase))
                        {
                            bool.TryParse(attribute.Value, out this.collectDumpAlways);
                        }
                        else
                        {
                            this.logger.LogWarning(this.context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, attribute.Name, Constants.TrueConfigurationValue, Constants.FalseConfigurationValue));
                        }

                        break;

                    case XmlAttribute attribute when string.Equals(attribute.Name, Constants.DumpTypeKey, StringComparison.OrdinalIgnoreCase):

                        if (string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Value, Constants.MiniConfigurationValue, StringComparison.OrdinalIgnoreCase))
                        {
                            this.processFullDumpEnabled = string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            this.logger.LogWarning(this.context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, attribute.Name, Constants.FullConfigurationValue, Constants.MiniConfigurationValue));
                        }

                        break;

                    default:

                        this.logger.LogWarning(this.context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterKeyIncorrect, blameAttribute.Name));
                        break;
                }
            }
        }

        private void ValidateAndAddHangBasedProcessDumpParameters(XmlElement collectDumpNode)
        {
            foreach (XmlAttribute blameAttribute in collectDumpNode.Attributes)
            {
                switch (blameAttribute)
                {
                    case XmlAttribute attribute when string.Equals(attribute.Name, Constants.TestTimeout, StringComparison.OrdinalIgnoreCase):

                        if (!string.IsNullOrWhiteSpace(attribute.Value) && TimeSpanParser.TryParse(attribute.Value, out var timeout))
                        {
                            this.inactivityTimespan = timeout;
                        }
                        else
                        {
                            this.logger.LogWarning(this.context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.UnexpectedValueForInactivityTimespanValue, attribute.Value));
                        }

                        break;

                    // allow HangDumpType attribute to be used on the hang dump this is the prefered way
                    case XmlAttribute attribute when string.Equals(attribute.Name, Constants.HangDumpTypeKey, StringComparison.OrdinalIgnoreCase):

                        if (string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Value, Constants.MiniConfigurationValue, StringComparison.OrdinalIgnoreCase))
                        {
                            this.processFullDumpEnabled = string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            this.logger.LogWarning(this.context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, attribute.Name, Constants.FullConfigurationValue, Constants.MiniConfigurationValue));
                        }

                        break;

                    // allow DumpType attribute to be used on the hang dump for backwards compatibility
                    case XmlAttribute attribute when string.Equals(attribute.Name, Constants.DumpTypeKey, StringComparison.OrdinalIgnoreCase):

                        if (string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Value, Constants.MiniConfigurationValue, StringComparison.OrdinalIgnoreCase))
                        {
                            this.processFullDumpEnabled = string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            this.logger.LogWarning(this.context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, attribute.Name, Constants.FullConfigurationValue, Constants.MiniConfigurationValue));
                        }

                        break;

                    default:

                        this.logger.LogWarning(this.context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterKeyIncorrect, blameAttribute.Name));
                        break;
                }
            }
        }

        /// <summary>
        /// Called when Test Case Start event is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">TestCaseStartEventArgs</param>
        private void EventsTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            this.ResetInactivityTimer();

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Test Case Start");
            }

            var blameTestObject = new BlameTestObject(e.TestElement);

            // Add guid to list of test sequence to maintain the order.
            this.testSequence.Add(blameTestObject.Id);

            // Add the test object to the dictionary.
            this.testObjectDictionary.Add(blameTestObject.Id, blameTestObject);

            // Increment test start count.
            this.testStartCount++;
        }

        /// <summary>
        /// Called when Test Case End event is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">TestCaseEndEventArgs</param>
        private void EventsTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            this.ResetInactivityTimer();

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Test Case End");
            }

            this.testEndCount++;

            // Update the test object in the dictionary as the test has completed.
            if (this.testObjectDictionary.ContainsKey(e.TestElement.Id))
            {
                this.testObjectDictionary[e.TestElement.Id].IsCompleted = true;
            }
        }

        /// <summary>
        /// Called when Session End event is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">SessionEndEventArgs</param>
        private void SessionEndedHandler(object sender, SessionEndEventArgs args)
        {
            this.ResetInactivityTimer();

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Session End");
            }

            try
            {
                // If the last test crashes, it will not invoke a test case end and therefore
                // In case of crash testStartCount will be greater than testEndCount and we need to write the sequence
                // And send the attachment
                if (this.testStartCount > this.testEndCount)
                {
                    var filepath = Path.Combine(this.GetTempDirectory(), Constants.AttachmentFileName + "_" + this.attachmentGuid);

                    filepath = this.blameReaderWriter.WriteTestSequence(this.testSequence, this.testObjectDictionary, filepath);
                    var fti = new FileTransferInformation(this.context.SessionDataCollectionContext, filepath, true);
                    this.dataCollectionSink.SendFileAsync(fti);
                }
                else
                {
                    this.logger.LogWarning(this.context.SessionDataCollectionContext, Resources.Resources.NotGeneratingSequenceFile);
                }

                if (this.uploadDumpFiles)
                {
                    try
                    {
                        var dumpFiles = this.processDumpUtility.GetDumpFiles(warnOnNoDumpFiles: this.collectDumpAlways);
                        foreach (var dumpFile in dumpFiles)
                        {
                            if (!string.IsNullOrEmpty(dumpFile))
                            {
                                try
                                {
                                    var fileTranferInformation = new FileTransferInformation(this.context.SessionDataCollectionContext, dumpFile, true);
                                    this.dataCollectionSink.SendFileAsync(fileTranferInformation);
                                }
                                catch (FileNotFoundException ex)
                                {
                                    EqtTrace.Warning(ex.ToString());
                                    this.logger.LogWarning(args.Context, ex.ToString());
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException ex)
                    {
                        EqtTrace.Warning(ex.ToString());
                        this.logger.LogWarning(args.Context, ex.ToString());
                    }
                }
                else
                {
                    EqtTrace.Info("BlameCollector.CollectDumpAndAbortTesthost: Custom path to dump directory was provided via VSTEST_DUMP_PATH. Skipping attachment upload, the caller is responsible for collecting and uploading the dumps themselves.");
                }
            }
            finally
            {
                // Attempt to terminate the proc dump process if proc dump was enabled
                if (this.collectProcessDumpOnTrigger)
                {
                    this.processDumpUtility.DetachFromTargetProcess(this.testHostProcessId);
                }

                this.DeregisterEvents();
            }
        }

        /// <summary>
        /// Called when Test Host Initialized is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">TestHostLaunchedEventArgs</param>
        private void TestHostLaunchedHandler(object sender, TestHostLaunchedEventArgs args)
        {
            this.ResetInactivityTimer();
            this.testHostProcessId = args.TestHostProcessId;

            if (!this.collectProcessDumpOnTrigger)
            {
                return;
            }

            try
            {
                var dumpDirectory = this.GetDumpDirectory();
                this.processDumpUtility.StartTriggerBasedProcessDump(args.TestHostProcessId, dumpDirectory, this.processFullDumpEnabled, this.targetFramework);
            }
            catch (TestPlatformException e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("BlameCollector.TestHostLaunchedHandler: Could not start process dump. {0}", e);
                }

                this.logger.LogWarning(args.Context, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.ProcDumpCouldNotStart, e.Message));
            }
            catch (Exception e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("BlameCollector.TestHostLaunchedHandler: Could not start process dump. {0}", e);
                }

                this.logger.LogWarning(args.Context, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.ProcDumpCouldNotStart, e.ToString()));
            }
        }

        /// <summary>
        /// Resets the inactivity timer
        /// </summary>
        private void ResetInactivityTimer()
        {
            if (!this.collectProcessDumpOnTestHostHang || this.inactivityTimerAlreadyFired)
            {
                return;
            }

            EqtTrace.Verbose("Reset the inactivity timer since an event was received.");
            try
            {
                this.inactivityTimer.ResetTimer(this.inactivityTimespan);
            }
            catch (Exception e)
            {
                EqtTrace.Warning($"Failed to reset the inactivity timer with error {e}");
            }
        }

        /// <summary>
        /// Method to de-register handlers and cleanup
        /// </summary>
        private void DeregisterEvents()
        {
            this.events.SessionEnd -= this.SessionEndedHandler;
            this.events.TestCaseStart -= this.EventsTestCaseStart;
            this.events.TestCaseEnd -= this.EventsTestCaseEnd;
        }

        private string GetTempDirectory()
        {
            if (string.IsNullOrWhiteSpace(this.tempDirectory))
            {
                this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(this.tempDirectory);
                return this.tempDirectory;
            }

            return this.tempDirectory;
        }

        private string GetDumpDirectory()
        {
            // Using a custom dump path for scenarios where we want to upload the
            // dump files ourselves, such as when running in Helix.
            // This will save into the directory specified via VSTEST_DUMP_PATH, and
            //  skip uploading dumps via attachments.
            var dumpDirectoryOverride = Environment.GetEnvironmentVariable("VSTEST_DUMP_PATH");
            var dumpDirectoryOverrideHasValue = !string.IsNullOrWhiteSpace(dumpDirectoryOverride);
            this.uploadDumpFiles = !dumpDirectoryOverrideHasValue;

            var dumpDirectory = dumpDirectoryOverrideHasValue ? dumpDirectoryOverride : this.GetTempDirectory();
            Directory.CreateDirectory(dumpDirectory);
            var dumpPath = Path.Combine(Path.GetFullPath(dumpDirectory));

            if (!this.uploadDumpFiles)
            {
                this.logger.LogWarning(this.context.SessionDataCollectionContext, $"VSTEST_DUMP_PATH is specified. Dump files will be saved in: {dumpPath}, and won't be added to attachments.");
            }

            return dumpPath;
        }
    }
}
