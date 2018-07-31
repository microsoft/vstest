// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public IntPtr GetProcessHandleById(int processId)
        {
            return Process.GetProcessById(processId).SafeHandle.DangerousGetHandle();
        }
    }
}