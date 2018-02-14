// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Test element.
    /// </summary>
    internal abstract class TestElement : ITestElement, IXmlTestStore
    {
        /// <summary>
        /// Default priority for a test method that does not specify a priority
        /// </summary>
        protected const int DefaultPriority = int.MaxValue;

        protected TestId id;
        protected string name;
        protected string owner;
        protected string storage;
        protected string adapter;
        protected int priority;
        protected bool isRunnable;
        protected TestExecId executionId;
        protected TestExecId parentExecutionId;
        protected TestCategoryItemCollection testCategories;
        protected TestListCategoryId catId;

        public TestElement(Guid id, string name, string adapter)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "name is null");
            Debug.Assert(!string.IsNullOrEmpty(adapter), "adapter is null");

            this.Initialize();

            this.id = new TestId(id);
            this.name = name;
            this.adapter = adapter;
        }


        /// <summary>
        /// Gets the id.
        /// </summary>
        public TestId Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name
        {
            get { return this.name; }

            set
            {
                EqtAssert.ParameterNotNull(value, "Name");
                this.name = value;
            }
        }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        public string Owner
        {
            get { return this.owner; }

            set
            {
                EqtAssert.ParameterNotNull(value, "Owner");
                this.owner = value;
            }
        }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        public int Priority
        {
            get { return this.priority; }
            set { this.priority = value; }
        }

        /// <summary>
        /// Gets or sets the storage.
        /// </summary>
        public string Storage
        {
            get { return this.storage; }

            set
            {
                EqtAssert.StringNotNullOrEmpty(value, "Storage");
                this.storage = value.ToLowerInvariant();
            }
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
        /// Gets or sets the parent execution id.
        /// </summary>
        public TestExecId ParentExecutionId
        {
            get { return this.parentExecutionId; }
            set { this.parentExecutionId = value; }
        }

        /// <summary>
        /// Gets the isRunnable value.
        /// </summary>
        public bool IsRunnable
        {
            get { return this.isRunnable; }
        }

        /// <summary>
        /// Gets or sets the category id.
        /// </summary>
        /// <remarks>
        /// Instead of setting to null use TestListCategoryId.Uncategorized
        /// </remarks>
        public TestListCategoryId CategoryId
        {
            get { return this.catId; }

            set
            {
                EqtAssert.ParameterNotNull(value, "CategoryId");
                this.catId = value;
            }
        }

        /// <summary>
        /// Gets or sets the test categories.
        /// </summary>
        public TestCategoryItemCollection TestCategories
        {
            get { return this.testCategories; }

            set
            {
                EqtAssert.ParameterNotNull(value, "value");
                this.testCategories = value;
            }
        }

        /// <summary>
        /// Gets the adapter name.
        /// </summary>
        public string Adapter
        {
            get { return adapter; }
        }

        /// <summary>
        /// Gets the test type.
        /// </summary>
        public abstract TestType TestType { get; }

        /// <summary>
        /// Override for ToString.
        /// </summary>
        /// <returns>String representation of test element.</returns>
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
            TestElement otherTest = other as TestElement;
            return (otherTest == null) ? 
                false :
                this.id.Equals(otherTest.id);
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

        public virtual void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence h = new XmlPersistence();

            h.SaveSimpleField(element, "@name", this.name, null);
            h.SaveSimpleField(element, "@storage", this.storage, string.Empty);
            h.SaveSimpleField(element, "@priority", this.priority, DefaultPriority);
            h.SaveSimpleField(element, "Owners/Owner/@name", this.owner, string.Empty);
            h.SaveObject(this.testCategories, element, "TestCategory", parameters);

            if (this.executionId != null)
                h.SaveGuid(element, "Execution/@id", this.executionId.Id);
            if (this.parentExecutionId != null)
                h.SaveGuid(element, "Execution/@parentId", this.parentExecutionId.Id);

            XmlTestStoreParameters testIdParameters = XmlTestStoreParameters.GetParameters();
            testIdParameters[TestId.IdLocationKey] = "@id";
            h.SaveObject(this.id, element, testIdParameters);
        }

        private void Initialize()
        {
            this.id = TestId.Empty;
            this.name = string.Empty;
            this.owner = string.Empty;
            this.priority = DefaultPriority;
            this.storage = string.Empty;
            this.executionId = TestExecId.Empty;
            this.parentExecutionId = TestExecId.Empty;
            this.testCategories = new TestCategoryItemCollection();
            this.isRunnable = true;
            this.catId = TestListCategoryId.Uncategorized;
        }
    }
}
