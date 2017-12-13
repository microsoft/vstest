// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// Test link.
    /// </summary>
    internal sealed class TestLink : IXmlTestStore
    {
        private Guid id;
        private string name = string.Empty;
        private string storage = string.Empty;

        public TestLink(Guid id, string name, string storage)
        {
            if (id == Guid.Empty)
            {
                Debug.Assert(id != Guid.Empty, "id == Guid.Empty!");
                throw new ArgumentException("ID cant be empty"); // error resource?
            }

            EqtAssert.StringNotNullOrEmpty(name, "name");
            EqtAssert.ParameterNotNull(storage, "storage");

            this.id = id;
            this.name = name;
            this.storage = storage;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public Guid Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets the storage.
        /// </summary>
        public string Storage
        {
            get { return this.storage; }
        }

        /// <summary>
        /// Whether this Link is equal to other Link. Compares by Id.
        /// </summary>
        public override bool Equals(object other)
        {
            TestLink link = other as TestLink;
            return (link == null) ? 
                false :
                this.id.Equals(link.id);
        }

        /// <summary>
        /// Whether this Link is exactly the same as other Link. Compares all fields.
        /// </summary>
        public bool IsSame(TestLink other)
        {
            if (other == null)
                return false;

            return this.id.Equals(other.id) &&
                this.name.Equals(other.name) &&
                this.storage.Equals(other.storage);
        }

        /// <summary>
        /// Override for GetHashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }

        /// <summary>
        /// Override for ToString.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Link to '{0}' {1} '{2}'.",
                this.name != null ? this.name : "(null)",
                this.id.ToString("B"),
                this.storage != null ? this.storage : "(null)");
        }

        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence h = new XmlPersistence();
            h.SaveGuid(element, "@id", this.Id);
            h.SaveSimpleField(element, "@name", this.name, null);
            h.SaveSimpleField(element, "@storage", this.storage, string.Empty);
        }
    }
}
