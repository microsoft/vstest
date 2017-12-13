// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// Test aggregation element.
    /// </summary>
    internal abstract class TestAggregation : TestElement, ITestAggregation
    {
        protected Dictionary<Guid, TestLink> testLinks = new Dictionary<Guid, TestLink>();

        public TestAggregation(Guid id, string name, string adapter) : base(id, name, adapter) { }

        /// <summary>
        /// Test links.
        /// </summary>
        public Dictionary<Guid, TestLink> TestLinks
        {
            get { return testLinks; }
        }

        public override void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            base.Save(element, parameters);

            XmlPersistence h = new XmlPersistence();
            if (testLinks.Count > 0)
                h.SaveIEnumerable(testLinks.Values, element, "TestLinks", ".", "TestLink", parameters);
        }
    }
}
