// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    #region TestCategoryItem
    /// <summary>
    /// Stores a string which categorizes the Test
    /// </summary>
    public sealed class TestCategoryItem : IXmlTestStore
    {
        #region Fields
        [StoreXmlSimpleField(Location = "@TestCategory", DefaultValue = "")]
        private string category = string.Empty;

        #endregion

        #region Constructors
        /// <summary>
        /// Create a new item with the category set
        /// </summary>
        /// <param name="category">The category.</param>
        public TestCategoryItem(string category)
        {
            // Treat null as empty.
            if (category == null)
            {
                category = String.Empty;
            }


            this.category = this.StripIllegalChars(category);
        }

        #endregion

        #region Properties/Methods
        /// <summary>
        /// Gets the category for this TestCategory
        /// </summary>
        public string TestCategory
        {
            get
            {
                return this.category;
            }
        }

        private string StripIllegalChars(string category)
        {
            string ret = category.Trim();
            ret = ret.Replace("&", String.Empty);
            ret = ret.Replace("|", String.Empty);
            ret = ret.Replace("!", String.Empty);
            ret = ret.Replace(",", String.Empty);
            return ret;
        }

        #endregion

        #region Methods - overrides
        /// <summary>
        /// Compare the values of the items
        /// </summary>
        /// <param name="other">Value being compared to.</param>
        /// <returns>True if the values are the same and false otherwise.</returns>
        public override bool Equals(object other)
        {
            TestCategoryItem otherItem = other as TestCategoryItem;
            if (otherItem == null)
            {
                return false;
            }
            Debug.Assert(this.category != null, "category is null");
            return String.Equals(this.category, otherItem.category, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Convert the category name to a hashcode
        /// </summary>
        /// <returns>Hashcode of the cagegory.</returns>
        public override int GetHashCode()
        {
            Debug.Assert(this.category != null, "category is null");
            return this.category.ToUpperInvariant().GetHashCode();
        }

        /// <summary>
        /// Convert the category name to a string
        /// </summary>
        /// <returns>The category.</returns>
        public override string ToString()
        {
            Debug.Assert(this.category != null, "category is null");
            return this.category;
        }
        #endregion

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement.
        /// </summary>
        /// <param name="element"> XmlElement element </param>
        /// <param name="parameters"> XmlTestStoreParameters parameters</param>
        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            new XmlPersistence().SaveSingleFields(element, this, parameters);
        }

        #endregion
    }
    #endregion

    #region TestCategoryItemCollection
    /// <summary>
    /// A collection of strings which categorize the test.
    /// </summary>
    public sealed class TestCategoryItemCollection : EqtBaseCollection<TestCategoryItem>
    {
        #region Constructors
        /// <summary>
        /// Creates an empty TestCategoryItemCollection.
        /// </summary>
        public TestCategoryItemCollection()
        {
        }

        /// <summary>
        /// Create a new TestCategoryItemCollection based on the string array.
        /// </summary>
        /// <param name="items">Add these items to the collection.</param>
        public TestCategoryItemCollection(string[] items)
        {
            EqtAssert.ParameterNotNull(items, "items");
            foreach (string s in items)
            {
                this.Add(s);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the category.
        /// </summary>
        /// <param name="item">Category to be added.</param>
        public void Add(string item)
        {
            this.Add(new TestCategoryItem(item));
        }

        /// <summary>
        /// Adds the category.
        /// </summary>
        /// <param name="item">Category to be added.</param>
        public override void Add(TestCategoryItem item)
        {
            EqtAssert.ParameterNotNull(item, "item");

            // Don't add empty items.
            if (!String.IsNullOrEmpty(item.TestCategory))
            {
                base.Add(item);
            }
        }

        /// <summary>
        /// Convert the TestCategoryItemCollection to a string.
        /// each item is surrounded by a comma (,)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            if (this.Count > 0)
            {
                returnString.Append(",");
                foreach (TestCategoryItem item in this)
                {
                    returnString.Append(item.TestCategory);
                    returnString.Append(",");
                }
            }

            return returnString.ToString();
        }

        /// <summary>
        /// Convert the TestCategoryItemCollection to an array of strings.
        /// </summary>
        /// <returns>Array of strings containing the test cagegories.</returns>
        public string[] ToArray()
        {
            string[] result = new string[this.Count];

            int i = 0;
            foreach (TestCategoryItem item in this)
            {
                result[i++] = item.TestCategory;
            }

            return result;
        }

        /// <summary>
        /// Compare the collection items
        /// </summary>
        /// <param name="obj">other collection</param>
        /// <returns>true if the collections contain the same items</returns>
        public override bool Equals(object obj)
        {
            TestCategoryItemCollection other = obj as TestCategoryItemCollection;
            bool result = false;

            if (other == null)
            {
                // Other object is not a TestCategoryItemCollection.
                result = false;
            }
            else if (Object.ReferenceEquals(this, other))
            {
                // The other object is the same object as this one.
                result = true;
            }
            else if (this.Count != other.Count)
            {
                // The count of categories in the other object does not
                // match this one, so they are not equal.
                result = false;
            }
            else
            {
                // Check each item and return on the first mismatch.
                foreach (TestCategoryItem item in this)
                {
                    if (!other.Contains(item))
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Return the hash code of this collection
        /// </summary>
        /// <returns>The hashcode.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion
    }
    #endregion
}
