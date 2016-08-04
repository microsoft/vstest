// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.DataCollection.Implementations;

    using Microsoft.VisualStudio.TestPlatform.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.Common;

    using DataCollectionEnvironmentContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionEnvironmentContext;
    using DataCollector = Microsoft.VisualStudio.TestTools.Execution.DataCollector;
    using DataCollectorInformation = Microsoft.VisualStudio.TestTools.Execution.DataCollectorInformation;
    using ITestExecutionEnvironmentSpecifier = Microsoft.VisualStudio.TestTools.Execution.ITestExecutionEnvironmentSpecifier;

    /// <summary>
    /// The test platform data collector info.
    /// </summary>
    public class TestPlatformDataCollectorInfo
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatformDataCollectorInfo"/> class by creating a message sink, data sink, and logger for the
        /// data collector
        /// </summary>
        /// <param name="dataCollector">
        /// The data collector
        /// </param>
        /// <param name="configurationElement">
        /// XML element containing configuration information for the collector, or null if
        /// there is no configuration
        /// </param>
        /// <param name="messageSink">
        /// the message sink to send messages to
        /// </param>
        /// <param name="dataCollectorInformation">
        /// Information about the data collector.
        /// </param>
        /// <param name="userWorkItemFactory">
        /// A factory for creating user work items
        /// </param>
        internal TestPlatformDataCollectorInfo(
            DataCollector dataCollector,
            XmlElement configurationElement,
            IMessageSink messageSink,
            DataCollectorInformation dataCollectorInformation,
            SafeAbortableUserWorkItemFactory userWorkItemFactory)
        {
            ValidateArg.NotNull<DataCollector>(dataCollector, "dataCollector");
            ValidateArg.NotNull<IMessageSink>(messageSink, "messageSink");
            ValidateArg.NotNull<DataCollectorInformation>(dataCollectorInformation, "dataCollectorInformation");
            ValidateArg.NotNull<SafeAbortableUserWorkItemFactory>(userWorkItemFactory, "userWorkItemFactory");

            this.DataCollectorInformation = dataCollectorInformation;
            this.DataCollector = dataCollector;
            this.ConfigurationElement = configurationElement;
            this.Events = new TestPlatformDataCollectionEvents(userWorkItemFactory);
            this.Logger = new TestPlatformDataCollectionLogger(messageSink, dataCollectorInformation);
            this.DataSink = new TestPlatformDataCollectionSink(messageSink, dataCollectorInformation);
        }

        #endregion

        /// <summary>
        /// Gets the data collector
        /// </summary>
        internal DataCollector DataCollector
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the configuration XML element
        /// </summary>
        internal XmlElement ConfigurationElement
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the events object on which the collector registers for events
        /// </summary>
        internal TestPlatformDataCollectionEvents Events
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets environment variables supplied by the data collector.
        /// These are available after the collector has been initialized.
        /// </summary>
        internal IEnumerable<KeyValuePair<string, string>> TestExecutionEnvironmentVariables
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the data sink
        /// </summary>
        internal TestPlatformDataCollectionSink DataSink
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the logger
        /// </summary>
        internal TestPlatformDataCollectionLogger Logger
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets information about the data collector.
        /// </summary>
        internal DataCollectorInformation DataCollectorInformation
        {
            get;
            private set;
        }

        #region Public methods

        /// <summary>
        /// Initializes the data collector synchronously
        /// </summary>
        /// <param name="dataCollectionEnvironmentContext">Context provided to the data collector</param>
        public void InitializeDataCollector(DataCollectionEnvironmentContext dataCollectionEnvironmentContext)
        {
            this.DataCollector.Initialize(this.ConfigurationElement, this.Events, this.DataSink, this.Logger, dataCollectionEnvironmentContext);
        }

        /// <summary>
        /// The get test execution environment variables sync.
        /// </summary>
        public void GetTestExecutionEnvironmentVariables()
        {
            var testExecutionEnvironmentSpecifier = this.DataCollector as ITestExecutionEnvironmentSpecifier;
            if (testExecutionEnvironmentSpecifier != null)
            {
                // Get the environment variables the data collector wants set in the test execution environment
                this.TestExecutionEnvironmentVariables = testExecutionEnvironmentSpecifier.GetTestExecutionEnvironmentVariables();
            }
        }

        /// <summary>
        /// Disposes the data collector
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void DisposeDataCollector()
        {
            try
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectorInfo.DisposeDataCollector: calling Dispose() on {0}", this.DataCollector.GetType());
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

        #endregion
    }
}