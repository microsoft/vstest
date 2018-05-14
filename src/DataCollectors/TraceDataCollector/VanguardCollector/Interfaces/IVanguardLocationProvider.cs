// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    /// <summary>
    /// Interface to provide vanguard directory and path.
    /// </summary>
    internal interface IVanguardLocationProvider
    {
        /// <summary>
        /// Get path to vanguard exe
        /// </summary>
        /// <returns>Vanguard path</returns>
        string GetVanguardPath();

        /// <summary>
        /// Get path to vanguard exe's directory
        /// </summary>
        /// <returns>Vanguard exe's directory. </returns>
        string GetVanguardDirectory();
    }
}