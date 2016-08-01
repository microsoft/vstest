// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    /// <summary>
    /// Return values from argument processors.
    /// </summary>
    public enum ArgumentProcessorResult
    {
        /// <summary>
        /// Return value indicating no errors.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Return value indicating an error occurred
        /// </summary>
        Fail = 1,

        /// <summary>
        /// Return value indicating that the current processor succeeded and subsequent processors should be skipped
        /// </summary>
        Abort = 2
    }
}
