// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System.Xml;

    /// <summary>
    /// Implementing this interface indicates a custom persistence logic is provided.
    /// The attribute based persistence is ignored in such a case.
    /// </summary>
    public interface IXmlTestStore
    {
        /// <summary>
        /// Saves the class under the XmlElement.
        /// </summary>
        /// <param name="element"> XmlElement element </param>
        /// <param name="parameters"> XmlTestStoreParameters parameters</param>
        void Save(XmlElement element, XmlTestStoreParameters parameters);
    }
}
