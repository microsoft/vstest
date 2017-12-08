// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System; // check where it should be? within namespace or outside?

namespace Microsoft.TestPlatform.Extensions.TrxLogger.Utility
{  
    internal static class Constants
    {
        /// <summary>
        /// Uri used to uniquely identify the TRX logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/TrxLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "Trx";

        /// <summary>
        /// Prefix of the data collector
        /// </summary>
        public const string DataCollectorUriPrefix = "dataCollector://";

        /// <summary>
        /// Log file parameter key
        /// </summary>
        public const string LogFileNameKey = "LogFileName";

        /// <summary>
        /// Property Id storing the ExecutionId.
        /// </summary>
        public const string ExecutionIdPropertyIdentifier = "ExecutionId";

        /// <summary>
        /// Property Id storing the ParentExecutionId.
        /// </summary>
        public const string ParentExecutionIdPropertyIdentifier = "ParentExecId";

        public const string TestTypePropertyIdentifier = "TestType";

        /// <summary>
        /// Property Id storing the TMITestId.
        /// </summary>
        public const string TmiTestIdPropertyIdentifier = "MSTestDiscoverer.TmiTestId";

        public static readonly Guid OrderedTestType = new Guid("ec4800e8-40e5-4ab3-8510-b8bf29b1904d");
        public static readonly Guid UnitTestType = new Guid("13CDC9D9-DDB5-4fa4-A97D-D965CCFC6D4B");
    }
}
