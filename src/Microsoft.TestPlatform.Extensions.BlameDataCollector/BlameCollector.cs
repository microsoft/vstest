// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The blame collector.
    /// </summary>
    [DataCollectorFriendlyName("Blame")]
    [DataCollectorTypeUri("datacollector://Microsoft/TestPlatform/Extensions/Blame/v1")]
    public class BlameCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        private DataCollectionSink dataCollectionSink;
        private DataCollectionEnvironmentContext context;
        private DataCollectionEvents events;
        private DataCollectionLogger logger;
        private IProcessDumpUtility processDumpUtility;
        private List<TestCase> testSequence;
        private IBlameReaderWriter blameReaderWriter;
        private XmlElement configurationElement;
        private int testStartCount;
        private int testEndCount;
        private bool processDumpEnabled;
        private string attachmentGuid;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameCollector"/> class.
        /// Using XmlReaderWriter by default
        /// </summary>
        public BlameCollector()
            : this(new XmlReaderWriter(), new ProcessDumpUtility())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameCollector"/> class.
        /// </summary>
        /// <param name="blameReaderWriter">
        /// BlameReaderWriter instance.
        /// </param>
        /// <param name="processDumpUtility">
        /// ProcessDumpUtility instance.
        /// </param>
        internal BlameCollector(IBlameReaderWriter blameReaderWriter, IProcessDumpUtility processDumpUtility)
        {
            this.blameReaderWriter = blameReaderWriter;
            this.processDumpUtility = processDumpUtility;
        }

        /// <summary>
        /// Gets environment variables that should be set in the test execution environment
        /// </summary>
        /// <returns>Environment variables that should be set in the test execution environment</returns>
        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return Enumerable.Empty<KeyValuePair<string, string>>();
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
            this.testSequence = new List<TestCase>();
            this.logger = logger;

            // Subscribing to events
            this.events.TestHostLaunched += this.TestHostLaunched_Handler;
            this.events.SessionEnd += this.SessionEnded_Handler;
            this.events.TestCaseStart += this.EventsTestCaseStart;
            this.events.TestCaseEnd += this.EventsTestCaseEnd;

            if (this.configurationElement != null)
            {
                this.processDumpEnabled = this.configurationElement[Constants.DumpModeKey] != null;
            }

            this.attachmentGuid = Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        /// <summary>
        /// Called when Test Case Start event is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">TestCaseStartEventArgs</param>
        private void EventsTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Test Case Start");
            }

            this.testSequence.Add(e.TestElement);
            this.testStartCount++;
        }

        /// <summary>
        /// Called when Test Case End event is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">TestCaseEndEventArgs</param>
        private void EventsTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Test Case End");
            }

            this.testEndCount++;
        }

        /// <summary>
        /// Called when Session End event is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">SessionEndEventArgs</param>
        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Session End");
            }

            // If the last test crashes, it will not invoke a test case end and therefore
            // In case of crash testStartCount will be greater than testEndCount and we need to write the sequence
            // And send the attachment
            if (this.testStartCount > this.testEndCount)
            {
                var filepath = Path.Combine(this.GetResultsDirectory(), Constants.AttachmentFileName + "_" + this.attachmentGuid);
                filepath = this.blameReaderWriter.WriteTestSequence(this.testSequence, filepath);
                var fileTranferInformation = new FileTransferInformation(this.context.SessionDataCollectionContext, filepath, true);
                this.dataCollectionSink.SendFileAsync(fileTranferInformation);
            }

            if (this.processDumpEnabled)
            {
                try
                {
                    var dumpFile = this.processDumpUtility.GetDumpFile();
                    if (!string.IsNullOrEmpty(dumpFile))
                    {
                        var fileTranferInformation = new FileTransferInformation(this.context.SessionDataCollectionContext, dumpFile, true);
                        this.dataCollectionSink.SendFileAsync(fileTranferInformation);
                    }
                    else
                    {
                        EqtTrace.Warning("BlameCollector.SessionEnded_Handler: blame:CollectDump was enabled but dump file was not generated.");
                    }
                }
                catch (FileNotFoundException ex)
                {
                    this.logger.LogWarning(args.Context, ex.Message);
                }
            }

            this.DeregisterEvents();
        }

        /// <summary>
        /// Called when Test Host Initialized is invoked
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">TestHostLaunchedEventArgs</param>
        private void TestHostLaunched_Handler(object sender, TestHostLaunchedEventArgs args)
        {
            if (!this.processDumpEnabled)
            {
                return;
            }

            try
            {
                this.processDumpUtility.StartProcessDump(args.TestHostProcessId, this.attachmentGuid, this.GetResultsDirectory());
            }
            catch (TestPlatformException e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("BlameCollector.TestHostLaunched_Handler: Could not start process dump. {0}", e);
                }

                this.logger.LogWarning(args.Context, e.Message);
            }
            catch (Exception e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("BlameCollector.TestHostLaunched_Handler: Could not start process dump. {0}", e);
                }

                this.logger.LogWarning(args.Context, e.ToString());
            }
        }

        /// <summary>
        /// Method to deregister handlers and cleanup
        /// </summary>
        private void DeregisterEvents()
        {
            this.events.SessionEnd -= this.SessionEnded_Handler;
            this.events.TestCaseStart -= this.EventsTestCaseStart;
            this.events.TestCaseEnd -= this.EventsTestCaseEnd;
        }

        private string GetResultsDirectory()
        {
            try
            {
                XmlElement resultsDirectoryElement = this.configurationElement["ResultsDirectory"];
                string resultsDirectory = resultsDirectoryElement != null ? resultsDirectoryElement.InnerText : string.Empty;
                return resultsDirectory;
            }
            catch (NullReferenceException exception)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("Blame Collector : " + exception);
                }

                return string.Empty;
            }
        }
    }
}
