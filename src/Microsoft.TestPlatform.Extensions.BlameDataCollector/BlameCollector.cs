// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The blame collector.
    /// </summary>
    [DataCollectorFriendlyName("Blame")]
    [DataCollectorTypeUri("datacollector://Microsoft/TestPlatform/Extensions/Blame")]
    public class BlameCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        private DataCollectionSink dataCollectionSink;
        private DataCollectionEnvironmentContext context;
        private DataCollectionEvents events;
        private List<TestCase> testSequence;
        private IBlameReaderWriter blameReaderWriter;
        private int testStartCount;
        private int testEndCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameCollector"/> class. 
        /// Using XmlReaderWriter by default
        /// </summary>
        public BlameCollector()
            : this(new XmlReaderWriter())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameCollector"/> class. 
        /// </summary>
        /// <param name="blameReaderWriter">
        /// BlameReaderWriter instance.
        /// </param>
        protected BlameCollector(IBlameReaderWriter blameReaderWriter)
        {
            this.blameReaderWriter = blameReaderWriter;
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
            this.testSequence = new List<TestCase>();

            // Subscribing to events
            this.events.SessionEnd += this.SessionEnded_Handler;
            this.events.TestCaseStart += this.EventsTestCaseStart;
            this.events.TestCaseEnd += this.EventsTestCaseEnd;
        }

        /// <summary>
        /// Called when Test Case Start event is invoked 
        /// </summary>
        private void EventsTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            if(EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Test Case Start");
            }
            this.testSequence.Add(e.TestElement);
            this.testStartCount++;
        }

        /// <summary>
        /// Called when Test Case End event is invoked 
        /// </summary>
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
        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Blame Collector : Session End");
            }

            // If the last test crashes, it will not invoke a test case end and therefore 
            // In case of crash testStartCount will be greater than testEndCount and we need to send write the sequence
            if (this.testStartCount > this.testEndCount)
            {
                var filepath = Path.Combine(AppContext.BaseDirectory, Constants.AttachmentFileName);
                filepath = this.blameReaderWriter.WriteTestSequence(this.testSequence, filepath);
                var fileTranferInformation = new FileTransferInformation(this.context.SessionDataCollectionContext, filepath, true);
                this.dataCollectionSink.SendFileAsync(fileTranferInformation);
            }

            this.DeregisterEvents();
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
    }
}
