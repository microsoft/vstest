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
    [DataCollectorTypeUri("my://blame/datacollector")]
    public class BlameCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        private DataCollectionSink dataCollectionSink;
        private DataCollectionEnvironmentContext context;
        private DataCollectionLogger logger;
        private DataCollectionEvents events;
        private List<TestCase> TestSequence;
        private BlameDataReaderWriter dataWriter;
        private IBlameFileManager blameFileManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameDataCollector"/> class.
        /// </summary>
        public BlameCollector()
            : this(new XmlFileManager())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameDataCollector"/> class.
        /// </summary>
        /// <param name="blameFileManager">BlameFileManager instance.</param>
        public BlameCollector(IBlameFileManager blameFileManager)
        {
            this.blameFileManager = blameFileManager;
        }

        /// <summary>
        /// Gets environment variables that should be set in the test execution environment
        /// </summary>
        /// <returns>Environment variables that should be set in the test execution environment</returns>
        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("key", "value") };
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
            this.logger = logger;
            TestSequence = new List<TestCase>();

            // Subscribing to events
            this.events.SessionEnd += this.SessionEnded_Handler;
            this.events.TestCaseStart += this.Events_TestCaseStart;
        }

        /// <summary>
        /// Called when Test Case Start event is invoked 
        /// </summary>
        private void Events_TestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            EqtTrace.Info(Constants.TestCaseStart);
            TestCase testcase = new TestCase(e.TestElement.FullyQualifiedName, e.TestElement.ExecutorUri, e.TestElement.Source);
            TestSequence.Add(testcase);
        }

        /// <summary>
        /// Called when Session End event is invoked 
        /// </summary>
        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            var filepath = Path.Combine(AppContext.BaseDirectory, Constants.AttachmentFileName);
            EqtTrace.Info(Constants.TestSessionEnd);
            this.dataWriter = new BlameDataReaderWriter(TestSequence, filepath, this.blameFileManager);
            this.dataWriter.WriteTestsToFile();
            this.dataCollectionSink.SendFileAsync(this.context.SessionDataCollectionContext, filepath, true);
        }

        /// <summary>
        /// Destructor to unregister methods and cleanup
        /// </summary>
        ~BlameCollector()
        {
            this.events.SessionEnd -= this.SessionEnded_Handler;
            this.events.TestCaseStart -= this.Events_TestCaseStart;
        }
    }
}
