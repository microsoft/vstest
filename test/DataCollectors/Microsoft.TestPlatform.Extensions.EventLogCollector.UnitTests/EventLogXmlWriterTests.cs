﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests;

using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using VisualStudio.TestTools.UnitTesting;

using Moq;

[TestClass]
public class EventLogXmlWriterTests
{
    private const string FileName = "Event Log.xml";

    private const string DefaultEventLog = @"<NewDataSet><xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"" xmlns:msprop=""urn:schemas-microsoft-com:xml-msprop""><xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:Locale=""""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""Table1"" msdata:Locale="""" msprop:IndexColumnNames=""Source,Type"" msprop:TimestampColumnName=""DateTime""><xs:complexType><xs:sequence><xs:element name=""Type"" minOccurs=""0""><xs:simpleType><xs:restriction base=""xs:string""><xs:maxLength value=""64"" /></xs:restriction></xs:simpleType></xs:element><xs:element name=""DateTime"" type=""xs:dateTime"" minOccurs=""0"" /><xs:element name=""Source"" minOccurs=""0""><xs:simpleType><xs:restriction base=""xs:string""><xs:maxLength value=""212"" /></xs:restriction></xs:simpleType></xs:element><xs:element name=""Category"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""EventID"" type=""xs:long"" minOccurs=""0"" /><xs:element name=""Description"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""User"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""Computer"" type=""xs:string"" minOccurs=""0"" /></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema></NewDataSet>";

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
