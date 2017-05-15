// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.IO;
    
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Encapsulates datacollector object and other objects required to facilitate datacollection.
    /// </summary>
    internal class DataCollectorInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorInformation"/> class.
        /// </summary>
        /// <param name="dataCollector">
        /// The data collector.
        /// </param>
        /// <param name="configurationElement">
        /// The configuration element.
        /// </param>
        /// <param name="dataCollectorConfig">
        /// The data collector config.
        /// </param>
        /// <param name="environmentContext">
        /// The environment Context.
        /// </param>
        /// <param name="attachmentManager">
        /// The attachment Manager.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="messageSink">
        /// The message Sink.
        /// </param>
        /// <param name="settingsXml"></param>
        internal DataCollectorInformation(DataCollector dataCollector, XmlElement configurationElement, DataCollectorConfig dataCollectorConfig, DataCollectionEnvironmentContext environmentContext, IDataCollectionAttachmentManager attachmentManager, TestPlatformDataCollectionEvents events, IMessageSink messageSink, string settingsXml)
        {
            this.DataCollector = dataCollector;
            this.ConfigurationElement = configurationElement;
            this.DataCollectorConfig = dataCollectorConfig;
            this.Events = events;
            this.EnvironmentContext = environmentContext;
            this.DataCollectionSink = new TestPlatformDataCollectionSink(attachmentManager, dataCollectorConfig);
            this.Logger = new TestPlatformDataCollectionLogger(messageSink, dataCollectorConfig);
            this.SettingsXml = settingsXml;
        }

        /// <summary>
        /// Gets or sets the data collector.
        /// </summary>
        public DataCollector DataCollector { get; set; }

        /// <summary>
        /// Gets or sets the configuration element.
        /// </summary>
        public XmlElement ConfigurationElement { get; set; }

        /// <summary>
        /// Gets or sets the data collector config.
        /// </summary>
        public DataCollectorConfig DataCollectorConfig { get; set; }

        /// <summary>
        /// Gets the events object on which the collector registers for events
        /// </summary>
        public TestPlatformDataCollectionEvents Events { get; private set; }

        /// <summary>
        /// Gets the datacollection sink.
        /// </summary>
        public TestPlatformDataCollectionSink DataCollectionSink { get; private set; }

        /// <summary>
        /// Gets the data collection environment context.
        /// </summary>
        public DataCollectionEnvironmentContext EnvironmentContext { get; private set; }

        /// <summary>
        /// Gets the data collection logger
        /// </summary>
        public TestPlatformDataCollectionLogger Logger { get; private set; }

        /// <summary>
        /// Gets the data collection logger
        /// </summary>
        private string SettingsXml { get; set; }

        private const string DefaultConfigurationSettings = @"<Configuration />";

        /// <summary>
        /// Gets or sets environment variables supplied by the data collector.
        /// These are available after the collector has been initialized.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> TestExecutionEnvironmentVariables
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes datacollectors.
        /// </summary>
        internal void InitializeDataCollector()
        {
            UpdateConfigurationElement();

            this.DataCollector.Initialize(this.ConfigurationElement, this.Events, this.DataCollectionSink, this.Logger, this.EnvironmentContext);
        }

        private void UpdateConfigurationElement()
        {
            var frameWork = XmlRunSettingsUtilities.GetRunConfigurationNode(this.SettingsXml).TargetFrameworkVersion;

            if (this.ConfigurationElement == null)
            {
                var doc = new XmlDocument();
                using (
                    var xmlReader = XmlReader.Create(
                        new StringReader(DefaultConfigurationSettings),
                        new XmlReaderSettings{ CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                {
                    doc.Load(xmlReader);
                }

                this.ConfigurationElement = doc.DocumentElement;
            }

            // Add Framework config, since it could be required by DataCollector, to determine whether they support this Framework or not
            if (frameWork != null)
            {
                AppendChildNodeOrInnerText(this.ConfigurationElement.OwnerDocument, this.ConfigurationElement, "Framework", "", frameWork.Name);
            }
        }

        private static void AppendChildNodeOrInnerText(XmlDocument doc, XmlElement owner, string elementName, string nameSpaceUri, string innerText)
        {
            var node = owner.SelectSingleNode(elementName) ?? doc.CreateNode("element", elementName, nameSpaceUri);
            node.InnerText = innerText;
            owner.AppendChild(node);
        }

        /// <summary>
        /// Disposes datacollector.
        /// </summary>
        internal void DisposeDataCollector()
        {
            try
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("dataCollectorInfo.DisposeDataCollector: calling Dispose() on {0}", this.DataCollector.GetType());
                }

                this.DataCollector.Dispose();
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectorInfo.DisposeDataCollector: exception while calling Dispose() on {0}: " + ex, this.DataCollector.GetType());
                }
            }
        }

        /// <summary>
        /// The get test execution environment variables sync.
        /// </summary>
        public void SetTestExecutionEnvironmentVariables()
        {
            var testExecutionEnvironmentSpecifier = this.DataCollector as ITestExecutionEnvironmentSpecifier;
            if (testExecutionEnvironmentSpecifier != null)
            {
                // Get the environment variables the data collector wants set in the test execution environment
                this.TestExecutionEnvironmentVariables = testExecutionEnvironmentSpecifier.GetTestExecutionEnvironmentVariables();
            }
        }
    }
}