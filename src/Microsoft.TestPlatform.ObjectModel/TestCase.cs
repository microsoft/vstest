// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Utilities;
    using Newtonsoft.Json;

    /// <summary>
    /// Stores information about a test case.
    /// </summary>
    [DataContract]
    [KnownType(typeof(string[]))]
    [KnownType(typeof(KeyValuePair<string, string>[]))]
    [KnownType(typeof(TestOutcome))]
    public sealed class TestCase : TestObject
    {

#if TODO
        /// <summary>
        /// LocalExtensionData which can be used by Adapter developers for local transfer of extended properties. 
        /// Note that this data is available only for in-Proc execution, and may not be available for OutProc executors
        /// </summary>
        private Object m_localExtensionData;

#endif
        private Guid m_defaultId = Guid.Empty;

        #region Constructor

        [JsonConstructor]
        private TestCase() { }

        /// <summary>
        /// Initializes with the name of the test case.
        /// </summary>
        /// <param name="fullyQualifiedName">Fully qualified name of the test case.</param>
        /// <param name="executorUri">The Uri of the executor to use for running this test.</param>
        /// <param name="source">Test container source from which the test is discovered.</param>
        public TestCase(string fullyQualifiedName, Uri executorUri, string source)
        {
            ValidateArg.NotNullOrEmpty(fullyQualifiedName, "fullyQualifiedName");
            ValidateArg.NotNull(executorUri, "executorUri");
            ValidateArg.NotNullOrEmpty(source, "source");

            this.FullyQualifiedName = fullyQualifiedName;
            this.ExecutorUri = executorUri;
            this.Source = source;
        }

        #endregion

        #region Properties

#if TODO
        /// <summary>
        /// LocalExtensionData which can be used by Adapter developers for local transfer of extended properties. 
        /// Note that this data is available only for in-Proc execution, and may not be available for OutProc executors
        /// </summary>
        public Object LocalExtensionData
        {
            get { return m_localExtensionData; }
            set { m_localExtensionData = value; }
        }
#endif

        /// <summary>
        /// Id of the test case.
        /// </summary>
        public Guid Id
        {
            get
            {
                var id = GetPropertyValue<Guid>(TestCaseProperties.Id, Guid.Empty);
                if (id == Guid.Empty)
                {
                    // user has not specified his own Id during ctor! We will cache Id if its empty
                    if (m_defaultId == Guid.Empty)
                    {
                        m_defaultId = this.GetTestId();
                    }

                    return m_defaultId;
                }

                return id;
            }
            set
            {
                SetPropertyValue(TestCaseProperties.Id, value);
            }
        }

        /// <summary>
        /// Name of the test case.
        /// </summary>
        [DataMember]
        public string FullyQualifiedName
        {
            get { return GetPropertyValue(TestCaseProperties.FullyQualifiedName, string.Empty); }
            set
            {
                SetPropertyValue(TestCaseProperties.FullyQualifiedName, value);

                // Id is based on Name/Source, will nulll out guid and it gets calc next time we access it.
                m_defaultId = Guid.Empty;
            }
        }

        /// <summary>
        /// Display name of the test case.
        /// </summary>
        [DataMember]
        public string DisplayName
        {
            get { return GetPropertyValue(TestCaseProperties.DisplayName, FullyQualifiedName); }
            set { SetPropertyValue(TestCaseProperties.DisplayName, value); }
        }

        /// <summary>
        /// The Uri of the Executor to use for running this test.
        /// </summary>
        [DataMember]
        public Uri ExecutorUri
        {
            get { return GetPropertyValue<Uri>(TestCaseProperties.ExecutorUri, null); }
            set { SetPropertyValue(TestCaseProperties.ExecutorUri, value); }
        }

        /// <summary>
        /// Test container source from which the test is discovered
        /// </summary>
        [DataMember]
        public string Source
        {
            get { return GetPropertyValue<string>(TestCaseProperties.Source, null); }
            private set
            {
                SetPropertyValue(TestCaseProperties.Source, value);

                // Id is based on Name/Source, will nulll out guid and it gets calc next time we access it.
                m_defaultId = Guid.Empty;
            }
        }

        /// <summary>
        /// File path of the test
        /// </summary>
        [DataMember]
        public string CodeFilePath
        {
            get { return GetPropertyValue<string>(TestCaseProperties.CodeFilePath, null); }
            set { SetPropertyValue(TestCaseProperties.CodeFilePath, value); }
        }

        /// <summary>
        /// Line number of the test
        /// </summary>
        [DataMember]
        public int LineNumber
        {
            get { return GetPropertyValue(TestCaseProperties.LineNumber, -1); }
            set { SetPropertyValue(TestCaseProperties.LineNumber, value); }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Creates a Id of TestCase
        /// </summary>
        /// <returns>Guid test id</returns>
        private Guid GetTestId()
        {
            //To generate id hash "ExecutorUri + source + Name";

            // HACK: if source is a file name then just use the filename for the identifier since the 
            // file might have moved between discovery and execution (in appx mode for example)
            // This is a hack because the Source contents should be a black box to the framework. For example in the database adapter case this is not a file path.
            string source = this.Source;

            if (File.Exists(source))
            {
                source = Path.GetFileName(source);
            }

            string testcaseFullName = ExecutorUri.ToString() + source + FullyQualifiedName;
            return EqtHash.GuidFromString(testcaseFullName);
        }
        #endregion

        /// <summary>
        /// Override to help during debugging
        /// </summary>
        public override string ToString()
        {
            return this.FullyQualifiedName;
        }
    }

    /// <summary>
    /// Well-known TestCase properties
    /// </summary>
    public static class TestCaseProperties
    {
        #region Private Constants

        /// <summary>
        /// These are the core Test properties and may be available in commandline/TeamBuild to filter tests.
        /// These Property names should not be localized.
        /// </summary>
        private const string IdLabel = "Id";
        private const string FullyQualifiedNameLabel = "FullyQualifiedName";
        private const string NameLabel = "Name";
        private const string ExecutorUriLabel = "Executor Uri";
        private const string SourceLabel = "Source";
        private const string FilePathLabel = "File Path";
        private const string LineNumberLabel = "Line Number";

        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty Id = TestProperty.Register("TestCase.Id", IdLabel, string.Empty, string.Empty, typeof(Guid), ValidateGuid, TestPropertyAttributes.Hidden, typeof(TestCase));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty FullyQualifiedName = TestProperty.Register("TestCase.FullyQualifiedName", FullyQualifiedNameLabel, string.Empty, string.Empty, typeof(string), ValidateName, TestPropertyAttributes.Hidden, typeof(TestCase));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty DisplayName = TestProperty.Register("TestCase.DisplayName", NameLabel, string.Empty, string.Empty, typeof(string), ValidateDisplay, TestPropertyAttributes.None, typeof(TestCase));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ExecutorUri = TestProperty.Register("TestCase.ExecutorUri", ExecutorUriLabel, string.Empty, string.Empty, typeof(Uri), ValidateExecutorUri, TestPropertyAttributes.Hidden, typeof(TestCase));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty Source = TestProperty.Register("TestCase.Source", SourceLabel, typeof(string), typeof(TestCase));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty CodeFilePath = TestProperty.Register("TestCase.CodeFilePath", FilePathLabel, typeof(string), typeof(TestCase));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty LineNumber = TestProperty.Register("TestCase.LineNumber", LineNumberLabel, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

        private static bool ValidateName(object value)
        {
            return !(string.IsNullOrWhiteSpace((string)value));
        }

        private static bool ValidateDisplay(object value)
        {
            // only check for null and pass the rest up to UI for validation
            return value != null;
        }

        private static bool ValidateExecutorUri(object value)
        {
            return value != null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "Required to validate the input value.")]
        private static bool ValidateGuid(object value)
        {
            try
            {
                new Guid(value.ToString());
                return true;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }
    }
}
