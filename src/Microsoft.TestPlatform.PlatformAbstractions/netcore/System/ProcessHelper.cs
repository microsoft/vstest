// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    public partial class ProcessHelper : IProcessHelper
    {
        /// <inheritdoc/>
        public string GetCurrentProcessLocation()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        /// <inheritdoc/>
        public IntPtr GetProcessHandle(int processId)
        {
            // An IntPtr representing the value of the handle field.
            // If the handle has been marked invalid with SetHandleAsInvalid, this method still returns the original handle value, which can be a stale value.
            return Process.GetProcessById(processId).SafeHandle.DangerousGetHandle();
        }
    }
}

#endif
