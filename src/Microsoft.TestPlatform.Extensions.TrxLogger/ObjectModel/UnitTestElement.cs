// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    using TrxLoggerResources = Microsoft.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Class for all tests
    /// </summary>
    internal class UnitTestElement : IXmlTestStore, IXmlTestStoreCustom
    {
        #region Constants
        /// <summary>
        /// Default priority for a test method that does not specify a priority
        /// </summary>
        internal const int DefaultPriority = int.MaxValue;

        /// <summary>
        /// Timeout value indicating a not-set timeout
        /// </summary>
        internal const int NotSetTimeout = 0;

        private static readonly Guid TestTypeGuid = new Guid("13CDC9D9-DDB5-4fa4-A97D-D965CCFC6D4B");
        private static readonly TestType TestTypeInstance = new TestType(TestTypeGuid);

        #endregion

        #region Fields

        private TestId id;

        private string name;

        private string owner;

        private int priority;

        // Todo: Once the Bug 233635 is fixed, check it should populate
        private TestCategoryItemCollection testCategories;

        private TestExecId executionId;

        private string storage;

        private string codeBase;

        // partial or fully qualified name of the adapter used to execute the test
        private string executorUriOfAdapter;

        private TestMethod testMethod;

        private bool isRunnable;

        private TestListCategoryId catId;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestElement"/> class.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="executorUriOfAdapter">
        /// The adapter type name.
        /// </param>
        /// <param name="testMethod">
        /// The test method.
        /// </param>
        public UnitTestElement(
            Guid id,
            string name,
            string executorUriOfAdapter,
            TestMethod testMethod)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "name is null");
            Debug.Assert(!string.IsNullOrEmpty(executorUriOfAdapter), "executorUriOfAdapter is null");
            Debug.Assert(testMethod != null, "testMethod is null");

            this.Initialize();

            this.id = new TestId(id);
            this.name = name;
            this.executorUriOfAdapter = executorUriOfAdapter;
            this.testMethod = testMethod;
            Debug.Assert(this.testMethod.ClassName != null, "className is null");
        }

        #endregion

        #region IXmlTestStoreCustom

        string IXmlTestStoreCustom.ElementName
        {
            get { return "UnitTest"; }
        }

        string IXmlTestStoreCustom.NamespaceUri
        {
            get { return null; }
        }

        #endregion

        /// <summary>
        /// Gets or sets the category id.
        /// </summary>
        /// <remarks>
        /// Instead of setting to null use TestListCategoryId.Uncategorized
        /// </remarks>
        public TestListCategoryId CategoryId
        {
            get
            {
                return this.catId;
            }

            set
            {
                EqtAssert.ParameterNotNull(value, "CategoryId");
                this.catId = value;
            }
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public TestId Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets or sets the execution id.
        /// </summary>
        public TestExecId ExecutionId
        {
            get { return this.executionId; }
            set { this.executionId = value; }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                EqtAssert.ParameterNotNull(value, "Name");

                this.name = value;
            }
        }

        /// <summary>
        /// Gets or sets the storage.
        /// </summary>
        public string Storage
        {
            get
            {
                return this.storage;
            }

            set
            {
                EqtAssert.StringNotNullOrEmpty(value, "Storage");
                this.storage = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        public int Priority
        {
            get
            {
                return this.priority;
            }

            set
            {
                this.priority = value;
            }
        }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        public string Owner
        {
            get
            {
                return this.owner;
            }

            set
            {
                EqtAssert.ParameterNotNull(value, "Owner");
                this.owner = value;
            }
        }

        /// <summary>
        /// Gets the test type.
        /// </summary>
        public TestType TestType
        {
            get { return TestTypeInstance; }
        }

        /// <summary>
        /// Gets or sets the test categories.
        /// </summary>
        public TestCategoryItemCollection TestCategories
        {
            get
            {
                return this.testCategories;
            }

            set
            {
                EqtAssert.ParameterNotNull(value, "value");
                this.testCategories = value;
            }
        }

        /// <summary>
        /// The assign code base.
        /// </summary>
        /// <param name="cb">
        /// The code base.
        /// </param>
        public void AssignCodeBase(string cb)
        {
            EqtAssert.StringNotNullOrEmpty(cb, "codeBase");
            this.codeBase = cb;
        }

        public bool IsRunnable
        {
            get { return this.isRunnable; }
        }

        #region Overrides

        /// <summary>
        /// Override for Tostring.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' {1}",
                this.name != null ? this.name : TrxLoggerResources.Common_NullInMessages,
                this.id != null ? this.id.ToString() : TrxLoggerResources.Common_NullInMessages);
        }

        /// <summary>
        /// Override for Equals.
        /// </summary>
        /// <param name="other">
        /// The object to compare.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object other)
        {
            UnitTestElement otherTest = other as UnitTestElement;
            if (otherTest == null)
            {
                return false;
            }

            return this.id.Equals(otherTest.id);
        }

        /// <summary>
        /// Override for GetHashCode
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }

        #endregion

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement..
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameter
        /// </param>
        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence h = new XmlPersistence();

            h.SaveSimpleField(element, "@name", this.name, null);
            h.SaveSimpleField(element, "@storage", this.storage, string.Empty);
            h.SaveSimpleField(element, "@priority", this.priority, DefaultPriority);
            h.SaveSimpleField(element, "Owners/Owner/@name", this.owner, string.Empty);
            h.SaveObject(this.testCategories, element, "TestCategory", parameters);

            // Save the test ID. We exclude "test" from the default locations used by TestId, since this is already a test
            // element. Ideally, we would let TestId save the IDs to the default locations, but the previous behavior of
            // TestElement was to store the test ID at @testId, and since we can't change this, TestId supports custom
            // locations for the IDs. See TestId.GetLocations for more info.
            XmlTestStoreParameters testIdParameters = XmlTestStoreParameters.GetParameters();
            testIdParameters[TestId.IdLocationKey] = "@id";
            h.SaveObject(this.id, element, testIdParameters);

            if (this.executionId != null)
            {
                h.SaveGuid(element, "Execution/@id", this.executionId.Id);
            }

            h.SaveSimpleField(element, "TestMethod/@codeBase", this.codeBase, string.Empty);
            h.SaveSimpleField(element, "TestMethod/@executorUriOfAdapter", this.executorUriOfAdapter, string.Empty);
            h.SaveObject(this.testMethod, element, "TestMethod", parameters);
        }

        #endregion //IXmlTestStore

        private void Initialize()
        {
            this.id = TestId.Empty;
            this.name = string.Empty;
            this.owner = string.Empty;
            this.priority = DefaultPriority;
            this.executionId = TestExecId.Empty;
            this.testCategories = new TestCategoryItemCollection();
            this.storage = string.Empty;
            this.isRunnable = true;
            this.catId = TestListCategoryId.Uncategorized;
        }
    }
}
