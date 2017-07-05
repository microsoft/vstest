// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    [DataCollectorFriendlyName("Blame")]
    [DataCollectorTypeUri("datacollector://microsoft/blame/1.0")]
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
        /// Initializes a new instance of the <see cref="BlameDataCollector"/> class.
        /// Using XmlReaderWriter by default
        /// </summary>
        public BlameCollector()
            : this(new XmlReaderWriter())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameDataCollector"/> class.
        /// </summary>
        /// <param name="blameReaderWriter">BlameReaderWriter instance.</param>
        public BlameCollector(IBlameReaderWriter blameReaderWriter)
        {
            this.blameReaderWriter = blameReaderWriter;
        }

        /// <summary>
        /// Gets environment variables that should be set in the test execution environment
        /// </summary>
        /// <returns>Environment variables that should be set in the test execution environment</returns>
        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return new List<KeyValuePair<string, string>> { };
        }

        /// <summary>
        /// Initializes parameters for the new instance of the class <see cref="BlameDataCollector"/>
        /// </summary>
        /// <param name="configurationElement">The Xml Element to save to</param>
        /// <param name="events">Data collection events to which methods subscribe</param>
        /// <param name="dataSink">A data collection sink for data transfer</param>
        /// <param name="logger">Data Collection Logger to send messages to the client </param>
        /// <param name="environmentContext">Context of data collector environment</param>
        public override void Initialize(XmlElement configurationElement,
            DataCollectionEvents events, DataCollectionSink dataSink,
            DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            ValidateArg.NotNull(logger, nameof(logger));

            this.events = events;
            this.dataCollectionSink = dataSink;
            this.context = environmentContext;
            testSequence = new List<TestCase>();

            // Subscribing to events
            this.events.SessionEnd += this.SessionEnded_Handler;
            this.events.TestCaseStart += this.Events_TestCaseStart;
            this.events.TestCaseEnd += this.Events_TestCaseEnd;
        }

        /// <summary>
        /// Called when Test Case Start event is invoked 
        /// </summary>
        private void Events_TestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            EqtTrace.Info("Blame Collector : " + Constants.TestCaseStart);
            TestCase testcase = new TestCase(e.TestElement.FullyQualifiedName, e.TestElement.ExecutorUri, e.TestElement.Source);
            this.testSequence.Add(testcase);
            this.testStartCount++;
        }

        /// <summary>
        /// Called when Test Case End event is invoked 
        /// </summary>
        private void Events_TestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            EqtTrace.Info("Blame Collector : " + Constants.TestCaseEnd);
            this.testEndCount++;
        }

        /// <summary>
        /// Called when Session End event is invoked 
        /// </summary>
        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            EqtTrace.Info("Blame Collector :" + Constants.TestSessionEnd);
            if (this.testStartCount != this.testEndCount)
            {
                var filepath = Path.Combine(AppContext.BaseDirectory, Constants.AttachmentFileName);
                this.blameReaderWriter.WriteTestSequence(this.testSequence, filepath);
                this.dataCollectionSink.SendFileAsync(this.context.SessionDataCollectionContext, filepath, true);
            }
            DeregisterEvents();
        }

        /// <summary>
        /// Method to deregister handlers and cleanup
        /// </summary>
        private void DeregisterEvents()
        {
            this.events.SessionEnd -= this.SessionEnded_Handler;
            this.events.TestCaseStart -= this.Events_TestCaseStart;
            this.events.TestCaseEnd -= this.Events_TestCaseEnd;
        }
    }
}
