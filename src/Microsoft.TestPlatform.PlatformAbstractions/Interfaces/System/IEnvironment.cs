// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces
{
    /// <summary>
    /// Operating system environment abstractions.
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>
        /// Operating System architecture.
        /// </summary>
        PlatformArchitecture Architecture { get; }

        /// <summary>
        /// Operating System name.
        /// </summary>
        PlatformOperatingSystem OperatingSystem { get; }

        /// <summary>
        /// Exits the current process as per Operating System
        /// </summary>
        /// <param name="exitcode">Exit code set by user</param>
        void ExitCurrentProcess(int exitcode);

        /// <summary>
        /// Returns Operating System managed thread Id
        /// </summary>
        /// <returns>Returns the thread Id</returns>
        int GetCurrentManagedThreadId();
    }
}
