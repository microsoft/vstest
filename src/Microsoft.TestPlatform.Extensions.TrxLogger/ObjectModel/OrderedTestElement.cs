// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    internal class OrderedTestElement : TestAggregation, IXmlTestStoreCustom
    {
        private static readonly Guid TestTypeGuid = new Guid("ec4800e8-40e5-4ab3-8510-b8bf29b1904d"); // move to constants
        private static readonly TestType TestTypeInstance = new TestType(TestTypeGuid);

        public OrderedTestElement(Guid id, string name, string adapter) : base(id, name, adapter) { }

        string IXmlTestStoreCustom.ElementName
        {
            get { return "OrderedTest"; }
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
            get { return TestTypeInstance; }
        }
    }
}
