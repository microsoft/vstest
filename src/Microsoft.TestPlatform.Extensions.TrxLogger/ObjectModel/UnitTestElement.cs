// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    internal class UnitTestElement : TestElement, IXmlTestStoreCustom
    {
        private static readonly Guid TestTypeGuid = new Guid("13CDC9D9-DDB5-4fa4-A97D-D965CCFC6D4B"); // move to constants
        private static readonly TestType TestTypeInstance = new TestType(TestTypeGuid);

        private string codeBase;
        private TestMethod testMethod;

        public UnitTestElement(
            Guid id,
            string name,
            string adapter,
            TestMethod testMethod) : base(id, name, adapter)
        {
            Debug.Assert(!string.IsNullOrEmpty(adapter), "adapter is null");
            Debug.Assert(testMethod != null, "testMethod is null");
            Debug.Assert(testMethod != null && testMethod.ClassName != null, "className is null");

            this.testMethod = testMethod;
        }

        string IXmlTestStoreCustom.ElementName
        {
            get { return "UnitTest"; }
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

        /// <summary>
        /// Gets or sets the storage.
        /// </summary>
        public string CodeBase
        {
            get { return this.codeBase; }

            set
            {
                EqtAssert.StringNotNullOrEmpty(value, "CodeBase");
                this.codeBase = value;
            }
        }

        /// <summary>
        /// Saves the class under the XmlElement..
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameter
        /// </param>
        public override void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            base.Save(element, parameters);
            XmlPersistence h = new XmlPersistence();

            h.SaveSimpleField(element, "TestMethod/@codeBase", this.codeBase, string.Empty);
            h.SaveSimpleField(element, "TestMethod/@executorUri", this.adapter, string.Empty);
            h.SaveObject(this.testMethod, element, "TestMethod", parameters);
        }
    }
}
