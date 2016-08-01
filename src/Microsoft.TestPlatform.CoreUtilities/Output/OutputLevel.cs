// ---------------------------------------------------------------------------
// <copyright file="OutputLevel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//     Defines the level of output.
// </summary>
// ---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    /// <summary>
    /// Defines the level of output.
    /// </summary>
    public enum OutputLevel
    {
        /// <summary>
        /// Informational message.
        /// </summary>
        Information = 0,

        /// <summary>
        /// Warning message.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error message.
        /// </summary>
        Error = 2,
    }
}
