// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    /// <summary>
    /// Class for constants used across the files.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// The test session end constant.
        /// </summary>
        public const string TestSessionEnd = "TestSessionEnd";

        /// <summary>
        /// The test case start constant.
        /// </summary>
        public const string TestCaseStart = "TestCaseStart";

        /// <summary>
        /// Root node name for Xml file.
        /// </summary>
        public const string BlameRootNode = "TestSequence";

        /// <summary>
        /// Node name for each Xml node.
        /// </summary>
        public const string BlameTestNode = "Test";

        /// <summary>
        /// Attachment File name.
        /// </summary>
        public const string AttachmentFileName = "TestSequence.xml";

        /// <summary>
        /// Test Name Attribute.
        /// </summary>
        public const string TestNameAttribute = "Name";

        /// <summary>
        /// Test Source Attribute.
        /// </summary>
        public const string TestSourceAttribute = "Source";
        
        /// <summary>
        /// Friendly name of the data collector
        /// </summary>
        public const string BlameDataCollectorName = "Blame";

    }
}
