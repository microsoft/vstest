// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests;

[TestClass]
public class EventLogXmlWriterTests
{
    private const string FileName = "Event Log.xml";

    private readonly EventLog _eventLog;
    private readonly EventLogEntry _eventLogEntry;
    private readonly List<EventLogEntry> _eventLogEntries;
    private readonly Mock<IFileHelper> _mockFileHelper;

    public EventLogXmlWriterTests()
    {
        _eventLog = new EventLog("Application");
        var count = _eventLog.Entries.Count;
        _eventLogEntry = _eventLog.Entries[count - 1];
        _eventLogEntries = new List<EventLogEntry>();
        _mockFileHelper = new Mock<IFileHelper>();
    }

    [TestMethod]
    public void WriteEventLogEntriesToXmlFileShouldWriteToXmlFile()
    {
        EventLogXmlWriter.WriteEventLogEntriesToXmlFile(
            FileName,
            _eventLogEntries,
            _mockFileHelper.Object);

        _mockFileHelper.Verify(x => x.WriteAllTextToFile(FileName, It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public void WriteEventLogEntriesToXmlFileShouldWriteLogEntryIfPresent()
    {
        _eventLogEntries.Add(_eventLogEntry);

        EventLogXmlWriter.WriteEventLogEntriesToXmlFile(FileName, _eventLogEntries, _mockFileHelper.Object);

        _mockFileHelper.Verify(x => x.WriteAllTextToFile(FileName, It.Is<string>(str => str.Contains(_eventLogEntry.Message))));
    }
}
