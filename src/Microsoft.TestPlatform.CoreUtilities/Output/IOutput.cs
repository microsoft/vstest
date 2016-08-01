// ---------------------------------------------------------------------------
// <copyright file="IOutput.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//     Interface for outputing information under the command line.
// </summary>
// ---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    /// <summary>
    /// Interface for outputing information under the command line.
    /// </summary>
    public interface IOutput
    {
        /// <summary>
        /// Writes the message with a new line.
        /// </summary>
        /// <param name="message">Message to be output.</param>
        /// <param name="level">Level of the message.</param>
        void WriteLine(string message, OutputLevel level);

        /// <summary>
        /// Writes the message with no new line.
        /// </summary>
        /// <param name="message">Message to be output.</param>
        /// <param name="level">Level of the message.</param>
        void Write(string message, OutputLevel level);
    }
}
