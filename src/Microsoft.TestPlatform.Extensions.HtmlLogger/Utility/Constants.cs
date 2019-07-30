// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.HtmlLogger.Utility
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal static class Constants
    {
        /// <summary>
        /// Uri used to uniquely identify the Html logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/HtmlLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "Html";

        ///  /// <summary>
        /// Property Id storing the TestType.
        /// </summary>
        public const string TestTypePropertyIdentifier = "TestType";

        /// <summary>
        /// Ordered test type guid
        /// </summary>
        public static readonly Guid OrderedTestTypeGuid = new Guid("ec4800e8-40e5-4ab3-8510-b8bf29b1904d");

        /// <summary>
        ///  Property Id storing the ParentExecutionId.
        /// </summary>
        public const string ParentExecutionIdPropertyIdentifier = "ParentExecId";

        /// <summary>
        ///  Property Id storing the ExecutionId.
        /// </summary>
        public const string ExecutionIdPropertyIdentifier = "ExecutionId";

        /// <summary>
        ///  Log file parameter key
        /// </summary>
        public const string LogFileNameKey = "LogFileName"; 

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ExecutionIdProperty = TestProperty.Register("ExecutionId", ExecutionIdPropertyIdentifier, typeof(Guid), TestPropertyAttributes.Hidden, typeof(VisualStudio.TestPlatform.ObjectModel.TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ParentExecIdProperty = TestProperty.Register("ParentExecId", ParentExecutionIdPropertyIdentifier, typeof(Guid), TestPropertyAttributes.Hidden, typeof(VisualStudio.TestPlatform.ObjectModel.TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty TestTypeProperty = TestProperty.Register("TestType", TestTypePropertyIdentifier, typeof(Guid), TestPropertyAttributes.Hidden, typeof(VisualStudio.TestPlatform.ObjectModel.TestResult));
    }
}