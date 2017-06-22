// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using FileHelper = Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.FileHelper;
    using System.Text;

    [DataCollectorFriendlyName("Blame")]
    [DataCollectorTypeUri("my://sample/datacollector")]
    class BlameDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        private DataCollectionSink dataCollectionSink;
        private DataCollectionEnvironmentContext context;
        private DataCollectionLogger logger;
        private DataCollectionEvents events;
        private IFileHelper fileHelper;
        private XmlDocument doc;
        private XmlElement blameTestRoot;
        private string filepath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameDataCollector"/> class.
        /// </summary>
        public BlameDataCollector()
            : this(new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameDataCollector"/> class.
        /// </summary>
        /// <param name="fileHelper">File helper instance.</param>
        internal BlameDataCollector(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
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
            this.events.SessionStart += this.SessionStarted_Handler;
            this.events.SessionEnd += this.SessionEnded_Handler;
            this.events.TestCaseStart += this.Events_TestCaseStart;
            this.events.TestCaseEnd += this.Events_TestCaseEnd;
            this.dataCollectionSink = dataSink;
            this.context = environmentContext;
            this.logger = logger;
            InitializeXml();
        }

        /// <summary>
        /// Called when Test Case End event is invoked 
        /// </summary>
        private void Events_TestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            EqtTrace.Info(Constants.TestCaseEnd);
        }

        /// <summary>
        /// Called when Test Case Start event is invoked 
        /// </summary>
        private void Events_TestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            EqtTrace.Info(Constants.TestCaseStart);
            WriteToXml(e.TestElement.FullyQualifiedName, e.TestElement.Source);
        }

        /// <summary>
        /// Called when Session Start Event is invoked 
        /// </summary>
        private void SessionStarted_Handler(object sender, SessionStartEventArgs args)
        {
            EqtTrace.Info(Constants.TestSessionStart);
        }

        /// <summary>
        /// Called when Session End event is invoked 
        /// </summary>
        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            EqtTrace.Info(Constants.TestSessionEnd);
            SaveXmlDocument();
            this.dataCollectionSink.SendFileAsync(this.context.SessionDataCollectionContext, this.filepath, true);
        }

        /// <summary>
        /// To Initialize xml writing for writing test cases to file
        /// </summary>
        private void InitializeXml()
        {
            this.filepath = Path.Combine(AppContext.BaseDirectory, Constants.AttachmentFileName);
            doc = new XmlDocument();
            var xmlDeclaration = doc.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty);
            blameTestRoot = doc.CreateElement(Constants.BlameRootNode);
            doc.AppendChild(xmlDeclaration);
        }

        /// <summary>
        /// Writing each test node to xml file
        /// </summary>
        private void WriteToXml(string fullyQualifiedName, string source)
        {
            var testElement = doc.CreateElement(Constants.BlameTestNode);
            testElement.SetAttribute(Constants.TestNameAttribute, fullyQualifiedName);
            testElement.SetAttribute(Constants.TestSourceAttribute, source);
            blameTestRoot.AppendChild(testElement);  
        }

        /// <summary>
        /// Saving Xml Document to file for attachment to be sent
        /// </summary>
        private void SaveXmlDocument()
        {
            doc.AppendChild(blameTestRoot);

            using (var stream = new FileHelper().GetStream(this.filepath, FileMode.Create))
            {
                doc.Save(stream);
            }
        }

        /// <summary>
        /// Destructor to unregister methods and cleanup
        /// </summary>
        ~BlameDataCollector()
        {
            this.events.SessionStart -= this.SessionStarted_Handler;
            this.events.SessionEnd -= this.SessionEnded_Handler;
            this.events.TestCaseStart -= this.Events_TestCaseStart;
            this.events.TestCaseEnd -= this.Events_TestCaseEnd;
        }

        /// <summary>
        /// Only used for testing purposes
        /// </summary>
        public XmlDocument ValidateXmlWriter(string fullyQualifiedName, string source)
        {
            InitializeXml();
            WriteToXml(fullyQualifiedName,source);
            doc.AppendChild(blameTestRoot);
            return doc;
        }

    }

    /// <summary>
    /// Class for constants used across the class.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// The test session start constant.
        /// </summary>
        public const string TestSessionStart = "TestSessionStart";

        /// <summary>
        /// The test session end constant.
        /// </summary>
        public const string TestSessionEnd = "TestSessionEnd";

        /// <summary>
        /// The test case start constant.
        /// </summary>
        public const string TestCaseStart = "TestCaseStart";

        /// <summary>
        /// The test case end method name.
        /// </summary>
        public const string TestCaseEnd = "TestCaseEnd";

        /// <summary>
        /// Root node name for Xml file.
        /// </summary>
        public const string BlameRootNode = "TestSequence";

        /// <summary>
        /// Node name for each Xml node.
        /// </summary>
        public const string BlameTestNode = "Test";

        /// <summary>
        /// Attachment File name.
        /// </summary>
        public const string AttachmentFileName = "TestSequence.xml";

        /// <summary>
        /// Test Name Attribute.
        /// </summary>
        public const string TestNameAttribute= "Name";

        /// <summary>
        /// Test Source Attribute.
        /// </summary>
        public const string TestSourceAttribute = "Source";
    }
}
