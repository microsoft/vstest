// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Logging
{
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;

    /// <summary>
    /// Hold data about the Test logger.
    /// </summary>
    public class TestLoggerMetadata : ITestLoggerCapabilities
    {
        /// <summary>
        /// Constructor for TestLoggerMetadata
        /// </summary>
        /// <param name="extension">
        /// Uri identifying the logger. 
        /// </param>
        /// <param name="friendlyName">
        /// The friendly Name.
        /// </param>
        public TestLoggerMetadata(string extension, string friendlyName)
        {
            this.ExtensionUri = extension;
            this.FriendlyName = friendlyName;
        }

        /// <summary>
        /// Gets Uri identifying the logger.
        /// </summary>
        public string ExtensionUri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets Friendly Name identifying the logger.
        /// </summary>
        public string FriendlyName
        {
            get;
            private set;
        }
    }
}
