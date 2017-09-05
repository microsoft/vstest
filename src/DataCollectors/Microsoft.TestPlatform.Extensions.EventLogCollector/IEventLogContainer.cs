// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Event log container interface
    /// </summary>
    internal interface IEventLogContainer
    {
        /// <summary>
        /// Event Handler for handling log entries.
        /// </summary>
        /// <param name="source">
        /// The source object that raised EventLog entry event.
        /// </param>
        /// <param name="e">
        /// Contains data related to EventLog entry.
        /// </param>
        void OnEventLogEntryWritten(object source, EntryWrittenEventArgs e);

        /// <summary>
        /// Writes EventLogs to xml file
        /// </summary>
        /// <param name="dataCollectionContext">
        /// The data Collection Context.
        /// </param>
        /// <param name="requestedDuration">
        /// The duration for which the EventLogs are collected.
        /// </param>
        /// <param name="timeRequestReceived">
        /// The time when the requrest for WriteEventLogs are received. Events received after this time will not be logged.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>
        /// Path to the xml log file.
        /// </returns>
        string WriteEventLogs(DataCollectionContext dataCollectionContext, TimeSpan requestedDuration, DateTime timeRequestReceived);
    }
}
