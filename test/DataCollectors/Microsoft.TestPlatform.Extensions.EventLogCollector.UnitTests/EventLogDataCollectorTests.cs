// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
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

        private Mock<DataCollectionEvents> mockDataCollectionEvents;

        private TestableDataCollectionSink mockDataCollectionSink;

        private Mock<DataCollectionLogger> mockDataCollectionLogger;

        private TestableDataCollectionEnvironmentContext dataCollectionEnvironmentContext;

        private EventLogDataCollector eventLogDataCollector;

        public EventLogDataCollectorTests()
        {
            this.mockDataCollectionEvents = new Mock<DataCollectionEvents>();
            this.mockDataCollectionSink = new TestableDataCollectionSink();
            TestCase tc = new TestCase();
            DataCollectionContext dataCollectionContext =
                new DataCollectionContext(new SessionId(Guid.NewGuid()));
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
                            this.mockDataCollectionSink,
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
                            this.mockDataCollectionEvents.Object,
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
                            this.mockDataCollectionEvents.Object,
                            this.mockDataCollectionSink,
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

            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventLogNames, this.eventLogDataCollector.EventLogNames.Keys);
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

            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventLogNames, this.eventLogDataCollector.EventLogNames.Keys);
        }

        [TestMethod]
        public void InitializeShouldInitializeDefaultLogEntryTypes()
        {
            List<EventLogEntryType> entryTypes = new List<EventLogEntryType>();
            entryTypes.Add(EventLogEntryType.Error);
            entryTypes.Add(EventLogEntryType.Warning);
            entryTypes.Add(EventLogEntryType.FailureAudit);

            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(entryTypes, this.eventLogDataCollector.EntryTypes.Keys);
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
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(entryTypes, this.eventLogDataCollector.EntryTypes.Keys);
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
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventSources, this.eventLogDataCollector.EventSources.Keys);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeEventSourcesByDefault()
        {
            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.IsNull(this.eventLogDataCollector.EventSources);
        }

        [TestMethod]
        public void InitializeShouldInitializeMaxEntriesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /></Configuration>";

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.AreEqual(20, this.eventLogDataCollector.MaxEntries);
        }

        [TestMethod]
        public void InitializeShouldSetDefaultMaxEntries()
        {
            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.AreEqual(50000, this.eventLogDataCollector.MaxEntries);
        }

        [TestMethod]
        public void InitializeShouldSubscribeToDataCollectionEvents()
        {
            var testableDataCollectionEvents = new TestableDataCollectionEvents();
            this.eventLogDataCollector.Initialize(null, testableDataCollectionEvents, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestCaseStartInvocationList().Length);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestCaseEndInvocationList().Length);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestSessionEndInvocationList().Length);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestSessionStartInvocationList().Length);
        }

        [TestMethod]
        public void TestSessionStartEventShouldCreateEventLogContainer()
        {
            var testableEventLogDataCollector = new EventLogDataCollector();
            Assert.AreEqual(testableEventLogDataCollector.ContextData.Count, 0);
            testableEventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            this.mockDataCollectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs());
            Assert.AreEqual(testableEventLogDataCollector.ContextData.Count, 1);
        }

        [TestMethod]
        public void TestCaseStartEventShouldCreateEventLogContainer()
        {
            var testableEventLogDataCollector = new EventLogDataCollector();
            Assert.AreEqual(testableEventLogDataCollector.ContextData.Count, 0);

            testableEventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            this.mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid())), new TestCase()));
            Assert.AreEqual(testableEventLogDataCollector.ContextData.Count, 1);
        }

        [TestMethod]

        public void TestCaseEndEventShouldWriteEventLogEntriesAndSendFile()
        {
            var testableEventLogDataCollector = new EventLogDataCollector();
            testableEventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));
            this.mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(context, tc));
            this.mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed));
            Assert.IsTrue(this.mockDataCollectionSink.IsSendFileAsyncInvoked);
        }

        public void TestCaseEndEventShouldInvokeSendFileAsync()
        {
            var testableEventLogDataCollector = new EventLogDataCollector();
            testableEventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));
            this.mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(context, tc));
            this.mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed));
            Assert.IsTrue(this.mockDataCollectionSink.IsSendFileAsyncInvoked);
        }

        [TestMethod]
        public void TestCaseEndEventShouldThrowIfTestCaseStartIsNotInvoked()
        {
            var testableEventLogDataCollector = new EventLogDataCollector();
            testableEventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));

            Assert.ThrowsException<EventLogCollectorException>(() =>
            {
                this.mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed));
            });
        }

        public void SessionEndEventShouldThrowIfSessionStartEventtIsNotInvoked()
        {
            var testableEventLogDataCollector = new EventLogDataCollector();
            testableEventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var tc = new TestCase();

            Assert.ThrowsException<EventLogCollectorException>(() =>
                {
                    this.mockDataCollectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext));
                });
        }

        [TestMethod]
        public void TestSessionEndEventShouldWriteEventLogEntriesAndSendFile()
        {
            var testableEventLogDataCollector = new EventLogDataCollector();
            testableEventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var testcase = new TestCase() { Id = Guid.NewGuid() };
            this.mockDataCollectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext));
            this.mockDataCollectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext));
            Assert.IsTrue(this.mockDataCollectionSink.IsSendFileAsyncInvoked);
        }
    }

    /// <summary>
    /// The testable data collection environment context.
    /// </summary>
    public class TestableDataCollectionEnvironmentContext : DataCollectionEnvironmentContext
    {
        public TestableDataCollectionEnvironmentContext(DataCollectionContext sessionDataCollectionContext)
            : base(sessionDataCollectionContext)
        {
        }
    }

    /// <summary>
    /// The testable data collection events.
    /// </summary>
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

    /// <summary>
    /// The testable data collection sink.
    /// </summary>
    public class TestableDataCollectionSink : DataCollectionSink
    {
        /// <summary>
        /// The send file completed.
        /// </summary>
        public override event AsyncCompletedEventHandler SendFileCompleted;

        /// <summary>
        /// Gets or sets a value indicating whether is send file async invoked.
        /// </summary>
        public bool IsSendFileAsyncInvoked { get; set; }

        public override void SendFileAsync(FileTransferInformation fileTransferInformation)
        {
            this.IsSendFileAsyncInvoked = true;
            if (this.SendFileCompleted == null)
            {
                return;
            }
        }
    }
}
