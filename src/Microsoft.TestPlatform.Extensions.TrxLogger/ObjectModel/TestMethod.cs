// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System.Diagnostics;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using System;
    /// <summary>
    /// TestMethod contains information about a unit test method that needs to be executed
    /// </summary>
    internal sealed class TestMethod : IXmlTestStore
    {
        private string className;

        private string name; // test method name


        private bool isValid;

        public TestMethod(string name, string className)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "name is null");
            Debug.Assert(!string.IsNullOrEmpty(className), "className is null");
            this.name = name;
            this.className = className;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets the class name.
        /// </summary>
        public string ClassName
        {
            get
            {
                return this.className;
            }

        }

        /// <summary>
        /// Gets or sets a value indicating whether is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return this.isValid;
            }
            set
            {
                this.isValid = value;
            }
        }

        #region Override

        /// <summary>
        /// Override function for Equals.
        /// </summary>
        /// <param name="obj">
        /// The object to compare.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            TestMethod otherTestMethod = obj as TestMethod;
            return otherTestMethod != null && this.name == otherTestMethod.name
                   && this.className == otherTestMethod.className && this.isValid == otherTestMethod.isValid;
        }

        /// <summary>
        /// Override function for GetHashCode.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.Name?.GetHashCode() ?? 0;
        }

        #endregion Override

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
        public void Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();
            helper.SaveSimpleField(element, "@className", this.className, string.Empty);
            helper.SaveSimpleField(element, "@name", this.name, string.Empty);
            helper.SaveSimpleField(element, "isValid", this.isValid, false);
        }

        #endregion
    }
}
