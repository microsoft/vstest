// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// The test entry.
    /// </summary>
    internal sealed class TestEntry : IXmlTestStore
    {
        #region Fields

        private TestId testId;
        private Guid executionId;
        private Guid parentExecutionId;
        private TestListCategoryId categoryId;
        private List<TestEntry> testEntries;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// Note that using this constructor has different effect as setting CategoryId property.
        /// When using this constructor, catId is used as specified, which CategoryId.set changes null to the root cat.
        /// </summary>
        /// <param name="testId">Test Id.</param>
        /// <param name="catId">Category Id. This gets into .</param>
        public TestEntry(TestId testId, TestListCategoryId catId)
        {
            Debug.Assert(testId != null, "testId is null");

            // CatId can be null.
            this.testId = testId;
            this.categoryId = catId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the exec id.
        /// </summary>
        public Guid ExecutionId
        {
            get { return this.executionId; }

            set
            {
                Debug.Assert(value != null, "ExecId is null");
                this.executionId = value;
            }
        }

        /// <summary>
        /// Gets or sets the parent exec id.
        /// </summary>
        public Guid ParentExecutionId
        {
            get { return this.parentExecutionId; }

            set
            {
                Debug.Assert(value != null, "ExecId is null");
                this.parentExecutionId = value;
            }
        }

        public List<TestEntry> TestEntries
        {
            get
            {
                if (this.testEntries == null)
                {
                    this.testEntries = new List<TestEntry>();
                }

                return this.testEntries;
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Override function for Equals.
        /// </summary>
        /// <param name="obj">
        /// The object to compare.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1405:DebugAssertMustProvideMessageText", Justification = "Reviewed. Suppression is OK here.")]
        public override bool Equals(object obj)
        {
            TestEntry e = obj as TestEntry;

            if (e == null)
            {
                return false;
            }

            Debug.Assert(this.executionId != null, "this.executionId is null");
            Debug.Assert(e.executionId != null, "e.executionId is null");

            if (!this.executionId.Equals(e.executionId))
            {
                return false;
            }

            Debug.Assert(object.Equals(this.testId, e.testId));
            Debug.Assert(object.Equals(this.categoryId, e.categoryId));
            return true;
        }

        /// <summary>
        /// Override function for GetHashCode.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.executionId.GetHashCode();
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
        /// The parameters.
        /// </param>
        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();
            helper.SaveSingleFields(element, this, parameters);

            helper.SaveObject(this.testId, element, null);
            helper.SaveGuid(element, "@executionId", this.executionId);
            if (parentExecutionId != null)
                helper.SaveGuid(element, "@parentExecutionId", this.parentExecutionId);
            helper.SaveGuid(element, "@testListId", this.categoryId.Id);
            if (this.TestEntries.Count > 0)
                helper.SaveIEnumerable(TestEntries, element, "TestEntries", ".", "TestEntry", parameters);
        }

        #endregion
    }
}
