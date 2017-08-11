// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class EventLogDataCollectorTests
    {
        private const string ConfigurationString =
            @"<Configuration><Setting name=""key"" value=""value"" /></Configuration>";

        private TestableDataCollectionEvents mockDataCollectionEvents;

        private Mock<DataCollectionSink> mockDataCollectionSink;

        private Mock<DataCollectionLogger> mockDataCollectionLogger;

        private TestableDataCollectionEnvironmentContext dataCollectionEnvironmentContext;

        private EventLogDataCollector eventLogDataCollector;

        public EventLogDataCollectorTests()
        {
            this.mockDataCollectionEvents = new TestableDataCollectionEvents();
            this.mockDataCollectionSink = new Mock<DataCollectionSink>();
            TestCase tc = new TestCase();
            DataCollectionContext dataCollectionContext = new DataCollectionContext(tc);
            this.dataCollectionEnvironmentContext = new TestableDataCollectionEnvironmentContext(dataCollectionContext);
            this.mockDataCollectionLogger = new Mock<DataCollectionLogger>();
            this.eventLogDataCollector = new EventLogDataCollector();
        }


        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.eventLogDataCollector.Initialize(
                            null,
                            null,
                            this.mockDataCollectionSink.Object,
                            this.mockDataCollectionLogger.Object,
                            this.dataCollectionEnvironmentContext);
                    });
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfCollectionSinkIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.eventLogDataCollector.Initialize(
                            null,
                            this.mockDataCollectionEvents,
                            null,
                            this.mockDataCollectionLogger.Object,
                            this.dataCollectionEnvironmentContext);
                    });
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.eventLogDataCollector.Initialize(
                            null,
                            this.mockDataCollectionEvents,
                            this.mockDataCollectionSink.Object,
                            null,
                            this.dataCollectionEnvironmentContext);
                    });
        }

        [TestMethod]
        public void InitializeShouldInitializeDefaultEventLogNames()
        {
            List<string> eventLogNames = new List<string>();
            eventLogNames.Add("System");
            eventLogNames.Add("Security");
            eventLogNames.Add("Application");

            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventLogNames, this.eventLogDataCollector.EventLogNames);
        }

        [TestMethod]
        public void InitializeShouldInitializeCustomEventLogNamesIfSpecifiedInConfiguration()
        {
            string configurationString =
            @"<Configuration><Setting name=""EventLogs"" value=""MyEventName,MyEventName2"" /></Configuration>";

            List<string> eventLogNames = new List<string>();
            eventLogNames.Add("MyEventName");
            eventLogNames.Add("MyEventName2");

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);

            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventLogNames, this.eventLogDataCollector.EventLogNames);
        }

        [TestMethod]
        public void InitializeShouldInitializeDefaultLogEntryTypes()
        {
            List<EventLogEntryType> entryTypes = new List<EventLogEntryType>();
            entryTypes.Add(EventLogEntryType.Error);
            entryTypes.Add(EventLogEntryType.Warning);
            entryTypes.Add(EventLogEntryType.FailureAudit);

            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(entryTypes, this.eventLogDataCollector.EntryTypes);
        }

        [TestMethod]
        public void InitializeShouldInitializeEntryTypesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""EntryTypes"" value=""Error"" /></Configuration>";

            List<EventLogEntryType> entryTypes = new List<EventLogEntryType>();
            entryTypes.Add(EventLogEntryType.Error);

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(entryTypes, this.eventLogDataCollector.EntryTypes);
        }

        [TestMethod]
        public void InitializeShouldInitializeEventSourcesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""EventSources"" value=""MyEventSource"" /></Configuration>";

            List<string> eventSources = new List<string>();
            eventSources.Add("MyEventSource");

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventSources, this.eventLogDataCollector.EventSources);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeEventSourcesByDefault()
        {
            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.IsNull(this.eventLogDataCollector.EventSources);
        }

        [TestMethod]
        public void InitializeShouldInitializeMaxEntriesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /></Configuration>";

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.AreEqual(20, this.eventLogDataCollector.MaxEntries);
        }

        [TestMethod]
        public void InitializeShouldSetDefaultMaxEntries()
        {
            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.AreEqual(50000, this.eventLogDataCollector.MaxEntries);
        }


        [TestMethod]
        public void InitializeShouldSubscribeToDataCollectionEvents()
        {
            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents, this.mockDataCollectionSink.Object, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            Assert.AreEqual(1, this.mockDataCollectionEvents.GetTestCaseStartInvocationList().Length);
            Assert.AreEqual(1, this.mockDataCollectionEvents.GetTestCaseEndInvocationList().Length);
            Assert.AreEqual(1, this.mockDataCollectionEvents.GetTestSessionEndInvocationList().Length);
            Assert.AreEqual(1, this.mockDataCollectionEvents.GetTestSessionStartInvocationList().Length);
        }
    }

    public class TestableDataCollectionEnvironmentContext : DataCollectionEnvironmentContext
    {
        public TestableDataCollectionEnvironmentContext(DataCollectionContext sessionDataCollectionContext)
            : base(sessionDataCollectionContext)
        {
        }
    }

    public class TestableDataCollectionEvents : DataCollectionEvents
    {
        public override event EventHandler<SessionStartEventArgs> SessionStart;

        public override event EventHandler<SessionEndEventArgs> SessionEnd;

        public override event EventHandler<TestCaseStartEventArgs> TestCaseStart;

        public override event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        public Delegate[] GetTestCaseStartInvocationList()
        {
            return this.TestCaseStart.GetInvocationList();
        }

        public Delegate[] GetTestCaseEndInvocationList()
        {
            return this.TestCaseEnd.GetInvocationList();
        }

        public Delegate[] GetTestSessionStartInvocationList()
        {
            return this.SessionStart.GetInvocationList();
        }

        public Delegate[] GetTestSessionEndInvocationList()
        {
            return this.SessionEnd.GetInvocationList();
        }
    }
}
