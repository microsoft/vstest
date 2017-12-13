// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// Ordered test element.
    /// </summary>
    internal class OrderedTestElement : TestAggregation, IXmlTestStoreCustom
    {
        public OrderedTestElement(Guid id, string name, string adapter) : base(id, name, adapter) { }

        string IXmlTestStoreCustom.ElementName
        {
            get { return Constants.OrderedTestElementName; }
        }

        string IXmlTestStoreCustom.NamespaceUri
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the test type.
        /// </summary>
        public override TestType TestType
        {
            get { return Constants.OrderedTestTypeInstance; }
        }
    }
}
