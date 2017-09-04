// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System.Diagnostics;

    /// <summary>
    /// Event log container interface
    /// </summary>
    internal interface IEventLogContainer
    {
        /// <summary>
        /// Gets or sets the event log.
        /// </summary>
        EventLog EventLog { get; set; }

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
    }
}
