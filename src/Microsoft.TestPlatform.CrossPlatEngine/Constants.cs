// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// The set of constants used throughout this project.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// The port option to be specified to the test host process.
        /// </summary>
        internal const string PortOption = "--port";

        internal const string ParentProcessIdOption = "--parentprocessid";

        /// <summary>
        /// The connection timeout for clients in milliseconds.
        /// </summary>
        internal const int ClientConnectionTimeout = 60 * 1000;
    }
}
