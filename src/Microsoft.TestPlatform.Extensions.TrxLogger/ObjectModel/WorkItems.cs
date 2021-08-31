// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    #region WorkItem
    /// <summary>
    /// Stores an int which represents a workitem
    /// </summary>
    internal sealed class WorkItem : IXmlTestStore
    {
        #region Fields
        [StoreXmlField(Location = ".")]
        private int id = 0;

        #endregion

        #region Constructors
        /// <summary>
        /// Create a new item with the workitem set
        /// </summary>
        /// <param name="workitemId">The workitem.</param>
        public WorkItem(int workitemId)
        {
            this.id = workitemId;
        }

        #endregion

        #region Properties/Methods
        /// <summary>
        /// Gets the id for this WorkItem
        /// </summary>
        public int Id
        {
            get
            {
                return this.id;
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
            WorkItem otherItem = other as WorkItem;
            if (otherItem == null)
            {
                return false;
            }
            return this.id == otherItem.id;
        }

        /// <summary>
        /// Convert the workitem to a hashcode
        /// </summary>
        /// <returns>Hashcode of the workitem.</returns>
        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }

        /// <summary>
        /// Convert the workitem to a string
        /// </summary>
        /// <returns>The workitem.</returns>
        public override string ToString()
        {
            return this.id.ToString(CultureInfo.InvariantCulture);
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

    #region WorkItemCollection
    /// <summary>
    /// A collection of ints represent the workitems
    /// </summary>
    internal sealed class WorkItemCollection : EqtBaseCollection<WorkItem>
    {
        #region Constructors
        /// <summary>
        /// Creates an empty WorkItemCollection.
        /// </summary>
        public WorkItemCollection()
        {
        }

        /// <summary>
        /// Create a new WorkItemCollection based on the int array.
        /// </summary>
        /// <param name="items">Add these items to the collection.</param>
        public WorkItemCollection(int[] items)
        {
            EqtAssert.ParameterNotNull(items, nameof(items));
            foreach (int i in items)
            {
                this.Add(new WorkItem(i));
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Adds the workitem.
        /// </summary>
        /// <param name="item">WorkItem to be added.</param>
        public void Add(int item)
        {
            this.Add(new WorkItem(item));
        }

        /// <summary>
        /// Adds the workitem.
        /// </summary>
        /// <param name="item">WorkItem to be added.</param>
        public override void Add(WorkItem item)
        {
            EqtAssert.ParameterNotNull(item, nameof(item));
            base.Add(item);
        }

        /// <summary>
        /// Convert the WorkItemCollection to a string.
        /// each item is separated by a comma (,)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            if (this.Count > 0)
            {
                returnString.Append(",");
                foreach (WorkItem item in this)
                {
                    returnString.Append(item);
                    returnString.Append(",");
                }
            }

            return returnString.ToString();
        }

        /// <summary>
        /// Convert the WorkItemCollection to an array of ints.
        /// </summary>
        /// <returns>Array of ints containing the workitems.</returns>
        public int[] ToArray()
        {
            int[] result = new int[this.Count];

            int i = 0;
            foreach (WorkItem item in this)
            {
                result[i++] = item.Id;
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
            WorkItemCollection other = obj as WorkItemCollection;
            bool result = false;

            if (other == null)
            {
                result = false;
            }
            else if (object.ReferenceEquals(this, other))
            {
                result = true;
            }
            else if (this.Count != other.Count)
            {
                result = false;
            }
            else
            {
                foreach (WorkItem item in this)
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

        public override void Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence xmlPersistence = new XmlPersistence();
            xmlPersistence.SaveHashtable(this.container, element, ".", ".", null, "Workitem", parameters);
        }
        #endregion
    }
    #endregion
}
