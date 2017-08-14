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
        /// This is the event handler for the EntryWritten event of the System.Diagnostics.EventLog class.
        /// Note that the documentation for the EntryWritten event includes these remarks:
        ///     "The system responds to WriteEntry only if the last write event occurred at least five seconds previously. 
        ///      This implies you will only receive one EntryWritten event notification within a five-second interval, even if more
        ///      than one event log change occurs. If you insert a sufficiently long sleep interval (around 10 seconds) between calls
        ///      to WriteEntry, no events will be lost. However, if write events occur more frequently, the most recent write events 
        ///      could be lost."
        /// This complicates this data collector because we don't want to sleep to wait for all events or lose the most recent events.
        /// To workaround, the implementation does several things:
        /// 1. We get the EventLog entries to collect from the EventLog.Entries collection and ignore the EntryWrittenEventArgs.
        /// 2. When event log collection ends for a data collection context, this method is called explicitly by the EventLogDataCollector
        ///    passing null for EntryWrittenEventArgs (which is fine since the argument is ignored.
        /// 3. We keep track of which EventLogEntry object in the EventLog.Entries we still need to collect.  We do this by inspecting
        ///    the value of the EventLogEntry.Index property.  The value of this property is an integer that is incremented for each entry
        ///    that is written to the event log, but is reset to 0 if the entire event log is cleared.
        /// Another behavior of event logs that we need to account for is that if the event log reaches a size limit, older events are
        /// automatically deleted.  In this case the collection EventLog.Entries contains only the entries remaining in the log,
        /// and the value of the EventLog.Entries[0].Index will not be 0; it will be the index of the oldest entry still in the log.
        /// For example, if the first 1000 entries written to an event log (since it was last completely cleared) are deleted because
        /// of the size limitation, then EventLog.Entries[0].Index would have a value of 1000 (this value is saved in the local variable
        /// "firstIndexInLog" in the method implementation.  Similarly "mostRecentIndexInLog" is the index of the last entry written
        /// to the log at the time we examine it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e">The System.Diagnostics.EntryWrittenEventArgs object describing the entry that was written.</param>
        void OnEventLogEntryWritten(object source, EntryWrittenEventArgs e);
    }
}
