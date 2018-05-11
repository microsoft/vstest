// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    /// <summary>
    /// The IVanguardCommandBuilder interface.
    /// </summary>
    internal interface IVanguardCommandBuilder
    {
        /// <summary>
        /// Generate a vanguardCommand line string, given some parameters
        /// </summary>
        /// <param name="vanguardCommand">VanguardCommand to execute</param>
        /// <param name="sessionName">Session name</param>
        /// <param name="outputName">Output file name (for collect vanguardCommand)</param>
        /// <param name="configurationFileName">Configuration file name</param>
        /// <returns>VanguardCommand line string</returns>
        string GenerateCommandLine(
            VanguardCommand vanguardCommand,
            string sessionName,
            string outputName,
            string configurationFileName);
    }
}