// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Runtime.InteropServices;

    public class Win32NamedEvent
    {
        private const uint EventModifyState = 0x0002;

        private string eventName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Win32NamedEvent"/> class.
        /// Create a NamedEvent object with the name of the event, and assume auto reset with
        /// an initial state of reset.
        /// </summary>
        /// <param name="eventName">The name of the win32 event</param>
        public Win32NamedEvent(string eventName)
        {
            this.eventName = eventName;
        }

        /// <summary>
        /// Set the named event to a signaled state. The Wait() method will not block any
        /// thread as long as the event is in a signaled state.
        /// </summary>
        public void Set()
        {
            IntPtr handle = OpenEvent(Win32NamedEvent.EventModifyState, false, this.eventName);
            SetEvent(handle);
            CloseHandle(handle);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool SetEvent(IntPtr hEvent);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenEvent(uint dwDesiredAccess, bool bInheritHandle, string lpName);
    }
}
