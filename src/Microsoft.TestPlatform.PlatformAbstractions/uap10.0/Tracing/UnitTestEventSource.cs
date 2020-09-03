// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Diagnostics.Tracing;

    internal sealed class UnitTestEventSource : EventSource
    {
        private static UnitTestEventSource log = new UnitTestEventSource();

        public static UnitTestEventSource Log
        {
            get
            {
                return log;
            }
        }

        [Event(1, Level = EventLevel.Verbose)]
        public void Verbose(string message)
        {
            this.WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void Info(string message)
        {
            this.WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        public void Warn(string message)
        {
            this.WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        public void Error(string message)
        {
            this.WriteEvent(4, message);
        }

        [Event(5, Level = EventLevel.Critical)]
        public void Critical(string message)
        {
            this.WriteEvent(5, message);
        }
    }
}

#endif
