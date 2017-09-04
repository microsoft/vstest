// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// This class writes event log entries to an XML file in a format that can be retrieved into a DataSet
    /// </summary>
    internal static class EventLogXmlWriter
    {
        #region Public methods

        /// <summary>
        /// The write event log entries to xml file.
        /// </summary>
        /// <param name="xmlFilePath">
        /// The xml file path.
        /// </param>
        /// <param name="eventLogEntries">
        /// The event log entries.
        /// </param>
        /// <param name="minDate">
        /// The min date.
        /// </param>
        /// <param name="maxDate">
        /// The max date.
        /// </param>
        public static void WriteEventLogEntriesToXmlFile(string xmlFilePath, List<EventLogEntry> eventLogEntries, DateTime minDate, DateTime maxDate)
        {
            DataTable dataTable = new DataTable();
            dataTable.Locale = CultureInfo.InvariantCulture;

            // The MaxLength of the Type and Source columns must be set to allow indices to be created on them
            DataColumn typeColumn = new DataColumn("Type", typeof(string));
            typeColumn.MaxLength = EventLogConstants.TypeColumnMaxLength;
            dataTable.Columns.Add(typeColumn);

            dataTable.Columns.Add(new DataColumn("DateTime", typeof(DateTime)));

            DataColumn sourceColumn = new DataColumn("Source", typeof(string));
            sourceColumn.MaxLength = EventLogConstants.SourceColumnMaxLength;
            dataTable.Columns.Add(sourceColumn);

            dataTable.Columns.Add(new DataColumn("Category", typeof(string)));
            dataTable.Columns.Add(new DataColumn("EventID", typeof(long)));
            dataTable.Columns.Add(new DataColumn("Description", typeof(string)));
            dataTable.Columns.Add(new DataColumn("User", typeof(string)));
            dataTable.Columns.Add(new DataColumn("Computer", typeof(string)));
            dataTable.ExtendedProperties.Add("TimestampColumnName", "DateTime");
            dataTable.ExtendedProperties.Add("IndexColumnNames", "Source,Type");

            foreach (EventLogEntry entry in eventLogEntries)
            {
                if (entry.TimeGenerated > minDate &&
                    entry.TimeGenerated < maxDate)
                {
                    DataRow row = dataTable.NewRow();
                    row["Type"] = entry.EntryType.ToString();
                    row["DateTime"] = entry.TimeGenerated;
                    row["Source"] = entry.Source;
                    row["Category"] = entry.Category;
                    row["EventID"] = entry.InstanceId;
                    row["Description"] = entry.Message;
                    row["User"] = entry.UserName;
                    row["Computer"] = entry.MachineName;
                    dataTable.Rows.Add(row);
                }
            }

            DataSet dataSet = new DataSet();
            dataSet.Locale = CultureInfo.InvariantCulture;
            dataSet.Tables.Add(dataTable);

            // Use UTF-16 encoding
            using (System.IO.StreamWriter xmlStreamWriter = new System.IO.StreamWriter(xmlFilePath, false, Encoding.UTF8))
            {
                dataSet.WriteXml(xmlStreamWriter, XmlWriteMode.WriteSchema);
                xmlStreamWriter.Close();
            }

            dataSet.Dispose();
        }
        #endregion
    }
}
