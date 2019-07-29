namespace Microsoft.TestPlatform.Extensions.HtmlLogger.Utility
{
    using System;
    // using Microsoft.TestPlatform.Extensions.HtmlLogger.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal static class Constants
    {
        /// <summary>
        /// Uri used to uniquely identify the TRX logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/HtmlLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "Html";
        public const string TestTypePropertyIdentifier = "TestType";
        public static readonly Guid OrderedTestTypeGuid = new Guid("ec4800e8-40e5-4ab3-8510-b8bf29b1904d");

        public const string ParentExecutionIdPropertyIdentifier = "ParentExecId";
        public const string ExecutionIdPropertyIdentifier = "ExecutionId";
        public const string LogFileNameKey = "LogFileName";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ExecutionIdProperty = TestProperty.Register("ExecutionId", ExecutionIdPropertyIdentifier, typeof(Guid), TestPropertyAttributes.Hidden, typeof(VisualStudio.TestPlatform.ObjectModel.TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ParentExecIdProperty = TestProperty.Register("ParentExecId", ParentExecutionIdPropertyIdentifier, typeof(Guid), TestPropertyAttributes.Hidden, typeof(VisualStudio.TestPlatform.ObjectModel.TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty TestTypeProperty = TestProperty.Register("TestType", TestTypePropertyIdentifier, typeof(Guid), TestPropertyAttributes.Hidden, typeof(VisualStudio.TestPlatform.ObjectModel.TestResult));
    }
}