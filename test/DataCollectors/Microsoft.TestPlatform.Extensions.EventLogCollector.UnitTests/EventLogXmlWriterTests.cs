// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;

    using Moq;

    [TestClass]
    public class EventLogXmlWriterTests
    {
        private const string FileName = "Event Log.xml";

        private const string DefaultEventLog = @"<NewDataSet><xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"" xmlns:msprop=""urn:schemas-microsoft-com:xml-msprop""><xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:Locale=""""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""Table1"" msdata:Locale="""" msprop:IndexColumnNames=""Source,Type"" msprop:TimestampColumnName=""DateTime""><xs:complexType><xs:sequence><xs:element name=""Type"" minOccurs=""0""><xs:simpleType><xs:restriction base=""xs:string""><xs:maxLength value=""64"" /></xs:restriction></xs:simpleType></xs:element><xs:element name=""DateTime"" type=""xs:dateTime"" minOccurs=""0"" /><xs:element name=""Source"" minOccurs=""0""><xs:simpleType><xs:restriction base=""xs:string""><xs:maxLength value=""212"" /></xs:restriction></xs:simpleType></xs:element><xs:element name=""Category"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""EventID"" type=""xs:long"" minOccurs=""0"" /><xs:element name=""Description"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""User"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""Computer"" type=""xs:string"" minOccurs=""0"" /></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema></NewDataSet>";
        private const string EventLogWithLogEntry = @"<NewDataSet><xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"" xmlns:msprop=""urn:schemas-microsoft-com:xml-msprop""><xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:Locale=""""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""Table1"" msdata:Locale="""" msprop:IndexColumnNames=""Source,Type"" msprop:TimestampColumnName=""DateTime""><xs:complexType><xs:sequence><xs:element name=""Type"" minOccurs=""0""><xs:simpleType><xs:restriction base=""xs:string""><xs:maxLength value=""64"" /></xs:restriction></xs:simpleType></xs:element><xs:element name=""DateTime"" type=""xs:dateTime"" minOccurs=""0"" /><xs:element name=""Source"" minOccurs=""0""><xs:simpleType><xs:restriction base=""xs:string""><xs:maxLength value=""212"" /></xs:restriction></xs:simpleType></xs:element><xs:element name=""Category"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""EventID"" type=""xs:long"" minOccurs=""0"" /><xs:element name=""Description"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""User"" type=""xs:string"" minOccurs=""0"" /><xs:element name=""Computer"" type=""xs:string"" minOccurs=""0"" /></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><Table1><Type>Warning</Type><DateTime>2017-08-11T14:31:21+05:30</DateTime><Source>MockSource</Source><Category>(0)</Category><EventID>123EventID><Description>MockMessage</Description><Computer>MachineName</Computer></Table1></NewDataSet>";

        [TestMethod]
        public void WriteEventLogEntriesToXmlFileShouldWriteToXMLFile()
        {
            try
            {
                List<EventLogEntry> eventLogEntries = new List<EventLogEntry>();
                EventLogXmlWriter.WriteEventLogEntriesToXmlFile(
                    FileName,
                    eventLogEntries,
                    DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)),
                    DateTime.Now.Add(new TimeSpan(1, 0, 0, 0)));

                XmlDocument document = new XmlDocument();
                document.LoadXml(DefaultEventLog);

                XmlDocument document1 = new XmlDocument();
                document1.Load(FileName);

                Assert.AreEqual(document1.InnerText, document.InnerText);
                Assert.IsTrue(File.Exists(FileName));
            }
            finally
            {
                File.Delete(FileName);
            }
        }

        //[TestMethod]
        //public void WriteEventLogEntriesToXmlFileShouldWriteLogEntryIfPresent()
        //{
        //    try
        //    {
        //        Mock<EventLogEntry> mockEventLogEntry = new Mock<EventLogEntry>();
        //        mockEventLogEntry.SetupGet(x => x.EntryType).Returns(EventLogEntryType.Warning);
        //        mockEventLogEntry.SetupGet(x => x.TimeGenerated).Returns(DateTime.Now);
        //        mockEventLogEntry.SetupGet(x => x.Source).Returns("MockSource");
        //        mockEventLogEntry.SetupGet(x => x.Category).Returns("(0)");
        //        mockEventLogEntry.SetupGet(x => x.InstanceId).Returns(123);
        //        mockEventLogEntry.SetupGet(x => x.Message).Returns("MockMessage");
        //        mockEventLogEntry.SetupGet(x => x.UserName).Returns("UserName");
        //        mockEventLogEntry.SetupGet(x => x.MachineName).Returns("MachineName");

        //        List<EventLogEntry> eventLogEntries = new List<EventLogEntry>();
        //        eventLogEntries.Add(mockEventLogEntry.Object);

        //        EventLogXmlWriter.WriteEventLogEntriesToXmlFile(
        //            FileName,
        //            eventLogEntries,
        //            DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)),
        //            DateTime.Now.Add(new TimeSpan(1, 0, 0, 0)));

        //        XmlDocument expectedXmlDoc = new XmlDocument();
        //        expectedXmlDoc.LoadXml(EventLogWithLogEntry);

        //        XmlDocument xmlDoc = new XmlDocument();
        //        xmlDoc.Load(FileName);

        //        Assert.AreEqual(expectedXmlDoc.InnerText, xmlDoc.InnerText);
        //        Assert.IsTrue(File.Exists(FileName));
        //    }
        //    finally
        //    {
        //        File.Delete(FileName);
        //    }
        //}
    }
}
