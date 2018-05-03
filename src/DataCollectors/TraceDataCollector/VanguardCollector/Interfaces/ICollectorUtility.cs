// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    /// <summary>
    /// The CollectorUtility interface.
    /// </summary>
    internal interface ICollectorUtility
    {
        /// <summary>
        /// Get path to vanguard.exe
        /// </summary>
        /// <returns>Vanguard path</returns>
        string GetVanguardPath();

        /// <summary>
        /// Get path to vanguard.exe
        /// </summary>
        /// <returns>Vanguard path</returns>
        string GetVanguardDirectory();
    }
}