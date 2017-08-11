// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    internal static class EventLogShared
    {
        // Supported configuration setting names
        public const string SETTING_EVENT_LOGS = "EventLogs";
        public const string SETTING_EVENT_SOURCES = "EventSources";
        public const string SETTING_ENTRY_TYPES = "EntryTypes";
        public const string SETTING_MAX_ENTRIES = "MaxEventLogEntriesToCollect";

        // default values
        public const int DEFAULT_MAX_ENTRIES = 50000;
    }
}