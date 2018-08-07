// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    /// <summary>
    /// Class for constants used across the files.
    /// </summary>
    internal static class Constants
    {
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
        public const string AttachmentFileName = "Sequence";

        /// <summary>
        /// Test Name Attribute.
        /// </summary>
        public const string TestNameAttribute = "Name";

        /// <summary>
        /// Test Source Attribute.
        /// </summary>
        public const string TestSourceAttribute = "Source";

        /// <summary>
        /// Test Completed Attribute.
        /// </summary>
        public const string TestCompletedAttribute = "Completed";

        /// <summary>
        /// Test Display Name Attribute.
        /// </summary>
        public const string TestDisplayNameAttribute = "DisplayName";

        /// <summary>
        /// Friendly name of the data collector
        /// </summary>
        public const string BlameDataCollectorName = "Blame";

        /// <summary>
        /// Configuration key name for dump mode
        /// </summary>
        public const string DumpModeKey = "CollectDump";

        /// <summary>
        /// Procdump 32 bit version
        /// </summary>
        public const string ProcdumpProcess = "procdump.exe";

        /// <summary>
        /// Procdump 64 bit version
        /// </summary>
        public const string Procdump64Process = "procdump64.exe";

        ///<summary>
        /// Configuration key name for collect dump always
        /// </summary>
        public const string CollectDumpAlwaysKey = "CollectAlways";

        /// <summary>
        /// Configuration key name for dump type
        /// </summary>
        public const string DumpTypeKey = "DumpType";

        /// <summary>
        /// Configuration value for true
        /// </summary>
        public const string TrueConfigurationValue = "True";

        /// <summary>
        /// Configuration value for false
        /// </summary>
        public const string FalseConfigurationValue = "False";

        /// <summary>
        /// Configuration value for full
        /// </summary>
        public const string FullConfigurationValue = "Full";

        /// <summary>
        /// Configuration value for mini
        /// </summary>
        public const string MiniConfigurationValue = "Mini";
    }
}
