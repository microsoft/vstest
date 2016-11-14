// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System.Diagnostics;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    using TrxLoggerResources = Microsoft.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// The test list category.
    /// </summary>
    public class TestListCategory : IXmlTestStore
    {
        #region Fields

        private static TestListCategory uncategorizedResults;

        private static TestListCategory allResults;

        private static object reservedCategoryLock = new object();

        private TestListCategoryId id = new TestListCategoryId();

        [StoreXmlSimpleField(DefaultValue = "")]
        private string name = string.Empty;

        private TestListCategoryId parentCategoryId;

        #endregion

        /// <summary>
        /// Constructor for TestListCategory .
        /// </summary>
        /// <param name="name">The name of new category.</param>
        /// <param name="parentCategoryId">Id of parent category. Use TestListCategoryId.Root for top level categories.</param>
        public TestListCategory(string name, TestListCategoryId parentCategoryId)
        {
            EqtAssert.StringNotNullOrEmpty(name, "name");
            EqtAssert.ParameterNotNull(parentCategoryId, "parentCategoryId");

            this.name = name;
            this.parentCategoryId = parentCategoryId;
        }

        /// <summary>
        /// Used internally for fake uncategorized category.
        /// </summary>
        /// <param name="name">
        /// Category name.
        /// </param>
        /// <param name="id">
        /// Category id.
        /// </param>
        /// <param name="parentId">
        /// The parent Id.
        /// </param>
        private TestListCategory(string name, TestListCategoryId id, TestListCategoryId parentId) : this(name, parentId)
        {
            EqtAssert.ParameterNotNull(id, "id");
            this.id = id;
        }

        #region Properties

        /// <summary>
        /// Gets the uncategorized results.
        /// </summary>
        public static TestListCategory UncategorizedResults
        {
            get
            {
                if (uncategorizedResults == null)
                {
                    lock (reservedCategoryLock)
                    {
                        if (uncategorizedResults == null)
                        {
                            uncategorizedResults = new TestListCategory(
                                TrxLoggerResources.TS_UncategorizedResults, TestListCategoryId.Uncategorized, TestListCategoryId.Root);
                        }
                    }
                }

                return uncategorizedResults;
            }
        }

        /// <summary>
        /// Gets the all results.
        /// </summary>
        public static TestListCategory AllResults
        {
            get
            {
                if (allResults == null)
                {
                    lock (reservedCategoryLock)
                    {
                        if (allResults == null)
                        {
                            allResults = new TestListCategory(
                                        TrxLoggerResources.TS_AllResults, TestListCategoryId.AllItems, TestListCategoryId.Root);
                        }
                    }
                }

                return allResults;
            }
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public TestListCategoryId Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets or sets id of parent category. Use TestCategoryId.Root for top level categories.
        /// We do not keep category children in Object Model, only parent.
        /// </summary>
        public TestListCategoryId ParentCategoryId
        {
            get
            {
                return this.parentCategoryId;
            }

            set
            {
                EqtAssert.ParameterNotNull(value, "ParentCategoryId.value");
                this.parentCategoryId = value;
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
        public override bool Equals(object obj)
        {
            TestListCategory cat = obj as TestListCategory;
            if (cat == null)
            {
                return false;
            }

            Debug.Assert(this.id != null, "id is null");
            return this.id.Equals(cat.id);
        }

        /// <summary>
        /// Override function for GetHashCode.
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

            h.SaveSingleFields(element, this, parameters);
            h.SaveGuid(element, "@id", this.Id.Id);
            h.SaveGuid(element, "@parentListId", this.ParentCategoryId.Id);
        }

        #endregion
    }
}
