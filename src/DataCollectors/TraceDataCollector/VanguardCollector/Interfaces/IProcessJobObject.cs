// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    using System;

    /// <summary>
    /// Helper interface used to add a child process to a job object so that it terminates when
    /// the parent process dies
    /// </summary>
    /// <summary>An interface to the Windows Job Objects API.</summary>
    internal interface IProcessJobObject : IDisposable
    {
        /// <summary>
        /// Helper function to add a process to the job object
        /// </summary>
        /// <param name="handle">Handle of the process to be added</param>
        ///
        void AddProcess(IntPtr handle);
    }
}