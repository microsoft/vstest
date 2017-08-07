// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.PlatformAbstractions.Interfaces
{
    /// <summary>
    /// IBlameDumpFolder interface to get dump file name
    /// </summary>
    public interface IBlameDumpFolder
    {
        /// <summary>
        /// Gets crash dump folder path
        /// </summary>
        /// <param name="applicationName">application name</param>
        /// <param name="crashDumpPath">crash dump path</param>
        /// <returns>If crash dum enabled</returns>
        bool GetCrashDumpFolderPath(string applicationName, out string crashDumpPath);
    }
}
