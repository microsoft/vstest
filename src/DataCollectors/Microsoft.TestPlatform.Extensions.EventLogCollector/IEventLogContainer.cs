// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System.Diagnostics;

    internal interface IEventLogContainer
    {
        EventLog EventLog { get; set; }

        void OnEventLogEntryWritten(object source, EntryWrittenEventArgs e);
    }
}
