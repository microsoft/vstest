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
        /// The on event log entry written.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        void OnEventLogEntryWritten(object source, EntryWrittenEventArgs e);
    }
}
