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
        /// The test session start constant.
        /// </summary>
        public const string TestSessionStart = "TestSessionStart";

        /// <summary>
        /// The test session end constant.
        /// </summary>
        public const string TestSessionEnd = "TestSessionEnd";

        /// <summary>
        /// The test case start constant.
        /// </summary>
        public const string TestCaseStart = "TestCaseStart";

        /// <summary>
        /// The test case end method name.
        /// </summary>
        public const string TestCaseEnd = "TestCaseEnd";

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
        /// Abort Message
        /// </summary>
        public const string TestRunAbort = "The active test run was aborted. Reason: ";

        /// <summary>
        /// Stakoverflow Message
        /// </summary>
        public const string TestRunAbortStackOverFlow = "The active test run was aborted. Reason: Process is terminated due to StackOverflowException.";

        /// <summary>
        /// Friendly name of the data collector
        /// </summary>
        public const string BlameDataCollectorName = "Blame";

    }
}
