// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.XML
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Implementing this interface allows you to customize XmlStore persistence.
    /// </summary>
    public interface IXmlTestStoreCustom
    {
        /// <summary>
        /// Gets the name of the tag to use to persist this object.
        /// </summary>
        string ElementName { get; }

        /// <summary>
        /// Gets the xml namespace to use when creating the element
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Reviewed. Suppression is OK here.")]
        string NamespaceUri { get; }
    }
}
