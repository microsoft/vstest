// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    /// <summary>
    /// The IVangurdCommandBuilder interface.
    /// </summary>
    internal interface IVangurdCommandBuilder
    {
        /// <summary>
        /// Generate a command line string, given some parameters
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="sessionName">Session name</param>
        /// <param name="outputName">Output file name (for collect command)</param>
        /// <param name="configurationFileName">Configuration file name</param>
        /// <returns>Command line string</returns>
        string GenerateCommandLine(
            Command command,
            string sessionName,
            string outputName,
            string configurationFileName);
    }
}