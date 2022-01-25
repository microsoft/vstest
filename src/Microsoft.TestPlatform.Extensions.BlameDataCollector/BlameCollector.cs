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
        private readonly IProcessDumpUtility processDumpUtility;
        private List<Guid> testSequence;
        private Dictionary<Guid, BlameTestObject> testObjectDictionary;
        private readonly IBlameReaderWriter blameReaderWriter;
        private readonly IFileHelper fileHelper;
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
        private readonly List<KeyValuePair<string, string>> environmentVariables = new();
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
            return environmentVariables;
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
            dataCollectionSink = dataSink;
            context = environmentContext;
            this.configurationElement = configurationElement;
            testSequence = new List<Guid>();
            testObjectDictionary = new Dictionary<Guid, BlameTestObject>();
            this.logger = logger;

            // Subscribing to events
            this.events.TestHostLaunched += TestHostLaunchedHandler;
            this.events.SessionEnd += SessionEndedHandler;
            this.events.TestCaseStart += EventsTestCaseStart;
            this.events.TestCaseEnd += EventsTestCaseEnd;

            if (this.configurationElement != null)
            {
                var collectDumpNode = this.configurationElement[Constants.DumpModeKey];
                collectProcessDumpOnTrigger = collectDumpNode != null;

                if (collectProcessDumpOnTrigger)
                {
                    ValidateAndAddTriggerBasedProcessDumpParameters(collectDumpNode);

                    // enabling dumps on MacOS needs to be done explicitly https://github.com/dotnet/runtime/pull/40105
                    environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgEnableElfDumpOnMacOS", "1"));
                    environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgEnableMiniDump", "1"));

                    // https://github.com/dotnet/coreclr/blob/master/Documentation/botr/xplat-minidump-generation.md
                    // 2   MiniDumpWithPrivateReadWriteMemory
                    // 4   MiniDumpWithFullMemory
                    environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgMiniDumpType", processFullDumpEnabled ? "4" : "2"));
                    var dumpDirectory = GetDumpDirectory();
                    var dumpPath = Path.Combine(dumpDirectory, $"%e_%p_%t_crashdump.dmp");
                    environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgMiniDumpName", dumpPath));
                }

                var collectHangBasedDumpNode = this.configurationElement[Constants.CollectDumpOnTestSessionHang];
                collectProcessDumpOnTestHostHang = collectHangBasedDumpNode != null;
                if (collectProcessDumpOnTestHostHang)
                {
                    // enabling dumps on MacOS needs to be done explicitly https://github.com/dotnet/runtime/pull/40105
                    environmentVariables.Add(new KeyValuePair<string, string>("COMPlus_DbgEnableElfDumpOnMacOS", "1"));

                    ValidateAndAddHangBasedProcessDumpParameters(collectHangBasedDumpNode);
                }

                var tfm = this.configurationElement[Constants.TargetFramework]?.InnerText;
                if (!string.IsNullOrWhiteSpace(tfm))
                {
                    targetFramework = tfm;
                }
            }

            attachmentGuid = Guid.NewGuid().ToString("N");

            if (collectProcessDumpOnTestHostHang)
            {
                inactivityTimer ??= new InactivityTimer(CollectDumpAndAbortTesthost);
                ResetInactivityTimer();
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
            inactivityTimerAlreadyFired = true;

            string value;
            string unit;

            if (inactivityTimespan.TotalSeconds <= 90)
            {
                value = ((int)inactivityTimespan.TotalSeconds).ToString();
                unit = Resources.Resources.Seconds;
            }
            else
            {
                value = Math.Round(inactivityTimespan.TotalMinutes, 2).ToString();
                unit = Resources.Resources.Minutes;
            }

            var message = string.Format(CultureInfo.CurrentUICulture, Resources.Resources.InactivityTimeout, value, unit);

            EqtTrace.Warning(message);
            logger.LogWarning(context.SessionDataCollectionContext, message);

            try
            {
                EqtTrace.Verbose("Calling dispose on Inactivity timer.");
                inactivityTimer.Dispose();
            }
            catch
            {
                EqtTrace.Verbose("Inactivity timer is already disposed.");
            }

            if (collectProcessDumpOnTrigger)
            {
                // Detach procdump from the testhost process to prevent testhost process from crashing
                // if/when we try to kill the existing proc dump process.
                // And also prevent collecting dump on exit of the process.
                processDumpUtility.DetachFromTargetProcess(testHostProcessId);
            }

            var hangDumpSuccess = false;
            try
            {
                Action<string> logWarning = m => logger.LogWarning(context.SessionDataCollectionContext, m);
                var dumpDirectory = GetDumpDirectory();
                processDumpUtility.StartHangBasedProcessDump(testHostProcessId, dumpDirectory, processFullDumpEnabled, targetFramework, logWarning);
                hangDumpSuccess = true;
            }
            catch (Exception ex)
            {
                logger.LogError(context.SessionDataCollectionContext, $"Blame: Creating hang dump failed with error.", ex);
            }

            if (uploadDumpFiles)
            {
                try
                {
                    var dumpFiles = processDumpUtility.GetDumpFiles(true, /* if we killed it by hang dumper, we already have our dump, otherwise it might have crashed, and we want all dumps */ !hangDumpSuccess);
                    foreach (var dumpFile in dumpFiles)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(dumpFile))
                            {
                                var fileTransferInformation = new FileTransferInformation(context.SessionDataCollectionContext, dumpFile, true, fileHelper);
                                dataCollectionSink.SendFileAsync(fileTransferInformation);
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
                    logger.LogError(context.SessionDataCollectionContext, $"Blame: Collecting hang dump failed with error.", ex);
                }
            }
            else
            {
                EqtTrace.Info("BlameCollector.CollectDumpAndAbortTesthost: Custom path to dump directory was provided via VSTEST_DUMP_PATH. Skipping attachment upload, the caller is responsible for collecting and uploading the dumps themselves.");
            }

            try
            {
                var p = Process.GetProcessById(testHostProcessId);
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
                            bool.TryParse(attribute.Value, out collectDumpAlways);
                        }
                        else
                        {
                            logger.LogWarning(context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, attribute.Name, Constants.TrueConfigurationValue, Constants.FalseConfigurationValue));
                        }

                        break;

                    case XmlAttribute attribute when string.Equals(attribute.Name, Constants.DumpTypeKey, StringComparison.OrdinalIgnoreCase):

                        if (string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Value, Constants.MiniConfigurationValue, StringComparison.OrdinalIgnoreCase))
                        {
                            processFullDumpEnabled = string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            logger.LogWarning(context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, attribute.Name, Constants.FullConfigurationValue, Constants.MiniConfigurationValue));
                        }

                        break;

                    default:

                        logger.LogWarning(context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterKeyIncorrect, blameAttribute.Name));
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
                            inactivityTimespan = timeout;
                        }
                        else
                        {
                            logger.LogWarning(context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.UnexpectedValueForInactivityTimespanValue, attribute.Value));
                        }

                        break;

                    // allow HangDumpType attribute to be used on the hang dump this is the prefered way
                    case XmlAttribute attribute when string.Equals(attribute.Name, Constants.HangDumpTypeKey, StringComparison.OrdinalIgnoreCase):

                        if (string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Value, Constants.MiniConfigurationValue, StringComparison.OrdinalIgnoreCase))
                        {
                            processFullDumpEnabled = string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            logger.LogWarning(context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, attribute.Name, Constants.FullConfigurationValue, Constants.MiniConfigurationValue));
                        }

                        break;

                    // allow DumpType attribute to be used on the hang dump for backwards compatibility
                    case XmlAttribute attribute when string.Equals(attribute.Name, Constants.DumpTypeKey, StringComparison.OrdinalIgnoreCase):

                        if (string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Value, Constants.MiniConfigurationValue, StringComparison.OrdinalIgnoreCase))
                        {
                            processFullDumpEnabled = string.Equals(attribute.Value, Constants.FullConfigurationValue, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            logger.LogWarning(context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, attribute.Name, Constants.FullConfigurationValue, Constants.MiniConfigurationValue));
                        }

                        break;

                    default:

                        logger.LogWarning(context.SessionDataCollectionContext, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterKeyIncorrect, blameAttribute.Name));
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
            ResetInactivityTimer();

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Test Case Start");
            }

            var blameTestObject = new BlameTestObject(e.TestElement);

            // Add guid to list of test sequence to maintain the order.
            testSequence.Add(blameTestObject.Id);

            // Add the test object to the dictionary.
            testObjectDictionary.Add(blameTestObject.Id, blameTestObject);

            // Increment test start count.
            testStartCount++;
        }

        /// <summary>
        /// Called when Test Case End event is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">TestCaseEndEventArgs</param>
        private void EventsTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            ResetInactivityTimer();

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Test Case End");
            }

            testEndCount++;

            // Update the test object in the dictionary as the test has completed.
            if (testObjectDictionary.ContainsKey(e.TestElement.Id))
            {
                testObjectDictionary[e.TestElement.Id].IsCompleted = true;
            }
        }

        /// <summary>
        /// Called when Session End event is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">SessionEndEventArgs</param>
        private void SessionEndedHandler(object sender, SessionEndEventArgs args)
        {
            ResetInactivityTimer();

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Session End");
            }

            try
            {
                // If the last test crashes, it will not invoke a test case end and therefore
                // In case of crash testStartCount will be greater than testEndCount and we need to write the sequence
                // And send the attachment. This won't indicate failure if there are 0 tests in the assembly, or when it fails in setup.
                var processCrashedWhenRunningTests = testStartCount > testEndCount;
                if (processCrashedWhenRunningTests)
                {
                    var filepath = Path.Combine(GetTempDirectory(), Constants.AttachmentFileName + "_" + attachmentGuid);

                    filepath = blameReaderWriter.WriteTestSequence(testSequence, testObjectDictionary, filepath);
                    var fti = new FileTransferInformation(context.SessionDataCollectionContext, filepath, true);
                    dataCollectionSink.SendFileAsync(fti);
                }
                else
                {
                    if (collectProcessDumpOnTestHostHang)
                    {
                        logger.LogWarning(context.SessionDataCollectionContext, Resources.Resources.NotGeneratingSequenceFile);
                    }
                }

                if (uploadDumpFiles)
                {
                    try
                    {
                        var dumpFiles = processDumpUtility.GetDumpFiles(warnOnNoDumpFiles: collectDumpAlways, processCrashedWhenRunningTests);
                        foreach (var dumpFile in dumpFiles)
                        {
                            if (!string.IsNullOrEmpty(dumpFile))
                            {
                                try
                                {
                                    var fileTranferInformation = new FileTransferInformation(context.SessionDataCollectionContext, dumpFile, true);
                                    dataCollectionSink.SendFileAsync(fileTranferInformation);
                                }
                                catch (FileNotFoundException ex)
                                {
                                    EqtTrace.Warning(ex.ToString());
                                    logger.LogWarning(args.Context, ex.ToString());
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException ex)
                    {
                        EqtTrace.Warning(ex.ToString());
                        logger.LogWarning(args.Context, ex.ToString());
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
                if (collectProcessDumpOnTrigger)
                {
                    processDumpUtility.DetachFromTargetProcess(testHostProcessId);
                }

                DeregisterEvents();
            }
        }

        /// <summary>
        /// Called when Test Host Initialized is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">TestHostLaunchedEventArgs</param>
        private void TestHostLaunchedHandler(object sender, TestHostLaunchedEventArgs args)
        {
            ResetInactivityTimer();
            testHostProcessId = args.TestHostProcessId;

            if (!collectProcessDumpOnTrigger)
            {
                return;
            }

            try
            {
                var dumpDirectory = GetDumpDirectory();
                processDumpUtility.StartTriggerBasedProcessDump(args.TestHostProcessId, dumpDirectory, processFullDumpEnabled, targetFramework, collectDumpAlways);
            }
            catch (TestPlatformException e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("BlameCollector.TestHostLaunchedHandler: Could not start process dump. {0}", e);
                }

                logger.LogWarning(args.Context, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.ProcDumpCouldNotStart, e.Message));
            }
            catch (Exception e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("BlameCollector.TestHostLaunchedHandler: Could not start process dump. {0}", e);
                }

                logger.LogWarning(args.Context, string.Format(CultureInfo.CurrentUICulture, Resources.Resources.ProcDumpCouldNotStart, e.ToString()));
            }
        }

        /// <summary>
        /// Resets the inactivity timer
        /// </summary>
        private void ResetInactivityTimer()
        {
            if (!collectProcessDumpOnTestHostHang || inactivityTimerAlreadyFired)
            {
                return;
            }

            EqtTrace.Verbose("Reset the inactivity timer since an event was received.");
            try
            {
                inactivityTimer.ResetTimer(inactivityTimespan);
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
            events.SessionEnd -= SessionEndedHandler;
            events.TestCaseStart -= EventsTestCaseStart;
            events.TestCaseEnd -= EventsTestCaseEnd;
        }

        private string GetTempDirectory()
        {
            if (string.IsNullOrWhiteSpace(tempDirectory))
            {
                // DUMP_TEMP_PATH will be used as temporary storage location
                // for the dumps, this won't affect the dump uploads. Just the place where
                // we store them before moving them to the final folder.

                // AGENT_TEMPDIRECTORY is AzureDevops variable, which is set to path
                // that is cleaned up after every job. This is preferable to use over
                // just the normal temp.
                var temp = Environment.GetEnvironmentVariable("VSTEST_DUMP_TEMP_PATH") ?? Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY") ?? Path.GetTempPath();
                tempDirectory = Path.Combine(temp, Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);
                return tempDirectory;
            }

            return tempDirectory;
        }

        private string GetDumpDirectory()
        {
            // Using a custom dump path for scenarios where we want to upload the
            // dump files ourselves, such as when running in Helix.
            // This will save into the directory specified via VSTEST_DUMP_PATH, and
            //  skip uploading dumps via attachments.
            var dumpDirectoryOverride = Environment.GetEnvironmentVariable("VSTEST_DUMP_PATH");
            var dumpDirectoryOverrideHasValue = !string.IsNullOrWhiteSpace(dumpDirectoryOverride);
            uploadDumpFiles = !dumpDirectoryOverrideHasValue;

            var dumpDirectory = dumpDirectoryOverrideHasValue ? dumpDirectoryOverride : GetTempDirectory();
            Directory.CreateDirectory(dumpDirectory);
            var dumpPath = Path.Combine(Path.GetFullPath(dumpDirectory));

            if (!uploadDumpFiles)
            {
                logger.LogWarning(context.SessionDataCollectionContext, $"VSTEST_DUMP_PATH is specified. Dump files will be saved in: {dumpPath}, and won't be added to attachments.");
            }

            return dumpPath;
        }
    }
}
