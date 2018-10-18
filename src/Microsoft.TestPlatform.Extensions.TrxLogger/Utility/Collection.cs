// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.Utility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    /// <summary>
    /// Base class for Eqt Collections. 
    /// Fast collection, default implementations (Add/Remove/etc) do not allow null items and ignore duplicates.
    /// </summary>
    internal class EqtBaseCollection<T> : ICollection<T>, IXmlTestStore
    {
        #region private classes
        /// <summary>
        /// Wraps non-generic enumerator.
        /// </summary>
        /// <typeparam name="TemplateType"></typeparam>
        private sealed class EqtBaseCollectionEnumerator<TemplateType> : IEnumerator<TemplateType>
        {
            private IEnumerator enumerator;

            internal EqtBaseCollectionEnumerator(IEnumerator e)
            {
                Debug.Assert(e != null, "e is null");
                this.enumerator = e;
            }

            public TemplateType Current
            {
                get { return (TemplateType)this.enumerator.Current; }
            }

            object IEnumerator.Current
            {
                get { return this.enumerator.Current; }
            }

            public bool MoveNext()
            {
                return this.enumerator.MoveNext();
            }

            public void Reset()
            {
                this.enumerator.Reset();
            }

            public void Dispose()
            {
            }
        }
        #endregion

        #region Fields
        protected Hashtable container; 

        protected string childElementName;
        #endregion

        #region Constructors
        protected EqtBaseCollection()
        {
            this.container = new Hashtable();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="comparer">For case insensitive comparison use StringComparer.InvariantCultureIgnoreCase.</param>
        protected EqtBaseCollection(IEqualityComparer comparer)
        {
            this.container = new Hashtable(0, comparer);   // Ad default Hashtable() constructor creates table with 0 items.
        }

        /// <summary>
        /// Copy constructor. Shallow copy.
        /// </summary>
        /// <param name="other">The object to copy items from.</param>
        protected EqtBaseCollection(EqtBaseCollection<T> other)
        {
            EqtAssert.ParameterNotNull(other, "other");
            this.container = new Hashtable(other.container);
        }
        #endregion

        #region Methods: ICollection<T>
        // TODO: Consider putting check for null to derived classes.
        public virtual void Add(T item)
        {
            EqtAssert.ParameterNotNull(item, "item");

            if (!this.container.Contains(item))
            {
                this.container.Add(item, null);    // Do not want to xml-persist the value.
            }
        }

        public virtual bool Contains(T item)
        {
            if (item == null)
            {
                return false;
            }

            return this.container.Contains(item);
        }

        /// <summary>
        /// Removes specified item from collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>True if collection contained the item, otherwise false.</returns>
        public virtual bool Remove(T item)
        {
            EqtAssert.ParameterNotNull(item, "item");   // This is to be consistent with Add...

            if (this.container.Contains(item))
            {
                this.container.Remove(item);
                return true;
            }
            return false;
        }

        public virtual void Clear()
        {
            this.container.Clear();
        }

        /// <summary>
        /// Shallow copy. Assumes that items are immutable. Override if your items are mutable.
        /// </summary>
        public virtual object Clone()
        {
            return new EqtBaseCollection<T>(this);
        }

        public virtual int Count
        {
            get { return this.container.Count; }
        }

        /// <summary>
        /// Copies all items to the array.
        /// As FxCop recommends, this is an explicit implementation and derived classes need to define strongly typed CopyTo.
        /// </summary>
        public virtual void CopyTo(T[] array, int index)
        {
            EqtAssert.ParameterNotNull(array, "array");
            this.container.Keys.CopyTo(array, index);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
        #endregion

        #region IEnumerable
        public virtual IEnumerator GetEnumerator()
        {
            return this.container.Keys.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new EqtBaseCollectionEnumerator<T>(this.GetEnumerator());
        }
        #endregion

        #region IXmlTestStore Members

        /// <summary>
        /// Default behavior is to create child elements with name same as name of type T. 
        /// Does not respect IXmlTestStoreCustom.
        /// </summary>
        public virtual void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence h = new XmlPersistence();
            h.SaveHashtable(this.container, element, ".", ".", null, ChildElementName, parameters);
        }
        #endregion

        #region Private
        private string ChildElementName
        {
            get
            {
                if (this.childElementName == null)
                {
                    // All we can do here is to delegate to T. Cannot cast T to IXmlTestStoreCustom as T is a type, not an instance.
                    this.childElementName = typeof(T).Name;
                }
                return this.childElementName;
            }
        }
        #endregion
    }
}
