// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;

    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// Class identifying test type.
    /// </summary>
    public sealed class TestType : IXmlTestStore
    {
        [StoreXmlSimpleField(".")]
        private Guid typeId;

        public TestType(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            this.typeId = id;
        }

        public Guid Id
        {
            get { return this.typeId; }
        }

        public override bool Equals(object obj)
        {
            TestType tt = obj as TestType;

            if (tt == null)
            {
                return false;
            }

            return this.typeId.Equals(tt.typeId);
        }


        public override int GetHashCode()
        {
            return this.typeId.GetHashCode();
        }

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
            XmlPersistence.SaveUsingReflection(element, this, null, parameters);
        }

        #endregion
    }
}
