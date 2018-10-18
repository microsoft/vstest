// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    #region TestPropertyItem
    /// <summary>
    /// Stores a string which categorizes the Test
    /// </summary>
    internal sealed class TestPropertyItem : IXmlTestStore
    {
        #region Fields
        [StoreXmlSimpleField(Location = "Key", DefaultValue = "")]
        private string key = string.Empty;

        [StoreXmlSimpleField(Location = "Value", DefaultValue = "")]
        private string value = string.Empty;

        #endregion

        #region Constructors
        /// <summary>
        /// Create a new item with the key/value set
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public TestPropertyItem(string key, string value)
        {
            // Treat null as empty.
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException();

            if (value == null)
            {
                value = String.Empty;
            }


            this.key = key;
            this.value = value;
        }

        #endregion

        #region Properties/Methods
        /// <summary>
        /// Gets the Key for this TestProperty
        /// </summary>
        public string Key
        {
            get
            {
                return this.key;
            }
        }

        /// <summary>
        /// Gets the Value for this TestProperty
        /// </summary>
        public string Value
        {
            get
            {
                return this.value;
            }
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
            TestPropertyItem otherItem = other as TestPropertyItem;
            if (otherItem == null)
            {
                return false;
            }
            return String.Equals(this.key, otherItem.key, StringComparison.OrdinalIgnoreCase) && String.Equals(this.value, otherItem.value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Convert the property name to a hashcode
        /// </summary>
        /// <returns>Hashcode of the cagegory.</returns>
        public override int GetHashCode()
        {
            return this.key.ToUpperInvariant().GetHashCode() ^ this.value.GetHashCode();
        }

        /// <summary>
        /// Convert the property name to a string
        /// </summary>
        /// <returns>The property.</returns>
        public override string ToString()
        {
            return this.key + " = " + this.value;
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

    #region TestPropertyItemCollection
    /// <summary>
    /// A collection of strings which categorize the test.
    /// </summary>
    internal sealed class TestPropertyItemCollection : EqtBaseCollection<TestPropertyItem>
    {
        #region Constructors
        /// <summary>
        /// Creates an empty TestPropertyItemCollection.
        /// </summary>
        public TestPropertyItemCollection()
        {
            childElementName = "Property";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the property.
        /// </summary>
        /// <param name="key">Key to be added.</param>
        /// <param name="value">Value to be added.</param>
        public void Add(string key, string value)
        {
            this.Add(new TestPropertyItem(key, value));
        }

        /// <summary>
        /// Adds the property.
        /// </summary>
        /// <param name="item">Property to be added.</param>
        public override void Add(TestPropertyItem item)
        {
            EqtAssert.ParameterNotNull(item, "item");

            // Don't add empty items.
            if (!String.IsNullOrEmpty(item.Key))
            {
                base.Add(item);
            }
        }

        /// <summary>
        /// Convert the TestPropertyItemCollection to a string.
        /// each item is surrounded by a comma (,)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            if (this.Count > 0)
            {
                returnString.Append(",");
                foreach (TestPropertyItem item in this)
                {
                    returnString.Append(item.ToString());
                    returnString.Append(",");
                }
            }

            return returnString.ToString();
        }

        /// <summary>
        /// Compare the collection items
        /// </summary>
        /// <param name="obj">other collection</param>
        /// <returns>true if the collections contain the same items</returns>
        public override bool Equals(object obj)
        {
            TestPropertyItemCollection other = obj as TestPropertyItemCollection;
            bool result = false;

            if (other == null)
            {
                // Other object is not a TestPropertyItemCollection.
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
                foreach (TestPropertyItem item in this)
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
