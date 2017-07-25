// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using global::System;
    using Microsoft.TestPlatform.PlatformAbstractions.Interfaces;

    public class BlameDumpFolder : IBlameDumpFolder
    {
        public bool GetCrashDumpFolderPath(string applicationName, out string crashDumpPath)
        {
            throw new NotImplementedException();
        }
    }
}
