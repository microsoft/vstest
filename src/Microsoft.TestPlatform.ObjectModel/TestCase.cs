// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Stores information about a test case.
    /// </summary>
    [DataContract]
    public sealed class TestCase : TestObject
    {
#if TODO
        /// <summary>
        /// LocalExtensionData which can be used by Adapter developers for local transfer of extended properties. 
        /// Note that this data is available only for in-Proc execution, and may not be available for OutProc executors
        /// </summary>
        private Object m_localExtensionData;
#endif
        private Guid defaultId = Guid.Empty;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCase"/> class.
        /// </summary>
        /// <remarks>This constructor doesn't perform any parameter validation, it is meant to be used for serialization."/></remarks>
        public TestCase()
        {
            // Default constructor for Serialization.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCase"/> class. 
        /// </summary>
        /// <param name="fullyQualifiedName">
        /// Fully qualified name of the test case.
        /// </param>
        /// <param name="executorUri">
        /// The Uri of the executor to use for running this test.
        /// </param>
        /// <param name="source">
        /// Test container source from which the test is discovered.
        /// </param>
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
        /// Gets or sets the id of the test case.
        /// </summary>
        [IgnoreDataMember]
        public Guid Id
        {
            get
            {
                var id = this.GetPropertyValue<Guid>(TestCaseProperties.Id, Guid.Empty);
                if (id == Guid.Empty)
                {
                    // user has not specified his own Id during ctor! We will cache Id if its empty
                    if (this.defaultId == Guid.Empty)
                    {
                        this.defaultId = this.GetTestId();
                    }

                    return this.defaultId;
                }

                return id;
            }

            set
            {
                this.SetPropertyValue(TestCaseProperties.Id, value);
            }
        }

        /// <summary>
        /// Gets or sets the fully qualified name of the test case.
        /// </summary>
        [IgnoreDataMember]
        public string FullyQualifiedName
        {
            get
            {
                return this.GetPropertyValue(TestCaseProperties.FullyQualifiedName, string.Empty);
            }

            set
            {
                this.SetPropertyValue(TestCaseProperties.FullyQualifiedName, value);

                // Id is based on Name/Source, will nulll out guid and it gets calc next time we access it.
                this.defaultId = Guid.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the display name of the test case.
        /// </summary>
        [IgnoreDataMember]
        public string DisplayName
        {
            get
            {
                return this.GetPropertyValue(TestCaseProperties.DisplayName, this.FullyQualifiedName);
            }

            set
            {
                this.SetPropertyValue(TestCaseProperties.DisplayName, value);
            }
        }

        /// <summary>
        /// Gets or sets the Uri of the Executor to use for running this test.
        /// </summary>
        [IgnoreDataMember]
        public Uri ExecutorUri
        {
            get
            {
                return this.GetPropertyValue<Uri>(TestCaseProperties.ExecutorUri, null);
            }

            set
            {
                this.SetPropertyValue(TestCaseProperties.ExecutorUri, value);
            }
        }

        /// <summary>
        /// Gets the test container source from which the test is discovered.
        /// </summary>
        [IgnoreDataMember]
        public string Source
        {
            get
            {
                return this.GetPropertyValue<string>(TestCaseProperties.Source, null);
            }

            private set
            {
                this.SetPropertyValue(TestCaseProperties.Source, value);

                // Id is based on Name/Source, will nulll out guid and it gets calc next time we access it.
                this.defaultId = Guid.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the source code file path of the test.
        /// </summary>
        [IgnoreDataMember]
        public string CodeFilePath
        {
            get
            {
                return this.GetPropertyValue<string>(TestCaseProperties.CodeFilePath, null);
            }

            set
            {
                this.SetPropertyValue(TestCaseProperties.CodeFilePath, value);
            }
        }

        /// <summary>
        /// Gets or sets the line number of the test.
        /// </summary>
        [IgnoreDataMember]
        public int LineNumber
        {
            get
            {
                return this.GetPropertyValue(TestCaseProperties.LineNumber, -1);
            }

            set
            {
                this.SetPropertyValue(TestCaseProperties.LineNumber, value);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.FullyQualifiedName;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Creates a Id of TestCase
        /// </summary>
        /// <returns>Guid test id</returns>
        private Guid GetTestId()
        {
            // To generate id hash "ExecutorUri + source + Name";

            // HACK: if source is a file name then just use the filename for the identifier since the 
            // file might have moved between discovery and execution (in appx mode for example)
            // This is a hack because the Source contents should be a black box to the framework. For example in the database adapter case this is not a file path.
            string source = this.Source;

            if (File.Exists(source))
            {
                source = Path.GetFileName(source);
            }

            string testcaseFullName = this.ExecutorUri + source + this.FullyQualifiedName;
            return EqtHash.GuidFromString(testcaseFullName);
        }

        #endregion
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
            return !string.IsNullOrWhiteSpace((string)value);
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
